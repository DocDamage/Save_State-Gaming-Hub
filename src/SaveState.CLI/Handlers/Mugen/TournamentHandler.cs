using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using Spectre.Console;

namespace SaveState.CLI.Handlers.Mugen;

/// <summary>
/// Handles MUGEN tournament-related CLI operations.
/// </summary>
public static class TournamentHandler
{
    /// <summary>
    /// Lists all tournaments.
    /// </summary>
    public static async Task ListTournamentsAsync(IServiceProvider services)
    {
        AnsiConsole.MarkupLine("[yellow]Tournament listing not yet implemented.[/]");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Creates a new tournament.
    /// </summary>
    public static async Task CreateTournamentAsync(
        IServiceProvider services,
        string name,
        TournamentFormat format,
        int participants)
    {
        var tournamentService = services.GetService<IMugenTournamentService>();
        if (tournamentService == null)
        {
            AnsiConsole.MarkupLine("[red]Tournament service not available.[/]");
            return;
        }

        var request = new CreateTournamentRequest(name, format, Array.Empty<Guid>());
        var result = await tournamentService.CreateTournamentAsync(request).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
            return;
        }

        var tournament = result.Value!;
        AnsiConsole.MarkupLine($"[green]Created tournament:[/] {tournament.Name}");
        AnsiConsole.MarkupLine($"[dim]Format:[/] {tournament.Format}");
    }

    /// <summary>
    /// Starts a tournament.
    /// </summary>
    public static async Task StartTournamentAsync(IServiceProvider services, string tournamentIdStr)
    {
        if (!Guid.TryParse(tournamentIdStr, out var tournamentId))
        {
            AnsiConsole.MarkupLine($"[red]Invalid tournament ID: {tournamentIdStr}[/]");
            return;
        }

        var tournamentService = services.GetService<IMugenTournamentService>();
        if (tournamentService == null)
        {
            AnsiConsole.MarkupLine("[red]Tournament service not available.[/]");
            return;
        }

        await AnsiConsole.Status()
            .StartAsync("Starting tournament...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Circle);
                var result = await tournamentService.StartTournamentAsync(tournamentId).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[green]Tournament started![/]");
            });
    }
}
