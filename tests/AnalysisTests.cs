// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Xunit;

namespace SharpShooter.Tests;

public sealed class AnalysisTests
{
    [Fact]
    public void FpsUsesTimeWeightAndSkipsLargeGaps()
    {
        var run = new LogRun { Path = "test", Duration = 30 };
        run.Fps.AddRange([new(0, 0, 0, 1), new(1, 10, 60, 2), new(3, 40, 60, 3), new(30, 999, 999, 4)]);
        var result = Analyzer.Analyze(run, new());
        Assert.Equal(30, result.Submitted);
        Assert.Equal(60, result.Presented);
        Assert.Equal(3, result.Coverage);
    }

    [Theory]
    [InlineData("Full run", 0, 96)]
    [InlineData("Last 20 seconds", 76, 96)]
    [InlineData("First 180 seconds", 0, 96)]
    [InlineData("Custom window", 10, 30)]
    public void WindowsApplyTailTrim(string mode, double from, double until)
    {
        var run = new LogRun { Path = "test", Duration = 100 };
        Assert.Equal((from, until), new WindowSelection(mode, 10, 30, 4).Resolve(run));
    }

    [Fact]
    public void RenderAveragesWeightFramesNotReportCount()
    {
        var run = new LogRun { Path = "test", Duration = 20 };
        run.Metrics.Add(new(10, "RENDER_MS", false, 10, 100, new() { ["Draw"] = 10 }, 1));
        run.Metrics.Add(new(20, "RENDER_MS", false, 10, 300, new() { ["Draw"] = 20 }, 2));
        Assert.Equal(17.5, Analyzer.Analyze(run, new()).RenderCosts["Draw"]);
    }

    [Fact]
    public void ExportEscapesLogContentAndScriptData()
    {
        var run = new LogRun { Path = "test", Label = "</script><script>alert(1)</script>" };
        run.Findings.Add(new(0, "Error", "<img src=x onerror=alert(1)>", 1));
        var html = HtmlReport.Create([run], new());
        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.DoesNotContain("<img src=x", html);
        Assert.Contains("&lt;img", html);
    }
}
