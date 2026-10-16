// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Net;
using System.Text;
using System.Text.Json;

namespace SharpShooter;

public static class HtmlReport
{
    public static void Write(string path, IReadOnlyList<LogRun> runs, WindowSelection selection) =>
        File.WriteAllText(path, Create(runs, selection), new UTF8Encoding(false));

    public static string Create(IReadOnlyList<LogRun> runs, WindowSelection selection)
    {
        var html = new StringBuilder("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width\"><title>SharpShooter report</title>");
        html.Append("<style>body{background:#0c0c10;color:#f7f8fb;font:15px system-ui;margin:32px}h1{color:#a18aff;margin-bottom:6px}small{color:#c0c5cf}section{background:#16161e;padding:20px;margin:20px 0;border-radius:12px}pre{white-space:pre-wrap;overflow-wrap:anywhere;font:13px Consolas,monospace}table{border-collapse:collapse;width:100%}td,th{text-align:left;padding:10px;border-bottom:1px solid #343441}canvas{width:100%;height:320px}button{background:#7c5cfc;color:white;border:0;padding:8px 14px;margin:4px;border-radius:6px}</style>");
        html.Append("<h1>SharpShooter</h1><b>SEE THE SIGNAL. FIND THE PROBLEM.</b><p><small>SHARPEMU LOG ANALYSIS &amp; VISUALIZATION</small></p>");
        html.Append("<p>Offline report. Window: " + Encode(selection.Mode) + ". Submitted FPS is the render-rate comparison. Match scenes, settings and cache state before drawing conclusions.</p>");
        html.Append("<section><h2>Run comparison</h2><canvas id=\"chart\"></canvas><div id=\"legend\"></div><table><tr><th>Run</th><th>Game</th><th>Window</th><th>Submitted FPS</th><th>Presented FPS</th><th>Coverage</th></tr>");
        foreach (var run in runs)
        {
            var stats = Analyzer.Analyze(run, selection);
            html.Append($"<tr><td>{Encode(run.Label)}<br><small>{Encode(run.FileName)}</small></td><td>{Encode(run.DisplayName)}<br><small>{Encode(run.TitleId)}</small></td><td>{stats.Start:F1}–{stats.End:F1}s</td><td>{Analyzer.Format(stats.Submitted)}</td><td>{Analyzer.Format(stats.Presented)}</td><td>{stats.Coverage:F1}s</td></tr>");
        }
        html.Append("</table></section>");
        foreach (var run in runs)
        {
            html.Append("<section><h2>" + Encode(run.DisplayName + " · " + run.TitleId + " · " + run.Label) + "</h2>");
            Detail("Overview", Analyzer.Summary(run, selection), true);
            Detail("20-second sections", Analyzer.Sections(run));
            Detail("Frame trace", Analyzer.FrameSummary(run));
            Detail("Numeric reports", Analyzer.Details(run, selection));
            Detail("Environment and configuration", Analyzer.Configuration(run));
            var window = selection.Resolve(run);
            Detail("Repeated issue patterns", IssueGrouping.Summary(run));
            Detail("Issue evidence", IssueGrouping.Window(run, window.Start, window.End));
            html.Append("</section>");
        }
        var data = runs.Select(r => new ChartSeries(
            r.DisplayName + " · " + r.Label + " · " + r.FileName,
            r.Fps.Where(p => p.Seconds >= selection.Resolve(r).Start && p.Seconds <= selection.Resolve(r).End)
                .Select(p => new[] { p.Seconds, p.Submitted }).ToArray())).ToArray();
        // Compact output; the escaping encoder keeps "</script>" in labels from closing the tag.
        html.Append("<script>const series=" + JsonSerializer.Serialize(data, CompactJson.Default.ChartSeriesArray) + ";</script>");
        html.Append(ChartScript.Source);
        html.Append("</html>");
        return html.ToString();

        void Detail(string heading, string text, bool open = false) => html.Append(
            $"<details{(open ? " open" : "")}><summary>{Encode(heading)}</summary><pre>{Encode(text)}</pre></details>");
    }
    private static string Encode(string value) => WebUtility.HtmlEncode(value);
}
