// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Diagnostics;

namespace SharpShooter;

/// <summary>Opens files and folders with the platform's default handlers.</summary>
public static class FileReveal
{
    /// <summary>Opens a file with its default application.</summary>
    public static void Open(string path)
    {
        if (OperatingSystem.IsMacOS()) Start("open", path);
        else if (OperatingSystem.IsWindows()) Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        else Start("xdg-open", path);
    }

    /// <summary>Shows a file in the platform file manager, selected where supported.</summary>
    public static void Show(string path)
    {
        if (OperatingSystem.IsWindows()) Start("explorer.exe", "/select,", path);
        else if (OperatingSystem.IsMacOS()) Start("open", "-R", path);
        else Start("xdg-open", Path.GetDirectoryName(path) ?? path);
    }

    // ArgumentList quotes each argument, so log file names cannot inject options.
    private static void Start(string program, params string[] arguments)
    {
        var info = new ProcessStartInfo(program) { UseShellExecute = false };
        foreach (var argument in arguments) info.ArgumentList.Add(argument);
        Process.Start(info)?.Dispose();
    }
}
