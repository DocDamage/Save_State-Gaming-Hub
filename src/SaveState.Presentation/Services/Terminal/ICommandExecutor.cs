using System.Collections.Generic;
using System.Threading.Tasks;

namespace SaveState.Presentation.Services.Terminal;

/// <summary>
/// Service for executing CLI-style commands within the application.
/// </summary>
public interface ICommandExecutor
{
    /// <summary>
    /// Executes a command string and returns the output.
    /// </summary>
    /// <param name="command">The command string to execute.</param>
    /// <returns>The text output of the command execution.</returns>
    Task<string> ExecuteAsync(string command);

    /// <summary>
    /// Gets the history of executed commands.
    /// </summary>
    /// <returns>A list of command strings.</returns>
    IEnumerable<string> GetHistory();

    /// <summary>
    /// Clears the command history.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Gets suggested completions for a partial command.
    /// </summary>
    /// <param name="text">The partial command text.</param>
    /// <returns>A list of suggested completions.</returns>
    IEnumerable<string> GetCompletions(string text);
}
