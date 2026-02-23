using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Core.Common.Services;
using SaveState.Presentation.Models.Data;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.ViewModels.Dialogs;
using SaveState.Presentation.ViewModels.Settings;

namespace SaveState.Presentation.Tests;

/// <summary>
/// Integration tests for Phase 2 UI Feature Surfacing.
/// Tests Performance Dashboard and Data Management ViewModels.
/// </summary>
public class UiSurfacingPhase2Tests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<PerformanceDashboardViewModel>> _performanceLoggerMock;
    private readonly Mock<ILogger<DataManagementViewModel>> _dataManagementLoggerMock;
    private readonly Mock<ILogger<ImportPreviewDialogViewModel>> _importPreviewLoggerMock;
    private readonly Mock<INavigationService> _navigationServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly Mock<ITimeProvider> _timeProviderMock;
    private readonly Mock<INotificationService> _notificationServiceMock;

    public UiSurfacingPhase2Tests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _performanceLoggerMock = new Mock<ILogger<PerformanceDashboardViewModel>>();
        _dataManagementLoggerMock = new Mock<ILogger<DataManagementViewModel>>();
        _importPreviewLoggerMock = new Mock<ILogger<ImportPreviewDialogViewModel>>();
        _navigationServiceMock = new Mock<INavigationService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _timeProviderMock = new Mock<ITimeProvider>();
        _notificationServiceMock = new Mock<INotificationService>();

        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns((string category) => Mock.Of<ILogger>());

        _loggerFactoryMock
            .Setup(x => x.CreateLogger(typeof(PerformanceDashboardViewModel).FullName!))
            .Returns(_performanceLoggerMock.Object);

        _loggerFactoryMock
            .Setup(x => x.CreateLogger(typeof(DataManagementViewModel).FullName!))
            .Returns(_dataManagementLoggerMock.Object);

        _timeProviderMock.Setup(x => x.Now).Returns(DateTime.Now);
        _timeProviderMock.Setup(x => x.UtcNow).Returns(DateTime.UtcNow);
    }

    #region Performance Dashboard Tests

    [Fact]
    public void PerformanceDashboardViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new PerformanceDashboardViewModel(
            _timeProviderMock.Object,
            null,  // performanceService
            null,  // systemResourceManager
            null,  // performanceMonitor
            null,  // cacheMonitor
            null,  // applicationMetrics
            null,  // errorTrackingService
            _notificationServiceMock.Object,
            _dialogServiceMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.GameStats.Should().NotBeNull();
        viewModel.GameStats.Should().NotBeEmpty();
        viewModel.Recommendations.Should().NotBeNull();
        viewModel.Recommendations.Should().NotBeEmpty();
    }

    [Fact]
    public void PerformanceDashboardViewModel_HasGameStats()
    {
        // Arrange & Act
        var viewModel = new PerformanceDashboardViewModel(
            _timeProviderMock.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            _notificationServiceMock.Object,
            _dialogServiceMock.Object);

        // Assert
        viewModel.GameStats.Should().HaveCountGreaterThan(0);
        viewModel.GameStats[0].GameName.Should().NotBeNullOrEmpty();
        viewModel.GameStats[0].AverageFps.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PerformanceDashboardViewModel_HasRecommendations()
    {
        // Arrange & Act
        var viewModel = new PerformanceDashboardViewModel(
            _timeProviderMock.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            _notificationServiceMock.Object,
            _dialogServiceMock.Object);

        // Assert
        viewModel.Recommendations.Should().HaveCountGreaterThan(0);
        viewModel.HasCriticalRecommendations.Should().BeTrue();
    }

    [Fact]
    public void PerformanceDashboardViewModel_RefreshCommand_Exists()
    {
        // Arrange
        var viewModel = new PerformanceDashboardViewModel(
            _timeProviderMock.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            _notificationServiceMock.Object,
            _dialogServiceMock.Object);

        // Act & Assert
        viewModel.RefreshCommand.Should().NotBeNull();
    }

    [Fact]
    public void PerformanceDashboardViewModel_RunBenchmarkCommand_Exists()
    {
        // Arrange
        var viewModel = new PerformanceDashboardViewModel(
            _timeProviderMock.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            _notificationServiceMock.Object,
            _dialogServiceMock.Object);

        // Act & Assert
        viewModel.RunBenchmarkCommand.Should().NotBeNull();
    }

    [Fact]
    public void PerformanceDashboardViewModel_ToggleRealTimeMonitoringCommand_Exists()
    {
        // Arrange
        var viewModel = new PerformanceDashboardViewModel(
            _timeProviderMock.Object,
            null,
            null,
            null,
            null,
            null,
            null,
            _notificationServiceMock.Object,
            _dialogServiceMock.Object);

        // Act & Assert
        viewModel.ToggleRealTimeMonitoringCommand.Should().NotBeNull();
    }

    #endregion

    #region Game Performance Detail Tests

    [Fact]
    public void GamePerformanceDetailViewModel_InitializesCorrectly()
    {
        // Arrange
        var gameStats = new GamePerformanceStats
        {
            GameId = Guid.NewGuid(),
            GameName = "Test Game",
            AverageFps = 60,
            MinFps = 45,
            MaxFps = 75,
            TotalPlaytime = TimeSpan.FromHours(10),
            SessionCount = 5
        };

        // Act
        var viewModel = new GamePerformanceDetailViewModel(
            _timeProviderMock.Object,
            gameStats,
            null,
            null,
            null,
            null,
            _notificationServiceMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.GameName.Should().Be("Test Game");
        viewModel.OverallAverageFps.Should().Be(60);
        viewModel.Sessions.Should().NotBeNull();
        viewModel.Sessions.Should().NotBeEmpty();
    }

    [Fact]
    public void GamePerformanceDetailViewModel_HasOptimizations()
    {
        // Arrange
        var gameStats = new GamePerformanceStats
        {
            GameId = Guid.NewGuid(),
            GameName = "Test Game",
            AverageFps = 60
        };

        // Act
        var viewModel = new GamePerformanceDetailViewModel(
            _timeProviderMock.Object,
            gameStats,
            null,
            null,
            null,
            null,
            _notificationServiceMock.Object);

        // Assert
        viewModel.Optimizations.Should().HaveCountGreaterThan(0);
    }

    #endregion

    #region Data Management Tests

    [Fact]
    public void DataManagementViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new DataManagementViewModel(
            _notificationServiceMock.Object,
            _dialogServiceMock.Object,
            _timeProviderMock.Object,
            null);  // dataManagementService

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.AvailableBackups.Should().NotBeNull();
        viewModel.AvailableBackups.Should().NotBeEmpty();
        viewModel.ExportOptions.Should().NotBeNull();
    }

    [Fact]
    public void DataManagementViewModel_ExportCommand_Exists()
    {
        // Arrange
        var viewModel = new DataManagementViewModel(
            _notificationServiceMock.Object,
            _dialogServiceMock.Object,
            _timeProviderMock.Object,
            null);

        // Act & Assert
        viewModel.ExportCommand.Should().NotBeNull();
    }

    [Fact]
    public void DataManagementViewModel_ImportCommand_Exists()
    {
        // Arrange
        var viewModel = new DataManagementViewModel(
            _notificationServiceMock.Object,
            _dialogServiceMock.Object,
            _timeProviderMock.Object,
            null);

        // Act & Assert
        viewModel.ImportCommand.Should().NotBeNull();
    }

    [Fact]
    public void DataManagementViewModel_CreateBackupCommand_Exists()
    {
        // Arrange
        var viewModel = new DataManagementViewModel(
            _notificationServiceMock.Object,
            _dialogServiceMock.Object,
            _timeProviderMock.Object,
            null);

        // Act & Assert
        viewModel.CreateBackupCommand.Should().NotBeNull();
    }

    [Fact]
    public void DataManagementViewModel_RestoreBackupCommand_Exists()
    {
        // Arrange
        var viewModel = new DataManagementViewModel(
            _notificationServiceMock.Object,
            _dialogServiceMock.Object,
            _timeProviderMock.Object,
            null);

        // Act & Assert
        viewModel.RestoreBackupCommand.Should().NotBeNull();
    }

    [Fact]
    public void DataManagementViewModel_PreviewImportCommand_Exists()
    {
        // Arrange
        var viewModel = new DataManagementViewModel(
            _notificationServiceMock.Object,
            _dialogServiceMock.Object,
            _timeProviderMock.Object,
            null);

        // Act & Assert
        viewModel.PreviewImportCommand.Should().NotBeNull();
    }

    #endregion

    #region Import Preview Dialog Tests

    [Fact]
    public void ImportPreviewDialogViewModel_InitializesCorrectly()
    {
        // Arrange & Act
        var viewModel = new ImportPreviewDialogViewModel(
            _dialogServiceMock.Object,
            _notificationServiceMock.Object,
            _timeProviderMock.Object);

        // Assert
        viewModel.Should().NotBeNull();
        viewModel.Conflicts.Should().NotBeNull();
    }

    [Fact]
    public void ImportPreviewDialogViewModel_Initialize_PopulatesPreview()
    {
        // Arrange
        var viewModel = new ImportPreviewDialogViewModel(
            _dialogServiceMock.Object,
            _notificationServiceMock.Object,
            _timeProviderMock.Object);

        var preview = new ImportPreview
        {
            GamesToAdd = 10,
            GamesToUpdate = 5,
            SaveStatesToImport = 20,
            AchievementsToImport = 50,
            Conflicts = 2,
            ConflictDetails = new List<ImportConflict>
            {
                new()
                {
                    ItemId = "game_001",
                    ItemName = "Test Game",
                    ItemType = "Game",
                    FieldName = "Playtime",
                    CurrentValue = "10 hours",
                    ImportedValue = "12 hours",
                    SelectedResolution = ConflictResolution.KeepCurrent
                }
            }
        };

        // Act
        viewModel.Initialize("test_import.json", preview);

        // Assert
        viewModel.ImportFileName.Should().Be("test_import.json");
        viewModel.Preview.Should().NotBeNull();
        viewModel.Preview!.GamesToAdd.Should().Be(10);
        viewModel.Conflicts.Should().HaveCount(1);
    }

    [Fact]
    public void ImportPreviewDialogViewModel_ChangeStrategy_UpdatesDestructiveFlag()
    {
        // Arrange
        var viewModel = new ImportPreviewDialogViewModel(
            _dialogServiceMock.Object,
            _notificationServiceMock.Object,
            _timeProviderMock.Object);

        var preview = new ImportPreview
        {
            GamesToAdd = 10,
            GamesToUpdate = 5,
            Conflicts = 1,
            ConflictDetails = new List<ImportConflict> { new() }
        };

        viewModel.Initialize("test.json", preview);

        // Act
        viewModel.ChangeStrategyCommand.Execute(ImportStrategy.Replace);

        // Assert
        viewModel.SelectedStrategy.Should().Be(ImportStrategy.Replace);
        viewModel.IsDestructive.Should().BeTrue();
    }

    [Fact]
    public void ImportPreviewDialogViewModel_ResolveAllConflicts_UpdatesAllResolutions()
    {
        // Arrange
        var viewModel = new ImportPreviewDialogViewModel(
            _dialogServiceMock.Object,
            _notificationServiceMock.Object,
            _timeProviderMock.Object);

        var preview = new ImportPreview
        {
            Conflicts = 2,
            ConflictDetails = new List<ImportConflict>
            {
                new() { ItemId = "1", SelectedResolution = ConflictResolution.KeepCurrent },
                new() { ItemId = "2", SelectedResolution = ConflictResolution.KeepCurrent }
            }
        };

        viewModel.Initialize("test.json", preview);

        // Act
        viewModel.ResolveAllConflictsCommand.Execute(ConflictResolution.UseImported);

        // Assert
        viewModel.Conflicts.Should().OnlyContain(c => c.SelectedResolution == ConflictResolution.UseImported);
    }

    [Fact]
    public void ImportPreviewDialogViewModel_CancelCommand_Exists()
    {
        // Arrange
        var viewModel = new ImportPreviewDialogViewModel(
            _dialogServiceMock.Object,
            _notificationServiceMock.Object,
            _timeProviderMock.Object);

        // Act & Assert
        viewModel.CancelCommand.Should().NotBeNull();
    }

    [Fact]
    public void ImportPreviewDialogViewModel_ConfirmImportCommand_Exists()
    {
        // Arrange
        var viewModel = new ImportPreviewDialogViewModel(
            _dialogServiceMock.Object,
            _notificationServiceMock.Object,
            _timeProviderMock.Object);

        // Act & Assert
        viewModel.ConfirmImportCommand.Should().NotBeNull();
    }

    [Fact]
    public void ImportConflictViewModel_InitializesFromConflict()
    {
        // Arrange
        var conflict = new ImportConflict
        {
            ItemId = "game_001",
            ItemName = "Test Game",
            ItemType = "Game",
            FieldName = "Playtime",
            CurrentValue = "10 hours",
            ImportedValue = "12 hours",
            SelectedResolution = ConflictResolution.KeepCurrent
        };

        // Act
        var viewModel = new ImportConflictViewModel(conflict);

        // Assert
        viewModel.ItemId.Should().Be("game_001");
        viewModel.ItemName.Should().Be("Test Game");
        viewModel.CurrentValue.Should().Be("10 hours");
        viewModel.ImportedValue.Should().Be("12 hours");
        viewModel.SelectedResolution.Should().Be(ConflictResolution.KeepCurrent);
    }

    #endregion

    #region Navigation Service Extension Tests

    [Fact]
    public async Task NavigationServiceExtensions_ShowPerformanceDashboard_CallsNavigateWithCorrectTab()
    {
        // Arrange
        var navigationService = new Mock<INavigationService>();

        // Act
        await navigationService.Object.ShowPerformanceDashboardAsync();

        // Assert
        navigationService.Verify(x => x.NavigateToAsync("PerformanceDashboard"), Times.Once);
    }

    [Fact]
    public async Task NavigationServiceExtensions_ShowDataManagement_CallsNavigateWithCorrectTab()
    {
        // Arrange
        var navigationService = new Mock<INavigationService>();

        // Act
        await navigationService.Object.ShowDataManagementAsync();

        // Assert
        navigationService.Verify(x => x.NavigateToAsync("DataManagement"), Times.Once);
    }

    #endregion

    #region Settings ViewModel Integration Tests

    [Fact]
    public void SettingsViewModel_HasPerformanceDashboardCommand()
    {
        // Arrange & Act
        var viewModel = CreateSettingsViewModel();

        // Assert
        viewModel.ShowPerformanceDashboardCommand.Should().NotBeNull();
    }

    [Fact]
    public void SettingsViewModel_HasDataManagementCommand()
    {
        // Arrange & Act
        var viewModel = CreateSettingsViewModel();

        // Assert
        viewModel.ShowDataManagementCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task SettingsViewModel_ShowPerformanceDashboard_InvokesNavigationService()
    {
        // Arrange
        var viewModel = CreateSettingsViewModel();

        // Act
        await viewModel.ShowPerformanceDashboardCommand.ExecuteAsync(null);

        // Assert
        _navigationServiceMock.Verify(x => x.NavigateToAsync("PerformanceDashboard"), Times.Once);
    }

    [Fact]
    public async Task SettingsViewModel_ShowDataManagement_InvokesNavigationService()
    {
        // Arrange
        var viewModel = CreateSettingsViewModel();

        // Act
        await viewModel.ShowDataManagementCommand.ExecuteAsync(null);

        // Assert
        _navigationServiceMock.Verify(x => x.NavigateToAsync("DataManagement"), Times.Once);
    }

    #endregion

    #region Dialog Service Integration Tests

    [Fact]
    public async Task DialogService_ShowGamePerformanceDetail_ReturnsWithoutError()
    {
        // Arrange
        var gameStats = new GamePerformanceStats
        {
            GameId = Guid.NewGuid(),
            GameName = "Test Game",
            AverageFps = 60
        };

        _dialogServiceMock
            .Setup(x => x.ShowGamePerformanceDetailAsync(It.IsAny<GamePerformanceStats>()))
            .Returns(Task.CompletedTask);

        // Act
        await _dialogServiceMock.Object.ShowGamePerformanceDetailAsync(gameStats);

        // Assert
        _dialogServiceMock.Verify(x => x.ShowGamePerformanceDetailAsync(gameStats), Times.Once);
    }

    [Fact]
    public async Task DialogService_ShowImportPreview_ReturnsResultWhenConfirmed()
    {
        // Arrange
        var preview = new ImportPreview
        {
            GamesToAdd = 5,
            GamesToUpdate = 2,
            Conflicts = 1
        };

        var expectedResult = new ImportPreviewResult(
            "test.json",
            ImportStrategy.Merge,
            new Dictionary<string, ConflictResolution>(),
            5, 2, 10, 20);

        _dialogServiceMock
            .Setup(x => x.ShowImportPreviewAsync(preview, null))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _dialogServiceMock.Object.ShowImportPreviewAsync(preview);

        // Assert
        result.Should().NotBeNull();
        result!.SelectedStrategy.Should().Be(ImportStrategy.Merge);
        result.GamesToAdd.Should().Be(5);
    }

    [Fact]
    public async Task DialogService_ShowImportPreview_ReturnsNullWhenCancelled()
    {
        // Arrange
        var preview = new ImportPreview();

        _dialogServiceMock
            .Setup(x => x.ShowImportPreviewAsync(preview, null))
            .ReturnsAsync((ImportPreviewResult?)null);

        // Act
        var result = await _dialogServiceMock.Object.ShowImportPreviewAsync(preview);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private SettingsViewModel CreateSettingsViewModel()
    {
        var cultureManagerMock = new Mock<SaveState.Core.Common.Services.ICultureManager>();
        var resources = new SaveState.Presentation.Resources.Resources();
        var themeServiceMock = new Mock<IThemeService>();
        var aiOrchestratorMock = new Mock<SaveState.Core.Ai.Services.IAiOrchestrator>();
        var preferencesServiceMock = new Mock<IUserPreferencesService>();

        cultureManagerMock.Setup(x => x.CurrentCulture).Returns(System.Globalization.CultureInfo.CurrentCulture);
        cultureManagerMock.Setup(x => x.SupportedCultures).Returns(new[] { System.Globalization.CultureInfo.CurrentCulture });
        themeServiceMock.Setup(x => x.CurrentTheme).Returns(ThemeType.Dark);
        themeServiceMock.Setup(x => x.AvailableThemes).Returns(new[] { ThemeType.Light, ThemeType.Dark, ThemeType.System });

        var systemHealthVm = new SystemHealthViewModel(
            _timeProviderMock.Object,
            null,
            _dialogServiceMock.Object,
            _notificationServiceMock.Object);

        var connectedAccountsVm = new ConnectedAccountsViewModel(
            _notificationServiceMock.Object,
            null,
            null,
            _dialogServiceMock.Object);

        var dataManagementVm = new DataManagementViewModel(
            _notificationServiceMock.Object,
            _dialogServiceMock.Object,
            _timeProviderMock.Object,
            null);

        var aiAdminVm = new AiAdministrationViewModel(
            Mock.Of<ILogger<AiAdministrationViewModel>>(),
            Mock.Of<IDialogService>(),
            Mock.Of<SaveState.Core.Ai.Services.IAiOrchestrator>(),
            Mock.Of<SaveState.Core.Common.Services.IUserPreferencesService>());

        var audioOptimizationVm = new AudioOptimizationViewModel();

        var voiceControlVm = new Shell.VoiceControlViewModel(
            Mock.Of<ILogger<Shell.VoiceControlViewModel>>(),
            Mock.Of<SaveState.Core.Input.Services.IVoiceCommandService>(),
            Mock.Of<INotificationService>());

        return new SettingsViewModel(
            cultureManagerMock.Object,
            resources,
            themeServiceMock.Object,
            aiOrchestratorMock.Object,
            preferencesServiceMock.Object,
            _dialogServiceMock.Object,
            voiceControlVm,
            audioOptimizationVm,
            systemHealthVm,
            connectedAccountsVm,
            dataManagementVm,
            aiAdminVm,
            _navigationServiceMock.Object);
    }

    #endregion
}
