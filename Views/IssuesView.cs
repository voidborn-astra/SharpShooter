// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia.Controls;
using Avalonia.Media;

namespace SharpShooter;

public sealed class IssuesView : SectionView
{
    public override void Show(LogRun run, WindowSelection selection)
    {
        Body.Children.Clear();
        if (run.IssueTotal == 0)
        {
            Empty("check_circle", "No issue lines", "No error, warning, validation, or fault lines were found in this log.");
            return;
        }
        var counts = new WrapPanel();
        foreach (var (category, count) in run.FindingCounts.OrderByDescending(p => p.Value))
            counts.Children.Add(Ui.Metric(category, $"{count:N0}", Ui.IssueTone(category)));
        Add(Ui.Section("Whole run", $"{run.IssueTotal:N0} matching lines · {run.IssueGroups.Count:N0} distinct patterns", counts));

        var top = new DataTable(new("Count", "Auto", true), new("Category", "Auto"), new("Message", "*"), new("First", "Auto", true));
        foreach (var group in run.IssueGroups.Values.OrderByDescending(g => g.Count).Take(50))
            top.AddRow(CountBadge(group.Count, group.Category), Category(group.Category), Message(group.Sample), Ui.Mono($"L{group.FirstLine}", "muted"));
        var more = run.IssueGroups.Count > 50 ? $" Showing the 50 largest of {run.IssueGroups.Count:N0}." : "";
        var overflow = run.UngroupedIssues > 0 ? $" {run.UngroupedIssues:N0} lines exceeded the pattern limit." : "";
        Add(Ui.Section("Most repeated · whole run", "Numbers and addresses are masked, so repeated lines are shown once with a count." + more + overflow, top));

        var (start, end) = selection.Resolve(run);
        var rows = IssueGrouping.WindowRows(run, start, end);
        var window = new DataTable(new("Count", "Auto", true), new("Category", "Auto"), new("Message", "*"), new("Lines", "Auto", true), new("Time", "Auto", true));
        foreach (var row in rows.Take(200))
        {
            var lines = row.Count > 1 ? $"L{row.First.Line}–{row.Last.Line}" : $"L{row.First.Line}";
            var time = row.Count > 1 ? $"{row.First.Seconds:F1}–{row.Last.Seconds:F1}s" : $"{row.First.Seconds:F1}s";
            window.AddRow(CountBadge(row.Count, row.First.Category), Category(row.First.Category), Message(row.Message), Ui.Mono(lines, "muted"), Ui.Mono(time, "muted"));
        }
        Control body = rows.Count > 0 ? window : Ui.Text("No retained issue lines in this window.", "muted");
        Add(Ui.Section($"Selected window · {start:F1}–{end:F1}s", $"{rows.Sum(r => r.Count):N0} retained lines in {rows.Count:N0} patterns", body));
        Add(Ui.Notes(
            "Counts are matching log lines, not unique failures. No diagnosis is implied.",
            "One masked pattern can cover unrelated calls that share wording.",
            $"Window evidence uses the first 5,000 issue lines.{(run.DroppedFindings > 0 ? $" {run.DroppedFindings:N0} later lines appear only in whole-run counts." : "")}",
            "Line numbers refer to the source log. Use Open log to see the full context."));
    }

    private static Border CountBadge(int count, string category) =>
        Ui.Pill(Ui.Mono($"{count:N0}×", Ui.IssueTone(category)), Ui.IssueTone(category));

    private static TextBlock Category(string category) => Ui.Text(category, "cell", Ui.IssueTone(category));

    private static SelectableTextBlock Message(string text)
    {
        var block = Ui.Code(text.Length > 400 ? text[..400] + " …" : text);
        block.TextWrapping = TextWrapping.Wrap;
        return block;
    }
}
