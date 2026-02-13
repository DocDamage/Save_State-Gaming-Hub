using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.RomManagement;

namespace SaveState.Application.RomManagement.Commands;

/// <summary>
/// Command to import a ROM into the game library as a playable game.
/// </summary>
public record ImportRomToLibraryCommand : IRequest<Result<ImportRomResult>>
{
    /// <summary>
    /// Gets the ROM file ID to import.
    /// </summary>
    public required RomFileId RomFileId { get; init; }

    /// <summary>
    /// Gets the optional game title override.
    /// </summary>
    public string? TitleOverride { get; init; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether to create the game if it doesn't exist.
    /// </summary>
    public bool CreateIfNotExists { get; init; } = true;
}

/// <summary>
/// Result of importing a ROM to the library.
/// </summary>
public record ImportRomResult(
    Guid GameId,
    string GameTitle,
    RomFileId RomFileId,
    string RomTitle,
    bool GameWasCreated);