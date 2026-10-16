// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using Xunit;

namespace SharpShooter.Tests;

public sealed class ParserTests
{
    private static LogRun Parse(string text)
    {
        var folder = Environment.GetEnvironmentVariable("SHARPSHOOTER_TEST_OUTPUT") ??
            Path.Combine(Path.GetTempPath(), "SharpShooterTests");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, $"fixture-PPSA01342-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, text);
        return LogParser.Parse(path);
    }

    [Fact]
    public void ReadsMidnightAndExplicitEnvironment()
    {
        var run = Parse("[23:59:59.000] SHARPEMU_VK_PIPELINE_CACHE='file'\n[00:00:01.000] submitted_fps=25 presented_fps=60\n");
        Assert.Equal(2, run.Duration, 5);
        Assert.Equal("PPSA01342", run.TitleId);
        Assert.Contains("file", Assert.Single(run.EnvironmentEvidence["SHARPEMU_VK_PIPELINE_CACHE"]));
        Assert.Equal(60, Assert.Single(run.Fps).Presented);
    }

    [Fact]
    public void ReadsFrameClockWithoutUsingDumpWallTime()
    {
        var run = Parse("[10:00:00.000] start\n[10:02:00.000] [PERF][FRAME_TRACE] frequency=1000 overwritten=20\n" +
            "[10:02:00.000] [PERF][FRAME] sequence=1 timestamp=1000 gap_ms=40 Compute=3.2\n" +
            "[10:02:00.000] [PERF][FRAME] sequence=2 timestamp=1040 gap_ms=40 Compute=4.2\n");
        Assert.Equal(1000, run.FrameFrequency);
        Assert.Equal(2, run.Frames.Count);
        Assert.Equal(4.2, run.Frames[1].Phases["Compute"]);
        Assert.Contains(run.Warnings, w => w.Contains("overwritten"));
    }

    [Fact]
    public void ReportsUnknownInsteadOfZeroWithoutFps()
    {
        var run = Parse("[12:00:00.000] start\n[12:00:10.000] [ERROR] failure\n");
        Assert.Null(Analyzer.Analyze(run, new()).Submitted);
        Assert.Equal(2, Assert.Single(run.Findings).Line);
        Assert.Contains("UNKNOWN", Analyzer.Configuration(run));
    }

    [Fact]
    public void RenderReportKeepsPerFrameValues()
    {
        var run = Parse("[12:00:00.000] start\n[12:00:10.000] [PERF][RENDER_MS] window_s=10 frames=200 Materialization=64.00ms/n500\n");
        Assert.Equal(64, Analyzer.Analyze(run, new()).RenderCosts["Materialization"]);
    }

    [Fact]
    public void CumulativeReportsUseEndpointDelta()
    {
        var run = Parse("[12:00:00.000] start\n[12:00:01.000] [PERF][COMPILE] cumulative=1 total_ms=10\n" +
            "[12:00:05.000] [PERF][COMPILE] cumulative=1 total_ms=35\n");
        Assert.Contains("total_ms: 25.000", Analyzer.Details(run, new()));
    }

    [Fact]
    public void ReadsGameNameFromLoaderLine()
    {
        var run = Parse("[12:00:00.000] Log file: x\n" +
            "[12:00:01.000] [LOADER] Title: Ratchet & Clank, Rift Apart, TitleId: PPSA01474, Version: 01.004.000\n");
        Assert.Equal("Ratchet & Clank, Rift Apart", run.GameName);
        Assert.Equal("PPSA01474", run.TitleId);
        Assert.Equal("01.004.000", run.GameVersion);
        Assert.Contains("Ratchet & Clank", Analyzer.Summary(run, new()));
    }

    [Fact]
    public void GameNameFallsBackToTitleId()
    {
        var run = Parse("[12:00:00.000] start\n");
        Assert.Null(run.GameName);
        Assert.Equal("PPSA01342", run.DisplayName);
    }

    [Fact]
    public void RepeatedIssuesCollapseToOnePattern()
    {
        var lines = Enumerable.Range(0, 300).Select(i =>
            $"[12:00:{i % 60:D2}.000] [LOADER][WARN] Import#{i} result: ORBIS_GEN2_ERROR_TRY_AGAIN rdi=0x{i:X16}");
        var run = Parse(string.Join('\n', lines) + "\n[12:01:00.000] [ERROR] device failed\n");
        Assert.Equal(2, run.IssueGroups.Count);
        Assert.Equal(300, run.IssueGroups.Values.Max(g => g.Count));
        var window = IssueGrouping.Window(run, 0, 1000);
        Assert.Contains("300×", window);
        Assert.Single(window.Split('\n'), l => l.Contains("ORBIS_GEN2_ERROR_TRY_AGAIN"));
    }

    [Fact]
    public void CancellationIsObserved()
    {
        var token = new CancellationToken(true);
        var run = Parse(string.Join('\n', Enumerable.Repeat("[12:00:00.000] line", 1024)));
        Assert.Throws<OperationCanceledException>(() => LogParser.Parse(run.Path, token));
    }
}
