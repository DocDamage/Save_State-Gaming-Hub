using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;

namespace SaveState.Application.RomManagement.Commands;

/// <summary>
/// Command to update an existing emulator.
/// </summary>
public record UpdateEmulatorCommand : IRequest<Result<EmulatorResult>>
{
    /// <summary>
    /// Gets the emulator ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the emulator name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the executable path.
    /// </summary>
    public required string ExecutablePath { get; init; }

    /// <summary>
    /// Gets the platform ID.
    /// </summary>
    public required Guid PlatformId { get; init; }

    /// <summary>
    /// Gets the version (optional).
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the description (optional).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the command line arguments (optional).
    /// </summary>
    public string? CommandLineArgs { get; init; }
}