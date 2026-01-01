using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using Spectre.Console;
using SaveState.CLI.Extensions;

namespace SaveState.CLI.Commands;

/// <summary>
/// Commands for MUGEN fighting game management.
/// </summary>
public class MugenCommands : CommandGroupBase
{
    /// <summary>
    /// Builds and registers the MUGEN-related commands.
    /// </summary>
    /// <param name="rootCommand">The root command to register commands with.</param>
    protected override void BuildCommands(RootCommand rootCommand)
    {
        // MUGEN command group
        var mugenCommand = new Command("mugen", "MUGEN fighting game management");

        // Characters subgroup
        var charsCommand = new Command("characters", "Manage MUGEN characters");
        charsCommand.AddAlias("chars");

        // List characters
        var listCharsCommand = new Command("list", "List all MUGEN characters");
        var limitOption = new Option<int>("--limit", () => 20, "Maximum number of characters to display");
        listCharsCommand.AddOption(limitOption);
        listCharsCommand.SetHandler(async (int limit) =>
        {
            var charRepo = Host.Services.GetService<IMugenCharacterRepository>();
            if (charRepo == null)
            {
                AnsiConsole.MarkupLine("[red]MUGEN character repository not available.[/]");
                return;
            }

            var characters = await charRepo.GetAllAsync().ConfigureAwait(false);
            if (!characters.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No MUGEN characters found.[/]");
                AnsiConsole.MarkupLine("[dim]Use 'mugen scan' to discover characters.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Name");
            table.AddColumn("Author");
            table.AddColumn("Version");
            table.AddColumn("Valid");

            foreach (var chr in characters.Take(limit))
            {
                table.AddRow(
                    chr.DisplayName ?? chr.Name,
                    chr.Author ?? "Unknown",
                    chr.Version ?? "-",
                    chr.IsValid ? "[green]Yes[/]" : "[red]No[/]");
            }

            AnsiConsole.Write(table);
            AnsiConsole.MarkupLine($"[dim]Showing {Math.Min(limit, characters.Count)} of {characters.Count} characters[/]");
        }, limitOption);

        // Character stats
        var statsCommand = new Command("stats", "Show character statistics");
        var charIdArg = new Argument<string>("characterId", "Character ID (GUID)");
        statsCommand.AddArgument(charIdArg);
        statsCommand.SetHandler(async (string charIdStr) =>
        {
            if (!Guid.TryParse(charIdStr, out var charId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid character ID: {charIdStr}[/]");
                return;
            }

            var statsService = Host.Services.GetService<IMugenStatsService>();
            if (statsService == null)
            {
                AnsiConsole.MarkupLine("[red]MUGEN stats service not available.[/]");
                return;
            }

            var result = await statsService.GetCharacterStatsAsync(charId).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            var stats = result.Value!;

            var panel = new Panel(new Markup(
                $"[bold]Total Matches:[/] {stats.TotalMatches}\n" +
                $"[bold]Wins:[/] [green]{stats.Wins}[/]\n" +
                $"[bold]Losses:[/] [red]{stats.Losses}[/]\n" +
                $"[bold]Win Rate:[/] {stats.WinRate:P1}\n" +
                $"[bold]Total Playtime:[/] {stats.TotalPlaytime.TotalHours:F1}h"))
            {
                Header = new PanelHeader("[blue]Character Statistics[/]"),
                Border = BoxBorder.Rounded
            };
            AnsiConsole.Write(panel);
        }, charIdArg);

        charsCommand.AddCommand(listCharsCommand);
        charsCommand.AddCommand(statsCommand);

        // Collections subgroup
        var collectionsCommand = new Command("collections", "Manage character collections");

        // List collections
        var listCollectionsCommand = new Command("list", "List character collections");
        listCollectionsCommand.SetHandler(async () =>
        {
            var collectionService = Host.Services.GetService<IMugenCollectionService>();
            if (collectionService == null)
            {
                AnsiConsole.MarkupLine("[red]MUGEN collection service not available.[/]");
                return;
            }

            var result = await collectionService.GetCollectionsAsync().ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            var collections = result.Value!;
            if (!collections.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No collections found.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("ID");
            table.AddColumn("Name");
            table.AddColumn("Characters");
            table.AddColumn("Created");

            foreach (var col in collections)
            {
                table.AddRow(
                    col.Id.ToString()[..8],
                    col.Name,
                    col.Characters.Count.ToString(),
                    col.CreatedAt.ToString("d"));
            }

            AnsiConsole.Write(table);
        });

        // Create collection
        var createCollectionCommand = new Command("create", "Create a new character collection");
        var nameArg = new Argument<string>("name", "Collection name");
        createCollectionCommand.AddArgument(nameArg);
        createCollectionCommand.SetHandler(async (string name) =>
        {
            var collectionService = Host.Services.GetService<IMugenCollectionService>();
            if (collectionService == null)
            {
                AnsiConsole.MarkupLine("[red]MUGEN collection service not available.[/]");
                return;
            }

            var result = await collectionService.CreateCollectionAsync(name).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Collection '{name}' created successfully![/]");
            AnsiConsole.MarkupLine($"[dim]ID: {result.Value!.Id}[/]");
        }, nameArg);

        collectionsCommand.AddCommand(listCollectionsCommand);
        collectionsCommand.AddCommand(createCollectionCommand);

        // Tournament subgroup
        var tournamentCommand = new Command("tournament", "Manage MUGEN tournaments");
        tournamentCommand.AddAlias("tourney");

        // Create tournament
        var createTourneyCommand = new Command("create", "Create a new tournament");
        var tourneyNameArg = new Argument<string>("name", "Tournament name");
        var formatOption = new Option<string>("--format", () => "SingleElimination", "Tournament format (SingleElimination, DoubleElimination, RoundRobin)");
        createTourneyCommand.AddArgument(tourneyNameArg);
        createTourneyCommand.AddOption(formatOption);
        createTourneyCommand.SetHandler(async (string name, string formatStr) =>
        {
            var tournamentService = Host.Services.GetService<IMugenTournamentService>();
            if (tournamentService == null)
            {
                AnsiConsole.MarkupLine("[red]MUGEN tournament service not available.[/]");
                return;
            }

            if (!Enum.TryParse<TournamentFormat>(formatStr, out var format))
            {
                AnsiConsole.MarkupLine($"[red]Invalid format: {formatStr}[/]");
                return;
            }

            var request = new CreateTournamentRequest(name, format, Array.Empty<Guid>());
            var result = await tournamentService.CreateTournamentAsync(request).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]Tournament '{name}' created successfully![/]");
            AnsiConsole.MarkupLine($"[dim]ID: {result.Value!.Id}[/]");
            AnsiConsole.MarkupLine("[dim]Use 'mugen tournament add-participant' to add fighters.[/]");
        }, tourneyNameArg, formatOption);

        // Tournament standings
        var standingsCommand = new Command("standings", "View tournament standings");
        var tourneyIdArg = new Argument<string>("tournamentId", "Tournament ID (GUID)");
        standingsCommand.AddArgument(tourneyIdArg);
        standingsCommand.SetHandler(async (string tourneyIdStr) =>
        {
            if (!Guid.TryParse(tourneyIdStr, out var tourneyId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid tournament ID: {tourneyIdStr}[/]");
                return;
            }

            var tournamentService = Host.Services.GetService<IMugenTournamentService>();
            if (tournamentService == null)
            {
                AnsiConsole.MarkupLine("[red]MUGEN tournament service not available.[/]");
                return;
            }

            var result = await tournamentService.GetStandingsAsync(tourneyId).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            var standings = result.Value!;
            if (!standings.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No standings available yet.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("#");
            table.AddColumn("Character");
            table.AddColumn("W-L");
            table.AddColumn("Points");
            table.AddColumn("Status");

            var rank = 1;
            foreach (var standing in standings.OrderByDescending(s => s.Points))
            {
                var statusColor = standing.IsEliminated ? "red" : "green";
                table.AddRow(
                    rank.ToString(),
                    standing.ParticipantName,
                    $"{standing.Wins}-{standing.Losses}",
                    standing.Points.ToString(),
                    $"[{statusColor}]{(standing.IsEliminated ? "Eliminated" : "Active")}[/]");
                rank++;
            }

            AnsiConsole.Write(table);
        }, tourneyIdArg);

        tournamentCommand.AddCommand(createTourneyCommand);
        tournamentCommand.AddCommand(standingsCommand);

        // Coach subgroup
        var coachCommand = new Command("coach", "AI coaching assistance");

        // Matchup advice
        var adviceCommand = new Command("matchup", "Get matchup advice");
        var yourCharArg = new Argument<string>("yourCharacter", "Your character ID (GUID)");
        var opponentArg = new Argument<string>("opponent", "Opponent character ID (GUID)");
        adviceCommand.AddArgument(yourCharArg);
        adviceCommand.AddArgument(opponentArg);
        adviceCommand.SetHandler(async (string yourCharStr, string opponentStr) =>
        {
            if (!Guid.TryParse(yourCharStr, out var yourCharId) || !Guid.TryParse(opponentStr, out var opponentId))
            {
                AnsiConsole.MarkupLine("[red]Invalid character ID format.[/]");
                return;
            }

            var coachService = Host.Services.GetService<IMugenCoachService>();
            if (coachService == null)
            {
                AnsiConsole.MarkupLine("[red]MUGEN coach service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Generating matchup advice...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    var result = await coachService.GetMatchupAdviceAsync(yourCharId, opponentId).ConfigureAwait(false);
                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                        return;
                    }

                    var advice = result.Value!;

                    AnsiConsole.MarkupLine($"[blue]Predicted Win Rate:[/] {advice.PredictedWinRate:P0}");
                    AnsiConsole.WriteLine();

                    AnsiConsole.MarkupLine("[green]Tips:[/]");
                    foreach (var tip in advice.Tips)
                    {
                        AnsiConsole.MarkupLine($"  • {tip}");
                    }

                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[red]Moves to Avoid:[/]");
                    foreach (var avoid in advice.MovesToAvoid)
                    {
                        AnsiConsole.MarkupLine($"  • {avoid}");
                    }

                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[yellow]Key Moves:[/]");
                    foreach (var key in advice.KeyMoves)
                    {
                        AnsiConsole.MarkupLine($"  • {key}");
                    }
                });
        }, yourCharArg, opponentArg);

        // Character guide
        var guideCommand = new Command("guide", "Get character guide");
        var guideCharArg = new Argument<string>("characterId", "Character ID (GUID)");
        guideCommand.AddArgument(guideCharArg);
        guideCommand.SetHandler(async (string charIdStr) =>
        {
            if (!Guid.TryParse(charIdStr, out var charId))
            {
                AnsiConsole.MarkupLine($"[red]Invalid character ID: {charIdStr}[/]");
                return;
            }

            var coachService = Host.Services.GetService<IMugenCoachService>();
            if (coachService == null)
            {
                AnsiConsole.MarkupLine("[red]MUGEN coach service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Generating character guide...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    var result = await coachService.GetCharacterGuideAsync(charId).ConfigureAwait(false);
                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                        return;
                    }

                    var guide = result.Value!;

                    var headerPanel = new Panel(new Markup(guide.Overview))
                    {
                        Header = new PanelHeader($"[blue]{guide.CharacterName} Guide[/]"),
                        Border = BoxBorder.Rounded
                    };
                    AnsiConsole.Write(headerPanel);
                    AnsiConsole.WriteLine();

                    // Strengths
                    AnsiConsole.MarkupLine("[green]Strengths:[/]");
                    foreach (var s in guide.Strengths)
                    {
                        AnsiConsole.MarkupLine($"  [green]+[/] {s}");
                    }

                    // Weaknesses
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("[red]Weaknesses:[/]");
                    foreach (var w in guide.Weaknesses)
                    {
                        AnsiConsole.MarkupLine($"  [red]-[/] {w}");
                    }

                    // Combos
                    if (guide.BasicCombos.Any())
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine("[yellow]Key Combos:[/]");
                        var comboTable = new Table();
                        comboTable.AddColumn("Name");
                        comboTable.AddColumn("Input");
                        comboTable.AddColumn("Damage");
                        comboTable.AddColumn("Difficulty");

                        foreach (var combo in guide.BasicCombos)
                        {
                            comboTable.AddRow(
                                combo.Name,
                                $"[dim]{combo.Input}[/]",
                                combo.Damage.ToString(),
                                combo.Difficulty);
                        }
                        AnsiConsole.Write(comboTable);
                    }

                    // Tips
                    if (guide.AdvancedTips.Any())
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine("[cyan]Advanced Tips:[/]");
                        foreach (var tip in guide.AdvancedTips)
                        {
                            AnsiConsole.MarkupLine($"  💡 {tip}");
                        }
                    }
                });
        }, guideCharArg);

        coachCommand.AddCommand(adviceCommand);
        coachCommand.AddCommand(guideCommand);

        // Recent matches
        var matchesCommand = new Command("matches", "View recent matches");
        var matchCountOption = new Option<int>("--count", () => 10, "Number of matches to show");
        matchesCommand.AddOption(matchCountOption);
        matchesCommand.SetHandler(async (int count) =>
        {
            var statsService = Host.Services.GetService<IMugenStatsService>();
            if (statsService == null)
            {
                AnsiConsole.MarkupLine("[red]MUGEN stats service not available.[/]");
                return;
            }

            var result = await statsService.GetRecentMatchesAsync(count).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            var matches = result.Value!;
            if (!matches.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No match history found.[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("Date");
            table.AddColumn("P1");
            table.AddColumn("Result");
            table.AddColumn("P2");
            table.AddColumn("Duration");

            foreach (var match in matches)
            {
                var resultColor = match.Result switch
                {
                    MatchResult.Player1Win => "green",
                    MatchResult.Player2Win => "red",
                    _ => "yellow"
                };
                table.AddRow(
                    match.PlayedAt.ToString("g"),
                    match.Player1CharacterId.ToString()[..8],
                    $"[{resultColor}]{match.Result}[/]",
                    match.Player2CharacterId.ToString()[..8],
                    $"{match.MatchDuration.TotalSeconds:F1}s");
            }

            AnsiConsole.Write(table);
        }, matchCountOption);

        // Add all subgroups
        mugenCommand.AddCommand(charsCommand);
        mugenCommand.AddCommand(collectionsCommand);
        mugenCommand.AddCommand(tournamentCommand);
        mugenCommand.AddCommand(coachCommand);
        mugenCommand.AddCommand(matchesCommand);

        // Register the main command
        rootCommand.AddCommandChecked(mugenCommand);
    }
}
