// Copyright (C) 2026 SharpShooter contributors
// SPDX-License-Identifier: GPL-2.0-or-later

using System.Text.Json.Serialization;

namespace SharpShooter;

/// <summary>One run's FPS points for the HTML export chart.</summary>
internal sealed record ChartSeries(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("points")] double[][] Points);

/// <summary>Compile-time JSON metadata so trimmed builds need no reflection.</summary>
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UserSettings))]
internal sealed partial class JsonContext : JsonSerializerContext;

/// <summary>Compact JSON for embedding in exported HTML.</summary>
[JsonSerializable(typeof(ChartSeries[]))]
internal sealed partial class CompactJson : JsonSerializerContext;
