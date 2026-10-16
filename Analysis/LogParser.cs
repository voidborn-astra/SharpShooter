// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpShooter;

public static partial class LogParser
{
    [GeneratedRegex(@"\[(\d{2}):(\d{2}):(\d{2})(?:\.(\d{1,7}))?\]")]
    private static partial Regex TimePattern();
    [GeneratedRegex(@"\b(PPSA\d{5}|CUSA\d{5})\b", RegexOptions.IgnoreCase)]
    private static partial Regex TitlePattern();
    [GeneratedRegex(@"(?<key>[A-Za-z_][A-Za-z0-9_]*)=(?<value>-?\d+(?:\.\d+)?)(?:ms)?(?:/n\d+)?")]
    private static partial Regex NumberPattern();
    [GeneratedRegex(@"(?:\$env:)?(?<key>SHARPEMU_[A-Z0-9_]+)\s*=\s*(?<value>'[^']*'|""[^""]*""|[^\s,;]+)")]
    private static partial Regex EnvironmentPattern();
    [GeneratedRegex(@"\[PERF\]\[(?<tag>[A-Z0-9_]+)\]")]
    private static partial Regex TagPattern();
    [GeneratedRegex(@"\bTitle:(?<name>.+?), TitleId: (?<id>[A-Z]{4}\d{5})(?:, Version: (?<version>[0-9.]+))?")]
    private static partial Regex GamePattern();

    public static LogRun Parse(string path, CancellationToken cancellation = default)
    {
        var info = new FileInfo(path);
        var run = new LogRun { Path = info.FullName, Bytes = info.Length, Modified = info.LastWriteTime };
        var title = TitlePattern().Match(info.Name);
        if (title.Success) run.TitleId = title.Value.ToUpperInvariant();
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 131072);
        double? first = null;
        double lastClock = 0, dayOffset = 0, elapsed = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            run.Lines++;
            if ((run.Lines & 1023) == 0) cancellation.ThrowIfCancellationRequested();
            var time = TimePattern().Match(line);
            if (time.Success)
            {
                var clock = int.Parse(time.Groups[1].Value) * 3600 + int.Parse(time.Groups[2].Value) * 60 + int.Parse(time.Groups[3].Value) +
                    (time.Groups[4].Success ? double.Parse("0." + time.Groups[4].Value, CultureInfo.InvariantCulture) : 0);
                if (first is not null && clock < lastClock - 43200) dayOffset += 86400;
                lastClock = clock;
                first ??= clock + dayOffset;
                elapsed = Math.Max(0, clock + dayOffset - first.Value);
                run.Duration = Math.Max(run.Duration, elapsed);
            }
            if (run.TitleId == "Unknown title" && run.Lines < 2000)
            {
                title = TitlePattern().Match(line);
                if (title.Success) run.TitleId = title.Value.ToUpperInvariant();
            }
            if (run.GameName is null && line.Contains("TitleId: ", StringComparison.Ordinal)) ReadGame(run, line);
            if (line.Contains("SHARPEMU_", StringComparison.Ordinal))
                foreach (Match match in EnvironmentPattern().Matches(line))
                {
                    var key = match.Groups["key"].Value;
                    if (!run.EnvironmentEvidence.TryGetValue(key, out var evidence)) run.EnvironmentEvidence[key] = evidence = [];
                    if (evidence.Count < 8) evidence.Add($"{match.Groups["value"].Value} (L{run.Lines})");
                }
            if (line.Contains("Shader prewarm", StringComparison.OrdinalIgnoreCase) || line.Contains("pipeline cache", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("PLAYBACK MODE", StringComparison.Ordinal) || line.Contains("Host hardware:", StringComparison.Ordinal) ||
                line.Contains("SHARPEMU_", StringComparison.Ordinal) || line.Contains("Local build", StringComparison.Ordinal))
                if (run.Configuration.Count < 80) run.Configuration.Add($"L{run.Lines}: {Clip(line, 1200)}");
            if (line.Contains("exit code 0", StringComparison.OrdinalIgnoreCase) || line.Contains("exited with code 0", StringComparison.OrdinalIgnoreCase))
                run.Status = "Exit code 0 observed";
            var category = Classify(line);
            if (category is not null)
            {
                run.FindingCounts[category] = run.FindingCounts.GetValueOrDefault(category) + 1;
                IssueGrouping.Add(run, category, line, run.Lines);
                if (run.Findings.Count < 5000) run.Findings.Add(new(elapsed, category, Clip(line, 1600), run.Lines)); else run.DroppedFindings++;
            }
            if (line.Contains("submitted_fps=", StringComparison.Ordinal))
            {
                var values = Numbers(line);
                if (values.TryGetValue("submitted_fps", out var submitted) && submitted >= 0 && run.Fps.Count < 250000)
                    run.Fps.Add(new(elapsed, submitted, values.GetValueOrDefault("presented_fps", double.NaN), run.Lines));
                else if (run.Fps.Count >= 250000) Warn(run, "FPS retention limit reached (250,000 samples).");
            }
            var tagMatch = line.Contains("[PERF][", StringComparison.Ordinal) ? TagPattern().Match(line) : Match.Empty;
            if (!tagMatch.Success) continue;
            var tag = tagMatch.Groups["tag"].Value;
            run.Tags.Add(tag);
            if (tag == "FRAME_TRACE")
            {
                var values = Numbers(line);
                run.FrameFrequency = (long)values.GetValueOrDefault("frequency");
                if (values.GetValueOrDefault("overwritten") > 0) Warn(run, "Frame trace is a retained tail; earlier frames were overwritten.");
                continue;
            }
            if (tag == "FRAME")
            {
                if (run.Frames.Count >= 100000) { Warn(run, "Frame retention limit reached (100,000)."); continue; }
                var values = Numbers(line);
                var sequence = (long)values.GetValueOrDefault("sequence");
                var timestamp = (long)values.GetValueOrDefault("timestamp");
                var gap = values.GetValueOrDefault("gap_ms");
                foreach (var key in new[] { "sequence", "timestamp", "gap_ms", "last_submission", "submissions_total" }) values.Remove(key);
                run.Frames.Add(new(sequence, timestamp, gap, values));
                continue;
            }
            if (line.Contains(" sequence=", StringComparison.Ordinal) && line.Contains(" timestamp=", StringComparison.Ordinal))
            {
                // Per-event ring-buffer dumps are not interval reports. Count them only.
                run.TraceEvents[tag] = run.TraceEvents.GetValueOrDefault(tag) + 1;
                continue;
            }
            if (run.Metrics.Count >= 250000) { run.DroppedMetrics++; continue; }
            if (tag is "RENDER" or "RESOURCE_CHANGED_WORD" or "RESOURCE_READ_CONSUMER" or "DESCRIPTOR_STALL") continue;
            var metrics = Numbers(line);
            if (metrics.Count == 0) continue;
            var identity = tag;
            foreach (var field in new[] { "hash=", "stage=", "scope=", "phase=", "operation=", "source=" })
            {
                var offset = line.IndexOf(field, StringComparison.Ordinal);
                if (offset < 0) continue;
                var end = line.IndexOf(' ', offset);
                identity += " " + (end < 0 ? line[offset..] : line[offset..end]);
            }
            run.Metrics.Add(new(elapsed, identity, line.Contains("cumulative=1", StringComparison.Ordinal),
                metrics.GetValueOrDefault("window_s"), metrics.GetValueOrDefault("frames"), metrics, run.Lines));
        }
        if (first is null) Warn(run, "No supported wall-clock timestamps. Time-window analysis is unavailable.");
        if (run.Fps.Count == 0) Warn(run, "No submitted-FPS samples found. FPS is unknown, not zero.");
        if (run.DroppedMetrics > 0) Warn(run, $"Metric retention limit reached; {run.DroppedMetrics:N0} report lines omitted.");
        if (run.DroppedFindings > 0) Warn(run, $"Issue evidence capped at 5,000 lines; totals include {run.DroppedFindings:N0} more.");
        if (info.Length != new FileInfo(path).Length) Warn(run, "File changed during analysis. Refresh after the run closes.");
        run.Fps.Sort((a, b) => a.Seconds.CompareTo(b.Seconds));
        run.Metrics.Sort((a, b) => a.Seconds.CompareTo(b.Seconds));
        run.Frames.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
        return run;
    }

    /// <summary>Reads a PPSA or CUSA title ID from a file name, or null.</summary>
    public static string? TitleFromName(string name)
    {
        var match = TitlePattern().Match(name);
        return match.Success ? match.Value.ToUpperInvariant() : null;
    }

    private static void ReadGame(LogRun run, string line)
    {
        var match = GamePattern().Match(line);
        if (!match.Success) return;
        var name = match.Groups["name"].Value.Trim();
        if (name.Length > 0) run.GameName = Clip(name, 120);
        run.TitleId = match.Groups["id"].Value;
        if (match.Groups["version"].Success) run.GameVersion = match.Groups["version"].Value;
    }

    private static void Warn(LogRun run, string message) { if (!run.Warnings.Contains(message)) run.Warnings.Add(message); }
    private static string Clip(string value, int limit) => value.Length <= limit ? value : value[..limit] + " …";
    private static string? Classify(string line)
    {
        if (line.Contains("VK_ERROR_DEVICE_LOST", StringComparison.OrdinalIgnoreCase)) return "Device lost";
        if (line.Contains("VUID-", StringComparison.Ordinal)) return "Vulkan validation";
        if (line.Contains("access violation", StringComparison.OrdinalIgnoreCase) || line.Contains("MEMORY_FAULT", StringComparison.Ordinal)) return "Memory fault";
        if (line.Contains("unsupported opcode", StringComparison.OrdinalIgnoreCase)) return "Unsupported opcode";
        if (line.Contains("cannot be compiled", StringComparison.OrdinalIgnoreCase) || line.Contains("shader translation failed", StringComparison.OrdinalIgnoreCase)) return "Shader failure";
        if (line.Contains("[FATAL]", StringComparison.OrdinalIgnoreCase) || line.Contains("Unhandled exception", StringComparison.OrdinalIgnoreCase)) return "Fatal / exception";
        if (line.Contains("[ERROR]", StringComparison.OrdinalIgnoreCase) || line.Contains("[ERR]", StringComparison.OrdinalIgnoreCase)) return "Error";
        if (line.Contains("[WARN]", StringComparison.OrdinalIgnoreCase) || line.Contains("[WARNING]", StringComparison.OrdinalIgnoreCase)) return "Warning";
        return null;
    }

    private static Dictionary<string, double> Numbers(string line)
    {
        var values = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (Match match in NumberPattern().Matches(line))
        {
            var end = match.Index + match.Length;
            if (end < line.Length && (line[end] is 'x' or 'X')) continue;
            if (double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
                values[string.Intern(match.Groups["key"].Value)] = number;
        }
        return values;
    }
}
