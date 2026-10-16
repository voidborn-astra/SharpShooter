// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SharpShooter;

/// <summary>Builds the side-by-side table: one column per run, one row per measure.</summary>
internal static class ComparisonTable
{
    private const int CostRows = 12;

    public static Control Build(IReadOnlyList<LogRun> runs, WindowSelection selection, Func<LogRun, string> color)
    {
        var stats = runs.Select(r => Analyzer.Analyze(r, selection)).ToArray();
        var columns = new List<TableColumn> { new("", "Auto") };
        columns.AddRange(runs.Select(_ => new TableColumn("", "Auto", true)));
        var table = new DataTable([.. columns]);
        table.AddRow([Ui.Text("", "cell"), .. runs.Select(r => Header(r, color(r)))]);

        Row(table, "Game", runs.Select(r => Ui.Text(r.DisplayName + (r.GameVersion is null ? "" : $"  v{r.GameVersion}"), "cell")));
        Row(table, "Log time", runs.Select(r => Ui.Mono(r.Modified.ToString("yyyy-MM-dd HH:mm"), "secondary")));
        Row(table, "Window", stats.Select(s => Ui.Mono($"{s.Start:F0}–{s.End:F0}s", "secondary")));
        table.AddGroup("Frame rate · higher is better");
        Numeric(table, "Submitted FPS", stats.Select(s => s.Submitted).ToArray(), higherIsBetter: true, fps: true);
        Numeric(table, "Presented FPS", stats.Select(s => s.Presented).ToArray(), higherIsBetter: true, fps: true);
        Numeric(table, "P50 sample FPS", stats.Select(s => s.P50).ToArray(), higherIsBetter: true);
        Numeric(table, "Lowest sample FPS", stats.Select(s => s.Minimum).ToArray(), higherIsBetter: true);
        Row(table, "FPS coverage", stats.Select(s =>
            Ui.Mono($"{s.Coverage:F0}s / {s.End - s.Start:F0}s", s.Coverage < (s.End - s.Start) * .8 ? "warn" : "secondary")));
        table.AddGroup("Stability");
        Numeric(table, "Issue lines in window", stats.Select(s => (double?)s.Issues).ToArray(), higherIsBetter: false, digits: 0);
        Row(table, "Exit", runs.Select(r => r.CleanExit ? Ui.Text("Clean exit", "cell", "good") : Ui.Text("No evidence", "cell", "warn")));

        var costs = stats.SelectMany(s => s.RenderCosts.OrderByDescending(p => p.Value).Take(8).Select(p => p.Key)).Distinct()
            .OrderByDescending(k => stats.Max(s => s.RenderCosts.GetValueOrDefault(k))).Take(CostRows).ToArray();
        if (costs.Length > 0)
        {
            table.AddGroup("Render costs · ms/frame · lower is better");
            foreach (var key in costs)
                Numeric(table, key, stats.Select(s => s.RenderCosts.TryGetValue(key, out var v) ? v : (double?)null).ToArray(), higherIsBetter: false, digits: 3);
        }
        return table;
    }

    private static Control Header(LogRun run, string color)
    {
        var stack = new StackPanel { Spacing = 2, MinWidth = 120 };
        var name = Ui.Text(run.Label == "Unlabeled" ? run.Modified.ToString("MM-dd HH:mm") : run.Label, "heading");
        name.FontSize = 13;
        stack.Children.Add(Ui.Row(6, Ui.Swatch(color), name));
        var file = Ui.Text(run.FileName, "muted");
        file.FontSize = 10;
        file.MaxWidth = 200;
        file.TextTrimming = TextTrimming.CharacterEllipsis;
        ToolTip.SetTip(file, run.Path);
        stack.Children.Add(file);
        var bar = new Border { Height = 3, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(Color.Parse(color)), Margin = new Thickness(0, 4, 0, 0) };
        stack.Children.Add(bar);
        return stack;
    }

    private static void Row(DataTable table, string name, IEnumerable<Control> cells) =>
        table.AddRow([Ui.Text(name, "cell", "secondary"), .. cells]);

    private static void Numeric(DataTable table, string name, double?[] values, bool higherIsBetter, bool fps = false, int digits = 2)
    {
        var known = values.Where(v => v is not null).Select(v => v!.Value).ToArray();
        var best = known.Length > 1 ? (higherIsBetter ? known.Max() : known.Min()) : (double?)null;
        var worst = known.Length > 1 ? (higherIsBetter ? known.Min() : known.Max()) : (double?)null;
        var baseline = values[0];
        var cells = new List<Control>();
        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            if (value is null) { cells.Add(Ui.Mono("—", "muted")); continue; }
            var tone = best is not null && best != worst && value == best ? "good"
                : fps ? Ui.FpsTone(value) : null;
            var text = Ui.Mono(value.Value.ToString("F" + digits), tone ?? "secondary");
            if (value == best && best != worst) text.FontWeight = FontWeight.Bold;
            var stack = new StackPanel { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
            stack.Children.Add(text);
            if (i > 0 && baseline is > 0) stack.Children.Add(Change(value.Value, baseline.Value, higherIsBetter));
            cells.Add(stack);
        }
        Row(table, name, cells);
    }

    private static TextBlock Change(double value, double baseline, bool higherIsBetter)
    {
        var change = (value - baseline) / baseline * 100;
        var better = higherIsBetter ? change > 0 : change < 0;
        var tone = Math.Abs(change) < 1 ? "muted" : better ? "good" : "bad";
        var text = Ui.Mono($"{(change >= 0 ? "+" : "")}{change:F1}%", tone);
        text.FontSize = 11;
        text.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
        return text;
    }
}
