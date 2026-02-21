using System.CommandLine;
using MediatR;
using Microsoft.Extensions.Hosting;
using Spectre.Console;

namespace SaveState.CLI.Commands;

/// <summary>
/// Base class for CLI command groups that provides common functionality.
/// </summary>
public abstract class CommandGroupBase : ICommandGroup
{
    /// <summary>
    /// Gets the MediatR mediator instance.
    /// </summary>
    protected IMediator? Mediator { get; private set; }

    /// <summary>
    /// Gets the host instance for service resolution.
    /// </summary>
    protected IHost? Host { get; private set; }

    /// <summary>
    /// Gets the console instance for output.
    /// </summary>
    protected IAnsiConsole? AnsiConsole { get; private set; }

    /// <summary>
    /// Registers all commands in this group with the root command.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    /// <param name="mediator">The MediatR mediator for sending commands/queries.</param>
    /// <param name="host">The host instance for service resolution.</param>
    /// <param name="console">The console instance for output.</param>
    public void RegisterCommands(RootCommand rootCommand, IMediator mediator, IHost host, IAnsiConsole console)
    {
        Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        Host = host ?? throw new ArgumentNullException(nameof(host));
        AnsiConsole = console ?? throw new ArgumentNullException(nameof(console));
        BuildCommands(rootCommand);
    }

    /// <summary>
    /// Builds and registers the commands for this group.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    protected abstract void BuildCommands(RootCommand rootCommand);
}
