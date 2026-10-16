// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace SharpShooter;

/// <summary>Small factories for themed controls. Styling lives in Themes/Styles.</summary>
internal static class Ui
{
    public static TextBlock Icon(string glyph, params string[] classes)
    {
        var icon = new TextBlock { Text = glyph };
        icon.Classes.Add("materialSymbol");
        icon.Classes.AddRange(classes);
        return icon;
    }

    public static TextBlock Text(string text, params string[] classes)
    {
        var block = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
        block.Classes.AddRange(classes);
        return block;
    }

    public static Border Pill(TextBlock text, string kind = "pill")
    {
        var border = new Border { Child = text };
        border.Classes.AddRange(kind == "pill" ? ["pill"] : ["badge", kind]);
        return border;
    }

    public static Border Card(Control child, double padding = 16)
    {
        var card = new Border { Child = child, Padding = new Thickness(padding) };
        card.Classes.Add("card");
        return card;
    }

    public static Button Button(object content, Action action, string style = "ghost", string? tip = null)
    {
        var button = new Button { Content = content, VerticalAlignment = VerticalAlignment.Center };
        button.Classes.Add(style);
        if (tip is not null) ToolTip.SetTip(button, tip);
        button.Click += (_, _) => action();
        return button;
    }

    public static Button IconButton(string glyph, string label, Action action, string style = "ghost")
    {
        var row = Row(4, Icon(glyph, "compact"), new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        row.Margin = new Thickness(0);
        var button = Button(row, action, style);
        Avalonia.Automation.AutomationProperties.SetName(button, label);
        return button;
    }

    public static StackPanel Row(double spacing, params Control[] controls)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing, VerticalAlignment = VerticalAlignment.Center };
        foreach (var control in controls) row.Children.Add(control);
        return row;
    }

    /// <summary>Card title with an optional muted subtitle.</summary>
    public static StackPanel Heading(string title, string? subtitle = null)
    {
        var stack = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 12) };
        stack.Children.Add(Text(title, "heading"));
        if (subtitle is not null)
        {
            var note = Text(subtitle, "muted");
            note.FontSize = 12;
            note.TextWrapping = TextWrapping.Wrap;
            stack.Children.Add(note);
        }
        return stack;
    }

    /// <summary>A titled card holding one body control.</summary>
    public static Border Section(string title, string? subtitle, Control body)
    {
        var stack = new StackPanel();
        stack.Children.Add(Heading(title, subtitle));
        stack.Children.Add(body);
        return Card(stack);
    }

    /// <summary>Collapsed measurement caveats.</summary>
    public static Expander Notes(params string[] lines)
    {
        var text = Text(string.Join("\n", lines), "muted");
        text.FontSize = 12;
        text.TextWrapping = TextWrapping.Wrap;
        text.LineHeight = 19;
        var expander = new Expander { Header = Row(6, Icon("info", "compact", "muted"), Text("About these numbers", "secondary")), Content = text };
        expander.HorizontalAlignment = HorizontalAlignment.Stretch;
        return expander;
    }

    /// <summary>Monospace number or code text.</summary>
    public static TextBlock Mono(string text, params string[] classes)
    {
        var block = Text(text, ["mono", .. classes]);
        return block;
    }

    /// <summary>Wrapping monospace text that can be selected and copied.</summary>
    public static SelectableTextBlock Code(string text)
    {
        var block = new SelectableTextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        block.Classes.Add("code");
        return block;
    }

    public static Border Swatch(string color, double size = 10) => new()
    {
        Width = size, Height = size, CornerRadius = new CornerRadius(size / 2),
        Background = new SolidColorBrush(Color.Parse(color)), VerticalAlignment = VerticalAlignment.Center
    };

    /// <summary>A small labeled value: muted key above a bold value.</summary>
    public static Border Metric(string key, string value, string? tone = null)
    {
        var stack = new StackPanel { Spacing = 2 };
        stack.Children.Add(Text(key, "metricKey"));
        stack.Children.Add(tone is null ? Mono(value, "metricValue") : Mono(value, "metricValue", tone));
        var border = new Border { Child = stack };
        border.Classes.Add("chip");
        return border;
    }

    /// <summary>Tone class for an FPS value against the 60 FPS target.</summary>
    public static string? FpsTone(double? fps) => fps switch
    {
        null => null,
        >= 55 => "good",
        >= 28 => "warn",
        _ => "bad"
    };

    /// <summary>Tone class for an issue category.</summary>
    public static string IssueTone(string category) => category switch
    {
        "Warning" => "warn",
        "Vulkan validation" => "info",
        _ => "bad"
    };

    public static T Dock<T>(T control, Avalonia.Controls.Dock dock) where T : Control
    {
        DockPanel.SetDock(control, dock);
        return control;
    }
}
