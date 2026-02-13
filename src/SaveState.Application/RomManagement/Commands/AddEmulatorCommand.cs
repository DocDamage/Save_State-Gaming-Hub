using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.RomManagement.Commands;

/// <summary>
/// Command to add a new emulator.
/// </summary>
public record AddEmulatorCommand : IRequest<Result<EmulatorResult>>
{
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

/// <summary>
/// Result containing the created emulator information.
/// </summary>
public record EmulatorResult(
    Guid Id,
    string Name,
    string ExecutablePath,
    Guid PlatformId,
    string? Version,
    string? Description,
    string? CommandLineArgs,
    bool IsAvailable);