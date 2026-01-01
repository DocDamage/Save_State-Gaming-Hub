using System.CommandLine;
using MediatR;
using SaveState.Application.SaveStates.Commands;
using SaveState.Application.SaveStates.Queries;
using SaveState.Core.SaveStates.Entities;
using Spectre.Console;
using SaveState.CLI.Extensions;

namespace SaveState.CLI.Commands;

/// <summary>
/// Commands for managing game save states.
/// </summary>
public class SaveStateCommands : CommandGroupBase
{
    /// <summary>
    /// Builds and registers the save state-related commands.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    protected override void BuildCommands(RootCommand rootCommand)
    {
        // Save States command group
        var saveStatesCommand = new Command("savestates", "Manage game save states");

        // Save States list subcommand
        var saveStatesListCommand = new Command("list", "List save states for a game");
        var saveStatesGameIdArgument = new Argument<string>("gameId", "Game ID (GUID)");
        saveStatesListCommand.AddArgument(saveStatesGameIdArgument);
        saveStatesListCommand.SetHandler(async (gameIdStr) =>
        {
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid game ID format: {gameIdStr}[/]");
                return;
            }

            var result = await Mediator.Send(new GetSaveStatesQuery(gameId)).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            var saveStates = result.Value!;
            if (!saveStates.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No save states found for this game.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Created");
            table.AddColumn("Description");
            table.AddColumn("Playtime");
            table.AddColumn("Size (MB)");
            table.AddColumn("Type");
            table.AddColumn("Favorite");

            foreach (var saveState in saveStates.OrderByDescending(ss => ss.CreatedAt))
            {
                table.AddRow(
                    saveState.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    saveState.Description ?? "No description",
                    saveState.PlaytimeAtSave.ToString(@"hh\:mm"),
                    $"{saveState.FileSizeBytes / 1024.0 / 1024.0:F1}",
                    saveState.IsAutoSave ? "Auto" : "Manual",
                    saveState.IsFavorite ? "★" : "");
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[dim]Showing {saveStates.Count} save states[/]");
        }, saveStatesGameIdArgument);

        // Save States create subcommand
        var saveStatesCreateCommand = new Command("create", "Create a new save state");
        var createSsGameIdArgument = new Argument<string>("gameId", "Game ID (GUID)");
        var createSsDescriptionOption = new Option<string?>("--description", "Save state description");
        var createSsScreenshotOption = new Option<bool>("--screenshot", () => true, "Capture screenshot");
        saveStatesCreateCommand.AddArgument(createSsGameIdArgument);
        saveStatesCreateCommand.AddOption(createSsDescriptionOption);
        saveStatesCreateCommand.AddOption(createSsScreenshotOption);
        saveStatesCreateCommand.SetHandler(async (string gameIdStr, string? description, bool screenshot) =>
        {
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid game ID format: {gameIdStr}[/]");
                return;
            }

            var command = new CreateSaveStateCommand(gameId, description, screenshot);
            var result = await Mediator.Send(command).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Save state created successfully![/]");
        }, createSsGameIdArgument, createSsDescriptionOption, createSsScreenshotOption);

        // Save States restore subcommand
        var saveStatesRestoreCommand = new Command("restore", "Restore a save state");
        var restoreSsIdArgument = new Argument<string>("saveStateId", "Save State ID (GUID)");
        saveStatesRestoreCommand.AddArgument(restoreSsIdArgument);
        saveStatesRestoreCommand.SetHandler(async (saveStateIdStr) =>
        {
            if (!Guid.TryParse(saveStateIdStr, out var saveStateId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid save state ID format: {saveStateIdStr}[/]");
                return;
            }

            var command = new RestoreSaveStateCommand(saveStateId);
            var result = await Mediator.Send(command).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Save state restored successfully![/]");
        }, restoreSsIdArgument);

        // Save States delete subcommand
        var saveStatesDeleteCommand = new Command("delete", "Delete a save state");
        var deleteSsIdArgument = new Argument<string>("saveStateId", "Save State ID (GUID)");
        saveStatesDeleteCommand.AddArgument(deleteSsIdArgument);
        saveStatesDeleteCommand.SetHandler(async (saveStateIdStr) =>
        {
            if (!Guid.TryParse(saveStateIdStr, out var saveStateId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid save state ID format: {saveStateIdStr}[/]");
                return;
            }

            var command = new DeleteSaveStateCommand(saveStateId);
            var result = await Mediator.Send(command).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Save state deleted successfully![/]");
        }, deleteSsIdArgument);

        // Save States timeline subcommand
        var saveStatesTimelineCommand = new Command("timeline", "Show save state timeline for a game");
        var timelineGameIdArgument = new Argument<string>("gameId", "Game ID (GUID)");
        saveStatesTimelineCommand.AddArgument(timelineGameIdArgument);
        saveStatesTimelineCommand.SetHandler(async (gameIdStr) =>
        {
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid game ID format: {gameIdStr}[/]");
                return;
            }

            var result = await Mediator.Send(new GetSaveStateTimelineQuery(gameId)).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            var timeline = result.Value!;
            if (timeline.Nodes.Count == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No save states in timeline.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Date/Time");
            table.AddColumn("Description");
            table.AddColumn("Playtime");
            table.AddColumn("Type");

            foreach (var node in timeline.Nodes.OrderBy(n => n.CreatedAt))
            {
                table.AddRow(
                    node.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    node.Description ?? "No description",
                    "N/A", // Would need to calculate from session data
                    "Save State");
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[dim]Timeline shows {timeline.Nodes.Count} save states[/]");
        }, timelineGameIdArgument);

        // Planned: Save state branching functionality for alternate playthroughs
        // var saveStatesBranchCommand = new Command("branch", "Manage save state branches");

        // Save States autosave subcommand group
        var saveStatesAutosaveCommand = new Command("autosave", "Manage automatic save state creation");
        var autosaveConfigureCommand = new Command("configure", "Configure auto-save settings for a game");
        var autosaveStatusCommand = new Command("status", "Show auto-save status for a game");
        var autosaveEnableCommand = new Command("enable", "Enable auto-save for a game");
        var autosaveDisableCommand = new Command("disable", "Disable auto-save for a game");

        // Autosave configure subcommand
        var autosaveConfigureGameArgument = new Argument<string>("game-id", "Game ID (GUID)");
        var autosaveConfigureIntervalOption = new Option<TimeSpan?>("--interval", "Auto-save interval (e.g. 00:05:00 for 5 minutes)");
        var autosaveConfigureMaxSavesOption = new Option<int>("--max-saves", () => 10, "Maximum number of auto-saves to keep");
        var autosaveConfigureTriggersOption = new Option<string[]>("--triggers", "Enabled triggers (TimeInterval, SessionStart, SessionEnd, SignificantProgress)") { AllowMultipleArgumentsPerToken = true };
        autosaveConfigureCommand.AddArgument(autosaveConfigureGameArgument);
        autosaveConfigureCommand.AddOption(autosaveConfigureIntervalOption);
        autosaveConfigureCommand.AddOption(autosaveConfigureMaxSavesOption);
        autosaveConfigureCommand.AddOption(autosaveConfigureTriggersOption);
        autosaveConfigureCommand.SetHandler(async (string gameIdStr, TimeSpan? interval, int maxSaves, string[] triggers) =>
        {
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid game ID format: {gameIdStr}[/]");
                return;
            }

            // Parse triggers
            var enabledTriggers = SaveState.Core.SaveStates.Services.SaveTrigger.None;
            foreach (var triggerStr in triggers)
            {
                if (Enum.TryParse<SaveState.Core.SaveStates.Services.SaveTrigger>(triggerStr, out var trigger))
                {
                    enabledTriggers |= trigger;
                }
            }

            // Note: Auto-save configuration would need to be implemented in the application layer
            AnsiConsole.MarkupLine($"[green]Auto-save configured for game {gameId} (placeholder)[/]");
            AnsiConsole.MarkupLine($"[dim]Interval: {interval?.ToString() ?? "default"}[/]");
            AnsiConsole.MarkupLine($"[dim]Max saves: {maxSaves}[/]");
            AnsiConsole.MarkupLine($"[dim]Triggers: {enabledTriggers}[/]");
        }, autosaveConfigureGameArgument, autosaveConfigureIntervalOption, autosaveConfigureMaxSavesOption, autosaveConfigureTriggersOption);

        // Autosave status subcommand
        var autosaveStatusGameArgument = new Argument<string>("game-id", "Game ID (GUID)");
        autosaveStatusCommand.AddArgument(autosaveStatusGameArgument);
        autosaveStatusCommand.SetHandler(async (gameIdStr) =>
        {
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid game ID format: {gameIdStr}[/]");
                return;
            }

            // Note: Auto-save status would need to be implemented in the application layer
            AnsiConsole.MarkupLine($"[yellow]Auto-save status for game {gameId}: Feature not yet implemented[/]");
        }, autosaveStatusGameArgument);

        // Autosave enable subcommand
        var autosaveEnableGameArgument = new Argument<string>("game-id", "Game ID (GUID)");
        autosaveEnableCommand.AddArgument(autosaveEnableGameArgument);
        autosaveEnableCommand.SetHandler(async (gameIdStr) =>
        {
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid game ID format: {gameIdStr}[/]");
                return;
            }

            // Note: Auto-save enable would need to be implemented in the application layer
            AnsiConsole.MarkupLine($"[green]Auto-save enabled for game {gameId} (placeholder)[/]");
        }, autosaveEnableGameArgument);

        // Autosave disable subcommand
        var autosaveDisableGameArgument = new Argument<string>("game-id", "Game ID (GUID)");
        autosaveDisableCommand.AddArgument(autosaveDisableGameArgument);
        autosaveDisableCommand.SetHandler(async (gameIdStr) =>
        {
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid game ID format: {gameIdStr}[/]");
                return;
            }

            // Note: Auto-save disable would need to be implemented in the application layer
            AnsiConsole.MarkupLine($"[green]Auto-save disabled for game {gameId} (placeholder)[/]");
        }, autosaveDisableGameArgument);

        // Add subcommands to autosave command
        saveStatesAutosaveCommand.AddCommand(autosaveConfigureCommand);
        saveStatesAutosaveCommand.AddCommand(autosaveStatusCommand);
        saveStatesAutosaveCommand.AddCommand(autosaveEnableCommand);
        saveStatesAutosaveCommand.AddCommand(autosaveDisableCommand);

        // Add subcommands to save states command
        saveStatesCommand.AddCommand(saveStatesListCommand);
        saveStatesCommand.AddCommand(saveStatesCreateCommand);
        saveStatesCommand.AddCommand(saveStatesRestoreCommand);
        saveStatesCommand.AddCommand(saveStatesDeleteCommand);
        saveStatesCommand.AddCommand(saveStatesTimelineCommand);
        saveStatesCommand.AddCommand(saveStatesAutosaveCommand);

        // Register the main command
        rootCommand.AddCommandChecked(saveStatesCommand);
    }
}
