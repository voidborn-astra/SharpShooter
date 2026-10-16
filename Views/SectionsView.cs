// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

namespace SharpShooter;

public sealed class SectionsView : SectionView
{
    public override void Show(LogRun run, WindowSelection selection)
    {
        Body.Children.Clear();
        var rows = Analyzer.SectionRows(run);
        if (rows.Count == 0)
        {
            Empty("schedule", "No timed sections", "This log has no supported wall-clock timestamps.");
            return;
        }
        var max = Math.Max(60, rows.Max(r => r.Submitted ?? 0));
        var table = new DataTable(new("Interval", "Auto"), new("Submitted FPS", "Auto", true), new("", "Auto"),
            new("Presented FPS", "Auto", true), new("Coverage", "Auto", true), new("Issues", "Auto", true), new("", "*"));
        foreach (var s in rows)
        {
            var tone = Ui.FpsTone(s.Submitted);
            table.AddRow(Ui.Mono($"{s.Start,5:F0}–{s.End,5:F0}s"),
                tone is null ? Ui.Mono(Analyzer.Format(s.Submitted)) : Ui.Mono(Analyzer.Format(s.Submitted), tone),
                new ValueBar(s.Submitted ?? 0, max, ValueBar.FpsColor(s.Submitted), 160),
                Ui.Mono(Analyzer.Format(s.Presented), "secondary"),
                s.Coverage < (s.End - s.Start) * .8 ? Ui.Mono($"{s.Coverage:F1}s", "warn") : Ui.Mono($"{s.Coverage:F1}s", "secondary"),
                s.Issues > 0 ? Ui.Mono($"{s.Issues:N0}", "warn") : Ui.Mono("0", "muted"), "");
        }
        Add(Ui.Section("20-second sections", "Fixed elapsed intervals across the whole run. Not affected by the window selector.", table));
        Add(Ui.Notes(
            "Each row is a separate 20-second window. FPS values are time-weighted between adjacent samples.",
            "Coverage is the observed FPS sampling time in the interval. Low coverage means missing samples, not zero FPS.",
            "Issue counts use retained evidence lines (the first 5,000 in the log)."));
    }
}
