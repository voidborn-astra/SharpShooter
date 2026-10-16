// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;

namespace SharpShooter;

public sealed class MetricsView : SectionView
{
    private readonly TextBox search = new() { PlaceholderText = "Filter report series or field names", Width = 320,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top };
    private readonly StackPanel list = new() { Spacing = 8 };
    private readonly TextBlock summary = Ui.Text("", "muted");
    private IReadOnlyList<MetricSeries> series = [];

    public MetricsView() => search.TextChanged += (_, _) => Render();

    public override void Show(LogRun run, WindowSelection selection)
    {
        Body.Children.Clear();
        series = Analyzer.MetricSeriesList(run, selection);
        if (series.Count == 0)
        {
            Empty("monitoring", "No numeric reports in this window", "No [PERF] interval or cumulative reports were found in the selected window.");
            return;
        }
        var header = new DockPanel();
        header.Children.Add(Ui.Dock(search, Dock.Right));
        header.Children.Add(Ui.Heading("Numeric reports",
            "Latest interval report in the window, or the cumulative delta between observed endpoints. Expand a series to see its fields."));
        Add(header);
        Add(summary);
        Add(list);
        Add(Ui.Notes(
            "Units follow each field. Counts, bytes, and milliseconds must not be added together.",
            "Cumulative deltas need two observed endpoints and show their actual range, which can differ from the requested window.",
            $"At most {Analyzer.SeriesLimit} series are shown. Per-event trace dumps are counted in Environment, not listed here.",
            "RENDER_MS is shown in Overview instead."));
        Render();
    }

    private void Render()
    {
        list.Children.Clear();
        var query = search.Text?.Trim() ?? "";
        var shown = 0;
        foreach (var item in series)
        {
            var fields = query.Length == 0 || item.Tag.Contains(query, StringComparison.OrdinalIgnoreCase)
                ? item.Values
                : item.Values.Where(v => v.Key.Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (query.Length > 0 && fields.Count == 0) continue;
            list.Children.Add(Series(item, fields, query.Length > 0));
            shown++;
        }
        summary.Text = $"{shown:N0} of {series.Count:N0} series";
    }

    private static Control Series(MetricSeries item, IReadOnlyList<KeyValuePair<string, string>> fields, bool open)
    {
        var name = Ui.Text(item.Tag, "heading");
        name.FontSize = 13;
        var header = Ui.Row(8, name, Ui.Pill(Ui.Text($"L{item.Last.Line} · {item.Last.Seconds:F1}s")));
        if (item.Cumulative) header.Children.Add(Ui.Pill(Ui.Text("cumulative", "info"), "info"));
        header.Children.Add(Ui.Text($"{fields.Count} fields", "muted"));
        var body = new StackPanel { Spacing = 6 };
        if (item.Note is not null) body.Children.Add(Ui.Text(item.Note, "muted"));
        var chips = new WrapPanel();
        foreach (var (key, value) in fields) chips.Children.Add(Ui.Metric(key, value, value.StartsWith("counter", StringComparison.Ordinal) ? "warn" : null));
        body.Children.Add(chips);
        var expander = new Expander { Header = header, Content = body, IsExpanded = open, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        var card = new Border { Child = expander, Padding = new Thickness(4) };
        card.Classes.Add("card");
        return card;
    }
}
