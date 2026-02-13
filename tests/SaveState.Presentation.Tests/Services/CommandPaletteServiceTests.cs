using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common;
using SaveState.Core.Input.Services.DTOs;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.Tests.Services;

public class CommandPaletteServiceTests
{
    private readonly Mock<ILogger<CommandPaletteService>> _loggerMock = new();

    [Fact]
    public async Task SearchAsync_WhenMatchingQuery_ReturnsCommandsOrderedByScore()
    {
        var service = CreateService();
        service.RegisterCommand(CreateCommand(
            id: "library.scan",
            name: "Scan for Games",
            description: "Scans game folders.",
            category: "Library",
            keywords: ["scan", "library"]));
        service.RegisterCommand(CreateCommand(
            id: "navigation.library",
            name: "Go to Library",
            description: "Open the game library tab.",
            category: "Navigation",
            keywords: ["library", "navigate"]));

        var result = await service.SearchAsync("scan", CommandContext.Default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().NotBeEmpty();
        result.Value![0].Id.Should().Be("library.scan");
    }

    [Fact]
    public async Task SearchAsync_WhenUsingKeywordPrefix_FindsCommand()
    {
        var service = CreateService();
        service.RegisterCommand(CreateCommand(
            id: "navigation.dashboard",
            name: "Go to Dashboard",
            description: "Open overview.",
            category: "Navigation",
            keywords: ["dashboard", "home"]));

        var result = await service.SearchAsync("dash", CommandContext.Default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(x => x.Id == "navigation.dashboard");
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandExists_InvokesDelegate()
    {
        var service = CreateService();
        var executed = false;

        service.RegisterCommand(CreateCommand(
            id: "system.test",
            name: "Test Command",
            description: "Executes test logic.",
            category: "System",
            executeAsync: _ =>
            {
                executed = true;
                return Task.FromResult(Result.Success());
            }));

        var result = await service.ExecuteAsync("system.test");

        result.IsSuccess.Should().BeTrue();
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCommandMissing_ReturnsNotFound()
    {
        var service = CreateService();

        var result = await service.ExecuteAsync("missing.command");

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task UnregisterCommand_RemovesCommandFromSearchResults()
    {
        var service = CreateService();
        service.RegisterCommand(CreateCommand(
            id: "system.cache.clear",
            name: "Clear Cache",
            description: "Clears cache",
            category: "System"));

        service.UnregisterCommand("system.cache.clear");
        var result = await service.SearchAsync("cache", CommandContext.Default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotContain(x => x.Id == "system.cache.clear");
    }

    private CommandPaletteService CreateService() => new(_loggerMock.Object);

    private static CommandDefinition CreateCommand(
        string id,
        string name,
        string description,
        string category,
        IReadOnlyList<string>? keywords = null,
        Func<CancellationToken, Task<Result>>? executeAsync = null) =>
        new()
        {
            Id = id,
            Name = name,
            Description = description,
            Category = category,
            Keywords = keywords ?? [],
            ExecuteAsync = executeAsync ?? (_ => Task.FromResult(Result.Success()))
        };
}
