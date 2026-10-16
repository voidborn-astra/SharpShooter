// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Text;

namespace SharpShooter;

public static partial class Analyzer
{
    /// <summary>Fixed-width elapsed intervals across the whole run.</summary>
    public static IReadOnlyList<WindowStats> SectionRows(LogRun run, double width = 20)
    {
        var rows = new List<WindowStats>();
        for (double start = 0; start < run.Duration && start / width < 2000; start += width)
            rows.Add(Analyze(run, new("Custom window", start, start + width)));
        return rows;
    }

    public static string Sections(LogRun run, double width = 20)
    {
        var text = new StringBuilder("ELAPSED SECTIONS  ·  submitted FPS / presented FPS / observed coverage\n\n");
        foreach (var s in SectionRows(run, width))
            text.AppendLine($"{s.Start,7:F0}–{s.End,7:F0}s    {Format(s.Submitted),8} / {Format(s.Presented),8}    {s.Coverage,6:F1}s    issues {s.Issues}");
        return text.ToString();
    }
}
