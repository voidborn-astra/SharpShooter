// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia.Controls;

namespace SharpShooter;

/// <summary>A labeled headline number with a short note, used above section charts.</summary>
public sealed class StatTile : Border
{
    private readonly TextBlock value = Ui.Text("—", "statValue");
    private readonly TextBlock note = Ui.Text("", "statNote");

    public StatTile(string caption)
    {
        Classes.Add("statTile");
        var stack = new StackPanel();
        stack.Children.Add(Ui.Text(caption.ToUpperInvariant(), "sectionTitle"));
        stack.Children.Add(value);
        stack.Children.Add(note);
        Child = stack;
    }

    public string Value => value.Text ?? "";

    /// <summary>Sets the value. Tone is one of good, warn, bad, or null for neutral.</summary>
    public void Set(string text, string detail, string? tone = null)
    {
        value.Text = text;
        note.Text = detail;
        value.Classes.Set("good", tone == "good");
        value.Classes.Set("warn", tone == "warn");
        value.Classes.Set("bad", tone == "bad");
    }
}
