namespace SaveState.Application.Mugen.DTOs;

/// <summary>
/// Data transfer object for MUGEN stage summary information.
/// </summary>
public record MugenStageSummary(
    Guid Id,
    string Name,
    string Author,
    string FilePath,
    bool HasMusic,
    string? MusicPath,
    DateTime LastUsed,
    int TimesUsed
);
