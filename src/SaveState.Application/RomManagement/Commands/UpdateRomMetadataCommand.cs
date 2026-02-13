using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;

namespace SaveState.Application.RomManagement.Commands;

/// <summary>
/// Command to update ROM metadata.
/// </summary>
public record UpdateRomMetadataCommand : IRequest<Result>
{
    /// <summary>
    /// Gets the ROM file ID.
    /// </summary>
    public required Guid RomFileId { get; init; }

    /// <summary>
    /// Gets the ROM title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the ROM description (optional).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the ROM region (optional).
    /// </summary>
    public string? Region { get; init; }

    /// <summary>
    /// Gets the ROM version (optional).
    /// </summary>
    public string? Version { get; init; }
}