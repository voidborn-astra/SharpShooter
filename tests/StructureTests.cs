// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Xunit;

namespace SharpShooter.Tests;

public sealed class StructureTests
{
    [Fact]
    public void MaintainedFilesStayUnderFiveHundredLines()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "SharpShooter.slnx"))) folder = folder.Parent;
        Assert.NotNull(folder);
        var files = Directory.EnumerateFiles(folder.FullName, "*", SearchOption.AllDirectories)
            .Where(path => !Path.GetRelativePath(folder.FullName, path).Split(Path.DirectorySeparatorChar)
                .Any(segment => segment is "bin" or "obj" or "artifacts" or ".git" or "Assets" or "LICENSES" or "publish"))
            .Where(path => Path.GetExtension(path).ToLowerInvariant() is not (".png" or ".ico" or ".icns" or ".ttf"));
        foreach (var path in files)
            Assert.True(File.ReadLines(path).Take(500).Count() < 500, $"File reaches 500 lines: {path}");
    }
}
