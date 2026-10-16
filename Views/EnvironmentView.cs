// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia.Controls;

namespace SharpShooter;

public sealed class EnvironmentView : SectionView
{
    public override void Show(LogRun run, WindowSelection selection)
    {
        Body.Children.Clear();
        var variables = new DataTable(new("Variable", "Auto"), new("Values found (line)", "*"));
        foreach (var (key, values) in run.EnvironmentEvidence.OrderBy(p => p.Key))
            variables.AddRow(Ui.Mono(key, "accent"), Ui.Code(string.Join("\n", values)));
        Control varBody = variables.Count > 0 ? variables : Ui.Text("No assignments found. Unrecorded variables are unknown, not disabled.", "muted");
        Add(Ui.Section("Environment variables", "Assignment text found in the log. Evidence, not the effective process environment.", varBody));

        var features = new StackPanel { Spacing = 6 };
        foreach (var feature in Analyzer.ObservedFeatures(run))
            features.Children.Add(Ui.Row(8, Ui.Icon("check_circle", "compact", "good"), Ui.Text(feature, "cell")));
        if (features.Children.Count == 0) features.Children.Add(Ui.Text("No profiling features observed.", "muted"));
        Add(Ui.Section("Observed features", "Inferred from output, not exact environment values.", features));

        Control config = run.Configuration.Count > 0 ? Ui.Code(string.Join("\n", run.Configuration)) : Ui.Text("None found.", "muted");
        Add(Ui.Section("Configuration, cache, and prewarm evidence", $"{run.Configuration.Count} lines (at most 80 retained)", config));

        if (run.TraceEvents.Count > 0)
        {
            var traces = new DataTable(new("Tag", "*"), new("Event lines", "Auto", true));
            foreach (var (tag, count) in run.TraceEvents.OrderByDescending(p => p.Value)) traces.AddRow(Ui.Mono(tag), Ui.Mono($"{count:N0}"));
            Add(Ui.Section("Trace event dumps", "Per-event ring-buffer lines. Counted only; not stored as numeric reports.", traces));
        }

        var tags = new WrapPanel();
        foreach (var tag in run.Tags.Order()) tags.Children.Add(new Border { Child = Ui.Mono(tag), Classes = { "chip" } });
        Add(Ui.Section("Report tags", $"{run.Tags.Count} [PERF] tags seen", tags));
    }
}
