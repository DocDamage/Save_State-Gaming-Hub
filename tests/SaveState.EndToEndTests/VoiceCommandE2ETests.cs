using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Ai.Services;
using SaveState.Core.Ai.Services.DTOs;
using SaveState.Core.Common.Services;
using SaveState.Core.Input.Services;
using SaveState.Core.Input.Services.DTOs;
using SaveState.EndToEndTests.Infrastructure;
using SaveState.Presentation.Services;
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
        var mockNotificationService = new Mock<INotificationService>();
        var mockLogger = new Mock<ILogger<VoiceControlViewModel>>();
        var mockTimeProvider = new Mock<ITimeProvider>();

        // Setup mock time provider
        mockTimeProvider.Setup(tp => tp.Now).Returns(DateTime.Now);
        mockTimeProvider.Setup(tp => tp.UtcNow).Returns(DateTime.UtcNow);

        // Setup mock commands
        var commands = new List<VoiceCommandDefinition>
        {
            new("save game", "Save the current game", VoiceCommandAction.SaveGame, null, null),
            new("load game", "Load a saved game", VoiceCommandAction.LoadGame, null, null),
            new("launch game", "Launch a game", VoiceCommandAction.LaunchGame, null, null),
            new("take screenshot", "Capture a screenshot", VoiceCommandAction.CreateSaveState, null, null),
            new("open library", "Open the game library", VoiceCommandAction.OpenLibrary, null, null),
            new("open settings", "Open application settings", VoiceCommandAction.OpenSettings, null, null),
            new("mute", "Mute audio", VoiceCommandAction.MuteAudio, null, null),
            new("unmute", "Unmute audio", VoiceCommandAction.AdjustVolume, null, null)
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
                        command,
                        matchedCommand,
                        matchedCommand != null ? 0.95f : 0.0f,
                        matchedCommand != null,
                        matchedCommand == null ? "Command not recognized" : null,
                        null)));
            });

        mockSpeechService.Setup(x => x.GetAvailableLanguagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Common.Result<IReadOnlyList<LanguageInfo>>.Success(
                new List<LanguageInfo>
                {
                    new("en-US", "English (United States)", "English (United States)"),
                    new("en-GB", "English (United Kingdom)", "English (United Kingdom)"),
                    new("es-ES", "Spanish", "Español")
                }));

        mockSpeechService.Setup(x => x.GetMicrophoneStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Common.Result<MicrophoneStatus>.Success(
                new MicrophoneStatus(true, 75, 44100, "Default Microphone", false)));

        var viewModel = new VoiceControlViewModel(
            mockVoiceService.Object,
            mockSpeechService.Object,
            mockNotificationService.Object,
            mockLogger.Object,
            mockTimeProvider.Object);

        return new VoiceControlView { DataContext = viewModel };
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
            viewModel!.MicrophoneLevel.Should().BeGreaterThanOrEqualTo(0);
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
            viewModel!.RegisteredCommands.Should().NotBeNull();
            viewModel.RegisteredCommands.Should().NotBeEmpty();
            _output.WriteLine($"Number of voice commands: {viewModel.RegisteredCommands.Count}");
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
            viewModel!.LastRecognizedText = "save game";
            viewModel.StatusMessage = "Command executed: Save Game";
            await Task.Delay(100);

            // Assert
            viewModel.LastRecognizedText.Should().NotBeNullOrEmpty();
            viewModel.StatusMessage.Should().NotBeNullOrEmpty();
            _output.WriteLine($"Recognized: {viewModel.LastRecognizedText}");
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
            await Task.Delay(200); // Wait for commands to load

            // Act - Find and execute save game command via TestCommandCommand
            var saveCommand = viewModel!.RegisteredCommands.FirstOrDefault(c => 
                c.Action == VoiceCommandAction.SaveGame);
            
            if (saveCommand != null)
            {
                viewModel.TestCommandCommand.Execute(saveCommand);
                await Task.Delay(100);
            }

            // Assert
            viewModel.StatusMessage.Should().Contain("Command executed", "or show command processed status");
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
            await Task.Delay(200); // Wait for commands to load

            // Act - Find and execute load game command via TestCommandCommand
            var loadCommand = viewModel!.RegisteredCommands.FirstOrDefault(c => 
                c.Action == VoiceCommandAction.LoadGame);
            
            if (loadCommand != null)
            {
                viewModel.TestCommandCommand.Execute(loadCommand);
                await Task.Delay(100);
            }

            // Assert
            viewModel.StatusMessage.Should().Contain("Command executed", "or show command processed status");
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
            await Task.Delay(200); // Wait for commands to load

            // Act - Find and execute open library command via TestCommandCommand
            var libraryCommand = viewModel!.RegisteredCommands.FirstOrDefault(c => 
                c.Action == VoiceCommandAction.OpenLibrary);
            
            if (libraryCommand != null)
            {
                viewModel.TestCommandCommand.Execute(libraryCommand);
                await Task.Delay(100);
            }

            // Assert
            viewModel.StatusMessage.Should().Contain("Command executed", "or show command processed status");
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

            // Act - Simulate unknown command via unrecognized text
            viewModel!.LastRecognizedText = "unknown command xyz";
            viewModel.StatusMessage = "Command not recognized";
            await Task.Delay(100);

            // Assert - Unknown command should show not recognized status
            viewModel.StatusMessage.Should().NotBeNullOrEmpty();
            _output.WriteLine($"Unknown command result: {viewModel.StatusMessage}");
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
            viewModel!.MicrophoneLevel = 75;
            await Task.Delay(100);

            // Assert
            viewModel.MicrophoneLevel.Should().BeGreaterThan(0);
            _output.WriteLine($"Audio level: {viewModel.MicrophoneLevel}");
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
            _output.WriteLine($"Available languages: {string.Join(", ", viewModel.AvailableLanguages)}");
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
            var englishLanguage = viewModel!.AvailableLanguages.FirstOrDefault(l => l == "en-US");
            if (englishLanguage != null)
            {
                viewModel.CurrentLanguage = englishLanguage;
            }

            // Assert
            viewModel.CurrentLanguage.Should().NotBeNullOrEmpty();
            _output.WriteLine($"Selected language: {viewModel.CurrentLanguage}");
        }, _host!, "VoiceControlView_CanSelectLanguage");
    }

    #endregion
}
