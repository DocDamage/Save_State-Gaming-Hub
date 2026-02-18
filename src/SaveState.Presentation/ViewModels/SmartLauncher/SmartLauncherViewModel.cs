// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.Commands;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.SmartLauncher;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.SmartLauncher;

/// <summary>
/// ViewModel for the Smart Launcher page.
/// </summary>
public sealed partial class SmartLauncherViewModel : ObservableObject
{
    private readonly ISmartLauncherService _launcherService;
    private readonly IGameRepository _gameRepository;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private readonly IMediator _mediator;
    private readonly ILogger<SmartLauncherViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<GameLaunchViewModel> _games = new();

    [ObservableProperty]
    private ObservableCollection<LaunchProfileViewModel> _profiles = new();

    [ObservableProperty]
    private ObservableCollection<LaunchSessionViewModel> _recentSessions = new();

    [ObservableProperty]
    private LaunchSessionViewModel? _activeSession;

    [ObservableProperty]
    private LaunchProfileViewModel? _selectedProfile;

    [ObservableProperty]
    private GameLaunchViewModel? _selectedGame;

    [ObservableProperty]
    private bool _isLaunching;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _previewOptimizations = new();

    public SmartLauncherViewModel(
        ISmartLauncherService launcherService,
        IGameRepository gameRepository,
        IDialogService dialogService,
        INotificationService notificationService,
        IMediator mediator,
        ILogger<SmartLauncherViewModel> logger)
    {
        _launcherService = launcherService ?? throw new ArgumentNullException(nameof(launcherService));
        _gameRepository = gameRepository ?? throw new ArgumentNullException(nameof(gameRepository));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            // Load games
            var games = await _gameRepository.GetAllAsync();
            Games.Clear();
            foreach (var game in games)
            {
                Games.Add(new GameLaunchViewModel(game));
            }

            // Load profiles
            await LoadProfilesAsync();

            // Check active session
            await CheckActiveSessionAsync();

            // Load recent sessions
            await LoadRecentSessionsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load smart launcher data");
        }
    }

    [RelayCommand]
    private async Task LoadProfilesAsync()
    {
        try
        {
            var profiles = await _launcherService.GetProfilesAsync(null);
            Profiles.Clear();
            foreach (var profile in profiles)
            {
                Profiles.Add(new LaunchProfileViewModel(profile));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profiles");
        }
    }

    [RelayCommand]
    private async Task CheckActiveSessionAsync()
    {
        try
        {
            var sessionResult = await _launcherService.GetActiveSessionAsync();
            if (sessionResult.IsSuccess)
            {
                ActiveSession = new LaunchSessionViewModel(sessionResult.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check active session");
        }
    }

    [RelayCommand]
    private async Task LoadRecentSessionsAsync()
    {
        try
        {
            // Get recent sessions for the first game (as example)
            if (Games.FirstOrDefault() is { } firstGame)
            {
                var sessions = await _launcherService.GetLaunchHistoryAsync(firstGame.Id, 5);
                RecentSessions.Clear();
                foreach (var session in sessions)
                {
                    RecentSessions.Add(new LaunchSessionViewModel(session));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent sessions");
        }
    }

    [RelayCommand]
    private async Task LaunchGameAsync(GameLaunchViewModel? game)
    {
        if (game == null) return;
        if (ActiveSession != null)
        {
            StatusMessage = "⚠️ Another game is already running!";
            return;
        }

        IsLaunching = true;
        StatusMessage = $"🚀 Launching {game.Title}...";

        try
        {
            var profileId = SelectedProfile?.Id;
            var result = await _launcherService.LaunchGameAsync(game.Id, profileId);

            if (result.Success)
            {
                StatusMessage = $"✅ {game.Title} launched successfully!";
                if (result.EstimatedPerformanceGain.HasValue)
                {
                    StatusMessage += $" (Estimated +{result.EstimatedPerformanceGain}% performance)";
                }
                _notificationService?.ShowSuccess($"{game.Title} launched with optimizations!", "Game Launched");
                await CheckActiveSessionAsync();
            }
            else
            {
                StatusMessage = $"❌ Failed to launch: {result.ErrorMessage}";
                _notificationService?.ShowError(result.ErrorMessage ?? "Failed to launch game", "Launch Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch game");
            StatusMessage = $"❌ Error: {ex.Message}";
        }
        finally
        {
            IsLaunching = false;
        }
    }

    [RelayCommand]
    private async Task StopGameAsync()
    {
        if (ActiveSession == null) return;

        try
        {
            StatusMessage = "⏹️ Stopping game and restoring system...";
            var result = await _launcherService.EndSessionAsync(ActiveSession.Id);

            if (result.IsSuccess)
            {
                StatusMessage = "✅ Game stopped. System restored.";
                _notificationService?.ShowSuccess("Game stopped and system restored", "Session Ended");
                ActiveSession = null;
                await LoadRecentSessionsAsync();
            }
            else
            {
                StatusMessage = $"❌ Failed to stop: {result.Error}";
                _notificationService?.ShowError(result.Error ?? "Failed to stop game", "Stop Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop game");
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateProfileAsync()
    {
        try
        {
            var profile = LaunchProfile.CreateBalancedProfile();
            profile.Name = "New Profile";
            var result = await _launcherService.CreateProfileAsync(profile);

            if (result.IsSuccess)
            {
                await LoadProfilesAsync();
                SelectedProfile = Profiles.LastOrDefault();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create profile");
        }
    }

    [RelayCommand]
    private async Task PreviewOptimizationsAsync()
    {
        try
        {
            var optimizations = await _launcherService.PreviewOptimizationsAsync(SelectedProfile?.Id);
            PreviewOptimizations.Clear();
            foreach (var opt in optimizations)
            {
                PreviewOptimizations.Add(opt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview optimizations");
        }
    }

    partial void OnSelectedProfileChanged(LaunchProfileViewModel? value)
    {
        _ = PreviewOptimizationsAsync();
    }

    [RelayCommand]
    private async Task ConfigureGameExecutableAsync(GameLaunchViewModel? game)
    {
        if (game == null) return;

        try
        {
            // Get the full game entity
            var gameEntity = await _gameRepository.GetByIdAsync(GameId.From(game.Id));
            if (gameEntity == null) return;

            // Show the configuration dialog
            var result = await _dialogService.ShowGameExecutableConfigAsync(
                game.Id,
                game.Title,
                gameEntity.ExecutablePath,
                gameEntity.LaunchArguments);

            if (result != null)
            {
                // Update the game configuration using MediatR command
                var command = new UpdateGameLaunchConfigurationCommand
                {
                    GameId = GameId.From(game.Id),
                    ExecutablePath = result.ExecutablePath,
                    LaunchArguments = result.LaunchArguments
                };

                var updateResult = await _mediator.Send(command);
                if (updateResult.IsSuccess)
                {
                    StatusMessage = $"✅ Executable configured for {game.Title}";
                    await LoadDataAsync(); // Refresh the list
                }
                else
                {
                    StatusMessage = $"❌ Failed to configure: {updateResult.Error}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure game executable");
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task EditProfileAsync(LaunchProfileViewModel? profileVm)
    {
        if (profileVm == null) return;

        try
        {
            // Get the full profile
            var profileResult = await _launcherService.GetDefaultProfileAsync(profileVm.GameId ?? Guid.Empty);
            LaunchProfile profile;
            if (!profileResult.IsSuccess)
            {
                profile = LaunchProfile.CreateBalancedProfile();
                profile.Name = profileVm.Name;
            }
            else
            {
                profile = profileResult.Value;
            }

            // Show the profile editor dialog
            var result = await _dialogService.ShowLaunchProfileEditorAsync(profile);

            if (result != null)
            {
                // Update profile properties
                profile.Name = result.Name;
                profile.Description = result.Description;
                profile.Priority = result.Priority;
                profile.ProcessesToSuspend = result.ProcessesToSuspend;
                profile.PerformanceSettings.EnableMemoryOptimization = result.EnableMemoryOptimization;
                profile.PerformanceSettings.ClearStandbyList = result.ClearStandbyList;
                profile.PerformanceSettings.DisableVisualEffects = result.DisableVisualEffects;

                var updateResult = await _launcherService.UpdateProfileAsync(profile);
                if (updateResult.IsSuccess)
                {
                    await LoadProfilesAsync();
                    StatusMessage = $"✅ Profile '{profile.Name}' updated";
                }
                else
                {
                    StatusMessage = $"❌ Failed to update profile: {updateResult.Error}";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit profile");
            StatusMessage = $"❌ Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteProfileAsync(LaunchProfileViewModel? profile)
    {
        if (profile == null) return;

        try
        {
            var result = await _launcherService.DeleteProfileAsync(profile.Id);
            if (result.IsSuccess)
            {
                await LoadProfilesAsync();
                StatusMessage = $"🗑️ Profile '{profile.Name}' deleted";
            }
            else
            {
                StatusMessage = $"❌ Failed to delete profile: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete profile");
        }
    }
}

/// <summary>
/// ViewModel for a game in the launcher.
/// </summary>
public sealed class GameLaunchViewModel : ObservableObject
{
    private readonly Game _game;

    public GameLaunchViewModel(Game game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
    }

    public Guid Id => _game.Id;
    public string Title => _game.Title;
    public string? CoverImagePath => _game.CoverImagePath;
    public TimeSpan TotalPlayTime => _game.TotalPlayTime;
    public DateTime? LastPlayedAt => _game.LastPlayedAt;

    public string PlayTimeText => TotalPlayTime.TotalHours >= 1
        ? $"{TotalPlayTime.TotalHours:F1}h played"
        : $"{TotalPlayTime.TotalMinutes:F0}m played";

    public string LastPlayedText => LastPlayedAt.HasValue
        ? $"Last played {LastPlayedAt.Value:MMM dd}"
        : "Never played";
}

/// <summary>
/// ViewModel for a launch profile.
/// </summary>
public sealed class LaunchProfileViewModel : ObservableObject
{
    private readonly LaunchProfile _profile;

    public LaunchProfileViewModel(LaunchProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public Guid Id => _profile.Id;
    public string Name => _profile.Name;
    public string? Description => _profile.Description;
    public ProcessPriority Priority => _profile.Priority;
    public bool IsDefault => _profile.IsDefault;
    public int? EstimatedPerformanceGain => _profile.EstimatedPerformanceGain;
    public Guid? GameId => _profile.GameId;

    public string PriorityIcon => Priority switch
    {
        ProcessPriority.RealTime => "🔴",
        ProcessPriority.High => "🟠",
        ProcessPriority.AboveNormal => "🟡",
        _ => "⚪"
    };

    public string PerformanceGainText => EstimatedPerformanceGain.HasValue
        ? $"+{EstimatedPerformanceGain}%"
        : "";
}

/// <summary>
/// ViewModel for a launch session.
/// </summary>
public sealed class LaunchSessionViewModel : ObservableObject
{
    private readonly LaunchSession _session;

    public LaunchSessionViewModel(LaunchSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public Guid Id => _session.Id;
    public string GameName => _session.GameName;
    public DateTime StartedAt => _session.StartedAt;
    public DateTime? EndedAt => _session.EndedAt;
    public TimeSpan? Duration => _session.Duration;
    public bool IsActive => _session.IsActive;

    public string DurationText => Duration.HasValue
        ? $"{Duration.Value.Hours}h {Duration.Value.Minutes}m"
        : (IsActive ? "Running..." : "Unknown");

    public string StatusIcon => IsActive ? "🎮" : "✓";
}
