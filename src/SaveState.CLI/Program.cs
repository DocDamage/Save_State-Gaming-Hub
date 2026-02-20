using System.CommandLine;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using SaveState.CLI.Commands;
using SaveState.CLI.Extensions;
using SaveState.Application.Common.DependencyInjection;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure;
using SaveState.Infrastructure.Logging;
using Spectre.Console;

// Initialize bootstrap logger for CLI
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting SaveState CLI v{Version}", "2.5.1");

    var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
    
    // Configure Serilog for CLI
    builder.Logging.ClearProviders();
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithProcessId()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}",
            theme: AnsiConsoleTheme.Code));

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplicationServices();

    var host = builder.Build();
    var mediator = host.Services.GetRequiredService<IMediator>();

    var rootCommand = new RootCommand("SaveState CLI - Game Library Manager");

    // Register all command groups (12 total)
    var commandGroups = new ICommandGroup[]
    {
        // Core functionality
        new GameCommands(),
        new SaveStateCommands(),
        new BacklogCommands(),

        // Performance & monitoring
        new PerformanceCommands(),
        new NetworkCommands(),
        new MemoryCommands(),

        // Cloud & automation
        new CloudCommands(),
        new AutomationCommands(),

        // MUGEN fighting game
        new MugenCommands(),
        new CoachingCommands(),

        // Social & voice
        new SocialCommands(),
        new VoiceCommands(),
    };

    foreach (var group in commandGroups)
    {
        group.RegisterCommands(rootCommand, mediator, host, AnsiConsole.Console);
    }

    Log.Information("CLI initialized with {Count} command groups", commandGroups.Length);

    var exitCode = await rootCommand.InvokeAsync(args).ConfigureAwait(false);
    Log.Information("CLI exiting with code {ExitCode}", exitCode);
    return exitCode;
}
catch (Exception ex)
{
    Log.Fatal(ex, "CLI terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
