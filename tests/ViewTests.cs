// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Xunit;

namespace SharpShooter.Tests;

public sealed class ViewTests
{
    private static LogRun Run(string path, string title, int fps)
    {
        var run = new LogRun { Path = path, TitleId = title, Duration = 20, Modified = DateTime.Now };
        for (var i = 0; i <= 20; i++) run.Fps.Add(new(i, fps, fps, i + 1));
        return run;
    }

    [Fact]
    public void OverviewShowsSubmittedFps()
    {
        var overview = new OverviewView();
        overview.Show(Run("a", "PPSA00001", 30), new());
        Assert.Equal("30.00", overview.SubmittedText);
    }

    [Fact]
    public void CompareStartsWithSameGameAndKeepsChoices()
    {
        var a = Run("a", "PPSA00001", 30);
        var b = Run("b", "PPSA00001", 40);
        var other = Run("c", "PPSA00002", 50);
        var view = new ComparisonView();
        view.Show([a, b, other], a, new());
        Assert.Equal([a, b], view.IncludedRuns);

        a.Label = "baseline";
        view.Show([a, b, other], b, new());
        Assert.Equal(2, view.IncludedRuns.Count);
        Assert.Equal("baseline", view.IncludedRuns[0].Label);
    }
}
