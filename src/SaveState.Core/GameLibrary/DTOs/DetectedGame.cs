namespace SaveState.Core.GameLibrary.DTOs;

/// <summary>
/// Represents a game discovered during automatic library scanning.
/// </summary>
public sealed record DetectedGame(
    string Title,
    string ExecutablePath,
    string Source,
    string? PlatformHint = null,
    string? ExternalId = null,
    long? SizeBytes = null,
    DateTime? InstallDate = null,
    string? IconPath = null,
    string? LaunchCommand = null,
    IReadOnlyDictionary<string, string>? Metadata = null);
