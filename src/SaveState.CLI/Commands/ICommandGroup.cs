using System.CommandLine;
using MediatR;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace SaveState.CLI.Commands;

/// <summary>
/// Interface for CLI command groups that organize related commands together.
/// </summary>
public interface ICommandGroup
{
    /// <summary>
    /// Registers all commands in this group with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    /// <param name="mediator">The MediatR mediator for sending commands/queries.</param>
    /// <param name="host">The host instance for service resolution.</param>
    /// <param name="console">The console instance for output.</param>
    void RegisterCommands(RootCommand rootCommand, IMediator mediator, IHost host, IAnsiConsole console);
}
