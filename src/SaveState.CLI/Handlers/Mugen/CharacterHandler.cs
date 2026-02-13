using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Repositories;
using SaveState.Core.Mugen.Services;
using Spectre.Console;

namespace SaveState.CLI.Handlers.Mugen;

/// <summary>
/// Handles MUGEN character-related CLI operations.
/// </summary>
public static class CharacterHandler
{
    /// <summary>
    /// Lists all MUGEN characters.
    /// </summary>
    public static async Task ListCharactersAsync(IServiceProvider services, int limit)
    {
        var charRepo = services.GetService<IMugenCharacterRepository>();
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
    }

    /// <summary>
    /// Shows statistics for a specific character.
    /// </summary>
    public static async Task ShowCharacterStatsAsync(IServiceProvider services, string charIdStr)
    {
        if (!Guid.TryParse(charIdStr, out var charId))
        {
            AnsiConsole.MarkupLine($"[red]Invalid character ID: {charIdStr}[/]");
            return;
        }

        var statsService = services.GetService<IMugenStatsService>();
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
    }
}
