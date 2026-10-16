// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace SharpShooter;

public sealed partial class MainWindow
{
    private Control BuildToolbar()
    {
        var recent = Ui.Button(Ui.Icon("history", "compact"), () => { }, "iconGhost", "Recent folders");
        recent.Flyout = recentMenu;
        recentMenu.Opening += (_, _) => FillRecentMenu();
        var source = new DockPanel { LastChildFill = true };
        source.Children.Add(Ui.Dock(Ui.Icon("folder_open", "muted"), Dock.Left));
        source.Children.Add(Ui.Dock(Ui.Row(8, recent,
            Ui.Button("Browse…", async () => await Browse()),
            Ui.Text("Newest", "fieldLabel"), limit, recursive,
            Ui.IconButton("radar", "Scan", async () => await Scan(), "accent"),
            Ui.Button("Cancel", () => cancellation?.Cancel(), "danger")), Dock.Right));
        directory.Margin = new Thickness(10, 0, 8, 0);
        directory.VerticalAlignment = VerticalAlignment.Center;
        source.Children.Add(directory);
        var card = Ui.Card(source, 10);
        card.Margin = new Thickness(0, 0, 0, 16);
        return card;
    }

    private Control BuildSidebar()
    {
        var panel = new DockPanel { Width = 330, Margin = new Thickness(0, 0, 16, 0) };
        runCount.Margin = new Thickness(4, 0, 0, 8);
        panel.Children.Add(Ui.Dock(runCount, Dock.Top));
        filter.Margin = new Thickness(0, 0, 0, 10);
        panel.Children.Add(Ui.Dock(filter, Dock.Top));
        panel.Children.Add(list);
        return panel;
    }

    private Control BuildRunPanel()
    {
        var panel = new DockPanel();
        var title = new StackPanel { Spacing = 6 };
        title.Children.Add(gameTitle);
        title.Children.Add(headerTags);
        headerFile.FontSize = 12;
        title.Children.Add(headerFile);
        loadMore = Ui.IconButton("library_add", "Load more runs", async () => await LoadMoreForTitle());
        loadMore.IsVisible = false;
        var actions = Ui.Row(8, loadMore,
            Ui.IconButton("description", "Open log", OpenSource),
            Ui.IconButton("folder", "Show in folder", OpenFolder),
            Ui.IconButton("ios_share", "Export HTML", async () => await Export(), "accent"));
        actions.VerticalAlignment = VerticalAlignment.Top;
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
        header.Children.Add(Ui.Dock(actions, Dock.Right));
        header.Children.Add(title);
        panel.Children.Add(Ui.Dock(header, Dock.Top));

        var window = Ui.Row(8, Ui.Text("WINDOW", "sectionTitle"), mode,
            Ui.Text("Start s", "fieldLabel"), start, Ui.Text("End s", "fieldLabel"), end,
            Ui.Text("Trim end s", "fieldLabel"), trim, Ui.Button("Apply", ApplyWindow));
        var labels = Ui.Row(8, Ui.Text("LABEL", "sectionTitle"), label, Ui.Button("Save", SaveLabel));
        var controls = new DockPanel();
        controls.Children.Add(Ui.Dock(labels, Dock.Right));
        controls.Children.Add(window);
        var controlCard = Ui.Card(controls, 10);
        controlCard.Margin = new Thickness(0, 0, 0, 12);
        panel.Children.Add(Ui.Dock(controlCard, Dock.Top));

        var switcher = Ui.Row(4);
        switcher.Margin = new Thickness(-8, 0, 0, 12);
        for (var i = 0; i < SectionNames.Length; i++)
        {
            var index = i;
            tabs[i] = Ui.Button(SectionNames[i], () => SelectSection(index), "segment");
            switcher.Children.Add(tabs[i]);
        }
        panel.Children.Add(Ui.Dock(new ScrollViewer
        {
            Content = switcher, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        }, Dock.Top));
        panel.Children.Add(sectionHost);
        return panel;
    }

    private Control BuildStatusBar()
    {
        var bar = new DockPanel { Margin = new Thickness(0, 12, 0, 0) };
        progress.Margin = new Thickness(12, 0, 0, 0);
        bar.Children.Add(Ui.Dock(progress, Dock.Right));
        status.Classes.Add("muted");
        status.FontSize = 12;
        bar.Children.Add(status);
        return bar;
    }

    private static Control BuildEmptyState()
    {
        var stack = new StackPanel { Spacing = 10, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        var icon = Ui.Icon("query_stats", "accent");
        icon.FontSize = 56;
        icon.LineHeight = 60;
        stack.Children.Add(icon);
        var title = Ui.Text("SEE THE SIGNAL. FIND THE PROBLEM.");
        title.FontSize = 20;
        title.FontWeight = Avalonia.Media.FontWeight.Bold;
        title.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(title);
        var hint = Ui.Text("Choose a SharpEmu log folder above and press Scan. Logs are read only.", "muted");
        hint.HorizontalAlignment = HorizontalAlignment.Center;
        stack.Children.Add(hint);
        return stack;
    }
}
