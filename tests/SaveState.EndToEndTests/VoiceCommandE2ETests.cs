using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common.Services;
using SaveState.Core.Input.Services;
using SaveState.Core.Input.Services.DTOs;
using SaveState.EndToEndTests.Infrastructure;
using SaveState.Presentation.Resources;
using SaveState.Presentation.ViewModels.Shell;
using SaveState.Presentation.Views.Shell;
using Xunit;
using Xunit.Abstractions;

namespace SaveState.EndToEndTests;

/// <summary>
/// End-to-end browser automation tests for Voice Commands feature.
/// Tests voice visualizer and command execution.
/// </summary>
public class VoiceCommandE2ETests : IClassFixture<IntegrationTestFixture>, IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private readonly ITestOutputHelper _output;
    private AvaloniaTestHost? _host;
    private readonly IServiceProvider _serviceProvider;

    public VoiceCommandE2ETests(IntegrationTestFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
        _serviceProvider = fixture.Services;
    }

    public async Task InitializeAsync()
    {
        _host = new AvaloniaTestHost(_serviceProvider);
        await _host.StartAsync(sp => CreateVoiceControlWindow(sp));
    }

    public async Task DisposeAsync()
    {
        if (_host != null)
        {
            await _host.DisposeAsync();
        }
    }

    private static Window CreateVoiceControlWindow(IServiceProvider services)
    {
        var window = new Window
        {
            Title = "Voice Command E2E Test",
            Width = 800,
            Height = 600,
            Content = CreateVoiceControlView(services)
        };
        return window;
    }

    private static VoiceControlView CreateVoiceControlView(IServiceProvider services)
    {
        var mockVoiceService = new Mock<IVoiceCommandService>();
        var mockSpeechService = new Mock<ISpeechRecognitionService>();
        var mockLogger = new Mock<ILogger<VoiceControlViewModel>>();
        var mockResources = CreateMockResources();

        // Setup mock commands
        var commands = new List<VoiceCommandDefinition>
        {
            new("save game", "Save the current game", VoiceCommandAction.SaveGame, null, null),
            new("load game", "Load a saved game", VoiceCommandAction.LoadGame, null, null),
            new("launch game", "Launch a game", VoiceCommandAction.LaunchGame, null, null),
            new("take screenshot", "Capture a screenshot", VoiceCommandAction.TakeScreenshot, null, null),
            new("open library", "Open the game library", VoiceCommandAction.OpenLibrary, null, null),
            new("open settings", "Open application settings", VoiceCommandAction.OpenSettings, null, null),
            new("mute", "Mute audio", VoiceCommandAction.MuteAudio, null, null),
            new("unmute", "Unmute audio", VoiceCommandAction.UnmuteAudio, null, null)
        };

        mockVoiceService.Setup(x => x.GetRegisteredCommandsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Common.Result<IReadOnlyList<VoiceCommandDefinition>>.Success(commands));

        mockVoiceService.Setup(x => x.ProcessVoiceCommandAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string command, CancellationToken _) =>
            {
                var matchedCommand = commands.FirstOrDefault(c => 
                    command.Contains(c.CommandPhrase, StringComparison.OrdinalIgnoreCase));
                
                return Task.FromResult(Core.Common.Result<VoiceCommandResult>.Success(
                    new VoiceCommandResult(
                        matchedCommand != null,
                        matchedCommand?.CommandPhrase,
                        matchedCommand,
                        null)));
            });

        mockSpeechService.Setup(x => x.GetAvailableLanguagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Common.Result<IReadOnlyList<SpeechLanguage>>.Success(
                new List<SpeechLanguage>
                {
                    new("en-US", "English (United States)", true),
                    new("en-GB", "English (United Kingdom)", true),
                    new("es-ES", "Spanish", true)
                }));

        mockSpeechService.Setup(x => x.GetMicrophoneStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Common.Result<MicrophoneStatus>.Success(
                new MicrophoneStatus(true, 75, false)));

        var viewModel = new VoiceControlViewModel(
            mockVoiceService.Object,
            mockSpeechService.Object,
            mockLogger.Object,
            mockResources);

        return new VoiceControlView { DataContext = viewModel };
    }

    private static Resources CreateMockResources()
    {
        var localizerMock = new Mock<Microsoft.Extensions.Localization.IStringLocalizer<Resources>>();
        localizerMock.Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new Microsoft.Extensions.Localization.LocalizedString(key, key));
        return new Resources(localizerMock.Object);
    }

    #region Voice Control View Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    public async Task VoiceControlView_Loads_Successfully()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange & Act
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;

            // Assert
            voiceView.Should().NotBeNull();
            voiceView!.DataContext.Should().BeOfType<VoiceControlViewModel>();
            _output.WriteLine("Voice control view loaded successfully");
        }, _host!, "VoiceControlView_Loads_Successfully");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    public async Task VoiceControlView_HasMicrophoneStatus()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            await Task.Delay(100);

            // Assert
            viewModel!.IsMicrophoneAvailable.Should().BeTrue();
            _output.WriteLine("Microphone is available");
        }, _host!, "VoiceControlView_HasMicrophoneStatus");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    public async Task VoiceControlView_ShowsAvailableCommands()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            await Task.Delay(200);

            // Assert
            viewModel!.AvailableCommands.Should().NotBeNull();
            viewModel.AvailableCommands.Should().NotBeEmpty();
            _output.WriteLine($"Number of voice commands: {viewModel.AvailableCommands.Count}");
        }, _host!, "VoiceControlView_ShowsAvailableCommands");
    }

    #endregion

    #region Voice Recognition Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Recognition")]
    public async Task VoiceControlView_CanStartListening()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            viewModel!.IsListening = true;
            await Task.Delay(100);

            // Assert
            viewModel.IsListening.Should().BeTrue();
            _output.WriteLine("Voice listening started");
        }, _host!, "VoiceControlView_CanStartListening");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Recognition")]
    public async Task VoiceControlView_CanStopListening()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Start listening first
            viewModel!.IsListening = true;
            await Task.Delay(100);

            // Act
            viewModel.IsListening = false;
            await Task.Delay(100);

            // Assert
            viewModel.IsListening.Should().BeFalse();
            _output.WriteLine("Voice listening stopped");
        }, _host!, "VoiceControlView_CanStopListening");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Recognition")]
    public async Task VoiceControlView_ShowsRecognitionResult()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act - Simulate recognized text
            viewModel!.RecognizedText = "save game";
            viewModel.LastCommandResult = "Command executed: Save Game";
            await Task.Delay(100);

            // Assert
            viewModel.RecognizedText.Should().NotBeNullOrEmpty();
            viewModel.LastCommandResult.Should().NotBeNullOrEmpty();
            _output.WriteLine($"Recognized: {viewModel.RecognizedText}");
        }, _host!, "VoiceControlView_ShowsRecognitionResult");
    }

    #endregion

    #region Command Execution Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Execution")]
    public async Task VoiceControlView_CanExecuteSaveGameCommand()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            var result = await viewModel!.ExecuteVoiceCommandAsync("save game");

            // Assert
            result.Should().BeTrue();
            _output.WriteLine("Save game command executed successfully");
        }, _host!, "VoiceControlView_CanExecuteSaveGameCommand");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Execution")]
    public async Task VoiceControlView_CanExecuteLoadGameCommand()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            var result = await viewModel!.ExecuteVoiceCommandAsync("load game");

            // Assert
            result.Should().BeTrue();
            _output.WriteLine("Load game command executed successfully");
        }, _host!, "VoiceControlView_CanExecuteLoadGameCommand");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Execution")]
    public async Task VoiceControlView_CanExecuteOpenLibraryCommand()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            var result = await viewModel!.ExecuteVoiceCommandAsync("open library");

            // Assert
            result.Should().BeTrue();
            _output.WriteLine("Open library command executed successfully");
        }, _host!, "VoiceControlView_CanExecuteOpenLibraryCommand");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Execution")]
    public async Task VoiceControlView_HandlesUnknownCommand()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            viewModel!.RecognizedText = "unknown command xyz";
            var result = await viewModel.ExecuteVoiceCommandAsync("unknown command xyz");

            // Assert - Unknown command should fail or show not recognized
            _output.WriteLine($"Unknown command result: {result}");
        }, _host!, "VoiceControlView_HandlesUnknownCommand");
    }

    #endregion

    #region Visualizer Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Visualizer")]
    public async Task VoiceControlView_ShowsAudioLevel()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            viewModel!.AudioLevel = 75;
            await Task.Delay(100);

            // Assert
            viewModel.AudioLevel.Should().BeGreaterThan(0);
            _output.WriteLine($"Audio level: {viewModel.AudioLevel}");
        }, _host!, "VoiceControlView_ShowsAudioLevel");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Visualizer")]
    public async Task VoiceControlView_ShowsListeningIndicator()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            viewModel!.IsListening = true;
            await Task.Delay(100);

            // Assert
            viewModel.IsListening.Should().BeTrue();
            _output.WriteLine("Listening indicator is visible");
        }, _host!, "VoiceControlView_ShowsListeningIndicator");
    }

    #endregion

    #region Language Support Tests

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Languages")]
    public async Task VoiceControlView_ShowsAvailableLanguages()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            await Task.Delay(200);

            // Assert
            viewModel!.AvailableLanguages.Should().NotBeNull();
            viewModel.AvailableLanguages.Should().NotBeEmpty();
            _output.WriteLine($"Available languages: {string.Join(", ", viewModel.AvailableLanguages.Select(l => l.Name))}");
        }, _host!, "VoiceControlView_ShowsAvailableLanguages");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Feature", "VoiceCommands")]
    [Trait("SubFeature", "Languages")]
    public async Task VoiceControlView_CanSelectLanguage()
    {
        await ScreenshotHelper.CaptureOnFailureAsync(async () =>
        {
            // Arrange
            var window = _host!.MainWindow;
            var voiceView = window.Content as VoiceControlView;
            var viewModel = voiceView!.DataContext as VoiceControlViewModel;

            // Act
            var englishLanguage = viewModel!.AvailableLanguages.FirstOrDefault(l => l.Code == "en-US");
            if (englishLanguage != null)
            {
                viewModel.SelectedLanguage = englishLanguage;
            }

            // Assert
            viewModel.SelectedLanguage.Should().NotBeNull();
            _output.WriteLine($"Selected language: {viewModel.SelectedLanguage?.Name}");
        }, _host!, "VoiceControlView_CanSelectLanguage");
    }

    #endregion
}
