// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Globalization;
using System.Text;

namespace SharpShooter;

/// <summary>
/// One report series in a window. Values are the latest interval report, or the
/// cumulative delta between observed endpoints. Note explains a missing delta.
/// </summary>
public sealed record MetricSeries(string Tag, MetricPoint Last, MetricPoint? First, bool Cumulative,
    IReadOnlyList<KeyValuePair<string, string>> Values, string? Note);

public static partial class Analyzer
{
    public const int SeriesLimit = 250;
    private static readonly HashSet<string> CumulativeFlags =
        ["cumulative", "inclusive", "sample_stride", "all_compile_threads", "includes_prewarm", "worker_wall_time"];

    public static IReadOnlyList<MetricSeries> MetricSeriesList(LogRun run, WindowSelection selection)
    {
        var (start, end) = selection.Resolve(run);
        var list = new List<MetricSeries>();
        foreach (var group in run.Metrics.Where(p => p.Tag != "RENDER_MS").GroupBy(p => p.Tag).OrderBy(g => g.Key).Take(SeriesLimit))
        {
            var last = group.LastOrDefault(p => p.Seconds >= start && p.Seconds <= end);
            if (last is null) continue;
            var first = group.LastOrDefault(p => p.Seconds <= start) ?? group.FirstOrDefault(p => p.Seconds >= start && p.Seconds <= end);
            if (!last.Cumulative)
            {
                list.Add(new(group.Key, last, null, false, last.Values
                    .Select(p => new KeyValuePair<string, string>(p.Key, p.Value.ToString("G8", CultureInfo.InvariantCulture))).ToArray(), null));
                continue;
            }
            if (first is null || first == last)
            {
                list.Add(new(group.Key, last, first, true, [], "Insufficient endpoints for a cumulative delta."));
                continue;
            }
            var values = new List<KeyValuePair<string, string>>();
            foreach (var (key, value) in last.Values)
            {
                if (CumulativeFlags.Contains(key) || !first.Values.TryGetValue(key, out var before)) continue;
                values.Add(new(key, value >= before ? (value - before).ToString("F3", CultureInfo.InvariantCulture) : "counter reset / unavailable"));
            }
            list.Add(new(group.Key, last, first, true, values,
                $"Cumulative delta: {first.Seconds:F2}–{last.Seconds:F2}s (not the full requested window)."));
        }
        return list;
    }

    public static string Details(LogRun run, WindowSelection selection)
    {
        var text = new StringBuilder("NUMERIC REPORT BREAKDOWN\nLatest interval report in the window, or cumulative delta between observed endpoints.\nUnits follow the fields/reports. Counts, bytes and milliseconds must not be added.\n\n");
        foreach (var series in MetricSeriesList(run, selection))
        {
            text.AppendLine($"{series.Tag}  [L{series.Last.Line}, {series.Last.Seconds:F2}s]");
            if (series.Note is not null) text.AppendLine("  " + series.Note);
            foreach (var (key, value) in series.Values) text.AppendLine($"  {key}: {value}");
            text.AppendLine();
        }
        text.AppendLine($"Display limited to {SeriesLimit} report series. Open the source log for complete evidence.");
        return text.ToString();
    }
}
