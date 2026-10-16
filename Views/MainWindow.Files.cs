// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace SharpShooter;

public sealed partial class MainWindow
{
    private readonly MenuFlyout recentMenu = new();
    private readonly List<FileInfo> unloaded = [];
    private Button loadMore = new();

    private void FillRecentMenu()
    {
        recentMenu.Items.Clear();
        foreach (var folder in settings.RecentDirectories)
        {
            var item = new MenuItem { Header = folder, IsEnabled = Directory.Exists(folder) };
            item.Click += (_, _) => directory.Text = folder;
            recentMenu.Items.Add(item);
        }
        if (recentMenu.Items.Count == 0) recentMenu.Items.Add(new MenuItem { Header = "No recent folders", IsEnabled = false });
    }

    private async Task Browse()
    {
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new() { Title = "Select SharpEmu log directory", AllowMultiple = false });
            if (folders.Count > 0) directory.Text = folders[0].TryGetLocalPath();
        }
        catch (Exception error) { status.Text = error.Message; }
    }

    private async Task Scan()
    {
        if (cancellation is not null) { status.Text = "A scan is active. Cancel it or wait."; return; }
        if (!Directory.Exists(directory.Text)) { status.Text = "Choose an existing directory."; return; }
        var maximum = (int)(limit.Value ?? 10);
        if (maximum < 1 || maximum > 100) { status.Text = "Choose 1–100 logs. Large frame traces can use substantial memory."; return; }
        var folder = directory.Text!;
        var recurse = recursive.IsChecked == true;
        cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var failures = new List<string>();
        progress.IsVisible = true;
        try
        {
            status.Text = "Finding logs…";
            var found = await Task.Run(() => Directory.EnumerateFiles(folder, "*", new EnumerationOptions
                { RecurseSubdirectories = recurse, IgnoreInaccessible = true })
                .Where(p => Path.GetExtension(p).Equals(".txt", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(p).Equals(".log", StringComparison.OrdinalIgnoreCase))
                .Select(p => { token.ThrowIfCancellationRequested(); return new FileInfo(p); })
                .OrderByDescending(f => f.LastWriteTimeUtc).ToArray(), token);
            runs.Clear(); FilterRuns();
            unloaded.Clear();
            unloaded.AddRange(found.Skip(maximum));
            await Load(found.Take(maximum).ToArray(), token, failures);
            settings.Remember(folder);
            settings.NewestCount = maximum;
            settings.IncludeSubfolders = recurse;
            settings.Save();
            var skipped = unloaded.Count == 0 ? "" : $" {unloaded.Count} older logs not loaded; raise Newest or use Load more runs.";
            status.Text = $"Loaded {runs.Count} of {found.Length} logs found.{skipped} " +
                (failures.Count == 0 ? "Compare game uses all loaded runs with the same title ID." : string.Join(" | ", failures));
        }
        catch (OperationCanceledException) { status.Text = $"Scan canceled. {runs.Count} completed logs retained."; }
        catch (Exception error) { status.Text = "Scan failed: " + error.Message; }
        finally { cancellation.Dispose(); cancellation = null; progress.IsVisible = false; FilterRuns(); }
    }

    private async Task Load(FileInfo[] files, CancellationToken token, List<string> failures)
    {
        for (var i = 0; i < files.Length; i++)
        {
            token.ThrowIfCancellationRequested();
            status.Text = $"Reading {i + 1}/{files.Length}: {files[i].Name}";
            try
            {
                var file = files[i].FullName;
                var run = await Task.Run(() => LogParser.Parse(file, token), token);
                run.Label = settings.Labels.GetValueOrDefault(run.Path, "Unlabeled");
                runs.Add(run);
                FilterRuns();
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { failures.Add(files[i].Name + ": " + error.Message); }
        }
    }

    /// <summary>Older scanned logs whose file name has the selected run's title ID.</summary>
    private FileInfo[] UnloadedForTitle(LogRun run) =>
        unloaded.Where(f => LogParser.TitleFromName(f.Name) == run.TitleId).ToArray();

    private async Task LoadMoreForTitle()
    {
        if (cancellation is not null || list.SelectedItem is not LogRun run) return;
        var files = UnloadedForTitle(run);
        if (files.Length == 0) return;
        cancellation = new CancellationTokenSource();
        var failures = new List<string>();
        progress.IsVisible = true;
        try
        {
            await Load(files, cancellation.Token, failures);
            status.Text = $"Loaded {files.Length - failures.Count} more {run.DisplayName} runs. " + string.Join(" | ", failures);
        }
        catch (OperationCanceledException) { status.Text = "Load canceled."; }
        finally
        {
            foreach (var file in files) unloaded.Remove(file);
            cancellation.Dispose(); cancellation = null; progress.IsVisible = false;
            FilterRuns(); list.SelectedItem = run;
        }
    }

    private async Task Export()
    {
        if (list.SelectedItem is not LogRun run) { status.Text = "Select a run first."; return; }
        try
        {
            var file = await StorageProvider.SaveFilePickerAsync(new()
            {
                Title = "Export same-game comparison", SuggestedFileName = $"SharpShooter-{run.TitleId}.html",
                DefaultExtension = "html", FileTypeChoices = [new FilePickerFileType("HTML report") { Patterns = ["*.html"] }]
            });
            if (file?.TryGetLocalPath() is not string path) return;
            var selected = runs.Where(r => r.TitleId == run.TitleId).ToArray();
            var window = selection;
            await Task.Run(() => HtmlReport.Write(path, selected, window));
            status.Text = "Exported offline report: " + path;
        }
        catch (Exception error) { status.Text = "Export failed: " + error.Message; }
    }
}
