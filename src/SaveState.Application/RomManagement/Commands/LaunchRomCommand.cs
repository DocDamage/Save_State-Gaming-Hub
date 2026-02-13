using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.Common.Options;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.RomManagement;

namespace SaveState.Application.RomManagement.Commands;

/// <summary>
/// Command to launch a ROM file.
/// </summary>
public record LaunchRomCommand : IRequest<Result<ProcessInfo>>
{
    /// <summary>
    /// Gets the ROM file ID to launch.
    /// </summary>
    public required RomFileId RomFileId { get; init; }

    /// <summary>
    /// Gets the optional launch options.
    /// </summary>
    public LaunchOptions? Options { get; init; }
}