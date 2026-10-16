// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace SharpShooter;

/// <summary>
/// Side-by-side runs. Starts with every loaded run of the selected game. The run
/// picker can remove runs or add runs from other games. Colors stay fixed per run.
/// </summary>
public sealed class ComparisonView : SectionView
{
    private readonly FpsChart chart = new() { Height = 280 };
    private readonly HashSet<string> included = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> colorSlots = new(StringComparer.OrdinalIgnoreCase);
    private readonly StackPanel picker = new() { Spacing = 2, MinWidth = 380 };
    private readonly TextBlock subtitle = Ui.Text("", "muted");
    private readonly Control toolbar;
    private readonly Border chartCard;
    private IReadOnlyList<LogRun> all = [];
    private LogRun? selected;
    private WindowSelection selection = new();
    private string? anchor;

    public ComparisonView()
    {
        var flyout = new Flyout { Content = new ScrollViewer { Content = picker, MaxHeight = 520 }, Placement = PlacementMode.BottomEdgeAlignedRight };
        flyout.Opening += (_, _) => FillPicker();
        var choose = Ui.IconButton("checklist", "Choose runs", () => { });
        choose.Flyout = flyout;
        var actions = Ui.Row(8, Ui.Button("Same game only", ResetToGame), choose);
        var title = new StackPanel { Spacing = 2 };
        title.Children.Add(Ui.Text("Compare runs", "heading"));
        subtitle.FontSize = 12;
        title.Children.Add(subtitle);
        var bar = new DockPanel();
        bar.Children.Add(Ui.Dock(actions, Dock.Right));
        bar.Children.Add(title);
        toolbar = bar;
        chartCard = Ui.Card(chart, 12);
    }

    public IReadOnlyList<LogRun> IncludedRuns =>
        all.Where(r => included.Contains(r.Path)).OrderBy(r => r.Modified).ToArray();

    public override void Show(LogRun run, WindowSelection window) => Show([run], run, window);

    public void Show(IEnumerable<LogRun> runs, LogRun current, WindowSelection window)
    {
        all = runs.ToArray();
        selected = current;
        selection = window;
        if (anchor != current.TitleId) ResetToGame();
        else
        {
            // Runs loaded after the last view join the comparison if they match the game.
            foreach (var run in all.Where(r => seen.Add(r.Path) && r.TitleId == anchor)) Include(run.Path);
            included.RemoveWhere(path => all.All(r => !string.Equals(r.Path, path, StringComparison.OrdinalIgnoreCase)));
            Render();
        }
    }

    private void ResetToGame()
    {
        if (selected is null) return;
        anchor = selected.TitleId;
        included.Clear();
        seen.Clear();
        foreach (var run in all.OrderBy(r => r.Modified))
        {
            seen.Add(run.Path);
            if (run.TitleId == anchor) Include(run.Path);
        }
        Render();
    }

    private void Include(string path)
    {
        included.Add(path);
        if (!colorSlots.ContainsKey(path)) colorSlots[path] = colorSlots.Count;
    }

    private void Toggle(LogRun run, bool on)
    {
        if (on) Include(run.Path); else included.Remove(run.Path);
        Render();
    }

    private string ColorOf(LogRun run) =>
        FpsChart.Colors[colorSlots.GetValueOrDefault(run.Path) % FpsChart.Colors.Length];

    private void FillPicker()
    {
        picker.Children.Clear();
        foreach (var game in all.GroupBy(r => r.TitleId).OrderBy(g => g.Key == anchor ? 0 : 1).ThenBy(g => g.First().DisplayName))
        {
            var header = Ui.Text($"{game.First().DisplayName.ToUpperInvariant()} · {game.Key}", "sectionTitle", game.Key == anchor ? "accent" : "muted");
            header.Margin = new Thickness(6, picker.Children.Count == 0 ? 2 : 12, 6, 4);
            picker.Children.Add(header);
            foreach (var run in game.OrderByDescending(r => r.Modified))
            {
                var check = new CheckBox { IsChecked = included.Contains(run.Path), Margin = new Thickness(6, 0) };
                var text = new StackPanel();
                text.Children.Add(Ui.Text(run.Label == "Unlabeled" ? run.Modified.ToString("yyyy-MM-dd HH:mm") : run.Label, "cell"));
                var detail = Ui.Text($"{run.Modified:MM-dd HH:mm} · {run.Duration:F0}s · {run.FileName}", "muted");
                detail.FontSize = 11;
                text.Children.Add(detail);
                check.Content = Ui.Row(8, Ui.Swatch(included.Contains(run.Path) || colorSlots.ContainsKey(run.Path) ? ColorOf(run) : "#4D5461"), text);
                check.IsCheckedChanged += (_, _) => Toggle(run, check.IsChecked == true);
                picker.Children.Add(check);
            }
        }
    }

    private void Render()
    {
        Body.Children.Clear();
        Add(toolbar);
        var runs = IncludedRuns;
        var games = runs.Select(r => r.TitleId).Distinct().Count();
        subtitle.Text = $"{runs.Count} of {all.Count} loaded runs selected" + (games > 1 ? $" · {games} games" : "") +
            " · labels and colors follow each run";
        if (runs.Count == 0)
        {
            Empty("checklist", "No runs selected", "Use Choose runs to add runs, or Same game only to reset.");
            return;
        }
        Add(Chips(runs));
        chart.SetRuns(runs, selection, runs.Select(ColorOf).ToArray());
        Add(chartCard);
        Add(Ui.Section("Side by side", runs.Count > 1
            ? "Change is relative to the first (oldest) run. Green marks the best value in a row."
            : "Add another run to compare.", new ScrollViewer
        {
            Content = ComparisonTable.Build(runs, selection, ColorOf),
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        }));
        Add(Ui.Notes(
            "Compare matching scenes, settings, and cache state. Time alone does not identify the scene.",
            "Submitted FPS is the render-rate comparison. Presented FPS can include repeated frames.",
            "Render costs are ms/frame and can overlap. Lower is better for costs.",
            "Each run uses the same window selection. Coverage shows how much of it has FPS samples."));
    }

    private WrapPanel Chips(IReadOnlyList<LogRun> runs)
    {
        var chips = new WrapPanel();
        foreach (var run in runs)
        {
            var name = run.Label == "Unlabeled" ? run.Modified.ToString("MM-dd HH:mm") : run.Label;
            var remove = Ui.Button(Ui.Icon("close", "compact"), () => Toggle(run, false), "iconGhost", "Remove from comparison");
            remove.Width = 24; remove.Height = 24; remove.BorderThickness = new Thickness(0);
            var text = Ui.Text(run.TitleId == anchor ? name : $"{run.DisplayName} · {name}", "cell");
            var chip = new Border { Child = Ui.Row(8, Ui.Swatch(ColorOf(run)), text, remove) };
            chip.Classes.Add("swatchChip");
            ToolTip.SetTip(chip, run.Path);
            chips.Children.Add(chip);
        }
        return chips;
    }
}
