using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.GameLibrary.Services;
using Spectre.Console;
using SaveState.CLI.Extensions;

namespace SaveState.CLI.Commands;

/// <summary>
/// Commands for game memory reading and state detection.
/// </summary>
public class MemoryCommands : CommandGroupBase
{
    /// <summary>
    /// Builds and registers the memory-related commands.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    protected override void BuildCommands(RootCommand rootCommand)
    {
        // Memory command group
        var memoryCommand = new Command("memory", "Game memory reading and state detection");
        memoryCommand.AddAlias("mem");

        // Attach to process
        var attachCommand = new Command("attach", "Attach to a game process for memory reading");
        var processIdArg = new Argument<int>("processId", "Process ID to attach to");
        attachCommand.AddArgument(processIdArg);
        attachCommand.SetHandler(async (int processId) =>
        {
            var memoryReader = Host.Services.GetService<IGameMemoryReader>();
            if (memoryReader == null)
            {
                AnsiConsole.MarkupLine("[red]Game memory reader not available.[/]");
                return;
            }

            if (memoryReader.IsAttached)
            {
                AnsiConsole.MarkupLine("[yellow]Already attached to a process. Detach first.[/]");
                return;
            }

            var result = await memoryReader.AttachToProcessAsync(processId).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Successfully attached to process {processId}[/]");
            AnsiConsole.MarkupLine("[dim]Memory reading is now active.[/]");
        }, processIdArg);

        // Detach
        var detachCommand = new Command("detach", "Detach from the current game process");
        detachCommand.SetHandler(async () =>
        {
            var memoryReader = Host.Services.GetService<IGameMemoryReader>();
            if (memoryReader == null)
            {
                AnsiConsole.MarkupLine("[red]Game memory reader not available.[/]");
                return;
            }

            if (!memoryReader.IsAttached)
            {
                AnsiConsole.MarkupLine("[yellow]Not attached to any process.[/]");
                return;
            }

            var result = await memoryReader.DetachAsync().ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine("[green]Detached from process.[/]");
        });

        // Status
        var statusCommand = new Command("status", "Show memory reader status");
        statusCommand.SetHandler(() =>
        {
            var memoryReader = Host.Services.GetService<IGameMemoryReader>();
            if (memoryReader == null)
            {
                AnsiConsole.MarkupLine("[red]Game memory reader not available.[/]");
                return;
            }

            var statusColor = memoryReader.IsAttached ? "green" : "dim";
            var statusText = memoryReader.IsAttached ? "Attached" : "Not attached";

            var panel = new Panel(new Markup(
                $"[bold]Status:[/] [{statusColor}]{statusText}[/]"))
            {
                Header = new PanelHeader("[blue]Memory Reader Status[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);

            return;
        });

        // Detect patterns
        var patternsCommand = new Command("patterns", "Detect known memory patterns in the attached game");
        patternsCommand.SetHandler(async () =>
        {
            var memoryReader = Host.Services.GetService<IGameMemoryReader>();
            if (memoryReader == null)
            {
                AnsiConsole.MarkupLine("[red]Game memory reader not available.[/]");
                return;
            }

            if (!memoryReader.IsAttached)
            {
                AnsiConsole.MarkupLine("[yellow]Not attached to any process. Use 'memory attach <pid>' first.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Scanning for memory patterns...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    var result = await memoryReader.DetectPatternsAsync().ConfigureAwait(false);
                    if (!result.IsSuccess || result.Value is null)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error ?? "Unknown error"}[/]");
                        return;
                    }

                    var patterns = result.Value;
                    if (!patterns.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No known memory patterns detected.[/]");
                        return;
                    }

                    AnsiConsole.MarkupLine($"[green]Found {patterns.Count} memory patterns:[/]");
                    AnsiConsole.WriteLine();

                    var table = new Table();
                    table.AddColumn("Name");
                    table.AddColumn("Address");
                    table.AddColumn("Type");
                    table.AddColumn("Value");

                    foreach (var pattern in patterns)
                    {
                        table.AddRow(
                            pattern.Name,
                            $"0x{pattern.Address:X}",
                            pattern.ValueType,
                            pattern.CurrentValue?.ToString() ?? "-");
                    }

                    AnsiConsole.Write(table);
                });
        });

        // List processes
        var listCommand = new Command("list", "List running game processes");
        listCommand.SetHandler(() =>
        {
            var processes = System.Diagnostics.Process.GetProcesses()
                .Where(p =>
                {
                    try
                    {
                        // Filter to likely game processes (has a main window)
                        return !string.IsNullOrEmpty(p.MainWindowTitle) && p.MainModule != null;
                    }
                    catch
                    {
                        return false;
                    }
                })
                .Take(30)
                .OrderBy(p => p.ProcessName)
                .ToList();

            if (!processes.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No game-like processes found.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("PID");
            table.AddColumn("Name");
            table.AddColumn("Title");
            table.AddColumn("Memory");

            foreach (var proc in processes)
            {
                try
                {
                    var memoryMb = proc.WorkingSet64 / (1024.0 * 1024.0);
                    table.AddRow(
                        proc.Id.ToString(),
                        proc.ProcessName,
                        proc.MainWindowTitle.Length > 40
                            ? proc.MainWindowTitle[..37] + "..."
                            : proc.MainWindowTitle,
                        $"{memoryMb:F0} MB");
                }
                catch
                {
                    // Skip processes we can't access
                }
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine("[dim]Use 'memory attach <PID>' to attach to a process.[/]");
        });

        // Add all subcommands
        memoryCommand.AddCommand(attachCommand);
        memoryCommand.AddCommand(detachCommand);
        memoryCommand.AddCommand(statusCommand);
        memoryCommand.AddCommand(patternsCommand);
        memoryCommand.AddCommand(listCommand);

        // Register the main command
        rootCommand.AddCommandChecked(memoryCommand);
    }
}
