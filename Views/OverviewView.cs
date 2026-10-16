// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace SharpShooter;

public sealed class OverviewView : SectionView
{
    private readonly FpsChart chart = new() { Height = 240 };
    private readonly StatTile submitted = new("Submitted FPS");
    private readonly StatTile presented = new("Presented FPS");
    private readonly StatTile coverage = new("FPS coverage");
    private readonly StatTile issues = new("Issue lines");
    private readonly StatTile duration = new("Run length");
    private readonly UniformGrid tiles = new() { Rows = 1, Margin = new Thickness(0, 0, -12, 0) };
    private readonly Border chartCard;

    public OverviewView()
    {
        foreach (var tile in new[] { submitted, presented, coverage, issues, duration }) tiles.Children.Add(tile);
        chartCard = Ui.Card(chart, 12);
    }

    public string SubmittedText => submitted.Value;

    public override void Show(LogRun run, WindowSelection selection)
    {
        Body.Children.Clear();
        var s = Analyzer.Analyze(run, selection);
        var width = s.End - s.Start;
        submitted.Set(Analyzer.Format(s.Submitted), $"P50 {Analyzer.Format(s.P50)} · P95 {Analyzer.Format(s.P95)}", Ui.FpsTone(s.Submitted));
        presented.Set(Analyzer.Format(s.Presented), "May include repeated frames");
        var share = width > 0 ? s.Coverage / width : 0;
        coverage.Set($"{s.Coverage:F0}s", $"of {width:F0}s window · {s.Samples:N0} samples", share >= .8 ? null : "warn");
        issues.Set($"{s.Issues:N0}", $"in window · {run.IssueTotal:N0} whole run", s.Issues > 0 ? "warn" : null);
        duration.Set($"{run.Duration:F0}s", run.Status, run.CleanExit ? "good" : null);
        Add(tiles);
        chart.SetRuns([run], selection);
        Add(chartCard);

        foreach (var warning in run.Warnings) Add(Notice(warning));

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,12,2*") };
        grid.Children.Add(RenderCosts(s));
        var details = RunDetails(run, s);
        Grid.SetColumn(details, 2);
        grid.Children.Add(details);
        Add(grid);
        Add(Ui.Notes(
            "FPS uses intervals between adjacent samples; the first sample and gaps over 10s are excluded.",
            "Exact FPS sampling duration is not logged. Averages estimate observed coverage, not missing time.",
            "Presented FPS may include repeated frames. Submitted FPS is the render-rate comparison.",
            "Render costs are ms/frame weighted by frames. Nested and worker timings overlap and cannot be added.",
            "Boundary render windows use proportional overlap.",
            "Equal elapsed windows do not prove equal scenes. Match route, cache state, settings, and run ordinal.",
            "Tile colors compare FPS with a 60 FPS target: green 55+, amber 28+, red below."));
    }

    private static Border RenderCosts(WindowStats s)
    {
        var table = new DataTable(new("Phase", "*"), new("ms/frame", "Auto", true), new("", "Auto"));
        var costs = s.RenderCosts.OrderByDescending(p => p.Value).Take(15).ToArray();
        var max = costs.Length > 0 ? costs[0].Value : 1;
        foreach (var (name, value) in costs) table.AddRow(name, Ui.Mono(value.ToString("F3")), new ValueBar(value, max, "#A18AFF", 120));
        Control body = costs.Length > 0 ? table : Ui.Text("No RENDER_MS data in this window.", "muted");
        return Ui.Section("Largest render costs", $"RENDER_MS · coverage {s.RenderCoverage:F1}s · costs may overlap", body);
    }

    private static Border RunDetails(LogRun run, WindowStats s)
    {
        var table = new DataTable(new("Field", "Auto"), new("Value", "*"));
        table.AddRow("Window", Ui.Mono($"{s.Start:F1}–{s.End:F1}s"));
        table.AddRow("Sample FPS range", Ui.Mono($"{Analyzer.Format(s.Minimum)} – {Analyzer.Format(s.Maximum)}"));
        table.AddRow("P50 / P95", Ui.Mono($"{Analyzer.Format(s.P50)} / {Analyzer.Format(s.P95)}"));
        table.AddRow("FPS samples", Ui.Mono($"{s.Samples:N0}"));
        table.AddRow("Run length", Ui.Mono($"{run.Duration:F1}s"));
        table.AddRow("Log size", Ui.Mono($"{run.Lines:N0} lines · {run.Bytes / 1048576.0:F1} MiB"));
        table.AddRow("Exit", run.CleanExit ? Ui.Text(run.Status, "cell", "good") : Ui.Text(run.Status, "cell", "warn"));
        if (run.GameVersion is not null) table.AddRow("Game version", Ui.Mono(run.GameVersion));
        return Ui.Section("Run details", null, table);
    }

    private static Border Notice(string text)
    {
        var row = Ui.Row(10, Ui.Icon("warning", "compact", "warn"), Ui.Text(text, "cell"));
        var card = Ui.Card(row, 12);
        card.Classes.Add("notice");
        return card;
    }
}
