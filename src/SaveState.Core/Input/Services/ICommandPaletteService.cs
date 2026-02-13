using SaveState.Core.Common;
using SaveState.Core.Input.Services.DTOs;

namespace SaveState.Core.Input.Services;

/// <summary>
/// Service for registering, searching, and executing command palette commands.
/// </summary>
public interface ICommandPaletteService
{
    /// <summary>
    /// Searches commands by free-text query and contextual filters.
    /// </summary>
    Task<Result<IReadOnlyList<CommandItem>>> SearchAsync(
        string query,
        CommandContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Executes a command by its unique identifier.
    /// </summary>
    Task<Result> ExecuteAsync(
        string commandId,
        CancellationToken ct = default);

    /// <summary>
    /// Registers or replaces a command definition.
    /// </summary>
    void RegisterCommand(CommandDefinition command);

    /// <summary>
    /// Unregisters a command.
    /// </summary>
    void UnregisterCommand(string commandId);
}
