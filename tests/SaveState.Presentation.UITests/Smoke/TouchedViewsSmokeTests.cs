using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.RetroArch.Commands;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Automation.Models;
using SaveState.Core.Automation.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Models.Recommendations;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Health.Models;
using SaveState.Core.Health.Services;
using SaveState.Core.RetroArch;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Automation;
using SaveState.Presentation.ViewModels.GameLibrary;
using SaveState.Presentation.ViewModels.Health;
using SaveState.Presentation.ViewModels.RetroArch;
using SaveState.Presentation.Views.Automation;
using SaveState.Presentation.Views.GameLibrary;
using SaveState.Presentation.Views.Health;
using SaveState.Presentation.Views.RetroArch;

// Assembly-level attribute to ensure Avalonia is initialized with the main App
// which contains all the converters and resources
[assembly: AvaloniaTestApplication(typeof(SaveState.Presentation.UITests.Smoke.TestApp))]

namespace SaveState.Presentation.UITests.Smoke;

/// <summary>
/// Test application for UI smoke tests.
/// Provides the AppBuilder that initializes the full application with all resources.
/// </summary>
public static class TestApp
{
    public static AppBuilder BuildAvaloniaApp() => TestAppBuilder.BuildAvaloniaApp();
}

/// <summary>
/// Smoke checks for views touched by Avalonia XAML remediation.
/// </summary>
public class TouchedViewsSmokeTests : HeadlessTestBase
{
    [AvaloniaFact]
    public async Task WorkflowEditorView_Smoke_InteractiveFlow_Works()
    {
        var automationService = new Mock<IAutomationStudioService>();
        var logger = new Mock<ILogger<WorkflowEditorViewModel>>();

        automationService
            .Setup(x => x.GetWorkflowsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<Workflow>>.Success(new[] { CreateWorkflow("Night Session") }));
        automationService
            .Setup(x => x.GetWorkflowTemplatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<WorkflowTemplate>>.Success(new[]
            {
                new WorkflowTemplate
                {
                    Id = Guid.NewGuid(),
                    Name = "Quick Start",
                    Description = "Basic smoke template.",
                    Category = "General",
                    Icon = "Q",
                    DefaultTrigger = AutomationTrigger.GameLaunched,
                    DefaultActions = new[] { AutomationAction.SendNotification }
                }
            }));
        automationService
            .Setup(x => x.GetAvailableTriggersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<AutomationTrigger>>.Success(new[] { AutomationTrigger.GameLaunched }));
        automationService
            .Setup(x => x.GetAvailableActionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<AutomationAction>>.Success(new[] { AutomationAction.SendNotification }));
        automationService
            .Setup(x => x.ValidateWorkflowAsync(It.IsAny<Workflow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<WorkflowValidationResult>.Success(WorkflowValidationResult.Success()));
        automationService
            .Setup(x => x.CreateWorkflowAsync(It.IsAny<Workflow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Workflow workflow, CancellationToken _) => Result<Workflow>.Success(workflow));

        var viewModel = new WorkflowEditorViewModel(automationService.Object, SystemTimeProvider.Instance, logger.Object);
        await WaitForAsync(() => viewModel.Workflows.Count > 0 && viewModel.Templates.Count > 0);

        var view = new WorkflowEditorView
        {
            DataContext = viewModel
        };
        LayoutControl(view);

        view.DataContext.Should().Be(viewModel);
        viewModel.CreateNewWorkflowCommand.Execute(null);
        viewModel.IsEditing.Should().BeTrue();

        viewModel.AddActionCommand.Execute(AutomationAction.SendNotification);
        viewModel.EditingWorkflow.Should().NotBeNull();
        viewModel.EditingWorkflow!.Actions.Count.Should().Be(1);

        await viewModel.SaveWorkflowCommand.ExecuteAsync(null);
        automationService.Verify(x => x.CreateWorkflowAsync(It.IsAny<Workflow>(), It.IsAny<CancellationToken>()), Times.Once);
        viewModel.ErrorMessage.Should().BeNull();
    }

    [AvaloniaFact]
    public async Task RecommendationsView_Smoke_InteractiveFlow_Works()
    {
        var recommendationEngine = new Mock<IRecommendationEngineV2>();
        var sessionTracking = new Mock<ISessionTrackingService>();
        var gamerDna = new Mock<IGamerDnaService>();
        var preferences = new Mock<IUserPreferencesService>();

        sessionTracking
            .Setup(x => x.GetAllActiveSessionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<GameSession>>.Success(Array.Empty<GameSession>()));

        recommendationEngine
            .Setup(x => x.GetRecommendationsAsync(It.IsAny<RecommendationContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<GameRecommendation>>.Success(new[] { CreateRecommendation("Hades", RecommendationReason.MoodMatch, 0.92f) }));
        recommendationEngine
            .Setup(x => x.GetPlayNextAsync(It.IsAny<PlayNextContext>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<GameRecommendation>>.Success(new[] { CreateRecommendation("Dead Cells", RecommendationReason.CompletionSuggestion, 0.88f) }));
        recommendationEngine
            .Setup(x => x.GetTrendingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<GameRecommendation>>.Success(new[] { CreateRecommendation("Balatro", RecommendationReason.Trending, 0.95f) }));
        recommendationEngine
            .Setup(x => x.GetHiddenGemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<GameRecommendation>>.Success(new[] { CreateRecommendation("CrossCode", RecommendationReason.HiddenGem, 0.90f) }));

        var viewModel = new SmartRecommendationsViewModel(
            recommendationEngine.Object,
            SystemTimeProvider.Instance,
            sessionTracking.Object,
            gamerDna.Object,
            preferences.Object);

        var view = new RecommendationsView
        {
            DataContext = viewModel
        };
        LayoutControl(view);

        viewModel.SetMultiplayerCommand.Execute(null);
        viewModel.PlayerCount.Should().Be(2);
        viewModel.ClearMoodCommand.Execute(null);

        await viewModel.LoadRecommendationsCommand.ExecuteAsync(null);
        await viewModel.LoadPlayNextCommand.ExecuteAsync(null);
        await viewModel.LoadTrendingCommand.ExecuteAsync(null);
        await viewModel.LoadHiddenGemsCommand.ExecuteAsync(null);

        viewModel.ErrorMessage.Should().BeNull();
        viewModel.Recommendations.Should().NotBeEmpty();
        viewModel.PlayNext.Should().NotBeEmpty();
        viewModel.Trending.Should().NotBeEmpty();
        viewModel.HiddenGems.Should().NotBeEmpty();
    }

    [AvaloniaFact]
    public async Task PlaylistView_Smoke_InteractiveFlow_Works()
    {
        var mediator = new Mock<IMediator>();
        var dialogService = new Mock<IDialogService>();
        var notificationService = new Mock<INotificationService>();
        var logger = new Mock<ILogger<RetroArchPlaylistViewModel>>();

        dialogService
            .Setup(x => x.ShowInputDialogAsync("Create Playlist", "Enter a name for the new playlist:", "My Playlist", false))
            .ReturnsAsync("Smoke Playlist");
        mediator
            .Setup(x => x.Send(It.IsAny<CreatePlaylistCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("Smoke Playlist"));
        mediator
            .Setup(x => x.Send(It.IsAny<RemoveGameFromPlaylistCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var viewModel = new RetroArchPlaylistViewModel(
            mediator.Object,
            dialogService.Object,
            logger.Object,
            notificationService.Object);

        var playlist = new RetroArchPlaylist
        {
            Name = "Favorites",
            Path = "favorites.lpl",
            GameCount = 1,
            Games = new List<RetroArchGame>
            {
                new()
                {
                    Id = "game-1",
                    Title = "Super Metroid",
                    Path = "C:/roms/snes/super_metroid.smc",
                    System = "SNES"
                }
            }
        };
        viewModel.Playlists.Add(playlist);

        var view = new PlaylistView
        {
            DataContext = viewModel
        };
        LayoutControl(view);

        viewModel.SelectPlaylistCommand.Execute(playlist);
        viewModel.SelectedPlaylist.Should().Be(playlist);

        var originalGridState = viewModel.IsGridView;
        viewModel.ToggleViewCommand.Execute(null);
        viewModel.IsGridView.Should().Be(!originalGridState);

        await viewModel.RemoveGameFromPlaylistCommand.ExecuteAsync(playlist.Games[0]);
        await viewModel.CreatePlaylistCommand.ExecuteAsync(null);

        mediator.Verify(x => x.Send(It.IsAny<RemoveGameFromPlaylistCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(x => x.Send(It.IsAny<CreatePlaylistCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [AvaloniaFact]
    public async Task HealthMonitorView_Smoke_InteractiveFlow_Works()
    {
        var healthService = new Mock<IGamingHealthMonitorService>();
        var overlayService = new Mock<IOverlayService>();

        var snapshot = CreateSnapshot();
        var alert = HealthAlert.Create("Posture", "Please sit up straighter.", AlertSeverity.Warning, SystemTimeProvider.Instance.UtcNow);

        healthService.SetupGet(x => x.IsMonitoring).Returns(false);
        healthService.SetupGet(x => x.CurrentConfig).Returns(HealthMonitoringConfig.Default);
        healthService
            .Setup(x => x.StartMonitoringAsync(It.IsAny<HealthMonitoringConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        healthService
            .Setup(x => x.StopMonitoringAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        healthService
            .Setup(x => x.GetCurrentStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<HealthSnapshot>.Success(snapshot));
        healthService
            .Setup(x => x.GetActiveAlertsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<IReadOnlyList<HealthAlert>>.Success(new[] { alert }));
        healthService
            .Setup(x => x.AcknowledgeAlertAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        healthService
            .Setup(x => x.RecordBreakTakenAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        healthService
            .Setup(x => x.UpdatePostureAsync(It.IsAny<PostureStatus>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        using var viewModel = new HealthMonitorViewModel(healthService.Object, overlayService.Object, SystemTimeProvider.Instance);
        var view = new HealthMonitorView
        {
            DataContext = viewModel
        };
        LayoutControl(view);

        await viewModel.StartMonitoringCommand.ExecuteAsync(null);
        viewModel.IsMonitoring.Should().BeTrue();
        viewModel.HealthScore.Should().BeGreaterThan(0);
        viewModel.HasActiveAlerts.Should().BeTrue();

        viewModel.ToggleSettingsCommand.Execute(null);
        viewModel.ShowSettings.Should().BeTrue();

        await viewModel.TakeEyeBreakCommand.ExecuteAsync(null);
        await viewModel.SimulatePostureCommand.ExecuteAsync("critical");

        var alertId = viewModel.ActiveAlerts[0].Id;
        await viewModel.AcknowledgeAlertCommand.ExecuteAsync(alertId);
        viewModel.ActiveAlerts.Should().BeEmpty();

        viewModel.CloseCommand.Execute(null);
        overlayService.Verify(x => x.HideAllOverlays(), Times.Once);
    }

    private static async Task WaitForAsync(Func<bool> predicate, int timeoutMs = 4000)
    {
        var started = DateTime.UtcNow;
        while (!predicate())
        {
            if ((DateTime.UtcNow - started).TotalMilliseconds > timeoutMs)
            {
                throw new TimeoutException("Smoke test timed out while waiting for expected state.");
            }

            await Task.Delay(25);
        }
    }

    private static void LayoutControl(Control control)
    {
        control.Measure(new Size(1600, 1000));
        control.Arrange(new Rect(control.DesiredSize));
    }

    private static Workflow CreateWorkflow(string name)
    {
        return new Workflow
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Smoke workflow",
            IsEnabled = true,
            Trigger = AutomationTrigger.GameLaunched,
            TriggerConfig = TriggerConfiguration.Default(),
            Actions = new[]
            {
                WorkflowAction.Create(AutomationAction.SendNotification)
            },
            CreatedAt = SystemTimeProvider.Instance.UtcNow,
            LastExecuted = null,
            ExecutionCount = 0
        };
    }

    private static GameRecommendation CreateRecommendation(string title, RecommendationReason reason, float score)
    {
        return new GameRecommendation
        {
            GameId = Guid.NewGuid(),
            GameTitle = title,
            Score = score,
            Reason = reason,
            Factors = new[] { "Smoke test factor" },
            CoverImageUrl = null,
            EstimatedPlaytime = TimeSpan.FromMinutes(45),
            Confidence = 0.8f
        };
    }

    private static HealthSnapshot CreateSnapshot()
    {
        return new HealthSnapshot
        {
            Posture = PostureStatus.Good,
            EyeStrain = EyeStrainLevel.Low,
            HeartRate = null,
            SessionDuration = TimeSpan.FromMinutes(35),
            BreakReminderCount = 1,
            HealthScore = 83f,
            Focus = FocusLevel.High,
            Stress = StressLevel.Normal,
            Fatigue = FatigueLevel.Alert,
            Timestamp = SystemTimeProvider.Instance.UtcNow,
            TimeUntilNextBreak = TimeSpan.FromMinutes(20),
            TimeSinceLastEyeBreak = TimeSpan.FromMinutes(10)
        };
    }
}
