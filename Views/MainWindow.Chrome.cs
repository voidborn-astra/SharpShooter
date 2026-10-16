// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace SharpShooter;

/// <summary>Custom title bar and resize edges. Matches the SharpEmu launcher chrome.</summary>
public sealed partial class MainWindow
{
    private readonly TextBlock maximizeGlyph = Ui.Icon("crop_square", "compact");
    private readonly Panel resizeHandles = new();

    private Control BuildTitleBar()
    {
        var bar = new Grid { Height = 44, ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        bar.Bind(Grid.BackgroundProperty, this.GetResourceObservable("ChromeBrush"));
        Control logo = new Border { Width = 22, Height = 22 };
        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://SharpShooter/Assets/icon-64.png"));
            logo = new Image { Source = new Bitmap(stream), Width = 22, Height = 22 };
            RenderOptions.SetBitmapInterpolationMode(logo, BitmapInterpolationMode.HighQuality);
            using var icon = AssetLoader.Open(new Uri("avares://SharpShooter/Assets/SharpShooter.ico"));
            Icon = new WindowIcon(icon);
        }
        catch (Exception error) when (error is IOException or FileNotFoundException) { }
        var name = Ui.Text("SharpShooter");
        name.FontSize = 14;
        name.FontWeight = FontWeight.SemiBold;
        var version = Ui.Text("v" + Program.Version, "muted");
        version.FontSize = 11;
        var brand = Ui.Row(10, logo, name, Ui.Pill(version), Ui.Pill(Ui.Text("SharpEmu log analysis")));
        brand.Margin = new Thickness(16, 0);
        bar.Children.Add(brand);

        var maximize = ChromeButton(maximizeGlyph, "Maximize", ToggleMaximized);
        var buttons = Ui.Row(0,
            ChromeButton(Ui.Icon("minimize", "compact"), "Minimize", () => WindowState = WindowState.Minimized),
            maximize,
            ChromeButton(Ui.Icon("close"), "Close", Close, "windowClose"));
        buttons.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(buttons, 1);
        bar.Children.Add(buttons);

        bar.PointerPressed += (_, e) =>
        {
            if (IsChromeButton(e.Source) || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
            if (e.ClickCount == 2) ToggleMaximized(); else BeginMoveDrag(e);
        };
        PropertyChanged += (_, e) =>
        {
            if (e.Property != WindowStateProperty) return;
            var maximized = WindowState == WindowState.Maximized;
            maximizeGlyph.Text = maximized ? "filter_none" : "crop_square";
            ToolTip.SetTip(maximize, maximized ? "Restore" : "Maximize");
            resizeHandles.IsVisible = WindowState == WindowState.Normal;
        };
        return bar;
    }

    private static Button ChromeButton(TextBlock glyph, string tip, Action action, string? extra = null)
    {
        var button = Ui.Button(glyph, action, "windowChrome", tip);
        if (extra is not null) button.Classes.Add(extra);
        Avalonia.Automation.AutomationProperties.SetName(button, tip);
        return button;
    }

    private static bool IsChromeButton(object? source) =>
        source is Visual visual && visual.FindAncestorOfType<Button>(includeSelf: true) is not null;

    private void ToggleMaximized() =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private Control BuildResizeHandles()
    {
        const double edge = 5, corner = 10;
        Add(WindowEdge.North, StandardCursorType.TopSide, double.NaN, edge, HorizontalAlignment.Stretch, VerticalAlignment.Top);
        Add(WindowEdge.South, StandardCursorType.BottomSide, double.NaN, edge, HorizontalAlignment.Stretch, VerticalAlignment.Bottom);
        Add(WindowEdge.West, StandardCursorType.LeftSide, edge, double.NaN, HorizontalAlignment.Left, VerticalAlignment.Stretch);
        Add(WindowEdge.East, StandardCursorType.RightSide, edge, double.NaN, HorizontalAlignment.Right, VerticalAlignment.Stretch);
        Add(WindowEdge.NorthWest, StandardCursorType.TopLeftCorner, corner, corner, HorizontalAlignment.Left, VerticalAlignment.Top);
        Add(WindowEdge.NorthEast, StandardCursorType.TopRightCorner, corner, corner, HorizontalAlignment.Right, VerticalAlignment.Top);
        Add(WindowEdge.SouthWest, StandardCursorType.BottomLeftCorner, corner, corner, HorizontalAlignment.Left, VerticalAlignment.Bottom);
        Add(WindowEdge.SouthEast, StandardCursorType.BottomRightCorner, corner, corner, HorizontalAlignment.Right, VerticalAlignment.Bottom);
        return resizeHandles;

        void Add(WindowEdge side, StandardCursorType cursor, double width, double height, HorizontalAlignment h, VerticalAlignment v)
        {
            var handle = new Border
            {
                Width = width, Height = height, HorizontalAlignment = h, VerticalAlignment = v,
                Background = Brushes.Transparent, Cursor = new Cursor(cursor)
            };
            handle.PointerPressed += (_, e) =>
            {
                if (WindowState != WindowState.Normal || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
                BeginResizeDrag(side, e);
                e.Handled = true;
            };
            resizeHandles.Children.Add(handle);
        }
    }
}
