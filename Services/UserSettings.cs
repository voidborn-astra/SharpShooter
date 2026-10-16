// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Text.Json;

namespace SharpShooter;

public sealed class UserSettings
{
    public const int RecentLimit = 10;
    public string LogDirectory { get; set; } = "";
    public List<string> RecentDirectories { get; set; } = [];
    public int NewestCount { get; set; } = 30;
    public bool IncludeSubfolders { get; set; }
    public Dictionary<string, string> Labels { get; set; } = [];
    private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SharpShooter", "settings.json");

    public static UserSettings Load()
    {
        try { return JsonSerializer.Deserialize(File.ReadAllText(SettingsPath), JsonContext.Default.UserSettings) ?? new(); }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }

    /// <summary>Moves a directory to the front of the recent list.</summary>
    public void Remember(string directory)
    {
        LogDirectory = directory;
        RecentDirectories.RemoveAll(d => string.Equals(d, directory, StringComparison.OrdinalIgnoreCase));
        RecentDirectories.Insert(0, directory);
        if (RecentDirectories.Count > RecentLimit) RecentDirectories.RemoveRange(RecentLimit, RecentDirectories.Count - RecentLimit);
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonContext.Default.UserSettings));
    }
}
