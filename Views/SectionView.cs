// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;

namespace SharpShooter;

/// <summary>A section is a scrolling stack of cards rebuilt on each Show.</summary>
public abstract class SectionView : UserControl
{
    protected readonly StackPanel Body = new() { Spacing = 12, Margin = new Thickness(0, 0, 12, 12) };

    protected SectionView() => Content = new ScrollViewer { Content = Body };

    public abstract void Show(LogRun run, WindowSelection selection);

    protected T Add<T>(T control) where T : Control
    {
        Body.Children.Add(control);
        return control;
    }

    protected void Empty(string icon, string title, string detail)
    {
        var stack = new StackPanel { Spacing = 8, Margin = new Thickness(0, 40) };
        var glyph = Ui.Icon(icon, "muted");
        glyph.FontSize = 40;
        glyph.LineHeight = 44;
        stack.Children.Add(glyph);
        var heading = Ui.Text(title, "heading");
        heading.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        stack.Children.Add(heading);
        var note = Ui.Text(detail, "muted");
        note.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
        note.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
        note.TextAlignment = Avalonia.Media.TextAlignment.Center;
        stack.Children.Add(note);
        Add(Ui.Card(stack));
    }
}
