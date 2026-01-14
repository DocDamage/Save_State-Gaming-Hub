namespace SaveState.Application.Mugen.DTOs;

/// <summary>
/// Data transfer object for MUGEN replay summary information.
/// </summary>
public record MugenReplaySummary(
    Guid Id,
    string Player1Name,
    string Player2Name,
    string WinnerName,
    DateTime RecordedAt,
    TimeSpan Duration,
    string FilePath,
    string? ThumbnailPath = null
);
