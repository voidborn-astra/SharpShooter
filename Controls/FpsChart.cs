// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Globalization;

namespace SharpShooter;

public sealed class FpsChart : Control
{
    public static readonly string[] Colors = ["#A18AFF", "#66F2A3", "#64A7FF", "#FFCB6B", "#FF829E", "#53D7DD", "#F78C6C", "#C3E88D", "#E58FFF", "#B0BEC5"];
    private static readonly IBrush LabelBrush = Brush.Parse("#777F8E");
    private static readonly IBrush TitleBrush = Brush.Parse("#C0C5CF");
    private static readonly IPen GridPen = new Pen(Brush.Parse("#1FFFFFFF"));
    private static readonly IPen TargetPen = new Pen(Brush.Parse("#9966F2A3"), 1, DashStyle.Dash);
    private static readonly Typeface Face = new("Inter, Segoe UI, sans-serif");
    private IReadOnlyList<LogRun> runs = [];
    private WindowSelection selection = new();
    private IReadOnlyList<string> colors = Colors;

    /// <summary>Sets the runs to draw. Colors pair with runs by index; the default palette cycles.</summary>
    public void SetRuns(IReadOnlyList<LogRun> values, WindowSelection window, IReadOnlyList<string>? runColors = null)
    {
        runs = values;
        colors = runColors is { Count: > 0 } ? runColors : Colors;
        selection = window;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        const double left = 44, top = 30, bottom = 22, right = 12;
        var width = Math.Max(1, Bounds.Width - left - right);
        var height = Math.Max(1, Bounds.Height - top - bottom);
        var windows = runs.Select(r => selection.Resolve(r)).ToArray();
        var points = runs.SelectMany((r, i) => r.Fps.Where(p => p.Seconds >= windows[i].Start && p.Seconds <= windows[i].End)).ToArray();
        var maximum = Math.Ceiling(Math.Max(60, points.Select(p => p.Submitted).DefaultIfEmpty(60).Max()) / 10) * 10;
        var start = windows.Select(w => w.Start).DefaultIfEmpty(0).Min();
        var end = Math.Max(start + 1, windows.Select(w => w.End).DefaultIfEmpty(1).Max());
        Label(context, "SUBMITTED FPS", 0, 0, TitleBrush, 11, FontWeight.SemiBold);
        Label(context, "elapsed seconds · dashed line is 60 FPS", 110, 1, LabelBrush, 11);
        for (var i = 0; i <= 4; i++)
        {
            var y = top + height * i / 4;
            context.DrawLine(GridPen, new Point(left, y), new Point(left + width, y));
            Label(context, (maximum * (4 - i) / 4).ToString("F0", CultureInfo.InvariantCulture), 0, y - 8, LabelBrush, 11);
        }
        var target = top + height * (1 - 60 / maximum);
        context.DrawLine(TargetPen, new Point(left, target), new Point(left + width, target));
        for (var index = 0; index < runs.Count; index++)
        {
            var color = Color.Parse(colors[index % colors.Count]);
            var pen = new Pen(new SolidColorBrush(color), 1.8, lineJoin: PenLineJoin.Round);
            var fill = runs.Count == 1 ? new SolidColorBrush(color, 0.12) : null;
            foreach (var segment in Segments(runs[index], windows[index]))
            {
                var geometry = new StreamGeometry();
                using (var g = geometry.Open())
                {
                    g.BeginFigure(Position(segment[0]), false);
                    foreach (var point in segment.Skip(1)) g.LineTo(Position(point));
                    g.EndFigure(false);
                }
                if (fill is not null && segment.Count > 1) context.DrawGeometry(fill, null, Area(segment));
                context.DrawGeometry(null, pen, geometry);
            }
        }
        Label(context, $"{start:F0}s", left, top + height + 4, LabelBrush, 11);
        Label(context, $"{end:F0}s", left + width - 30, top + height + 4, LabelBrush, 11);
        if (points.Length == 0) Label(context, "No FPS samples in this window", left + 16, top + 16, TitleBrush, 13);

        Point Position(FpsPoint p) => new(left + (p.Seconds - start) / (end - start) * width, top + height * (1 - p.Submitted / maximum));
        StreamGeometry Area(List<FpsPoint> segment)
        {
            var area = new StreamGeometry();
            using var g = area.Open();
            g.BeginFigure(new Point(Position(segment[0]).X, top + height), true);
            foreach (var point in segment) g.LineTo(Position(point));
            g.LineTo(new Point(Position(segment[^1]).X, top + height));
            g.EndFigure(true);
            return area;
        }
    }

    /// <summary>Splits samples at window edges and at gaps over 10 seconds.</summary>
    private static IEnumerable<List<FpsPoint>> Segments(LogRun run, (double Start, double End) window)
    {
        var current = new List<FpsPoint>();
        foreach (var point in run.Fps)
        {
            var outside = point.Seconds < window.Start || point.Seconds > window.End;
            if (outside || (current.Count > 0 && point.Seconds - current[^1].Seconds > 10))
            {
                if (current.Count > 1) yield return current;
                current = [];
            }
            if (!outside) current.Add(point);
        }
        if (current.Count > 1) yield return current;
    }

    private static void Label(DrawingContext context, string text, double x, double y, IBrush brush, double size,
        FontWeight weight = FontWeight.Normal) =>
        context.DrawText(new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(Face.FontFamily, FontStyle.Normal, weight), size, brush), new Point(x, y));
}
