// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpShooter;

public static partial class Analyzer
{
    public static string Summary(LogRun run, WindowSelection selection)
    {
        var s = Analyze(run, selection);
        var version = run.GameVersion is null ? "" : $"  v{run.GameVersion}";
        var text = new StringBuilder($"{run.DisplayName}  ·  {run.TitleId}{version}  /  {run.Label}\n{run.FileName}\n\nWINDOW   {s.Start:F1}–{s.End:F1}s after log start\n");
        text.AppendLine($"SUBMITTED FPS   {Format(s.Submitted)}     PRESENTED FPS   {Format(s.Presented)}");
        text.AppendLine($"Sample FPS range {Format(s.Minimum)}–{Format(s.Maximum)}   P50 {Format(s.P50)}   P95 {Format(s.P95)}");
        text.AppendLine($"FPS coverage {s.Coverage:F1}s / {s.End - s.Start:F1}s  ·  {s.Samples:N0} samples  ·  {s.Issues:N0} retained issue lines");
        text.AppendLine($"Run {run.Duration:F1}s  ·  {run.Lines:N0} lines  ·  {run.Bytes / 1048576.0:F1} MiB\n{run.Status}\n");
        text.AppendLine("LARGEST REPORTED RENDER COSTS  ·  ms/frame, may overlap");
        foreach (var pair in s.RenderCosts.OrderByDescending(p => p.Value).Take(15)) text.AppendLine($"{pair.Value,10:F3}  {pair.Key}");
        if (s.RenderCosts.Count == 0) text.AppendLine("No RENDER_MS data in this window.");
        text.AppendLine($"Render-report coverage: {s.RenderCoverage:F1}s. Boundary windows use proportional overlap.\n");
        text.AppendLine("FPS uses intervals between adjacent samples; the first sample and gaps over 10s are excluded.");
        text.AppendLine("Exact FPS sampling duration is not logged. Averages estimate observed coverage, not missing time.");
        text.AppendLine("Presented FPS may include repeated frames. Nested and worker timings cannot be added to obtain frame cost.");
        text.AppendLine("Equal elapsed windows do not prove equal scenes. Match route, cache state, settings, and run ordinal.");
        foreach (var warning in run.Warnings) text.AppendLine("NOTICE: " + warning);
        return text.ToString();
    }


}
