using System.CommandLine;
using MediatR;
using SaveState.Application.GameLibrary.Queries;
using SaveState.Application.Analytics.Queries;
using Spectre.Console;
using SaveState.CLI.Extensions;

namespace SaveState.CLI.Commands;

/// <summary>
/// Commands for managing games in the library.
/// </summary>
public class GameCommands : CommandGroupBase
{
    /// <summary>
    /// Builds and registers the game-related commands.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    protected override void BuildCommands(RootCommand rootCommand)
    {
        // List command
        var listCommand = new Command("list", "List all games");
        var platformOption = new Option<string?>("--platform", "Filter by platform");
        listCommand.AddOption(platformOption);
        listCommand.SetHandler(async (platform) =>
        {
            var query = new GetGameSummariesQuery
            {
                PlatformFilter = platform,
                PageSize = 100 // Show more games in CLI
            };

            var result = await Mediator.Send(query).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Title");
            table.AddColumn("Platform");
            table.AddColumn("Playtime");
            table.AddColumn("Last Played");

            foreach (var game in result.Value!.Items)
            {
                var lastPlayed = game.LastPlayed?.ToString("yyyy-MM-dd") ?? "Never";
                table.AddRow(game.Title, game.Platform, game.TotalPlayTime.ToString(@"hh\:mm"), lastPlayed);
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[dim]Showing {result.Value.Items.Count} games[/]");
        }, platformOption);

        // Search command
        var searchCommand = new Command("search", "Search games by title");
        var searchTermArgument = new Argument<string>("term", "Search term");
        searchCommand.AddArgument(searchTermArgument);
        searchCommand.SetHandler(async (term) =>
        {
            var query = new GetGameSummariesQuery
            {
                SearchTerm = term,
                PageSize = 50
            };

            var result = await Mediator.Send(query).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            if (!result.Value!.Items.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]No games found matching '{term}'[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Title");
            table.AddColumn("Platform");
            table.AddColumn("Status");

            foreach (var game in result.Value.Items)
            {
                table.AddRow(game.Title, game.Platform, game.Status.ToString());
            }

            AnsiConsole.Write(table);
        }, searchTermArgument);

        // Stats command
        var statsCommand = new Command("stats", "Show library statistics");
        statsCommand.SetHandler(async () =>
        {
            var result = await Mediator.Send(new GetLibraryStatisticsQuery()).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            var stats = result.Value!;
            var gamesByStatus = string.Join(", ", stats.GamesByStatus.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
            var gamesByPlatform = string.Join(", ", stats.GamesByPlatform.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

            var panel = new Panel($"[bold]Total Games:[/] {stats.TotalGames}\n" +
                                 $"[bold]Installed Games:[/] {stats.InstalledGames}\n" +
                                 $"[bold]Running Games:[/] {stats.RunningGames}\n" +
                                 $"[bold]Total Playtime:[/] {stats.TotalPlayTime:hh\\:mm}\n" +
                                 $"[bold]Games by Status:[/] {gamesByStatus}\n" +
                                 $"[bold]Games by Platform:[/] {gamesByPlatform}")
            {
                Header = new PanelHeader("Library Statistics")
            };

            AnsiConsole.Write(panel);
        });

        // Heatmap command
        var heatmapCommand = new Command("heatmap", "Show gaming activity heatmap");
        var yearOption = new Option<int>("--year", () => DateTime.Now.Year, "Year to display (default: current year)");
        heatmapCommand.AddOption(yearOption);
        heatmapCommand.SetHandler(async (int year) =>
        {
            var result = await Mediator.Send(new GetGamingHeatmapQuery(year)).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            var heatmap = result.Value!;

            // Display summary statistics
            var summaryPanel = new Panel($"[bold]Year:[/] {year}\n" +
                                        $"[bold]Total Playtime:[/] {heatmap.TotalPlaytime:hh\\:mm}\n" +
                                        $"[bold]Active Days:[/] {heatmap.ActiveDays}/{heatmap.TotalDays}\n" +
                                        $"[bold]Current Streak:[/] {heatmap.CurrentStreak} days\n" +
                                        $"[bold]Longest Streak:[/] {heatmap.LongestStreak} days")
            {
                Header = new PanelHeader("Gaming Activity Summary")
            };

            AnsiConsole.Write(summaryPanel);
            AnsiConsole.WriteLine();

            // Simple text-based heatmap representation
            // Group activities by month for display
            var monthlyActivities = heatmap.Activities
                .GroupBy(a => new { a.Key.Year, a.Key.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month);

            foreach (var monthGroup in monthlyActivities)
            {
                var monthName = new DateTime(monthGroup.Key.Year, monthGroup.Key.Month, 1).ToString("MMMM yyyy");
                AnsiConsole.MarkupLine($"[bold]{monthName}[/]");
                AnsiConsole.WriteLine();

                var daysInMonth = DateTime.DaysInMonth(monthGroup.Key.Year, monthGroup.Key.Month);
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var date = new DateOnly(monthGroup.Key.Year, monthGroup.Key.Month, day);
                    var activity = monthGroup.FirstOrDefault(a => a.Key == date);

                    var level = activity.Key != default ? activity.Value.Level : SaveState.Core.Analytics.DTOs.ActivityLevel.None;

                    var symbol = level switch
                    {
                        SaveState.Core.Analytics.DTOs.ActivityLevel.None => "[dim]□[/]",
                        SaveState.Core.Analytics.DTOs.ActivityLevel.Low => "[green]■[/]",
                        SaveState.Core.Analytics.DTOs.ActivityLevel.Medium => "[yellow]■[/]",
                        SaveState.Core.Analytics.DTOs.ActivityLevel.High => "[red]■[/]",
                        SaveState.Core.Analytics.DTOs.ActivityLevel.VeryHigh => "[bold red]■[/]",
                        _ => "[dim]□[/]"
                    };

                    AnsiConsole.Markup(symbol);

                    if (day % 7 == 0)
                    {
                        AnsiConsole.WriteLine();
                    }
                }
                AnsiConsole.WriteLine();
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]□ No activity  ■ Low  ■ Medium  ■ High  ■ Very High[/]");
            AnsiConsole.WriteLine();
        }, yearOption);

        // Scan command
        var scanCommand = new Command("scan", "Scan all game libraries for new content");
        scanCommand.SetHandler(async () =>
        {
            await AnsiConsole.Status()
                .StartAsync("Scanning libraries...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    var result = await Mediator.Send(new SaveState.Application.GameLibrary.Commands.ScanLibraryCommand()).ConfigureAwait(false);

                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[green]Library scan completed successfully![/]");
                    }
                });
        });

        // Setup Emulators command
        var setupEmulatorsCommand = new Command("setup-emulators", "Register installed emulators and cores");
        setupEmulatorsCommand.SetHandler(async () =>
        {
            await AnsiConsole.Status()
                .StartAsync("Registering emulators...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    var result = await Mediator.Send(new SaveState.Application.RomManagement.Commands.RegisterEmulatorsCommand()).ConfigureAwait(false);

                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[green]Emulators registered successfully![/]");
                    }
                });
        });

        // Register commands
        rootCommand.AddCommandChecked(setupEmulatorsCommand);
        rootCommand.AddCommandChecked(scanCommand);
        rootCommand.AddCommandChecked(listCommand);
        rootCommand.AddCommandChecked(searchCommand);
        rootCommand.AddCommandChecked(statsCommand);
        rootCommand.AddCommandChecked(heatmapCommand);
    }
}
