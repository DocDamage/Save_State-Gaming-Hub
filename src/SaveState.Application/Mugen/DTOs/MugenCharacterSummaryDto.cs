namespace SaveState.Application.Mugen.DTOs;

/// <summary>
/// Data transfer object for MUGEN character summary information.
/// </summary>
public record MugenCharacterSummaryDto(
    Guid Id,
    string Name,
    string DisplayName,
    string Author,
    string Version,
    bool IsValid,
    DateTime LastScannedAt,
    long FileSize
);
