// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpShooter;

public static partial class Analyzer
{
    public static WindowStats Analyze(LogRun run, WindowSelection selection)
    {
        var (start, end) = selection.Resolve(run);
        double weight = 0, submitted = 0, presented = 0, presentWeight = 0;
        var rates = new List<double>();
        for (var i = 1; i < run.Fps.Count; i++)
        {
            var point = run.Fps[i];
            var from = run.Fps[i - 1].Seconds;
            var duration = point.Seconds - from;
            if (duration <= 0 || duration > 10) continue;
            var overlap = Math.Max(0, Math.Min(end, point.Seconds) - Math.Max(start, from));
            if (overlap <= 0) continue;
            weight += overlap; submitted += point.Submitted * overlap;
            if (double.IsFinite(point.Presented)) { presented += point.Presented * overlap; presentWeight += overlap; }
            rates.Add(point.Submitted);
        }
        double frameWeight = 0, renderCoverage = 0;
        var costs = new Dictionary<string, double>();
        foreach (var point in run.Metrics.Where(p => p.Tag == "RENDER_MS" && p.Window > 0 && p.Frames > 0))
        {
            var overlap = Math.Max(0, Math.Min(end, point.Seconds) - Math.Max(start, point.Seconds - point.Window));
            if (overlap <= 0) continue;
            var frames = point.Frames * overlap / point.Window;
            frameWeight += frames; renderCoverage += overlap;
            foreach (var (key, value) in point.Values)
                if (key is not "window_s" and not "frames") costs[key] = costs.GetValueOrDefault(key) + value * frames;
        }
        if (frameWeight > 0) foreach (var key in costs.Keys.ToArray()) costs[key] /= frameWeight;
        return new(start, end, run.Fps.Count(p => p.Seconds >= start && p.Seconds <= end), weight > 0 ? submitted / weight : null,
            presentWeight > 0 ? presented / presentWeight : null, weight, rates.Count > 0 ? rates.Min() : null, rates.Count > 0 ? rates.Max() : null,
            Percentile(rates, .5), Percentile(rates, .95), costs, renderCoverage, run.Findings.Count(f => f.Seconds >= start && f.Seconds <= end));
    }

    public static double? Percentile(IEnumerable<double> source, double quantile)
    {
        var sorted = source.Order().ToArray();
        return sorted.Length == 0 ? null : sorted[(int)Math.Clamp(Math.Ceiling(sorted.Length * quantile) - 1, 0, sorted.Length - 1)];
    }
    public static string Format(double? value) => value?.ToString("F2", CultureInfo.InvariantCulture) ?? "N/A";

}
