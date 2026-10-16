// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace SharpShooter;

/// <summary>A column definition: header text, grid width ("Auto", "*", "120"), and alignment.</summary>
public sealed record TableColumn(string Header, string Width = "Auto", bool Right = false);

/// <summary>A light table built on Grid with a header row and striped body rows.</summary>
public sealed class DataTable : Grid
{
    private readonly TableColumn[] columns;
    private int rowCount;

    public DataTable(params TableColumn[] columns)
    {
        this.columns = columns;
        ColumnDefinitions = new ColumnDefinitions(string.Join(",", columns.Select(c => c.Width)));
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        if (columns.All(c => c.Header.Length == 0)) return;
        for (var i = 0; i < columns.Length; i++)
        {
            var header = Ui.Text(columns[i].Header.ToUpperInvariant(), "sectionTitle");
            header.Margin = new Thickness(10, 0, 10, 8);
            header.HorizontalAlignment = columns[i].Right ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            Place(header, 0, i);
        }
    }

    public int Count => rowCount;

    /// <summary>Adds a row. Strings become text cells; controls are placed as given.</summary>
    public void AddRow(params object[] cells)
    {
        var row = ++rowCount;
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var stripe = new Border();
        stripe.Classes.Add(row % 2 == 1 ? "tableRow" : "tableRowAlt");
        SetColumnSpan(stripe, columns.Length);
        Place(stripe, row, 0);
        for (var i = 0; i < cells.Length && i < columns.Length; i++)
        {
            var control = cells[i] as Control ?? Ui.Text(cells[i]?.ToString() ?? "", "cell");
            control.Margin = new Thickness(10, 7, 10, 7);
            control.VerticalAlignment = VerticalAlignment.Center;
            if (columns[i].Right) control.HorizontalAlignment = HorizontalAlignment.Right;
            Place(control, row, i);
        }
    }

    /// <summary>Adds a full-width group label between rows.</summary>
    public void AddGroup(string title)
    {
        var row = ++rowCount;
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var label = Ui.Text(title.ToUpperInvariant(), "sectionTitle", "accent");
        label.Margin = new Thickness(10, 16, 10, 6);
        SetColumnSpan(label, columns.Length);
        Place(label, row, 0);
    }

    private void Place(Control control, int row, int column)
    {
        SetRow(control, row);
        SetColumn(control, column);
        Children.Add(control);
    }
}
