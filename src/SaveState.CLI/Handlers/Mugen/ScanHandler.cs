using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Application.Mugen.Commands;
using Spectre.Console;

namespace SaveState.CLI.Handlers.Mugen;

/// <summary>
/// Handles MUGEN scanning-related CLI operations.
/// </summary>
public static class ScanHandler
{
    /// <summary>
    /// Scans for MUGEN characters at the specified path.
    /// </summary>
    public static async Task ScanCharactersAsync(IServiceProvider services, string? path)
    {
        var targetPath = path ?? Path.Combine(Environment.CurrentDirectory, "data", "characters");

        // Get mediator from services
        var mediator = services.GetService<IMediator>();
        if (mediator == null)
        {
            AnsiConsole.MarkupLine("[red]Mediator service not available.[/]");
            return;
        }

        await AnsiConsole.Status()
            .StartAsync("Scanning characters...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                await mediator.Send(new ScanMugenCharactersCommand(targetPath)).ConfigureAwait(false);
                AnsiConsole.MarkupLine($"[green]Scanned characters in {targetPath}[/]");
            });
    }
}
