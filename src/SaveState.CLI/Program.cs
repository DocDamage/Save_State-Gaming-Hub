using System.CommandLine;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaveState.CLI.Commands;
using SaveState.CLI.Extensions;
using SaveState.Application.Common.DependencyInjection;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure;
using Spectre.Console;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);
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
    group.RegisterCommands(rootCommand, mediator, host);
}

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);
