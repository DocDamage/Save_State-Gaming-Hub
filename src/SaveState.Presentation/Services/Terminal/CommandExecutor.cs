using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Hosting;
using SaveState.CLI.Commands;
using Spectre.Console;

namespace SaveState.Presentation.Services.Terminal;

/// <summary>
/// Implementation of ICommandExecutor that integrates with the SaveState CLI commands.
/// </summary>
public class CommandExecutor : ICommandExecutor
{
    private readonly IMediator _mediator;
    private readonly IHost _host;
    private readonly List<string> _history = new();
    private readonly int _maxHistory = 100;

    public CommandExecutor(IMediator mediator, IHost host)
    {
        _mediator = mediator;
        _host = host;
    }

    public async Task<string> ExecuteAsync(string command)
    {
        if (string.IsNullOrWhiteSpace(command)) return string.Empty;

        _history.Add(command);
        if (_history.Count > _maxHistory) _history.RemoveAt(0);

        // Capture Spectre.Console output
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer),
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor
        });

        // Initialize commands with the captured console
        var rootCommand = new RootCommand("SaveState UI Terminal");
        var commandGroups = GetCommandGroups();

        foreach (var group in commandGroups)
        {
            group.RegisterCommands(rootCommand, _mediator, _host, console);
        }

        try
        {
            // Split command string into args, handling quotes
            var args = ParseArguments(command);

            // Execute the command
            await rootCommand.InvokeAsync(args.ToArray()).ConfigureAwait(false);

            // Return captured output
            return writer.ToString();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private IEnumerable<ICommandGroup> GetCommandGroups()
    {
        return new ICommandGroup[]
        {
            new GameCommands(),
            new SaveStateCommands(),
            new BacklogCommands(),
            new PerformanceCommands(),
            new NetworkCommands(),
            new MemoryCommands(),
            new CloudCommands(),
            new AutomationCommands(),
            new MugenCommands(),
            new CoachingCommands(),
            new SocialCommands(),
            new VoiceCommands(),
        };
    }

    private IEnumerable<string> ParseArguments(string command)
    {
        // Simple argument parser that respects quotes
        var args = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < command.Length; i++)
        {
            char c = command[i];
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    args.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            args.Add(current.ToString());
        }

        return args;
    }

    public IEnumerable<string> GetHistory() => _history;

    public void ClearHistory() => _history.Clear();

    public IEnumerable<string> GetCompletions(string text)
    {
        // For completions, we need a rootCommand to inspect
        var rootCommand = new RootCommand();
        foreach (var group in GetCommandGroups())
        {
            // We can pass a dummy console here
            group.RegisterCommands(rootCommand, _mediator, _host, AnsiConsole.Console);
        }

        if (string.IsNullOrEmpty(text))
            return rootCommand.Children.OfType<Command>().Select(c => c.Name);

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Basic completion for top-level commands
        if (parts.Length <= 1)
        {
            var match = parts.Length == 0 ? "" : parts[0];
            return rootCommand.Children
                .OfType<Command>()
                .Select(c => c.Name)
                .Where(name => name.StartsWith(match, StringComparison.OrdinalIgnoreCase));
        }

        // Deeper completion would require more complex logic using rootCommand.Parse(text)
        return Enumerable.Empty<string>();
    }
}
