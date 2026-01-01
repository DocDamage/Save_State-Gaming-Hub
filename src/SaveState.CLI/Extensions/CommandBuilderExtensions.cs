using System.CommandLine;

namespace SaveState.CLI.Extensions;

/// <summary>
/// Extension methods for building CLI commands with common patterns.
/// </summary>
public static class CommandBuilderExtensions
{
    /// <summary>
    /// Adds a command to the root command with validation.
    /// </summary>
    public static void AddCommandChecked(this RootCommand rootCommand, Command command)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));

        // Check for duplicate command names
        if (rootCommand.Subcommands.Any(c => c.Name == command.Name))
        {
            throw new InvalidOperationException($"Command '{command.Name}' is already registered");
        }

        rootCommand.AddCommand(command);
    }
}