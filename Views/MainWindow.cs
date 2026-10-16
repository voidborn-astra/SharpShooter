// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using System.Globalization;

namespace SharpShooter;

public sealed partial class MainWindow : Window
{
    private static readonly string[] SectionNames = ["Overview", "Sections", "Frame trace", "Metrics", "Issues", "Environment", "Compare game"];
    private readonly UserSettings settings = UserSettings.Load();
    private readonly List<LogRun> runs = [];
    private readonly TextBox directory = new() { PlaceholderText = "Log directory", MinWidth = 260 };
    private readonly TextBox filter = new() { PlaceholderText = "Filter game, title ID, file, or label" };
    private readonly TextBox label = new() { PlaceholderText = "Run label: baseline / change / run 2", Width = 240 };
    private readonly NumericUpDown limit = new() { Minimum = 1, Maximum = 100, Increment = 1, FormatString = "0", Width = 110 };
    private readonly CheckBox recursive = new() { Content = "Subfolders" };
    private readonly ListBox list = new();
    private readonly ComboBox mode = new() { ItemsSource = new[] { "Full run", "First 180 seconds", "Last 20 seconds", "Custom window" }, SelectedIndex = 0, Width = 170 };
    private readonly TextBox start = new() { Text = "0", Width = 70 };
    private readonly TextBox end = new() { Text = "180", Width = 70 };
    private readonly TextBox trim = new() { Text = "0", Width = 70 };
    private readonly TextBlock status = new() { Text = "Choose a directory, then scan. Logs are read only.", TextWrapping = TextWrapping.Wrap };
    private readonly ProgressBar progress = new() { IsIndeterminate = true, Width = 120, IsVisible = false, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock runCount = Ui.Text("RUNS", "sectionTitle");
    private readonly TextBlock gameTitle = Ui.Text("No run selected", "gameTitle");
    private readonly StackPanel headerTags = Ui.Row(6);
    private readonly TextBlock headerFile = Ui.Text("", "muted");
    private readonly SectionView[] sections =
        [new OverviewView(), new SectionsView(), new FrameTraceView(), new MetricsView(), new IssuesView(), new EnvironmentView(), new ComparisonView()];
    private readonly Button[] tabs = new Button[SectionNames.Length];
    private readonly ContentControl sectionHost = new();
    private readonly Panel runPanel = new();
    private readonly Control emptyState;
    private CancellationTokenSource? cancellation;
    private WindowSelection selection = new();
    private int activeSection;

    public MainWindow()
    {
        Title = "SharpShooter — SharpEmu log analysis";
        Width = 1480; Height = 940; MinWidth = 1100; MinHeight = 680;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowDecorations = WindowDecorations.None;
        directory.Text = settings.LogDirectory;
        limit.Value = Math.Clamp(settings.NewestCount, 1, 100);
        recursive.IsChecked = settings.IncludeSubfolders;
        list.Classes.Add("runs");
        list.ItemTemplate = RunTemplate.Create();
        emptyState = BuildEmptyState();

        var body = new DockPanel { Margin = new Thickness(24, 18, 24, 12) };
        body.Children.Add(Ui.Dock(BuildToolbar(), Dock.Top));
        body.Children.Add(Ui.Dock(BuildStatusBar(), Dock.Bottom));
        body.Children.Add(Ui.Dock(BuildSidebar(), Dock.Left));
        runPanel.Children.Add(BuildRunPanel());
        runPanel.Children.Add(emptyState);
        body.Children.Add(runPanel);

        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        root.Children.Add(BuildTitleBar());
        Grid.SetRow(body, 1);
        root.Children.Add(body);
        var layers = new Panel();
        layers.Children.Add(root);
        layers.Children.Add(BuildResizeHandles());
        Content = layers;

        filter.TextChanged += (_, _) => FilterRuns();
        list.SelectionChanged += (_, _) => Refresh();
        mode.SelectionChanged += (_, _) => { UpdateWindowInputs(); ApplyWindow(); };
        Closed += (_, _) => cancellation?.Cancel();
        UpdateWindowInputs();
        SelectSection(0);
        ShowRun(null);
    }

    private void SelectSection(int index)
    {
        activeSection = index;
        for (var i = 0; i < tabs.Length; i++) tabs[i].Classes.Set("active", i == index);
        sectionHost.Content = sections[index];
        ShowActiveSection();
    }

    private void ShowActiveSection()
    {
        if (list.SelectedItem is not LogRun run) return;
        if (sections[activeSection] is ComparisonView comparison) comparison.Show(runs, run, selection);
        else sections[activeSection].Show(run, selection);
    }

    private void UpdateWindowInputs()
    {
        var custom = mode.SelectedItem as string == "Custom window";
        start.IsEnabled = custom;
        end.IsEnabled = custom;
    }

    private void ApplyWindow()
    {
        if (!TrySeconds(start.Text, out var from) || !TrySeconds(end.Text, out var until) || !TrySeconds(trim.Text, out var tail) || until < from)
        { status.Text = "Use finite, non-negative seconds. End must be at least start."; return; }
        selection = new(mode.SelectedItem as string ?? "Full run", from, until, tail);
        Refresh();
    }

    private static bool TrySeconds(string? value, out double number) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number) && number >= 0;

    private void Refresh()
    {
        var run = list.SelectedItem as LogRun;
        ShowRun(run);
        if (run is null) return;
        label.Text = run.Label == "Unlabeled" ? "" : run.Label;
        ShowActiveSection();
    }

    private void ShowRun(LogRun? run)
    {
        emptyState.IsVisible = run is null;
        runPanel.Children[0].IsVisible = run is not null;
        if (run is null) return;
        gameTitle.Text = run.DisplayName;
        headerTags.Children.Clear();
        headerTags.Children.Add(Ui.Pill(Ui.Text(run.TitleId)));
        if (run.GameVersion is not null) headerTags.Children.Add(Ui.Pill(Ui.Text("v" + run.GameVersion)));
        if (run.Label != "Unlabeled") headerTags.Children.Add(Ui.Pill(Ui.Text(run.Label), "info"));
        headerTags.Children.Add(run.CleanExit
            ? Ui.Pill(Ui.Text("Clean exit", "good"), "good")
            : Ui.Pill(Ui.Text("No exit evidence", "warn"), "warn"));
        if (run.Warnings.Count > 0) headerTags.Children.Add(Ui.Pill(Ui.Text($"{run.Warnings.Count} notices", "warn"), "warn"));
        var more = UnloadedForTitle(run).Length;
        loadMore.IsVisible = more > 0;
        ToolTip.SetTip(loadMore, $"{more} older {run.TitleId} logs in the scanned folder were not loaded.");
        headerFile.Text = $"{run.FileName}  ·  {run.Modified:yyyy-MM-dd HH:mm}  ·  {run.Bytes / 1048576.0:F1} MiB  ·  {run.Lines:N0} lines";
        ToolTip.SetTip(headerFile, run.Path);
    }

    private void FilterRuns()
    {
        var selected = list.SelectedItem;
        var query = filter.Text ?? "";
        var visible = runs.Where(r => r.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        list.ItemsSource = visible;
        list.SelectedItem = visible.Contains(selected) ? selected : visible.FirstOrDefault();
        runCount.Text = visible.Length == runs.Count ? $"RUNS · {runs.Count}" : $"RUNS · {visible.Length} OF {runs.Count}";
    }

    private void SaveLabel()
    {
        if (list.SelectedItem is not LogRun run) return;
        run.Label = string.IsNullOrWhiteSpace(label.Text) ? "Unlabeled" : label.Text.Trim();
        settings.Labels[run.Path] = run.Label;
        try
        {
            settings.Save();
            var items = runs.ToArray();
            list.ItemsSource = null;
            FilterRuns();
            list.SelectedItem = items.Contains(run) ? run : list.SelectedItem;
            status.Text = "Label saved locally. Source log is unchanged.";
        }
        catch (Exception error) { status.Text = "Cannot save label: " + error.Message; }
    }

    private void OpenSource()
    {
        if (list.SelectedItem is not LogRun run) return;
        try { FileReveal.Open(run.Path); }
        catch (Exception error) { status.Text = error.Message; }
    }

    private void OpenFolder()
    {
        if (list.SelectedItem is not LogRun run) return;
        try { FileReveal.Show(run.Path); }
        catch (Exception error) { status.Text = error.Message; }
    }
}
