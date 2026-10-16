// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace SharpShooter;

/// <summary>A thin horizontal bar that shows a value as a share of a maximum.</summary>
public sealed class ValueBar : Grid
{
    public ValueBar(double value, double maximum, string color = "#7C5CFC", double width = 140)
    {
        Width = width;
        Height = 6;
        var share = maximum > 0 && double.IsFinite(value) ? Math.Clamp(value / maximum, 0, 1) : 0;
        Children.Add(new Border { Background = new SolidColorBrush(Color.Parse("#1FFFFFFF")), CornerRadius = new CornerRadius(3) });
        Children.Add(new Border
        {
            Background = new SolidColorBrush(Color.Parse(color)), CornerRadius = new CornerRadius(3),
            Width = Math.Max(share > 0 ? 3 : 0, width * share), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        });
    }

    /// <summary>Hex color for an FPS value against the 60 FPS target.</summary>
    public static string FpsColor(double? fps) => Ui.FpsTone(fps) switch
    {
        "good" => "#66F2A3",
        "warn" => "#FFCB6B",
        "bad" => "#FF646E",
        _ => "#777F8E"
    };
}
