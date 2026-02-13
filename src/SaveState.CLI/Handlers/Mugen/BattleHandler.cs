using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Mugen.Services;
using Spectre.Console;

namespace SaveState.CLI.Handlers.Mugen;

/// <summary>
/// Handles MUGEN battle and match-related CLI operations.
/// </summary>
public static class BattleHandler
{
    /// <summary>
    /// Lists recent matches.
    /// </summary>
    public static async Task ListMatchesAsync(IServiceProvider services, int count)
    {
        var statsService = services.GetService<IMugenStatsService>();
        if (statsService == null)
        {
            AnsiConsole.MarkupLine("[red]Stats service not available.[/]");
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
            AnsiConsole.MarkupLine("[yellow]No matches found.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Date");
        table.AddColumn("Player 1");
        table.AddColumn("Result");
        table.AddColumn("Player 2");

        foreach (var match in matches)
        {
            table.AddRow(
                match.PlayedAt.ToString("g"),
                match.Player1CharacterId.ToString()[..8],
                match.Result.ToString(),
                match.Player2CharacterId.ToString()[..8]);
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Runs deathmatch simulation between two characters.
    /// </summary>
    public static async Task RunDeathMatchAsync(IServiceProvider services, string p1Str, string p2Str, int sims)
    {
        if (!Guid.TryParse(p1Str, out var p1Id) || !Guid.TryParse(p2Str, out var p2Id))
        {
            AnsiConsole.MarkupLine("[red]Invalid character ID format.[/]");
            return;
        }

        var simulator = services.GetService<IDeathMatchSimulator>();
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
    }
}
