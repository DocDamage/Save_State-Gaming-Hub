using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using Spectre.Console;

namespace SaveState.CLI.Handlers.Mugen;

/// <summary>
/// Handles MUGEN graphics-related CLI operations.
/// </summary>
public static class GraphicsHandler
{
    /// <summary>
    /// Applies dynamic lighting effects.
    /// </summary>
    public static async Task ApplyLightingAsync(
        IServiceProvider services,
        string target,
        bool shadows,
        float ambientIntensity)
    {
        await AnsiConsole.Status()
            .StartAsync("Applying dynamic lighting...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                var graphicsEngine = services.GetRequiredService<IMugenGraphicsEngine>();
                var config = new DynamicLightingConfig
                {
                    EnableShadows = shadows,
                    AmbientIntensity = ambientIntensity
                };

                var result = await graphicsEngine.ApplyDynamicLightingAsync(target, config).ConfigureAwait(false);

                if (result.IsSuccess)
                {
                    AnsiConsole.MarkupLine("[green]Dynamic lighting applied successfully![/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to apply lighting: {result.Error}[/]");
                }
            });
    }

    /// <summary>
    /// Lists available graphics presets.
    /// </summary>
    public static async Task ListPresetsAsync(IServiceProvider services)
    {
        var graphicsEngine = services.GetService<IMugenGraphicsEngine>();
        if (graphicsEngine == null)
        {
            AnsiConsole.MarkupLine("[red]Graphics engine not available.[/]");
            return;
        }

        var result = await graphicsEngine.GetAvailablePresetsAsync().ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            AnsiConsole.MarkupLine($"[red]Error: {result.Error}[/]");
            return;
        }

        var presets = result.Value!;
        if (!presets.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No graphics presets available.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Description");

        foreach (var preset in presets)
        {
            table.AddRow(preset.Name, preset.Description ?? "-");
        }

        AnsiConsole.Write(table);
    }
}
