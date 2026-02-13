using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using SaveState.CLI.Handlers.Mugen;
using SaveState.Core.Mugen.Entities;
using Spectre.Console;

namespace SaveState.CLI.Commands;

/// <summary>
/// Commands for MUGEN fighting game management.
/// </summary>
public class MugenCommands : CommandGroupBase
{
    /// <summary>
    /// Builds and registers the MUGEN-related commands.
    /// </summary>
    protected override void BuildCommands(RootCommand rootCommand)
    {
        var mugenCommand = new Command("mugen", "MUGEN fighting game management");

        // Scan command
        var scanCommand = new Command("scan", "Scan for MUGEN characters");
        var pathOption = new Option<string?>("--path", "Path to scan (defaults to data/characters)");
        scanCommand.AddOption(pathOption);
        scanCommand.SetHandler(async (string? path) =>
        {
            await ScanHandler.ScanCharactersAsync(Host.Services, path);
        }, pathOption);
        mugenCommand.AddCommand(scanCommand);

        // Characters subgroup
        var charsCommand = BuildCharactersCommand();
        mugenCommand.AddCommand(charsCommand);

        // Collections subgroup
        var collectionsCommand = BuildCollectionsCommand();
        mugenCommand.AddCommand(collectionsCommand);

        // Tournament subgroup
        var tournamentCommand = BuildTournamentCommand();
        mugenCommand.AddCommand(tournamentCommand);

        // Coaching subgroup
        var coachCommand = BuildCoachingCommand();
        mugenCommand.AddCommand(coachCommand);

        // Matches subgroup
        var matchesCommand = BuildMatchesCommand();
        mugenCommand.AddCommand(matchesCommand);

        // Deathmatch subgroup
        var deathmatchCommand = BuildDeathmatchCommand();
        mugenCommand.AddCommand(deathmatchCommand);

        // Graphics subgroup
        var graphicsCommand = BuildGraphicsCommand();
        mugenCommand.AddCommand(graphicsCommand);

        rootCommand.AddCommand(mugenCommand);
    }

    private Command BuildCharactersCommand()
    {
        var charsCommand = new Command("characters", "Manage MUGEN characters");
        charsCommand.AddAlias("chars");

        // List characters
        var listCharsCommand = new Command("list", "List all MUGEN characters");
        var limitOption = new Option<int>("--limit", () => 20, "Maximum number of characters to display");
        listCharsCommand.AddOption(limitOption);
        listCharsCommand.SetHandler(async (int limit) =>
        {
            await CharacterHandler.ListCharactersAsync(Host.Services, limit);
        }, limitOption);

        // Character stats
        var statsCommand = new Command("stats", "Show character statistics");
        var charIdArg = new Argument<string>("characterId", "Character ID (GUID)");
        statsCommand.AddArgument(charIdArg);
        statsCommand.SetHandler(async (string charIdStr) =>
        {
            await CharacterHandler.ShowCharacterStatsAsync(Host.Services, charIdStr);
        }, charIdArg);

        charsCommand.AddCommand(listCharsCommand);
        charsCommand.AddCommand(statsCommand);

        return charsCommand;
    }

    private Command BuildCollectionsCommand()
    {
        var collectionsCommand = new Command("collections", "Manage character collections");

        // List collections
        var listCollectionsCommand = new Command("list", "List character collections");
        listCollectionsCommand.SetHandler(async () =>
        {
            await CollectionHandler.ListCollectionsAsync(Host.Services);
        });

        // Create collection
        var createCollectionCommand = new Command("create", "Create a new character collection");
        var nameArg = new Argument<string>("name", "Collection name");
        createCollectionCommand.AddArgument(nameArg);
        createCollectionCommand.SetHandler(async (string name) =>
        {
            await CollectionHandler.CreateCollectionAsync(Host.Services, name);
        }, nameArg);

        collectionsCommand.AddCommand(listCollectionsCommand);
        collectionsCommand.AddCommand(createCollectionCommand);

        return collectionsCommand;
    }

    private Command BuildTournamentCommand()
    {
        var tournamentCommand = new Command("tournament", "Manage MUGEN tournaments");

        // List tournaments
        var listTournamentsCommand = new Command("list", "List all tournaments");
        listTournamentsCommand.SetHandler(async () =>
        {
            await TournamentHandler.ListTournamentsAsync(Host.Services);
        });

        // Create tournament
        var createTournamentCommand = new Command("create", "Create a new tournament");
        var nameArg = new Argument<string>("name", "Tournament name");
        var formatOption = new Option<TournamentFormat>("--format", () => TournamentFormat.SingleElimination, "Tournament format");
        var participantsOption = new Option<int>("--participants", () => 8, "Maximum participants");
        createTournamentCommand.AddArgument(nameArg);
        createTournamentCommand.AddOption(formatOption);
        createTournamentCommand.AddOption(participantsOption);
        createTournamentCommand.SetHandler(async (string name, TournamentFormat format, int participants) =>
        {
            await TournamentHandler.CreateTournamentAsync(Host.Services, name, format, participants);
        }, nameArg, formatOption, participantsOption);

        // Start tournament
        var startTournamentCommand = new Command("start", "Start a tournament");
        var tournamentIdArg = new Argument<string>("tournamentId", "Tournament ID (GUID)");
        startTournamentCommand.AddArgument(tournamentIdArg);
        startTournamentCommand.SetHandler(async (string tournamentIdStr) =>
        {
            await TournamentHandler.StartTournamentAsync(Host.Services, tournamentIdStr);
        }, tournamentIdArg);

        tournamentCommand.AddCommand(listTournamentsCommand);
        tournamentCommand.AddCommand(createTournamentCommand);
        tournamentCommand.AddCommand(startTournamentCommand);

        return tournamentCommand;
    }

    private Command BuildCoachingCommand()
    {
        var coachCommand = new Command("coach", "AI coaching and training");

        // Coaching advice
        var adviceCommand = new Command("advice", "Get coaching advice for a character");
        var charIdArg = new Argument<string>("characterId", "Character ID (GUID)");
        adviceCommand.AddArgument(charIdArg);
        adviceCommand.SetHandler(async (string charIdStr) =>
        {
            await CoachingHandler.GetCoachingAdviceAsync(Host.Services, charIdStr);
        }, charIdArg);

        coachCommand.AddCommand(adviceCommand);

        return coachCommand;
    }

    private Command BuildMatchesCommand()
    {
        var matchesCommand = new Command("matches", "View match history");

        var recentCommand = new Command("recent", "Show recent matches");
        var countOption = new Option<int>("--count", () => 10, "Number of matches to show");
        recentCommand.AddOption(countOption);
        recentCommand.SetHandler(async (int count) =>
        {
            await BattleHandler.ListMatchesAsync(Host.Services, count);
        }, countOption);

        matchesCommand.AddCommand(recentCommand);

        return matchesCommand;
    }

    private Command BuildDeathmatchCommand()
    {
        var deathmatchCommand = new Command("deathmatch", "Simulate death matches");
        var p1Arg = new Argument<string>("player1", "Player 1 ID (GUID)");
        var p2Arg = new Argument<string>("player2", "Player 2 ID (GUID)");
        var simsOption = new Option<int>("--simulations", () => 1000, "Number of simulations");

        deathmatchCommand.AddArgument(p1Arg);
        deathmatchCommand.AddArgument(p2Arg);
        deathmatchCommand.AddOption(simsOption);

        deathmatchCommand.SetHandler(async (string p1Str, string p2Str, int sims) =>
        {
            await BattleHandler.RunDeathMatchAsync(Host.Services, p1Str, p2Str, sims);
        }, p1Arg, p2Arg, simsOption);

        return deathmatchCommand;
    }

    private Command BuildGraphicsCommand()
    {
        var graphicsCommand = new Command("graphics", "Advanced graphics enhancements");

        // List presets
        var listPresetsCommand = new Command("presets", "List available graphics presets");
        listPresetsCommand.SetHandler(async () =>
        {
            await GraphicsHandler.ListPresetsAsync(Host.Services);
        });

        // Apply lighting
        var applyLightingCommand = new Command("lighting", "Apply dynamic lighting effects");
        var targetOption = new Option<string>("--target", "Target to apply lighting to (character or stage)") { IsRequired = true };
        var shadowsOption = new Option<bool>("--shadows", "Enable real-time shadows");
        var ambientIntensityOption = new Option<float>("--ambient-intensity", "Ambient lighting intensity (0.0-1.0)");
        applyLightingCommand.AddOption(targetOption);
        applyLightingCommand.AddOption(shadowsOption);
        applyLightingCommand.AddOption(ambientIntensityOption);
        applyLightingCommand.SetHandler(async (string target, bool shadows, float ambientIntensity) =>
        {
            await GraphicsHandler.ApplyLightingAsync(Host.Services, target, shadows, ambientIntensity);
        }, targetOption, shadowsOption, ambientIntensityOption);

        graphicsCommand.AddCommand(listPresetsCommand);
        graphicsCommand.AddCommand(applyLightingCommand);

        return graphicsCommand;
    }
}
