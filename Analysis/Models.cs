// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

namespace SharpShooter;

public sealed record FpsPoint(double Seconds, double Submitted, double Presented, int Line);
public sealed record MetricPoint(double Seconds, string Tag, bool Cumulative, double Window, double Frames, Dictionary<string, double> Values, int Line);
public sealed record FramePoint(long Sequence, long Timestamp, double Gap, Dictionary<string, double> Phases);
public sealed record Finding(double Seconds, string Category, string Message, int Line);

/// <summary>Repeated issue lines that match after numbers and addresses are masked.</summary>
public sealed class IssueGroup(string category, string pattern, string sample, int line)
{
    public string Category { get; } = category;
    public string Pattern { get; } = pattern;
    public string Sample { get; } = sample;
    public int FirstLine { get; } = line;
    public int Count { get; set; }
}
public sealed record WindowSelection(string Mode = "Full run", double Start = 0, double End = 180, double TrimEnd = 0)
{
    public (double Start, double End) Resolve(LogRun run)
    {
        var end = Math.Max(0, run.Duration - Math.Max(0, TrimEnd));
        return Mode switch
        {
            "Last 20 seconds" => (Math.Max(0, end - 20), end),
            "First 180 seconds" => (0, Math.Min(180, end)),
            "Custom window" => (Math.Clamp(Start, 0, end), Math.Clamp(End, Math.Clamp(Start, 0, end), end)),
            _ => (0, end)
        };
    }
}

public sealed class LogRun
{
    public required string Path { get; init; }
    public string FileName => System.IO.Path.GetFileName(Path);
    public string TitleId { get; set; } = "Unknown title";
    public string? GameName { get; set; }
    public string? GameVersion { get; set; }
    public string DisplayName => GameName ?? TitleId;
    public string Label { get; set; } = "Unlabeled";
    public long Bytes { get; set; }
    public int Lines { get; set; }
    public double Duration { get; set; }
    public DateTime Modified { get; set; }
    public string Status { get; set; } = "No clean-exit evidence";
    public List<FpsPoint> Fps { get; } = [];
    public List<MetricPoint> Metrics { get; } = [];
    public List<FramePoint> Frames { get; } = [];
    public List<Finding> Findings { get; } = [];
    public Dictionary<string, int> FindingCounts { get; } = [];
    public Dictionary<string, IssueGroup> IssueGroups { get; } = [];
    public int UngroupedIssues { get; set; }
    public Dictionary<string, List<string>> EnvironmentEvidence { get; } = [];
    public List<string> Configuration { get; } = [];
    public HashSet<string> Tags { get; } = [];
    public Dictionary<string, int> TraceEvents { get; } = [];
    public List<string> Warnings { get; } = [];
    public long FrameFrequency { get; set; }
    public int DroppedMetrics { get; set; }
    public int DroppedFindings { get; set; }
    public int IssueTotal => FindingCounts.Values.Sum();
    public bool CleanExit => Status.StartsWith("Exit code 0", StringComparison.Ordinal);
    public override string ToString() =>
        $"{DisplayName}  ·  {TitleId}  ·  {Label}\n{FileName}\n{Duration:F0}s  ·  {Fps.Count:N0} FPS samples  ·  {IssueTotal:N0} issue lines";
}

public sealed record WindowStats(double Start, double End, int Samples, double? Submitted, double? Presented,
    double Coverage, double? Minimum, double? Maximum, double? P50, double? P95,
    Dictionary<string, double> RenderCosts, double RenderCoverage, int Issues);
