// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia.Controls;

namespace SharpShooter;

public sealed class FrameTraceView : SectionView
{
    public override void Show(LogRun run, WindowSelection selection)
    {
        Body.Children.Clear();
        var blocks = Analyzer.FrameBlocks(run);
        if (blocks.Count == 0)
        {
            Empty("timeline", "No retained frame trace",
                "This log has no [PERF][FRAME] lines. Frame traces appear only when SharpEmu frame tracing is enabled.");
            return;
        }
        foreach (var block in blocks)
        {
            var stack = new StackPanel();
            if (block.P50 is null)
            {
                stack.Children.Add(Ui.Text("Insufficient gaps or missing trace frequency.", "muted"));
                Add(Ui.Section(block.Name, $"{block.Frames:N0} frames", stack));
                continue;
            }
            var chips = new WrapPanel();
            chips.Children.Add(Ui.Metric("Gap P50", $"{Analyzer.Format(block.P50)} ms", GapTone(block.P50)));
            chips.Children.Add(Ui.Metric("Gap P95", $"{Analyzer.Format(block.P95)} ms", GapTone(block.P95)));
            chips.Children.Add(Ui.Metric("Gap P99", $"{Analyzer.Format(block.P99)} ms", GapTone(block.P99)));
            chips.Children.Add(Ui.Metric("> 16.67 ms", $"{block.Over16:N0}", block.Over16 > 0 ? "warn" : null));
            chips.Children.Add(Ui.Metric("> 33.33 ms", $"{block.Over33:N0}", block.Over33 > 0 ? "warn" : null));
            chips.Children.Add(Ui.Metric("> 100 ms", $"{block.Over100:N0}", block.Over100 > 0 ? "bad" : null));
            stack.Children.Add(chips);
            var table = new DataTable(new("Phase", "*"), new("ms / retained frame", "Auto", true), new("", "Auto"));
            var max = block.Phases.Count > 0 ? block.Phases[0].Value : 1;
            foreach (var (phase, value) in block.Phases) table.AddRow(phase, Ui.Mono(value.ToString("F3")), new ValueBar(value, max, "#64A7FF", 140));
            table.Margin = new Avalonia.Thickness(0, 8, 0, 0);
            stack.Children.Add(table);
            Add(Ui.Section(block.Name, $"{block.Frames:N0} frames", stack));
        }
        Add(Ui.Notes(
            "Frame traces use their own timestamp frequency, not the shutdown time on dumped lines.",
            "They are not filtered by the window selector. Gaps are present-loop gaps, not GPU-only time.",
            "Phase averages divide by all retained frames. Nested phases can overlap."));
    }

    private static string? GapTone(double? gap) => gap switch
    {
        null => null,
        <= 1000.0 / 55 => "good",
        <= 1000.0 / 28 => "warn",
        _ => "bad"
    };
}
