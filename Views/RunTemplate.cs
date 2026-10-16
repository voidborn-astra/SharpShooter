// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace SharpShooter;

/// <summary>Run list item: game name, title ID, label, and headline numbers.</summary>
internal static class RunTemplate
{
    public static IDataTemplate Create() => new FuncDataTemplate<LogRun>((run, _) => run is null ? new TextBlock() : Build(run));

    private static Control Build(LogRun run)
    {
        var stack = new StackPanel { Spacing = 3 };
        var name = Ui.Text(run.DisplayName);
        name.FontSize = 14;
        name.FontWeight = FontWeight.SemiBold;
        name.TextTrimming = TextTrimming.CharacterEllipsis;
        stack.Children.Add(name);

        var tags = Ui.Row(6, Ui.Pill(Ui.Text(run.TitleId)));
        if (run.Label != "Unlabeled") tags.Children.Add(Ui.Pill(Ui.Text(run.Label), "info"));
        if (run.CleanExit) tags.Children.Add(Ui.Pill(Ui.Text("exit 0", "good"), "good"));
        tags.Margin = new Thickness(0, 2, 0, 2);
        stack.Children.Add(tags);

        var stats = Analyzer.Analyze(run, new());
        var fps = stats.Submitted is null ? "FPS unknown" : $"{Analyzer.Format(stats.Submitted)} FPS";
        stack.Children.Add(Ui.Text($"{fps}  ·  {run.Duration:F0}s  ·  {run.IssueTotal:N0} issues", "secondary"));
        var file = Ui.Text($"{run.Modified:yyyy-MM-dd HH:mm}  ·  {run.FileName}", "muted");
        file.FontSize = 11;
        file.TextTrimming = TextTrimming.CharacterEllipsis;
        ToolTip.SetTip(file, run.Path);
        stack.Children.Add(file);
        return stack;
    }
}
