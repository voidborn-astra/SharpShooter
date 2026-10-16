// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SharpShooter;

public static partial class Analyzer
{
    public static string Configuration(LogRun run)
    {
        var text = new StringBuilder("EXPLICIT ENVIRONMENT-VARIABLE TEXT\nAssignments found in the log are evidence, not proof of the effective process environment.\n\n");
        foreach (var (key, values) in run.EnvironmentEvidence.OrderBy(p => p.Key)) text.AppendLine(key + " = " + string.Join("; ", values));
        if (run.EnvironmentEvidence.Count == 0) text.AppendLine("No assignments found. Unrecorded variables are UNKNOWN, not disabled.");
        text.AppendLine("\nOBSERVED FEATURES  ·  inferred from output, not exact environment values");
        foreach (var feature in ObservedFeatures(run)) text.AppendLine(feature);
        text.AppendLine("\nCONFIGURATION / CACHE / PREWARM EVIDENCE");
        foreach (var line in run.Configuration) text.AppendLine(line);
        text.AppendLine("\nTRACE EVENT DUMPS  ·  per-event lines counted, not stored as reports");
        foreach (var (tag, count) in run.TraceEvents.OrderByDescending(p => p.Value)) text.AppendLine($"{count,9:N0}  {tag}");
        if (run.TraceEvents.Count == 0) text.AppendLine("None found.");
        text.AppendLine("\nREPORT TAGS\n" + string.Join(", ", run.Tags.Order()));
        return text.ToString();
    }

    /// <summary>Features inferred from log output. Absence does not mean disabled.</summary>
    public static IReadOnlyList<string> ObservedFeatures(LogRun run)
    {
        var features = new List<string>();
        if (run.Fps.Count > 0) features.Add("FPS logging active (LOG_VIDEOOUT_FPS or a performance profile).");
        if (run.Tags.Contains("RENDER_MS")) features.Add("Render profiling active (PROFILE_RENDER or PROFILE_PERFORMANCE).");
        if (run.Tags.Contains("FRAME_TRACE")) features.Add("Frame trace output present.");
        if (run.Tags.Contains("RESOURCE_MATERIALIZATION")) features.Add("Detailed materialization reports present (performance + frame trace in current Sharp).");
        return features;
    }
}
