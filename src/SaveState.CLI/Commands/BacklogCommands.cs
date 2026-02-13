using System.CommandLine;
using MediatR;
using SaveState.Application.Analytics.Commands;
using SaveState.Application.Analytics.Queries;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Core.GameLibrary.Entities;
using Spectre.Console;
using SaveState.CLI.Extensions;

namespace SaveState.CLI.Commands;

/// <summary>
/// Commands for managing gaming backlog and goals.
/// </summary>
public class BacklogCommands : CommandGroupBase
{
    /// <summary>
    /// Builds and registers the backlog and goals-related commands.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    protected override void BuildCommands(RootCommand rootCommand)
    {
        // Backlog command group
        var backlogCommand = new Command("backlog", "Manage gaming backlog");

        // Backlog list subcommand
        var backlogListCommand = new Command("list", "List backlog entries");
        var backlogStatusOption = new Option<string?>("--status", "Filter by status (NotStarted, InProgress, OnHold, Completed, Abandoned, Wishlisted)");
        backlogListCommand.AddOption(backlogStatusOption);
        backlogListCommand.SetHandler(async (string? status) =>
        {
            BacklogStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<BacklogStatus>(status, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }

            var result = await Mediator.Send(new GetBacklogQuery(Status: statusFilter)).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error ?? "Unknown error"}[/]");
                return;
            }

            var backlog = result.Value;
            if (!backlog.Items.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No backlog entries found.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Title");
            table.AddColumn("Platform");
            table.AddColumn("Status");
            table.AddColumn("Priority");
            table.AddColumn("Added");

            foreach (var entry in backlog.Items)
            {
                table.AddRow(
                    entry.Game.Title,
                    entry.Game.Platform?.Name ?? "Unknown",
                    entry.Status.ToString(),
                    entry.Priority.ToString(),
                    entry.AddedAt.ToString("yyyy-MM-dd"));
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[dim]Showing {backlog.Items.Count} backlog entries[/]");
        }, backlogStatusOption);

        // Backlog add subcommand
        var backlogAddCommand = new Command("add", "Add game to backlog");
        var gameIdArgument = new Argument<string>("gameId", "Game ID (GUID)");
        var priorityOption = new Option<int>("--priority", () => 50, "Priority (1-100, higher = more important)");
        backlogAddCommand.AddArgument(gameIdArgument);
        backlogAddCommand.AddOption(priorityOption);
        backlogAddCommand.SetHandler(async (string gameIdStr, int priority) =>
        {
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid game ID format: {gameIdStr}[/]");
                return;
            }

            var result = await Mediator.Send(new AddToBacklogCommand(gameId, priority)).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Game added to backlog successfully![/]");
        }, gameIdArgument, priorityOption);

        // Backlog status subcommand
        var backlogStatusCommand = new Command("status", "Update backlog entry status");
        var statusGameIdArgument = new Argument<string>("gameId", "Game ID (GUID)");
        var statusArgument = new Argument<string>("status", "New status (NotStarted, InProgress, OnHold, Completed, Abandoned, Wishlisted)");
        backlogStatusCommand.AddArgument(statusGameIdArgument);
        backlogStatusCommand.AddArgument(statusArgument);
        backlogStatusCommand.SetHandler(async (gameIdStr, statusStr) =>
        {
            if (!Guid.TryParse(gameIdStr, out var gameId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid game ID format: {gameIdStr}[/]");
                return;
            }

            if (!Enum.TryParse<BacklogStatus>(statusStr, out var status))
            {
                AnsiConsole.MarkupLine($"[red]Invalid status: {statusStr}[/]");
                return;
            }

            var result = await Mediator.Send(new UpdateBacklogStatusCommand(gameId, status)).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Backlog status updated successfully![/]");
        }, statusGameIdArgument, statusArgument);

        // Add subcommands to backlog command
        backlogCommand.AddCommand(backlogListCommand);
        backlogCommand.AddCommand(backlogAddCommand);
        backlogCommand.AddCommand(backlogStatusCommand);

        // Goals command group
        var goalsCommand = new Command("goals", "Manage gaming goals");

        // Goals list subcommand
        var goalsListCommand = new Command("list", "List active goals");
        goalsListCommand.SetHandler(async () =>
        {
            var result = await Mediator.Send(new GetActiveGoalsQuery()).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error ?? "Unknown error"}[/]");
                return;
            }

            var goals = result.Value;
            if (!goals.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No active goals found.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Title");
            table.AddColumn("Type");
            table.AddColumn("Target");
            table.AddColumn("Current");
            table.AddColumn("Progress");
            table.AddColumn("Status");

            foreach (var goal in goals)
            {
                table.AddRow(
                    goal.Title,
                    goal.Type.ToString(),
                    goal.TargetValue.ToString(),
                    goal.CurrentValue.ToString(),
                    $"{goal.ProgressPercent:F1}%",
                    goal.Status.ToString());
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[dim]Showing {goals.Count} active goals[/]");
        });

        // Goals create subcommand
        var goalsCreateCommand = new Command("create", "Create a new goal");
        var titleArgument = new Argument<string>("title", "Goal title");
        var typeArgument = new Argument<string>("type", "Goal type (GamesCompleted, PlaytimeHours, PlaytimePerGame, AchievementsEarned, DailyStreak, GenreExploration, SessionsCount)");
        var targetArgument = new Argument<int>("target", "Target value");
        var endDateOption = new Option<string?>("--end-date", "End date (yyyy-MM-dd)");
        var gameIdOption = new Option<string?>("--game-id", "Specific game ID for game-specific goals");

        goalsCreateCommand.AddArgument(titleArgument);
        goalsCreateCommand.AddArgument(typeArgument);
        goalsCreateCommand.AddArgument(targetArgument);
        goalsCreateCommand.AddOption(endDateOption);
        goalsCreateCommand.AddOption(gameIdOption);

        goalsCreateCommand.SetHandler(async (string title, string typeStr, int target, string? endDateStr, string? gameIdStr) =>
        {
            if (!Enum.TryParse<SaveState.Core.Analytics.Entities.GoalType>(typeStr, out var type))
            {
                AnsiConsole.MarkupLine($"[red]Invalid goal type: {typeStr}[/]");
                return;
            }

            DateOnly? endDate = null;
            if (!string.IsNullOrEmpty(endDateStr) && DateOnly.TryParse(endDateStr, out var parsedDate))
            {
                endDate = parsedDate;
            }

            Guid? gameId = null;
            if (!string.IsNullOrEmpty(gameIdStr) && Guid.TryParse(gameIdStr, out var parsedGameId))
            {
                gameId = parsedGameId;
            }

            var command = new CreateGoalCommand(title, type, target, endDate, gameId);
            var result = await Mediator.Send(command).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Goal created successfully![/]");
        }, titleArgument, typeArgument, targetArgument, endDateOption, gameIdOption);

        // Goals update subcommand
        var goalsUpdateCommand = new Command("update", "Update goal progress");
        goalsUpdateCommand.SetHandler(async () =>
        {
            var command = new UpdateGoalProgressCommand();
            var result = await Mediator.Send(command).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Goal progress updated successfully![/]");
        });

        // Goals cancel subcommand
        var goalsCancelCommand = new Command("cancel", "Cancel a goal");
        var cancelGoalIdArgument = new Argument<string>("goalId", "Goal ID (GUID)");
        goalsCancelCommand.AddArgument(cancelGoalIdArgument);
        goalsCancelCommand.SetHandler(async (goalIdStr) =>
        {
            if (!Guid.TryParse(goalIdStr, out var goalId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid goal ID format: {goalIdStr}[/]");
                return;
            }

            var result = await Mediator.Send(new CancelGoalCommand(goalId)).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Goal cancelled successfully![/]");
        }, cancelGoalIdArgument);

        // Add subcommands to goals command
        goalsCommand.AddCommand(goalsListCommand);
        goalsCommand.AddCommand(goalsCreateCommand);
        goalsCommand.AddCommand(goalsUpdateCommand);
        goalsCommand.AddCommand(goalsCancelCommand);

        // Register the main commands
        rootCommand.AddCommandChecked(backlogCommand);
        rootCommand.AddCommandChecked(goalsCommand);
    }
}