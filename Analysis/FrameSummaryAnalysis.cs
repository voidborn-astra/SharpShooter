// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Text;

namespace SharpShooter;

/// <summary>Gap and phase statistics for one set of retained frames.</summary>
public sealed record FrameBlock(string Name, int Frames, double? P50, double? P95, double? P99,
    int Over16, int Over33, int Over100, IReadOnlyList<KeyValuePair<string, double>> Phases);

public static partial class Analyzer
{
    public static IReadOnlyList<FrameBlock> FrameBlocks(LogRun run)
    {
        if (run.Frames.Count == 0) return [];
        var end = run.Frames[^1].Timestamp;
        var tail = run.FrameFrequency > 0 ? run.Frames.Where(f => (end - f.Timestamp) / (double)run.FrameFrequency <= 20).ToArray() : [];
        var blocks = new List<FrameBlock>();
        foreach (var (name, frames) in new[] { ("All retained frames", run.Frames.ToArray()), ("Last 20 trace seconds", tail) })
        {
            var gaps = frames.Where(f => f.Gap > 0).Select(f => f.Gap).ToArray();
            var sums = new Dictionary<string, double>();
            foreach (var frame in frames) foreach (var (phase, value) in frame.Phases) sums[phase] = sums.GetValueOrDefault(phase) + value;
            var phases = frames.Length == 0 ? [] : sums.OrderByDescending(p => p.Value).Take(10)
                .Select(p => new KeyValuePair<string, double>(p.Key, p.Value / frames.Length)).ToArray();
            blocks.Add(new(name, frames.Length, Percentile(gaps, .5), Percentile(gaps, .95), Percentile(gaps, .99),
                gaps.Count(g => g > 1000.0 / 60), gaps.Count(g => g > 1000.0 / 30), gaps.Count(g => g > 100), phases));
        }
        return blocks;
    }

    public static string FrameSummary(LogRun run)
    {
        var text = new StringBuilder("RETAINED FRAME TRACE\nUses trace timestamps, not shutdown timestamps on dumped lines. Not aligned to the elapsed-log selector.\nGaps are present-loop gaps, not GPU-only time.\n\n");
        if (run.Frames.Count == 0) return text + "No retained frame data found.";
        foreach (var block in FrameBlocks(run))
        {
            text.AppendLine($"{block.Name}: {block.Frames:N0} frames");
            if (block.P50 is null) { text.AppendLine("Insufficient gaps or missing trace frequency.\n"); continue; }
            text.AppendLine($"Gap P50 {Format(block.P50)}ms  P95 {Format(block.P95)}ms  P99 {Format(block.P99)}ms");
            text.AppendLine($"Gaps >16.67ms: {block.Over16}  >33.33ms: {block.Over33}  >100ms: {block.Over100}");
            foreach (var pair in block.Phases) text.AppendLine($"  {pair.Value,9:F3} ms/retained frame  {pair.Key}");
            text.AppendLine();
        }
        return text.ToString();
    }
}
