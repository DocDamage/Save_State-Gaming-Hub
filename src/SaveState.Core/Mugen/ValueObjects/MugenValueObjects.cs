using System;
using System.Collections.Generic;

namespace SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Fusion modes for merged MUGEN characters.
/// Extended to cover additional plugin scenarios.
/// </summary>
public enum FusionType
{
    Balanced,
    Dominant,
    GodLike,
    Chain,
    Multi,
    Custom
}

/// <summary>
/// Options that control the fusion process.
/// </summary>
public sealed class FusionOptions
{
    public bool PreserveSprites { get; init; } = true;
    public bool PreserveAnimations { get; init; } = true;
    public bool PreserveSound { get; init; } = true;
    public bool PreserveStates { get; init; } = true;
}

/// <summary>
/// Result of creating a fusion character.
/// </summary>
public sealed record FusionResult(Guid Id, string Name, string Directory, MugenCharacterStats Stats);

/// <summary>
/// Metadata exposed for available fusions.
/// </summary>
public sealed record FusionMetadata(Guid Id, string Name, DateTime CreatedAt, IReadOnlyList<string> SourceCharacters, int PowerLevel);

/// <summary>
/// Simple stats used in fusion calculations.
/// </summary>
public sealed record MugenCharacterStats(int Health, int Attack, int Defense, float Speed, int PowerLevel);

/// <summary>
/// Options provided to the move exporter.
/// </summary>
public sealed class ExportOptions
{
    public string OutputDirectory { get; init; } = "./output";
    public bool GenerateAirVersion { get; init; }
    public bool IncludeComments { get; init; } = true;

    public ExportOptions()
    {
    }

    public ExportOptions(string outputDirectory, bool generateAirVersion = false, bool includeComments = true)
    {
        OutputDirectory = outputDirectory;
        GenerateAirVersion = generateAirVersion;
        IncludeComments = includeComments;
    }
}

/// <summary>
/// Result returned by the export process.
/// </summary>
public sealed record MoveExportResult(
    string CnsFilePath,
    string CmdFilePath,
    string? AirFilePath,
    long CnsFileSize,
    long CmdFileSize,
    long AirFileSize,
    IReadOnlyList<string> GeneratedStates,
    IReadOnlyList<string> Warnings);
