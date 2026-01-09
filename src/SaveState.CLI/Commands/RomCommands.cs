using System.CommandLine;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Application.RomManagement.Services;
using Spectre.Console;
using SaveState.CLI.Extensions;
using SaveState.Core.Common;

namespace SaveState.CLI.Commands;

/// <summary>
/// Commands for managing ROMs, emulators, and system scanning.
/// </summary>
public class RomCommands : CommandGroupBase
{
    /// <summary>
    /// Builds and registers the ROM-related commands.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    protected override void BuildCommands(RootCommand rootCommand)
    {
        // ROM command group
        var romCommand = new Command("rom", "ROM and emulator management");
        romCommand.AddAlias("roms");

        // Configure ROM paths
        var configCommand = new Command("config", "Configure ROM scanning settings");
        romCommand.AddCommand(configCommand);

        // Show current configuration
        var showConfigCommand = new Command("show-config", "Show current ROM scanning configuration");
        showConfigCommand.SetHandler(async () =>
        {
            var romPathManager = Host.Services.GetService<SaveState.Application.RomManagement.Services.IRomPathManager>();
            if (romPathManager == null)
            {
                AnsiConsole.MarkupLine("[red]ROM path manager service not available.[/]");
                return;
            }

            var config = romPathManager.GetConfiguration();
            var romDirs = await romPathManager.GetRomDirectoriesAsync();

            var table = new Table();
            table.AddColumn("Setting");
            table.AddColumn("Value");

            table.AddRow("Auto Scan on Startup", config.AutoScanOnStartup.ToString());
            table.AddRow("Recursive Scanning", config.ScanRecursively.ToString());
            table.AddRow("BIOS Directory", config.BiosDirectory ?? "Not set");

            var romDirsStr = string.Join("\n", romDirs.Select(d => $"• {d}"));
            table.AddRow("ROM Directories", string.IsNullOrEmpty(romDirsStr) ? "None configured" : romDirsStr);

            table.AddRow("Platform Extensions", $"{config.PlatformExtensions.Count} platforms configured");

            AnsiConsole.Write(table);

            // Show platform extensions in a separate table
            if (config.PlatformExtensions.Any())
            {
                AnsiConsole.WriteLine();
                var extTable = new Table();
                extTable.AddColumn("Platform");
                extTable.AddColumn("Extensions");

                foreach (var kvp in config.PlatformExtensions.OrderBy(x => x.Key))
                {
                    extTable.AddRow(kvp.Key, string.Join(", ", kvp.Value));
                }

                AnsiConsole.Write(extTable);
            }
        });
        configCommand.AddCommand(showConfigCommand);

        // Add ROM directory
        var addDirCommand = new Command("add-dir", "Add a ROM directory");
        var dirArgument = new Argument<string>("directory", "Path to the ROM directory");
        var validateOption = new Option<bool>("--validate", () => true, "Validate that the directory exists");
        addDirCommand.AddArgument(dirArgument);
        addDirCommand.AddOption(validateOption);
        addDirCommand.SetHandler(async (string directory, bool validate) =>
        {
            var romPathManager = Host.Services.GetService<SaveState.Application.RomManagement.Services.IRomPathManager>();
            if (romPathManager == null)
            {
                AnsiConsole.MarkupLine("[red]ROM path manager service not available.[/]");
                return;
            }

            var result = await romPathManager.AddRomDirectoryAsync(directory, validate);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Added ROM directory: {directory}[/]");
        }, dirArgument, validateOption);
        configCommand.AddCommand(addDirCommand);

        // Remove ROM directory
        var removeDirCommand = new Command("remove-dir", "Remove a ROM directory");
        var removeDirArgument = new Argument<string>("directory", "Path to the ROM directory to remove");
        removeDirCommand.AddArgument(removeDirArgument);
        removeDirCommand.SetHandler(async (string directory) =>
        {
            var romPathManager = Host.Services.GetService<SaveState.Application.RomManagement.Services.IRomPathManager>();
            if (romPathManager == null)
            {
                AnsiConsole.MarkupLine("[red]ROM path manager service not available.[/]");
                return;
            }

            var result = await romPathManager.RemoveRomDirectoryAsync(directory);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Removed ROM directory: {directory}[/]");
        }, removeDirArgument);
        configCommand.AddCommand(removeDirCommand);

        // Scan ROMs
        var scanCommand = new Command("scan", "Scan for ROMs in configured directories");
        var platformOption = new Option<string?>("--platform", "Filter by platform name");
        var recursiveOption = new Option<bool>("--recursive", () => true, "Scan subdirectories recursively");
        scanCommand.AddOption(platformOption);
        scanCommand.AddOption(recursiveOption);
        scanCommand.SetHandler(async (string? platform, bool recursive) =>
        {
            await AnsiConsole.Status()
                .StartAsync("Scanning for ROMs...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    // For now, we'll scan all platforms. In the future, this could be enhanced
                    // to use the platform filter and the new scanning services.
                    var result = await Mediator.Send(new SaveState.Application.GameLibrary.Commands.ScanLibraryCommand()).ConfigureAwait(false);

                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[green]ROM scan completed successfully![/]");
                    }
                });
        }, platformOption, recursiveOption);
        romCommand.AddCommand(scanCommand);

        // Scan system for emulators
        var scanEmulatorsCommand = new Command("scan-emulators", "Scan system for installed emulators");
        scanEmulatorsCommand.SetHandler(async () =>
        {
            var scanner = Host.Services.GetService<SaveState.Application.RomManagement.Services.ISystemEmulatorScanner>();
            if (scanner == null)
            {
                AnsiConsole.MarkupLine("[red]System emulator scanner service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Scanning system for emulators...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    var progress = new Progress<ScanProgress>(p =>
                    {
                        ctx.Status($"Scanning... {p.FilesScanned}/{p.FilesTotal} paths, {p.RomsFound} emulators found");
                    });

                    var result = await scanner.ScanSystemAsync(
                        new SaveState.Core.Configuration.EmulatorScanningOptions(),
                        progress).ConfigureAwait(false);

                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                        return;
                    }

                    var emulators = result.Value;
                    if (emulators is null || !emulators.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No emulators found.[/]");
                        return;
                    }

                    AnsiConsole.MarkupLine($"[green]Found {emulators.Count} emulators:[/]");

                    var table = new Table();
                    table.AddColumn("Name");
                    table.AddColumn("Type");
                    table.AddColumn("Version");
                    table.AddColumn("Path");

                    foreach (var emu in emulators.OrderBy(e => e.Name))
                    {
                        table.AddRow(
                            emu.Name,
                            emu.Type.ToString(),
                            emu.Version ?? "Unknown",
                            emu.ExecutablePath.Length > 60 ?
                                $"...{emu.ExecutablePath[^57..]}" :
                                emu.ExecutablePath);
                    }

                    AnsiConsole.Write(table);
                });
        });
        romCommand.AddCommand(scanEmulatorsCommand);

        // Scan system for MUGEN installations
        var scanMugenCommand = new Command("scan-mugen", "Scan system for MUGEN installations");
        scanMugenCommand.SetHandler(async () =>
        {
            var scanner = Host.Services.GetService<SaveState.Application.RomManagement.Services.ISystemMugenScanner>();
            if (scanner == null)
            {
                AnsiConsole.MarkupLine("[red]System MUGEN scanner service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Scanning system for MUGEN installations...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    var progress = new Progress<ScanProgress>(p =>
                    {
                        ctx.Status($"Scanning... {p.FilesScanned}/{p.FilesTotal} paths, {p.RomsFound} installations found");
                    });

                    var result = await scanner.ScanSystemAsync(
                        new SaveState.Core.Configuration.MugenScanningOptions(),
                        progress).ConfigureAwait(false);

                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                        return;
                    }

                    var installations = result.Value;
                    if (installations is null || !installations.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No MUGEN installations found.[/]");
                        return;
                    }

                    AnsiConsole.MarkupLine($"[green]Found {installations.Count} MUGEN installations:[/]");

                    var table = new Table();
                    table.AddColumn("Name");
                    table.AddColumn("Engine");
                    table.AddColumn("Version");
                    table.AddColumn("Characters");
                    table.AddColumn("Stages");
                    table.AddColumn("Path");

                    foreach (var install in installations.OrderBy(i => i.Name))
                    {
                        var sizeStr = install.TotalSizeBytes > 1024 * 1024 * 1024 ?
                            $"{install.TotalSizeBytes / (1024.0 * 1024 * 1024):F1}GB" :
                            $"{install.TotalSizeBytes / (1024.0 * 1024):F1}MB";

                        table.AddRow(
                            install.Name,
                            install.EngineType.ToString(),
                            install.Version ?? "Unknown",
                            install.CharacterCount.ToString(),
                            install.StageCount.ToString(),
                            install.InstallPath.Length > 50 ?
                                $"...{install.InstallPath[^47..]}" :
                                install.InstallPath);
                    }

                    AnsiConsole.Write(table);
                });
        });
        romCommand.AddCommand(scanMugenCommand);

        // Register the main command
        rootCommand.AddCommandChecked(romCommand);
    }
}
