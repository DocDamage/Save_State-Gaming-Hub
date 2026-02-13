using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Mugen.Services;
using Spectre.Console;

namespace SaveState.CLI.Handlers.Mugen;

/// <summary>
/// Handles MUGEN collection-related CLI operations.
/// </summary>
public static class CollectionHandler
{
    /// <summary>
    /// Lists all character collections.
    /// </summary>
    public static async Task ListCollectionsAsync(IServiceProvider services)
    {
        var collectionService = services.GetService<IMugenCollectionService>();
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
    }

    /// <summary>
    /// Creates a new character collection.
    /// </summary>
    public static async Task CreateCollectionAsync(IServiceProvider services, string name)
    {
        var collectionService = services.GetService<IMugenCollectionService>();
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

        var collection = result.Value!;
        AnsiConsole.MarkupLine($"[green]Created collection:[/] {collection.Name} [dim]({collection.Id})[/]");
    }
}
