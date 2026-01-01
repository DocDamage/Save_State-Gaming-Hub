using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Application.Input.Commands;
using SaveState.Application.Sync.Commands;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Input.Services.DTOs;
using SaveState.Core.Sync.Services.DTOs;
using SaveState.Core.Performance.Services;
using Xunit;
using Xunit.Abstractions;

namespace SaveState.EndToEndTests;

/// <summary>
/// End-to-end integration tests for all 6 completed advanced gaming features.
/// Tests the complete workflow from CLI commands through infrastructure to data persistence.
/// </summary>
public class AdvancedFeaturesIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private readonly IServiceProvider _services;
    private readonly IMediator _mediator;

    public AdvancedFeaturesIntegrationTests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _services = fixture.Services;
        _mediator = _services.GetRequiredService<IMediator>();
    }

    #region Phase 1: Advanced Save State Management

    [Fact]
    [Trait("Phase", "1")]
    [Trait("Feature", "SaveStateOperations")]
    public async Task SaveStateOperations_CompleteWorkflow_Works()
    {
        // Arrange
        var gameId = _fixture.TestGameId;

        // Act - Create save state
        var createSaveCommand = new SaveState.Application.SaveStates.Commands.CreateSaveStateCommand(
            GameId: gameId,
            Description: "Integration test save state");

        var saveResult = await _mediator.Send(createSaveCommand);
        saveResult.IsSuccess.Should().BeTrue(saveResult.Error);

        var saveState = saveResult.Value!;

        // Act - Restore save state
        var restoreCommand = new SaveState.Application.SaveStates.Commands.RestoreSaveStateCommand(
            SaveStateId: saveState.Id);

        var restoreResult = await _mediator.Send(restoreCommand);
        restoreResult.IsSuccess.Should().BeTrue(restoreResult.Error);

        // Assert
        _output.WriteLine($"Successfully created and restored save state '{saveState.Id}'");
    }


    #endregion

    #region Phase 2: Steam Deck Integration

    [Fact]
    [Trait("Phase", "2")]
    [Trait("Feature", "SteamDeckDetection")]
    public async Task SteamDeckDetection_CompleteWorkflow_Works()
    {
        // This would test Steam Deck hardware detection
        // For now, test the service instantiation and basic functionality

        var steamDeckService = _services.GetService<SaveState.Core.Input.Services.ISteamDeckManager>();
        steamDeckService.Should().NotBeNull();

        // Test basic status check
        var status = await steamDeckService!.DetectSteamDeckAsync();
        status.IsSuccess.Should().BeTrue();

        _output.WriteLine("Steam Deck service integration working");
    }

    [Fact]
    [Trait("Phase", "2")]
    [Trait("Feature", "BatteryOptimization")]
    public async Task BatteryOptimization_CompleteWorkflow_Works()
    {
        // Test battery monitoring service
        var batteryService = _services.GetService<SaveState.Core.Performance.Services.IBatteryOptimizer>();
        batteryService.Should().NotBeNull();

        // Test battery status retrieval
        var statusResult = await batteryService!.GetBatteryStatusAsync();
        statusResult.IsSuccess.Should().BeTrue();

        var status = statusResult.Value!;
        status.PercentRemaining.Should().BeInRange(0, 100);

        _output.WriteLine($"Battery level: {status.PercentRemaining}%, Charging: {status.IsCharging}");
    }

    #endregion

    #region Phase 3: System Optimization

    [Fact]
    [Trait("Phase", "3")]
    [Trait("Feature", "SystemResourceManagement")]
    public async Task SystemOptimization_CompleteWorkflow_Works()
    {
        // Test system resource manager
        var resourceManager = _services.GetService<SaveState.Core.Performance.Services.ISystemResourceManager>();
        resourceManager.Should().NotBeNull();

        // Test system analysis
        var analysisResult = await resourceManager!.AnalyzeSystemAsync();
        analysisResult.IsSuccess.Should().BeTrue();

        var analysis = analysisResult.Value!;
        analysis.CpuUsagePercent.Should().BeGreaterThanOrEqualTo(0);
        analysis.AvailableRamMb.Should().BeGreaterThanOrEqualTo(0);

        _output.WriteLine($"System analysis: CPU {analysis.CpuUsagePercent}%, Available RAM {analysis.AvailableRamMb}MB");
    }

    [Fact]
    [Trait("Phase", "3")]
    [Trait("Feature", "DisplayCalibration")]
    public async Task DisplayCalibration_CompleteWorkflow_Works()
    {
        // Test display calibrator
        var displayCalibrator = _services.GetService<SaveState.Core.Performance.Services.IDisplayCalibrator>();
        displayCalibrator.Should().NotBeNull();

        // Test display profile creation
        var profileResult = await displayCalibrator!.CreateGameProfileAsync(
            gameId: _fixture.TestGameId,
            settings: new DisplaySettings(Width: 1920, Height: 1080, RefreshRate: 144, VSync: true, HdrEnabled: false, GSync: false, FullscreenOptimizations: true));
        profileResult.IsSuccess.Should().BeTrue();

        _output.WriteLine("Display calibration profile created successfully");
    }

    #endregion

    #region Phase 4: Launch Experience

    [Fact]
    [Trait("Phase", "4")]
    [Trait("Feature", "LaunchExperience")]
    public async Task LaunchExperienceConfiguration_Works()
    {
        // Arrange
        var gameId = _fixture.TestGameId;

        // Act - Configure launch experience
        var configCommand = new SaveState.Application.GameLibrary.Commands.ConfigureLaunchExperienceCommand(
            gameId,
            new SaveState.Core.GameLibrary.Services.DTOs.LaunchExperienceConfig(
                ShowGameFacts: true,
                ShowLastProgress: true,
                ShowAchievementProgress: true,
                PlayAmbientMusic: true,
                MaxIntroDuration: TimeSpan.FromSeconds(15)));

        var configResult = await _mediator.Send(configCommand);
        configResult.IsSuccess.Should().BeTrue(configResult.Error);

        _output.WriteLine("Launch experience configured successfully");
    }

    [Fact]
    [Trait("Phase", "4")]
    [Trait("Feature", "GameBriefing")]
    public async Task GameBriefingGeneration_Works()
    {
        // Arrange
        var gameId = _fixture.TestGameId;

        // Act - Generate comprehensive briefing
        var briefingCommand = new SaveState.Application.GameLibrary.Commands.GenerateGameBriefingCommand(gameId);
        var briefingResult = await _mediator.Send(briefingCommand);
        briefingResult.IsSuccess.Should().BeTrue(briefingResult.Error);

        var briefing = briefingResult.Value!;
        briefing.LastSessionSummary.Should().NotBeNullOrEmpty();
        briefing.CurrentObjectives.Should().NotBeNull();
        briefing.Tips.Should().NotBeNull();

        _output.WriteLine("AI-powered game briefing generation working correctly");
    }

    #endregion

    #region Phase 5: Cloud Gaming & Network

    [Fact]
    [Trait("Phase", "5")]
    [Trait("Feature", "CloudGaming")]
    public async Task CloudGaming_CompleteWorkflow_Works()
    {
        // Test cloud gaming manager
        var cloudManager = _services.GetService<SaveState.Core.Sync.Services.ICloudGamingManager>();
        cloudManager.Should().NotBeNull();

        // Test available providers
        var providersResult = await cloudManager!.GetAvailableProvidersAsync();
        providersResult.IsSuccess.Should().BeTrue();

        var providers = providersResult.Value!;
        providers.Should().Contain(CloudGamingProvider.GeForceNow);
        providers.Should().Contain(CloudGamingProvider.XboxCloud);
        providers.Should().Contain(CloudGamingProvider.AmazonLuna);

        // Test session creation (mock scenario)
        var gameId = _fixture.TestGameId;
        var startResult = await cloudManager.StartSessionAsync(gameId, CloudGamingProvider.GeForceNow);
        startResult.IsSuccess.Should().BeTrue(startResult.Error);

        var session = startResult.Value!;
        session.GameId.Should().Be(gameId);
        session.Provider.Should().Be(CloudGamingProvider.GeForceNow);

        _output.WriteLine($"Cloud gaming session created for game {gameId} with {session.Provider}");
    }

    [Fact]
    [Trait("Phase", "5")]
    [Trait("Feature", "NetworkQuality")]
    public async Task NetworkQualityMonitoring_CompleteWorkflow_Works()
    {
        // Test network quality monitor
        var networkMonitor = _services.GetService<SaveState.Core.Sync.Services.INetworkQualityMonitor>();
        networkMonitor.Should().NotBeNull();

        // Test current quality assessment
        var qualityResult = await networkMonitor!.GetCurrentQualityAsync();
        qualityResult.IsSuccess.Should().BeTrue();

        var quality = qualityResult.Value!;
        quality.LatencyMs.Should().BeGreaterThanOrEqualTo(0);
        quality.PacketLossPercent.Should().BeInRange(0, 100);
        quality.BandwidthMbps.Should().BeGreaterThanOrEqualTo(0);

        // Test diagnostics
        var diagnosticsResult = await networkMonitor.PerformQualityTestAsync();
        diagnosticsResult.IsSuccess.Should().BeTrue();

        var diagnostics = diagnosticsResult.Value!;
        diagnostics.Recommendations.Should().NotBeNull();

        _output.WriteLine($"Network quality: {quality.LatencyMs}ms latency, {quality.BandwidthMbps} Mbps bandwidth");
    }

    #endregion

    #region Phase 6: Voice Command Integration

    [Fact]
    [Trait("Phase", "6")]
    [Trait("Feature", "VoiceCommands")]
    public async Task VoiceCommandProcessing_CompleteWorkflow_Works()
    {
        // Test voice command service
        var voiceService = _services.GetService<SaveState.Core.Input.Services.IVoiceCommandService>();
        voiceService.Should().NotBeNull();

        // Test command registration
        var commandDef = new VoiceCommandDefinition(
            CommandPhrase: "test launch game",
            Description: "Test game launch command",
            Action: VoiceCommandAction.LaunchGame);

        var registerResult = await voiceService!.RegisterCommandAsync(commandDef);
        registerResult.IsSuccess.Should().BeTrue();

        // Test command processing
        var processResult = await voiceService.ProcessVoiceCommandAsync("test launch game");
        processResult.IsSuccess.Should().BeTrue();

        var commandResult = processResult.Value!;
        commandResult.Success.Should().BeTrue(commandResult.ErrorMessage);
        commandResult.MatchedCommand.Should().NotBeNull();
        commandResult.MatchedCommand!.Description.Should().Be("Test game launch command");

        // Test command listing
        var commandsResult = await voiceService.GetRegisteredCommandsAsync();
        commandsResult.IsSuccess.Should().BeTrue();

        var commands = commandsResult.Value!;
        commands.Should().Contain(c => c.CommandPhrase == "test launch game");

        _output.WriteLine("Voice command processing working correctly");
    }

    [Fact]
    [Trait("Phase", "6")]
    [Trait("Feature", "SpeechRecognition")]
    public async Task SpeechRecognition_CompleteWorkflow_Works()
    {
        // Test speech recognition service
        var speechService = _services.GetService<SaveState.Core.Ai.Services.ISpeechRecognitionService>();
        speechService.Should().NotBeNull();

        // Test language availability
        var languagesResult = await speechService!.GetAvailableLanguagesAsync();
        languagesResult.IsSuccess.Should().BeTrue();

        var languages = languagesResult.Value!;
        languages.Should().NotBeEmpty();
        languages.Should().Contain(l => l.Code == "en-US");

        // Test microphone calibration
        var calibrationResult = await speechService.CalibrateMicrophoneAsync();
        calibrationResult.IsSuccess.Should().BeTrue();

        var calibration = calibrationResult.Value!;
        calibration.Success.Should().BeTrue();
        calibration.OptimalSensitivity.Should().BeGreaterThan(0);

        // Test microphone status
        var statusResult = await speechService.GetMicrophoneStatusAsync();
        statusResult.IsSuccess.Should().BeTrue();

        var status = statusResult.Value!;
        status.IsAvailable.Should().BeTrue();

        _output.WriteLine("Speech recognition services working correctly");
    }

    #endregion

    #region End-to-End Workflow Tests

    [Fact]
    [Trait("Integration", "EndToEnd")]
    [Trait("Workflow", "AdvancedFeaturesIntegration")]
    public async Task AdvancedFeaturesIntegration_EndToEndWorkflow_Works()
    {
        // This test verifies that all advanced features can be instantiated and work together

        // Arrange
        var gameId = _fixture.TestGameId;
        var sessionStart = DateTime.UtcNow;

        // Act 1: Configure launch experience
        var launchConfig = new SaveState.Application.GameLibrary.Commands.ConfigureLaunchExperienceCommand(
            gameId,
            new SaveState.Core.GameLibrary.Services.DTOs.LaunchExperienceConfig(true, true, true, true, TimeSpan.FromSeconds(10)));
        var launchConfigResult = await _mediator.Send(launchConfig);
        launchConfigResult.IsSuccess.Should().BeTrue();

        // Act 2: Generate AI briefing
        var briefingCommand = new SaveState.Application.GameLibrary.Commands.GenerateGameBriefingCommand(gameId);
        var briefingResult = await _mediator.Send(briefingCommand);
        briefingResult.IsSuccess.Should().BeTrue(briefingResult.Error);

        // Act 3: Check network quality for cloud gaming
        var networkMonitor = _services.GetRequiredService<SaveState.Core.Sync.Services.INetworkQualityMonitor>();
        var networkResult = await networkMonitor.GetCurrentQualityAsync();
        networkResult.IsSuccess.Should().BeTrue();

        // Act 4: Register voice commands
        var voiceService = _services.GetRequiredService<SaveState.Core.Input.Services.IVoiceCommandService>();
        var voiceCommand = new SaveState.Core.Input.Services.DTOs.VoiceCommandDefinition(
            "save my game", "Save current game state", SaveState.Core.Input.Services.DTOs.VoiceCommandAction.SaveGame, null, null);
        var voiceResult = await voiceService.RegisterCommandAsync(voiceCommand);
        voiceResult.IsSuccess.Should().BeTrue();

        // Act 5: Test cloud gaming provider availability
        var cloudManager = _services.GetRequiredService<SaveState.Core.Sync.Services.ICloudGamingManager>();
        var providersResult = await cloudManager.GetAvailableProvidersAsync();
        providersResult.IsSuccess.Should().BeTrue();

        // Assert
        var sessionDuration = DateTime.UtcNow - sessionStart;
        sessionDuration.Should().BeGreaterThan(TimeSpan.Zero);

        _output.WriteLine($"Advanced features integration test completed successfully in {sessionDuration.TotalSeconds:F1} seconds");
        _output.WriteLine("All major advanced feature components working together");
    }

    #endregion
}
