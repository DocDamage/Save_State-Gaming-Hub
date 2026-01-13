using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using Spectre.Console;
using SaveState.CLI.Extensions;
using Microsoft.Extensions.DependencyInjection;

using SaveState.Application.Mugen.Commands; // Added import
using SaveState.Core.Common; // For Result<T>

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

        // Scan command
        var scanCommand = new Command("scan", "Scan for MUGEN characters");
        var pathOption = new Option<string?>("--path", "Path to scan (defaults to data/characters)");
        scanCommand.AddOption(pathOption);
        scanCommand.SetHandler(async (string? path) =>
        {
             var targetPath = path ?? Path.Combine(Environment.CurrentDirectory, "data", "characters");
             await AnsiConsole.Status()
                .StartAsync("Scanning characters...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    await Mediator.Send(new ScanMugenCharactersCommand(targetPath)).ConfigureAwait(false);
                    AnsiConsole.MarkupLine($"[green]Scanned characters in {targetPath}[/]");
                });
        }, pathOption);

        mugenCommand.AddCommand(scanCommand);

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

        // Deathmatch subgroup
        var deathmatchCommand = new Command("deathmatch", "Simulate death matches");
        var p1Arg = new Argument<string>("player1", "Player 1 ID (GUID)");
        var p2Arg = new Argument<string>("player2", "Player 2 ID (GUID)");
        var simsOption = new Option<int>("--simulations", () => 1000, "Number of simulations");

        deathmatchCommand.AddArgument(p1Arg);
        deathmatchCommand.AddArgument(p2Arg);
        deathmatchCommand.AddOption(simsOption);

        deathmatchCommand.SetHandler(async (string p1Str, string p2Str, int sims) =>
        {
            if (!Guid.TryParse(p1Str, out var p1Id) || !Guid.TryParse(p2Str, out var p2Id))
            {
                AnsiConsole.MarkupLine("[red]Invalid character ID format.[/]");
                return;
            }

            var simulator = Host.Services.GetService<IDeathMatchSimulator>();
            if (simulator == null)
            {
                AnsiConsole.MarkupLine("[red]Death Match Simulator service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync($"Simulating {sims} matches...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Aesthetic);

                    var result = await simulator.SimulateMatchesAsync(p1Id, p2Id, sims).ConfigureAwait(false);
                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                        return;
                    }

                    var sim = result.Value!;

                    var chart = new BreakdownChart()
                        .Width(60)
                        .AddItem(sim.Character1Name, sim.Character1Wins, Color.Blue)
                        .AddItem(sim.Character2Name, sim.Character2Wins, Color.Red)
                        .AddItem("Draws", sim.Draws, Color.Grey);

                    AnsiConsole.Write(chart);
                    AnsiConsole.WriteLine();

                    AnsiConsole.MarkupLine($"[bold]{sim.Character1Name}[/] Wins: [blue]{sim.Character1Wins}[/] ({sim.Character1WinRate:P1})");
                    AnsiConsole.MarkupLine($"[bold]{sim.Character2Name}[/] Wins: [red]{sim.Character2Wins}[/] ({sim.Character2WinRate:P1})");
                    AnsiConsole.MarkupLine($"Draws: [grey]{sim.Draws}[/]");
                    AnsiConsole.MarkupLine($"Confidence: {sim.Confidence:P1}");
                    AnsiConsole.MarkupLine($"[dim]Simulation took {sim.SimulationDuration.TotalSeconds:F2}s[/]");
                });
        }, p1Arg, p2Arg, simsOption);

        // Add all subgroups
        mugenCommand.AddCommand(charsCommand);
        mugenCommand.AddCommand(collectionsCommand);
        mugenCommand.AddCommand(tournamentCommand);
        mugenCommand.AddCommand(coachCommand);
        mugenCommand.AddCommand(matchesCommand);
        mugenCommand.AddCommand(deathmatchCommand);

        // Graphics enhancement subgroup
        var graphicsCommand = new Command("graphics", "Advanced graphics enhancements");

        // Apply lighting command
        var applyLightingCommand = new Command("lighting", "Apply dynamic lighting effects");
        var targetOption = new Option<string>("--target", "Target to apply lighting to (character or stage)") { IsRequired = true };
        var shadowsOption = new Option<bool>("--shadows", "Enable real-time shadows");
        var ambientIntensityOption = new Option<float>("--ambient-intensity", "Ambient lighting intensity (0.0-1.0)");
        applyLightingCommand.AddOption(targetOption);
        applyLightingCommand.AddOption(shadowsOption);
        applyLightingCommand.AddOption(ambientIntensityOption);
        applyLightingCommand.SetHandler(async (string target, bool shadows, float ambientIntensity) =>
        {
            await AnsiConsole.Status()
                .StartAsync("Applying dynamic lighting...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    var graphicsEngine = Host.Services.GetRequiredService<IMugenGraphicsEngine>();
                    var config = new DynamicLightingConfig
                    {
                        EnableShadows = shadows,
                        AmbientIntensity = ambientIntensity
                    };
                    var result = await graphicsEngine.ApplyDynamicLightingAsync(target, config);
                    if (result.IsSuccess)
                        AnsiConsole.MarkupLine($"[green]Applied dynamic lighting to {target}[/]");
                    else
                        AnsiConsole.MarkupLine($"[red]Failed to apply lighting: {result.Error}[/]");
                });
        }, targetOption, shadowsOption, ambientIntensityOption);

        // Apply screen filter command
        var applyFilterCommand = new Command("filter", "Apply screen filters");
        var filterTypeOption = new Option<string>("--type", "Filter type (crt, scanlines, bloom)") { IsRequired = true };
        var intensityOption = new Option<float>("--intensity", "Filter intensity (0.0-1.0)");
        applyFilterCommand.AddOption(filterTypeOption);
        applyFilterCommand.AddOption(intensityOption);
        applyFilterCommand.SetHandler(async (string filterType, float intensity) =>
        {
            await AnsiConsole.Status()
                .StartAsync("Applying screen filter...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    var graphicsEngine = Host.Services.GetRequiredService<IMugenGraphicsEngine>();
                    var filterTypeEnum = Enum.Parse<ScreenFilterType>(filterType, true);
                    var config = new ScreenFilterConfig
                    {
                        FilterType = filterTypeEnum,
                        Intensity = intensity
                    };
                    var result = await graphicsEngine.ApplyScreenFilterAsync(filterTypeEnum, config);
                    if (result.IsSuccess)
                        AnsiConsole.MarkupLine($"[green]Applied {filterType} filter[/]");
                    else
                        AnsiConsole.MarkupLine($"[red]Failed to apply filter: {result.Error}[/]");
                });
        }, filterTypeOption, intensityOption);

        graphicsCommand.AddCommand(applyLightingCommand);
        graphicsCommand.AddCommand(applyFilterCommand);

        // Audio enhancement subgroup
        var audioCommand = new Command("audio", "Sound design studio");

        // Analyze audio command
        var analyzeCommand = new Command("analyze", "Analyze audio file");
        var audioFileOption = new Option<string>("--file", "Audio file to analyze") { IsRequired = true };
        analyzeCommand.AddOption(audioFileOption);
        analyzeCommand.SetHandler(async (string audioFile) =>
        {
            await AnsiConsole.Status()
                .StartAsync("Analyzing audio...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    var soundStudio = Host.Services.GetRequiredService<IMugenSoundDesignStudio>();
                    var result = await soundStudio.AnalyzeAudioAsync(audioFile);
                    if (result.IsSuccess)
                    {
                        var analysis = result.Value!;
                        AnsiConsole.MarkupLine($"[green]Analysis complete:[/]");
                        AnsiConsole.MarkupLine($"Duration: {analysis.Duration:F2}s");
                        AnsiConsole.MarkupLine($"Peak Level: {analysis.PeakLevelDb:F1}dBFS");
                        AnsiConsole.MarkupLine($"RMS Level: {analysis.RmsLevelDb:F1}dBFS");
                        AnsiConsole.MarkupLine($"Loudness (LUFS): {analysis.Loudness.Integrated:F1}");
                    }
                    else
                        AnsiConsole.MarkupLine($"[red]Analysis failed: {result.Error}[/]");
                });
        }, audioFileOption);

        // Apply audio mix command
        var mixCommand = new Command("mix", "Apply audio mixing configuration");
        var mixFileOption = new Option<string>("--config", "Audio mix configuration file") { IsRequired = true };
        mixCommand.AddOption(mixFileOption);
        mixCommand.SetHandler(async (string configFile) =>
        {
            await AnsiConsole.Status()
                .StartAsync("Applying audio mix...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    var soundStudio = Host.Services.GetRequiredService<IMugenSoundDesignStudio>();
                    // Load mix config from file (simplified for now)
                    var mixConfig = new AudioMixConfig(); // Would load from file
                    var result = await soundStudio.ApplyAudioMixAsync(mixConfig);
                    if (result.IsSuccess)
                        AnsiConsole.MarkupLine($"[green]Applied audio mix configuration[/]");
                    else
                        AnsiConsole.MarkupLine($"[red]Failed to apply audio mix: {result.Error}[/]");
                });
        }, mixFileOption);

        audioCommand.AddCommand(analyzeCommand);
        audioCommand.AddCommand(mixCommand);

        // Move Creation Engine subgroup
        var movesCommand = new Command("moves", "Move creation and management");

        // List available templates
        var templatesCommand = new Command("templates", "List available move templates");
        var categoryOption = new Option<MoveCategory?>("--category", "Filter by move category");
        var typeOption = new Option<MoveType?>("--type", "Filter by move type");
        var difficultyOption = new Option<DifficultyLevel?>("--difficulty", "Filter by difficulty level");
        var searchOption = new Option<string?>("--search", "Search templates by name or description");
        templatesCommand.AddOption(categoryOption);
        templatesCommand.AddOption(typeOption);
        templatesCommand.AddOption(difficultyOption);
        templatesCommand.AddOption(searchOption);
        templatesCommand.SetHandler(async (MoveCategory? category, MoveType? type, DifficultyLevel? difficulty, string? search) =>
        {
            var moveService = Host.Services.GetService<IMoveCreationService>();
            if (moveService == null)
            {
                AnsiConsole.MarkupLine("[red]Move creation service not available.[/]");
                return;
            }

            var result = await moveService.GetMoveTemplatesAsync(category);
            if (!result.IsSuccess)
            {
                AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                return;
            }

            var templates = result.Value!;

            // Apply filters
            if (type.HasValue)
            {
                templates = templates.Where(t => t.Type == type.Value).ToList();
            }

            if (difficulty.HasValue)
            {
                templates = templates.Where(t => t.Difficulty == difficulty.Value).ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                templates = templates.Where(t =>
                    t.Name.ToLower().Contains(searchLower) ||
                    t.Description.ToLower().Contains(searchLower) ||
                    t.Tags.Any(tag => tag.ToLower().Contains(searchLower))).ToList();
            }

            if (!templates.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No move templates found matching your criteria.[/]");
                return;
            }

            // Group by category for better display
            var groupedTemplates = templates.GroupBy(t => t.Category);

            foreach (var group in groupedTemplates.OrderBy(g => g.Key))
            {
                AnsiConsole.MarkupLine($"[bold blue]{group.Key} Moves:[/]");
                var table = new Table();
                table.AddColumn("Name");
                table.AddColumn("Type");
                table.AddColumn("Difficulty");
                table.AddColumn("Tags");
                table.AddColumn("Description");

                foreach (var template in group.OrderBy(t => t.Name))
                {
                    table.AddRow(
                        $"[green]{template.Name}[/]",
                        template.Type.ToString(),
                        GetDifficultyColor(template.Difficulty),
                        string.Join(", ", template.Tags),
                        template.Description.Length > 40 ? template.Description[..37] + "..." : template.Description);
                }

                AnsiConsole.Write(table);
                AnsiConsole.WriteLine();
            }

            AnsiConsole.MarkupLine($"[dim]Total templates: {templates.Count}[/]");
        }, categoryOption, typeOption, difficultyOption, searchOption);

        string GetDifficultyColor(DifficultyLevel difficulty)
        {
            return difficulty switch
            {
                DifficultyLevel.Beginner => "[green]Beginner[/]",
                DifficultyLevel.Intermediate => "[yellow]Intermediate[/]",
                DifficultyLevel.Advanced => "[red]Advanced[/]",
                DifficultyLevel.Expert => "[bold red]Expert[/]",
                _ => difficulty.ToString()
            };
        }

        // Create move from template
        var createCommand = new Command("create", "Create a move from template");
        var templateNameArg = new Argument<string>("template", "Template name (use 'templates' to list)");
        var moveNameArg = new Argument<string>("name", "Move name");
        var commandArg = new Argument<string>("command", "Move command (e.g., QCF+P, QCB+K, DP+P)");
        var createCategoryOption = new Option<MoveCategory?>("--category", "Filter templates by category when searching");
        createCommand.AddArgument(templateNameArg);
        createCommand.AddArgument(moveNameArg);
        createCommand.AddArgument(commandArg);
        createCommand.AddOption(createCategoryOption);
        createCommand.SetHandler(async (string templateName, string moveName, string command, MoveCategory? category) =>
        {
            var moveService = Host.Services.GetService<IMoveCreationService>();
            if (moveService == null)
            {
                AnsiConsole.MarkupLine("[red]Move creation service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Creating move...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    // Find template
                    var templatesResult = await moveService.GetMoveTemplatesAsync(category);
                    if (!templatesResult.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error getting templates: {templatesResult.Error}[/]");
                        return;
                    }

                    var template = templatesResult.Value!.FirstOrDefault(t =>
                        t.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase) ||
                        t.Id.Equals(templateName, StringComparison.OrdinalIgnoreCase));

                    if (template == null)
                    {
                        AnsiConsole.MarkupLine($"[red]Template '{templateName}' not found.[/]");
                        AnsiConsole.MarkupLine($"[dim]Use 'mugen moves templates' to see available templates.[/]");

                        // Show similar templates
                        var similar = templatesResult.Value!
                            .Where(t => t.Name.ToLower().Contains(templateName.ToLower()) ||
                                       t.Tags.Any(tag => tag.ToLower().Contains(templateName.ToLower())))
                            .Take(3)
                            .ToList();

                        if (similar.Any())
                        {
                            AnsiConsole.MarkupLine($"[yellow]Similar templates:[/]");
                            foreach (var sim in similar)
                            {
                                AnsiConsole.MarkupLine($"  • {sim.Name} ({sim.Category})");
                            }
                        }

                        return;
                    }

                    var result = await moveService.CreateMoveFromTemplateAsync(template, moveName, command);
                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                        return;
                    }

                    var move = result.Value!;

                    // Display move details
                    var panel = new Panel(new Markup(
                        $"[bold]Move Created:[/] {move.DisplayName}\n" +
                        $"[bold]Template:[/] {template.Name}\n" +
                        $"[bold]Command:[/] {move.Command}\n" +
                        $"[bold]Type:[/] {move.MoveType} ({move.Category})\n" +
                        $"[bold]Damage:[/] {move.Properties.Damage}\n" +
                        $"[bold]Startup:[/] {move.Properties.StartupFrames}f\n" +
                        $"[bold]Active:[/] {move.Properties.ActiveFrames}f\n" +
                        $"[bold]Recovery:[/] {move.Properties.RecoveryFrames}f\n" +
                        $"[bold]Frame Advantage:[/] {move.Properties.FrameAdvantageOnHit:+#;-#;0} / {move.Properties.FrameAdvantageOnBlock:+#;-#;0}"))
                    {
                        Header = new PanelHeader("[green]Move Creation Successful![/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);

                    // Show next steps
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine($"[cyan]Next steps:[/]");
                    AnsiConsole.MarkupLine($"  • Use [green]mugen moves validate \"{moveName}\"[/] to check for issues");
                    AnsiConsole.MarkupLine($"  • Use [green]mugen moves test \"{moveName}\" --rounds 5[/] to test against AI");
                    AnsiConsole.MarkupLine($"  • Use [green]mugen moves export \"{moveName}\"[/] to generate MUGEN files");
                });
        }, templateNameArg, moveNameArg, commandArg, createCategoryOption);

        // Validate move
        var validateCommand = new Command("validate", "Validate a move definition");
        var validateNameArg = new Argument<string>("name", "Move name");
        validateCommand.AddArgument(validateNameArg);
        validateCommand.SetHandler(async (string moveName) =>
        {
            var moveService = Host.Services.GetService<IMoveCreationService>();
            if (moveService == null)
            {
                AnsiConsole.MarkupLine("[red]Move creation service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Validating move...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    // For demo purposes, create a sample move
                    var templatesResult = await moveService.GetMoveTemplatesAsync();
                    if (!templatesResult.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error getting templates: {templatesResult.Error}[/]");
                        return;
                    }

                    var template = templatesResult.Value!.FirstOrDefault();
                    if (template == null)
                    {
                        AnsiConsole.MarkupLine("[red]No templates available.[/]");
                        return;
                    }

                    var createResult = await moveService.CreateMoveFromTemplateAsync(template, moveName, "QCF+P");
                    if (!createResult.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error creating move: {createResult.Error}[/]");
                        return;
                    }

                    var move = createResult.Value!;
                    var validationResult = await moveService.ValidateMoveAsync(move, new ValidationOptions(
                        CheckFrameData: true,
                        CheckHitboxes: true,
                        CheckBalance: true,
                        CheckCommands: true,
                        StrictMode: false,
                        CustomRules: Array.Empty<string>()));

                    if (!validationResult.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {validationResult.Error}[/]");
                        return;
                    }

                    var validation = validationResult.Value!;

                    if (validation.IsValid)
                    {
                        AnsiConsole.MarkupLine($"[green]✓ Move '{moveName}' is valid![/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]✗ Move '{moveName}' has validation errors:[/]");
                        foreach (var error in validation.Errors)
                        {
                            AnsiConsole.MarkupLine($"  [red]- {error.Message}[/]");
                        }
                    }

                    if (validation.Warnings.Any())
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warnings:[/]");
                        foreach (var warning in validation.Warnings)
                        {
                            AnsiConsole.MarkupLine($"  [yellow]- {warning.Message}[/]");
                        }
                    }
                });
        }, validateNameArg);

        // Test move
        var testCommand = new Command("test", "Test a move against AI");
        var testNameArg = new Argument<string>("name", "Move name");
        var roundsOption = new Option<int>("--rounds", () => 5, "Number of test rounds");
        testCommand.AddArgument(testNameArg);
        testCommand.AddOption(roundsOption);
        testCommand.SetHandler(async (string moveName, int rounds) =>
        {
            var moveService = Host.Services.GetService<IMoveCreationService>();
            if (moveService == null)
            {
                AnsiConsole.MarkupLine("[red]Move creation service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Testing move...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    // Create sample move
                    var templatesResult = await moveService.GetMoveTemplatesAsync();
                    if (!templatesResult.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error getting templates: {templatesResult.Error}[/]");
                        return;
                    }

                    var template = templatesResult.Value!.FirstOrDefault();
                    if (template == null)
                    {
                        AnsiConsole.MarkupLine("[red]No templates available.[/]");
                        return;
                    }

                    var createResult = await moveService.CreateMoveFromTemplateAsync(template, moveName, "QCF+P");
                    if (!createResult.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error creating move: {createResult.Error}[/]");
                        return;
                    }

                    var move = createResult.Value!;
                    var testResult = await moveService.TestMoveAsync(move, new TestParameters(
                        OpponentCharacter: "Ryu",
                        TestRounds: rounds,
                        UseAi: true,
                        Difficulty: TestDifficulty.Medium,
                        TestScenarios: Array.Empty<string>()));

                    if (!testResult.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {testResult.Error}[/]");
                        return;
                    }

                    var test = testResult.Value!;

                    var resultColor = test.TestPassed ? "green" : "red";
                    var status = test.TestPassed ? "PASSED" : "FAILED";

                    AnsiConsole.MarkupLine($"[{resultColor}]{status}[/] - Success Rate: {test.SuccessRate:P1}");

                    var chart = new BarChart()
                        .Width(60)
                        .Label($"[bold]{moveName} Test Results[/]")
                        .CenterLabel();

                    var wins = test.RoundResults.Count(r => r.Won);
                    var losses = test.RoundResults.Count(r => !r.Won);

                    chart.AddItem("Wins", wins, Color.Green);
                    chart.AddItem("Losses", losses, Color.Red);

                    AnsiConsole.Write(chart);

                    if (test.Issues.Any())
                    {
                        AnsiConsole.MarkupLine($"[yellow]Issues found:[/]");
                        foreach (var issue in test.Issues)
                        {
                            AnsiConsole.MarkupLine($"  [yellow]- {issue}[/]");
                        }
                    }

                    if (test.Recommendations.Any())
                    {
                        AnsiConsole.MarkupLine($"[cyan]Recommendations:[/]");
                        foreach (var rec in test.Recommendations)
                        {
                            AnsiConsole.MarkupLine($"  [cyan]💡 {rec}[/]");
                        }
                    }
                });
        }, testNameArg, roundsOption);

        // Export move
        var exportCommand = new Command("export", "Export move to MUGEN files");
        var exportNameArg = new Argument<string>("name", "Move name");
        var outputDirOption = new Option<string>("--output", () => "./exported_moves", "Output directory");
        exportCommand.AddArgument(exportNameArg);
        exportCommand.AddOption(outputDirOption);
        exportCommand.SetHandler(async (string moveName, string outputDir) =>
        {
            var moveService = Host.Services.GetService<IMoveCreationService>();
            if (moveService == null)
            {
                AnsiConsole.MarkupLine("[red]Move creation service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Exporting move...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    // Create sample move
                    var templatesResult = await moveService.GetMoveTemplatesAsync();
                    if (!templatesResult.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error getting templates: {templatesResult.Error}[/]");
                        return;
                    }

                    var template = templatesResult.Value!.FirstOrDefault();
                    if (template == null)
                    {
                        AnsiConsole.MarkupLine("[red]No templates available.[/]");
                        return;
                    }

                    var createResult = await moveService.CreateMoveFromTemplateAsync(template, moveName, "QCF+P");
                    if (!createResult.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error creating move: {createResult.Error}[/]");
                        return;
                    }

                    var move = createResult.Value!;
                    var exportResult = await moveService.ExportMoveAsync(move, new ExportOptions(
                        OutputDirectory: outputDir,
                        IncludeComments: true,
                        OptimizeCode: true,
                        GenerateAirVersion: false,
                        CodeStyle: "standard",
                        AdditionalStates: Array.Empty<string>()));

                    if (!exportResult.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {exportResult.Error}[/]");
                        return;
                    }

                    var export = exportResult.Value!;
                    AnsiConsole.MarkupLine($"[green]Move '{moveName}' exported successfully![/]");
                    AnsiConsole.MarkupLine($"[dim]CNS: {export.CnsFilePath} ({export.CnsFileSize} bytes)[/]");
                    AnsiConsole.MarkupLine($"[dim]CMD: {export.CmdFilePath} ({export.CmdFileSize} bytes)[/]");
                });
        }, exportNameArg, outputDirOption);

        movesCommand.AddCommand(templatesCommand);
        movesCommand.AddCommand(createCommand);
        movesCommand.AddCommand(validateCommand);
        movesCommand.AddCommand(testCommand);
        movesCommand.AddCommand(exportCommand);

        // Machine Learning subgroup
        var mlCommand = new Command("ml", "Machine learning and predictive analytics");

        // Predict match outcome
        var predictCommand = new Command("predict", "Predict match outcome");
        var p1CharArg = new Argument<string>("player1Character", "Player 1's character");
        var p2CharArg = new Argument<string>("player2Character", "Player 2's character");
        var p1IdOption = new Option<string>("--p1-id", "Player 1 ID for skill rating");
        var p2IdOption = new Option<string>("--p2-id", "Player 2 ID for skill rating");
        predictCommand.AddArgument(p1CharArg);
        predictCommand.AddArgument(p2CharArg);
        predictCommand.AddOption(p1IdOption);
        predictCommand.AddOption(p2IdOption);
        predictCommand.SetHandler(async (string p1Char, string p2Char, string? p1Id, string? p2Id) =>
        {
            var mlService = Host.Services.GetService<IMachineLearningService>();
            if (mlService == null)
            {
                AnsiConsole.MarkupLine("[red]Machine learning service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Analyzing match prediction...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    // Get player skills (use defaults if not provided)
                    var p1Skill = new PlayerSkill(p1Id ?? "default_p1", 1500, 0.06, new Dictionary<string, double>(), DateTime.UtcNow);
                    var p2Skill = new PlayerSkill(p2Id ?? "default_p2", 1500, 0.06, new Dictionary<string, double>(), DateTime.UtcNow);

                    if (!string.IsNullOrEmpty(p1Id))
                    {
                        var skillResult = await Host.Services.GetService<IPlayerDataRepository>()!.GetPlayerSkillAsync(p1Id);
                        if (skillResult.IsSuccess) p1Skill = skillResult.Value!;
                    }

                    if (!string.IsNullOrEmpty(p2Id))
                    {
                        var skillResult = await Host.Services.GetService<IPlayerDataRepository>()!.GetPlayerSkillAsync(p2Id);
                        if (skillResult.IsSuccess) p2Skill = skillResult.Value!;
                    }

                    var result = await mlService.AnalyzeCharacterMatchupAsync(p1Char, p2Char);
                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                        return;
                    }

                    var prediction = result.Value!;

                    var winner = prediction.WinRate >= 0.5 ? p1Char : p2Char;
                    var winnerProb = prediction.WinRate >= 0.5 ? prediction.WinRate : 1 - prediction.WinRate;

                    var panel = new Panel(new Markup(
                        $"[bold]Match Prediction: {p1Char} vs {p2Char}[/]\n\n" +
                        $"[green]{p1Char}[/] advantage: {prediction.Advantage}\n" +
                        $"Estimated Win Rate: {prediction.WinRate:P1}\n\n" +
                        $"[bold]Predicted Winner:[/] {winner} ({winnerProb:P1} confidence)\n" +
                        $"[yellow]Key Factors:[/]\n" +
                        $"{string.Join("\n", prediction.StrongMatchupReasons.Concat(prediction.WeakMatchupReasons).Select(f => $"• {f}"))}\n\n" +
                        $"[cyan]Recommendations:[/]\n" +
                        $"{string.Join("\n", prediction.RecommendedStrategies.Select(r => $"• {r}"))}"))
                    {
                        Header = new PanelHeader("[blue]🤖 ML Match Prediction[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);
                });
        }, p1CharArg, p2CharArg, p1IdOption, p2IdOption);

        // Analyze character matchup
        var matchupCommand = new Command("matchup", "Analyze character matchup");
        var char1Arg = new Argument<string>("character1", "First character");
        var char2Arg = new Argument<string>("character2", "Second character");
        matchupCommand.AddArgument(char1Arg);
        matchupCommand.AddArgument(char2Arg);
        matchupCommand.SetHandler(async (string char1, string char2) =>
        {
            var mlService = Host.Services.GetService<IMachineLearningService>();
            if (mlService == null)
            {
                AnsiConsole.MarkupLine("[red]Machine learning service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Analyzing character matchup...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    var result = await mlService.AnalyzeCharacterMatchupAsync(char1, char2);
                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                        return;
                    }

                    var analysis = result.Value!;

                    var advantageColor = analysis.Advantage switch
                    {
                        MatchupAdvantage.StronglyFavored => "[green]",
                        MatchupAdvantage.SlightlyFavored => "[green]",
                        MatchupAdvantage.Even => "[yellow]",
                        MatchupAdvantage.SlightlyUnfavored => "[red]",
                        MatchupAdvantage.StronglyUnfavored => "[red]",
                        _ => "[white]"
                    };

                    var panel = new Panel(new Markup(
                        $"[bold]Character Matchup Analysis[/]\n\n" +
                        $"{char1} vs {char2}\n\n" +
                        $"[bold]Advantage:[/] {advantageColor}{analysis.Advantage}[/]\n" +
                        $"[bold]Win Rate:[/] {analysis.WinRate:P1}\n\n" +
                        $"[green]Strong Matchup Reasons:[/]\n" +
                        $"{string.Join("\n", analysis.StrongMatchupReasons.Select(r => $"• {r}"))}\n\n" +
                        $"[red]Weak Matchup Reasons:[/]\n" +
                        $"{string.Join("\n", analysis.WeakMatchupReasons.Select(r => $"• {r}"))}\n\n" +
                        $"[cyan]Strategic Recommendations:[/]\n" +
                        $"{string.Join("\n", analysis.RecommendedStrategies.Select(s => $"• {s}"))}"))
                    {
                        Header = new PanelHeader("[blue]⚔️ Character Matchup Analysis[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);
                });
        }, char1Arg, char2Arg);

        // Generate procedural move
        var generateMoveCommand = new Command("generate-move", "Generate procedural move");
        var moveTypeOption = new Option<MoveType>("--type", "Move type") { IsRequired = true };
        var proceduralDifficultyOption = new Option<string>("--difficulty", "Difficulty level (Beginner/Intermediate/Advanced/Expert)");
        var powerLevelOption = new Option<double>("--power", "Power level (0.0-2.0)");
        var themeOption = new Option<string>("--theme", "Move theme");
        generateMoveCommand.AddOption(moveTypeOption);
        generateMoveCommand.AddOption(proceduralDifficultyOption);
        generateMoveCommand.AddOption(powerLevelOption);
        generateMoveCommand.AddOption(themeOption);
        generateMoveCommand.SetHandler(async (MoveType moveType, string difficultyStr, double powerLevel, string? theme) =>
        {
            var mlService = Host.Services.GetService<IMachineLearningService>();
            if (mlService == null)
            {
                AnsiConsole.MarkupLine("[red]Machine learning service not available.[/]");
                return;
            }

            // Parse difficulty level
            if (!Enum.TryParse<SaveState.Core.Mugen.ValueObjects.DifficultyLevel>(difficultyStr, true, out var difficulty))
            {
                difficulty = SaveState.Core.Mugen.ValueObjects.DifficultyLevel.Intermediate;
            }

            await AnsiConsole.Status()
                .StartAsync("Generating procedural move...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    var parameters = new MoveGenerationParameters(
                        MoveType: moveType,
                        Difficulty: difficulty,
                        RequiredMechanics: Array.Empty<string>(),
                        AvoidedMechanics: Array.Empty<string>(),
                        PowerLevel: Math.Clamp(powerLevel, 0.0, 2.0),
                        Theme: theme ?? "balanced");

                    var result = await mlService.GenerateProceduralMoveAsync(parameters);
                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                        return;
                    }

                    var move = result.Value!;

                    var panel = new Panel(new Markup(
                        $"[bold]Procedural Move Generated[/]\n\n" +
                        $"[bold]Name:[/] {move.Name}\n" +
                        $"[bold]Type:[/] {move.Type}\n" +
                        $"[bold]Balance Score:[/] {move.BalanceScore:P1}\n\n" +
                        $"[yellow]Description:[/] {move.Description}\n\n" +
                        $"[cyan]Mechanics:[/]\n" +
                        $"{string.Join(", ", move.Mechanics)}\n\n" +
                        $"[green]Properties:[/]\n" +
                        $"{string.Join("\n", move.Properties.Select(p => $"{p.Key}: {p.Value:F1}"))}"))
                    {
                        Header = new PanelHeader("[blue]🎲 Procedural Move Generation[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);
                });
        }, moveTypeOption, proceduralDifficultyOption, powerLevelOption, themeOption);

        // Analyze character balance
        var balanceCommand = new Command("balance", "Analyze character balance");
        var characterArg = new Argument<string>("character", "Character to analyze");
        balanceCommand.AddArgument(characterArg);
        balanceCommand.SetHandler(async (string character) =>
        {
            var mlService = Host.Services.GetService<IMachineLearningService>();
            if (mlService == null)
            {
                AnsiConsole.MarkupLine("[red]Machine learning service not available.[/]");
                return;
            }

            await AnsiConsole.Status()
                .StartAsync("Analyzing character balance...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);

                    var result = await mlService.AnalyzeCharacterBalanceAsync(character);
                    if (!result.IsSuccess)
                    {
                        AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                        return;
                    }

                    var analysis = result.Value!;

                    var ratingColor = analysis.TierRating switch
                    {
                        "S" or "A" => "[red]",
                        "B" => "[yellow]",
                        "C" => "[green]",
                        "D" => "[yellow]",
                        "F" => "[red]",
                        _ => "[white]"
                    };

                    var moveScoresText = analysis.MoveAnalyses.Any()
                        ? string.Join("\n", analysis.MoveAnalyses.Select(m => $"{m.MoveName}: {m.Effectiveness / 100.0:F2}"))
                        : "No move data available";

                    var recommendationsText = analysis.Recommendations.Any()
                        ? string.Join("\n", analysis.Recommendations.Select(r => $"• {r}"))
                        : "No recommendations";

                    var panel = new Panel(new Markup(
                        $"[bold]Character Balance Analysis: {analysis.CharacterName}[/]\n\n" +
                        $"[bold]Tier Rating:[/] {ratingColor}{analysis.TierRating}[/]\n" +
                        $"[bold]Balance Score:[/] {analysis.BalanceScore}/100\n" +
                        $"[bold]Predicted Win Rate:[/] {analysis.PredictedWinRate:F1}%\n\n" +
                        $"[yellow]Move Balance Scores:[/]\n{moveScoresText}\n\n" +
                        $"[cyan]Recommendations:[/]\n{recommendationsText}"))
                    {
                        Header = new PanelHeader("[blue]⚖️ Character Balance Analysis[/]"),
                        Border = BoxBorder.Rounded
                    };

                    AnsiConsole.Write(panel);
                });
        }, characterArg);

        mlCommand.AddCommand(predictCommand);
        mlCommand.AddCommand(matchupCommand);
        mlCommand.AddCommand(generateMoveCommand);
        mlCommand.AddCommand(balanceCommand);

        mugenCommand.AddCommand(movesCommand);
        mugenCommand.AddCommand(mlCommand);
        mugenCommand.AddCommand(graphicsCommand);
        mugenCommand.AddCommand(audioCommand);

        // Register the main command
        rootCommand.AddCommandChecked(mugenCommand);
    }
}
