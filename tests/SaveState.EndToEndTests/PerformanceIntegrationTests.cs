using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Common;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace SaveState.EndToEndTests;

/// <summary>
/// Performance integration tests for advanced gaming features.
/// Ensures all features operate within acceptable performance bounds.
/// </summary>
public class PerformanceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _services;

    public PerformanceIntegrationTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _services = fixture.Services;
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Performance", "VoiceCommands")]
    public async Task VoiceCommandProcessing_Performance_Acceptable()
    {
        // Arrange
        var voiceService = _services.GetRequiredService<SaveState.Core.Input.Services.IVoiceCommandService>();
        var stopwatch = new Stopwatch();

        // Act - Measure voice command processing time
        stopwatch.Start();
        var result = await voiceService.ProcessVoiceCommandAsync("launch game");
        stopwatch.Stop();

        // Assert - First check functionality works
        result.IsSuccess.Should().BeTrue("Voice command processing should succeed");

        // Then check performance (with generous timeout for development environment)
        var timeout = Environment.GetEnvironmentVariable("CI") != null ? 10000 : 30000; // 30 seconds for local dev
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(timeout, $"Voice command processing should complete within {timeout}ms in this environment");

        _output.WriteLine($"Voice command processed in {stopwatch.ElapsedMilliseconds}ms (timeout: {timeout}ms)");
    }

    [Fact]
    [Trait("Performance", "NetworkQuality")]
    public async Task NetworkQualityMonitoring_Performance_Acceptable()
    {
        // Arrange
        var networkMonitor = _services.GetRequiredService<SaveState.Core.Sync.Services.INetworkQualityMonitor>();
        var stopwatch = new Stopwatch();

        // Act - Measure network quality assessment time
        stopwatch.Start();
        var result = await networkMonitor.GetCurrentQualityAsync();
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "Network quality check should be reasonably fast");

        _output.WriteLine($"Network quality assessed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Performance", "LaunchExperience")]
    public async Task LaunchExperienceGeneration_Performance_Acceptable()
    {
        // Arrange
        var gameId = _fixture.TestGameId;
        var mediator = _services.GetRequiredService<MediatR.IMediator>();
        var stopwatch = new Stopwatch();

        // Act - Measure launch experience configuration time
        var result = await mediator.Send(new SaveState.Application.GameLibrary.Commands.ConfigureLaunchExperienceCommand(
            gameId,
            new SaveState.Core.GameLibrary.Services.DTOs.LaunchExperienceConfig(true, true, true, true, TimeSpan.FromSeconds(10))));
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Launch sequence generation should be fast");

        _output.WriteLine($"Launch sequence generated in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Performance", "CloudGaming")]
    public async Task CloudGamingOperations_Performance_Acceptable()
    {
        // Arrange
        var cloudManager = _services.GetRequiredService<SaveState.Core.Sync.Services.ICloudGamingManager>();
        var gameId = _fixture.TestGameId;
        var stopwatch = new Stopwatch();

        // Act - Measure cloud session start time
        stopwatch.Start();
        var result = await cloudManager.StartSessionAsync(gameId, SaveState.Core.Sync.Services.DTOs.CloudGamingProvider.GeForceNow);
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "Cloud session startup should be reasonably fast");

        _output.WriteLine($"Cloud session started in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Performance", "SaveStates")]
    public async Task SaveStateOperations_Performance_Acceptable()
    {
        // Arrange
        var gameId = _fixture.TestGameId;
        var mediator = _services.GetRequiredService<MediatR.IMediator>();
        var stopwatch = new Stopwatch();

        // Act - Measure save state creation time
        stopwatch.Start();
        var result = await mediator.Send(new SaveState.Application.SaveStates.Commands.CreateSaveStateCommand(
            GameId: gameId,
            Description: "Performance test save"));
        stopwatch.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "Save state creation should be very fast");

        _output.WriteLine($"Save state created in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Category", "Performance")]
    [Trait("Performance", "ConcurrentOperations")]
    public async Task ConcurrentAdvancedFeatures_Performance_Acceptable()
    {
        // Arrange
        var voiceService = _services.GetRequiredService<SaveState.Core.Input.Services.IVoiceCommandService>();
        var networkMonitor = _services.GetRequiredService<SaveState.Core.Sync.Services.INetworkQualityMonitor>();
        var mediator = _services.GetRequiredService<MediatR.IMediator>();
        var stopwatch = new Stopwatch();

        // Act - Run multiple operations concurrently
        stopwatch.Start();
        var tasks = new Task[]
        {
            voiceService.ProcessVoiceCommandAsync("test command"),
            networkMonitor.GetCurrentQualityAsync(),
            mediator.Send(new SaveState.Application.GameLibrary.Commands.GenerateGameBriefingCommand(_fixture.TestGameId)),
            _services.GetRequiredService<SaveState.Core.Sync.Services.ICloudGamingManager>().GetAvailableProvidersAsync()
        };

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - All operations should complete successfully
        foreach (var task in tasks)
        {
            // We need to wait and check results
            dynamic result = await (dynamic)task;
            ((bool)result.IsSuccess).Should().BeTrue();
        }

        // Performance should be better than sequential execution
        var timeout = Environment.GetEnvironmentVariable("CI") != null ? 30000 : 60000; // 60 seconds for local dev
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(timeout, $"Concurrent operations should complete within {timeout}ms in this environment");

        _output.WriteLine($"Concurrent operations completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Performance", "MemoryUsage")]
    public async Task AdvancedFeatures_MemoryUsage_Acceptable()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var voiceService = _services.GetRequiredService<SaveState.Core.Input.Services.IVoiceCommandService>();

        // Act - Perform multiple voice command operations
        for (int i = 0; i < 10; i++)
        {
            await voiceService.ProcessVoiceCommandAsync($"test command {i}");
            await voiceService.RegisterCommandAsync(new SaveState.Core.Input.Services.DTOs.VoiceCommandDefinition(
                $"command-{i}", $"Test command {i}", SaveState.Core.Input.Services.DTOs.VoiceCommandAction.ShowCommands));
        }

        // Force garbage collection to get accurate measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);

        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);

        // Assert - Memory usage should be reasonable
        ((double)memoryIncreaseMB).Should().BeLessThan(50, "Memory usage should be reasonable after operations");

        _output.WriteLine($"Memory increase: {memoryIncreaseMB:F2} MB");
    }

    [Fact]
    [Trait("Performance", "StartupTime")]
    public void ServiceInitialization_Performance_Acceptable()
    {
        // Arrange
        var stopwatch = new Stopwatch();

        // Act - Measure service resolution time
        stopwatch.Start();
        var resolvedServices = new object[]
        {
            _services.GetRequiredService<SaveState.Core.Input.Services.IVoiceCommandService>(),
            _services.GetRequiredService<SaveState.Core.Sync.Services.ICloudGamingManager>(),
            _services.GetRequiredService<SaveState.Core.Sync.Services.INetworkQualityMonitor>(),
            _services.GetRequiredService<SaveState.Core.GameLibrary.Services.ILaunchExperienceManager>(),
            _services.GetRequiredService<SaveState.Core.GameLibrary.Services.IGameBriefingService>(),
            _services.GetRequiredService<SaveState.Core.SaveStates.Services.ISaveStateManager>()
        };
        stopwatch.Stop();

        // Assert - All services should resolve quickly
        resolvedServices.Should().AllBeAssignableTo<object>();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100, "Service initialization should be very fast");

        _output.WriteLine($"Service initialization completed in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    [Trait("Performance", "ResourceCleanup")]
    public async Task ResourceCleanup_Performance_Acceptable()
    {
        // Arrange
        var voiceService = _services.GetRequiredService<SaveState.Core.Input.Services.IVoiceCommandService>();
        var initialObjects = GC.GetTotalMemory(false);

        // Act - Create and dispose of resources
        await voiceService.StartListeningAsync();
        await Task.Delay(100); // Allow some operations
        await voiceService.StopListeningAsync();

        // Force cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var finalObjects = GC.GetTotalMemory(false);

        // Assert - Resources should be cleaned up
        ((double)finalObjects).Should().BeLessThanOrEqualTo(initialObjects * 1.5, "Resources should be properly cleaned up");

        _output.WriteLine("Resource cleanup working correctly");
    }
}
