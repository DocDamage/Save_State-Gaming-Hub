using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Mugen.Services;
using Spectre.Console;

namespace SaveState.CLI.Handlers.Mugen;

/// <summary>
/// Handles MUGEN coaching-related CLI operations.
/// </summary>
public static class CoachingHandler
{
    /// <summary>
    /// Gets coaching advice for a character.
    /// </summary>
    public static async Task GetCoachingAdviceAsync(IServiceProvider services, string charIdStr)
    {
        if (!Guid.TryParse(charIdStr, out var charId))
        {
            AnsiConsole.MarkupLine($"[red]Invalid character ID: {charIdStr}[/]");
            return;
        }

        var coachingService = services.GetService<IMugenCoachService>();
        if (coachingService == null)
        {
            AnsiConsole.MarkupLine("[red]Coaching service not available.[/]");
            return;
        }

        await AnsiConsole.Status()
            .StartAsync("Getting coaching advice...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                var result = await coachingService.GetCoachingAdviceAsync(charId).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
                    return;
                }

                var advice = result.Value!;

                var panel = new Panel(advice)
                {
                    Header = new PanelHeader("[blue]Coaching Advice[/]"),
                    Border = BoxBorder.Rounded
                };

                AnsiConsole.Write(panel);
            });
    }
}
