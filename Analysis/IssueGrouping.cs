// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Text;
using System.Text.RegularExpressions;

namespace SharpShooter;

/// <summary>
/// Groups issue lines by a masked pattern so one repeated warning does not hide
/// rare failures. Groups count every matching line, not only retained evidence.
/// </summary>
public static partial class IssueGrouping
{
    public const int GroupLimit = 2000;

    [GeneratedRegex(@"^\[\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?\]\s*")]
    private static partial Regex TimestampPattern();
    [GeneratedRegex(@"0x[0-9A-Fa-f]+|\b[0-9A-Fa-f]{8,}\b")]
    private static partial Regex HexPattern();
    [GeneratedRegex(@"\d+(?:\.\d+)?")]
    private static partial Regex NumberPattern();

    public static string Mask(string line)
    {
        var text = TimestampPattern().Replace(line, "", 1);
        if (text.Length > 400) text = text[..400];
        text = HexPattern().Replace(text, "0x…");
        return NumberPattern().Replace(text, "#");
    }

    public static void Add(LogRun run, string category, string line, int lineNumber)
    {
        var pattern = Mask(line);
        var key = category + "\u0001" + pattern;
        if (!run.IssueGroups.TryGetValue(key, out var group))
        {
            if (run.IssueGroups.Count >= GroupLimit) { run.UngroupedIssues++; return; }
            var sample = TimestampPattern().Replace(line, "", 1);
            run.IssueGroups[key] = group = new(category, pattern, sample.Length > 600 ? sample[..600] + " …" : sample, lineNumber);
        }
        group.Count++;
    }

    /// <summary>Groups retained evidence lines inside a time window, one row per pattern.</summary>
    public static IReadOnlyList<IssueRow> WindowRows(LogRun run, double start, double end) =>
        run.Findings.Where(f => f.Seconds >= start && f.Seconds <= end)
            .GroupBy(f => f.Category + "\u0001" + Mask(f.Message))
            .Select(g => new IssueRow(g.First(), g.Last(), g.Count(), TimestampPattern().Replace(g.First().Message, "", 1)))
            .OrderByDescending(g => g.Count).ThenBy(g => g.First.Line).ToArray();

    public static string Window(LogRun run, double start, double end, int limit = 200)
    {
        var groups = WindowRows(run, start, end);
        var text = new StringBuilder($"SELECTED WINDOW {start:F1}–{end:F1}s  ·  {groups.Sum(g => g.Count):N0} retained lines in {groups.Count:N0} patterns\n\n");
        foreach (var (first, last, count, message) in groups.Take(limit))
        {
            var span = count > 1 ? $"  ·  L{first.Line}–L{last.Line}  ·  {first.Seconds:F2}–{last.Seconds:F2}s" : $"  ·  L{first.Line}  ·  {first.Seconds:F2}s";
            text.AppendLine($"{count,7:N0}×  {first.Category}{span}");
            text.AppendLine($"          {message}\n");
        }
        if (groups.Count > limit) text.AppendLine($"{groups.Count - limit:N0} more patterns not shown.");
        if (run.DroppedFindings > 0)
            text.AppendLine($"Window evidence uses the first 5,000 issue lines. {run.DroppedFindings:N0} later lines are in the whole-run counts only.");
        return text.ToString();
    }

    public static string Summary(LogRun run, int limit = 40)
    {
        var text = new StringBuilder("REPEATED ISSUE PATTERNS  ·  whole run, numbers and addresses masked as # and 0x…\n");
        text.AppendLine("A pattern count is a line count. One pattern can span unrelated calls.\n");
        if (run.IssueGroups.Count == 0) return text.AppendLine("No issue lines found.").ToString();
        foreach (var group in run.IssueGroups.Values.OrderByDescending(g => g.Count).Take(limit))
            text.AppendLine($"{group.Count,9:N0}×  {group.Category}  ·  first L{group.FirstLine}\n           {group.Sample}");
        if (run.IssueGroups.Count > limit) text.AppendLine($"\n{run.IssueGroups.Count - limit:N0} smaller patterns not shown.");
        if (run.UngroupedIssues > 0)
            text.AppendLine($"Pattern limit reached ({GroupLimit:N0}). {run.UngroupedIssues:N0} lines were counted but not grouped.");
        return text.ToString();
    }
}

/// <summary>Retained issue lines in a window that share one masked pattern.</summary>
public sealed record IssueRow(Finding First, Finding Last, int Count, string Message);
