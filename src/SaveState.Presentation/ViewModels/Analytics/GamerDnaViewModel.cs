using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Analytics.Models.GamerProfile;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.UserManagement.Services;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Analytics;

/// <summary>
/// ViewModel for displaying and managing the Gaming DNA Profile.
/// Shows gamer archetype analysis, genre preferences, and evolution tracking.
/// </summary>
public partial class GamerDnaViewModel : ObservableObject, IDisposable
{
    private readonly IGamerDnaService _gamerDnaService;
    private readonly IUserContextService _userContextService;
    private readonly IDialogService _dialogService;
    private readonly IClipboardService _clipboardService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<GamerDnaViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private GamerDnaProfile? _currentProfile;

    [ObservableProperty]
    private GamerArchetype _primaryArchetype;

    [ObservableProperty]
    private string _primaryArchetypeName = string.Empty;

    [ObservableProperty]
    private string _primaryArchetypeDescription = string.Empty;

    [ObservableProperty]
    private string _primaryArchetypeIcon = "🎮";

    [ObservableProperty]
    private string _primaryArchetypeColor = "#808080";

    [ObservableProperty]
    private ObservableCollection<ArchetypeScoreViewModel> _archetypeScores = new();

    [ObservableProperty]
    private ObservableCollection<GenrePreferenceViewModel> _genrePreferences = new();

    [ObservableProperty]
    private ObservableCollection<PlatformPreferenceViewModel> _platformPreferences = new();

    [ObservableProperty]
    private PlaystyleMetricsViewModel? _playstyleMetrics;

    [ObservableProperty]
    private ObservableCollection<DnaEvolutionSnapshotViewModel> _evolutionHistory = new();

    [ObservableProperty]
    private bool _hasProfile;

    [ObservableProperty]
    private bool _showShareCard;

    [ObservableProperty]
    private ShareableProfileCard? _shareableCard;

    [ObservableProperty]
    private ProfileCardTheme _selectedTheme = ProfileCardTheme.Cyberpunk;

    [ObservableProperty]
    private ObservableCollection<ProfileCardTheme> _availableThemes = new();

    [ObservableProperty]
    private string _shareMessage = string.Empty;

    public GamerDnaViewModel(
        IGamerDnaService gamerDnaService,
        IUserContextService userContextService,
        IDialogService dialogService,
        IClipboardService clipboardService,
        INotificationService notificationService,
        ILogger<GamerDnaViewModel> logger,
        ITimeProvider timeProvider)
    {
        _gamerDnaService = gamerDnaService;
        _userContextService = userContextService;
        _dialogService = dialogService;
        _clipboardService = clipboardService;
        _notificationService = notificationService;
        _logger = logger;
        _timeProvider = timeProvider;

        // Initialize themes
        AvailableThemes = new ObservableCollection<ProfileCardTheme>(
            Enum.GetValues<ProfileCardTheme>());

        PrimaryArchetype = GamerArchetype.Casual;

        _ = LoadProfileAsync();
    }

    [RelayCommand]
    private async Task LoadProfileAsync()
    {
        IsLoading = true;

        try
        {
            var userId = _userContextService.CurrentUserId ?? Guid.Empty;
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("No current user found, cannot load DNA profile");
                return;
            }

            var result = await _gamerDnaService.AnalyzeProfileAsync(userId);

            if (result.IsFailure)
            {
                _notificationService.ShowError($"Failed to load profile: {result.Error}");
                return;
            }

            CurrentProfile = result.Value;
            HasProfile = true;

            UpdateProfileDisplay();
            _ = LoadEvolutionHistoryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading gamer DNA profile");
            _notificationService.ShowError("Failed to load gamer DNA profile");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshProfileAsync()
    {
        await LoadProfileAsync();
    }

    [RelayCommand]
    private async Task TakeQuizAsync()
    {
        try
        {
            // Get quiz questions
            var questionsResult = await _gamerDnaService.GetQuizQuestionsAsync();

            if (questionsResult.IsFailure)
            {
                _notificationService.ShowError($"Failed to load quiz: {questionsResult.Error}");
                return;
            }

            // For now, show a simple message about the quiz feature
            // In a full implementation, this would open a custom dialog
            await _dialogService.ShowInformationAsync(
                "Gamer Type Quiz",
                "The quiz feature will ask you 5 questions to determine your gamer archetype. " +
                "Your current profile shows your analyzed gaming habits!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing gamer type quiz");
            _notificationService.ShowError("Failed to load quiz");
        }
    }

    [RelayCommand]
    private async Task GenerateShareCardAsync()
    {
        if (CurrentProfile == null) return;

        try
        {
            var userId = _userContextService.CurrentUserId ?? Guid.Empty;
            var result = await _gamerDnaService.GenerateShareableCardAsync(
                userId,
                SelectedTheme);

            if (result.IsFailure)
            {
                _notificationService.ShowError($"Failed to generate card: {result.Error}");
                return;
            }

            ShareableCard = result.Value;
            ShowShareCard = true;

            ShareMessage = $"I'm a {ShareableCard.PrimaryArchetype.GetDisplayName()} " +
                          $"with {ShareableCard.KeyStats.GetValueOrDefault("Total Playtime", "0h")} of gaming! " +
                          $"Code: {ShareableCard.ShareCode}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating share card");
            _notificationService.ShowError("Failed to generate share card");
        }
    }

    [RelayCommand]
    private async Task CopyShareCodeAsync()
    {
        if (ShareableCard == null) return;

        try
        {
            await _clipboardService.SetTextAsync(ShareMessage);
            _notificationService.ShowSuccess("Share message copied to clipboard!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying share code");
            _notificationService.ShowError("Failed to copy share code");
        }
    }

    [RelayCommand]
    private void CloseShareCard()
    {
        ShowShareCard = false;
    }

    [RelayCommand]
    private async Task CompareWithFriendAsync()
    {
        _notificationService.ShowInfo("Friend comparison coming soon!");
        await Task.CompletedTask;
    }

    private async Task LoadEvolutionHistoryAsync()
    {
        try
        {
            var userId = _userContextService.CurrentUserId ?? Guid.Empty;
            if (userId == Guid.Empty) return;

            var result = await _gamerDnaService.GetEvolutionHistoryAsync(userId, 12);

            if (result.IsSuccess)
            {
                var viewModels = result.Value!.Select(s => new DnaEvolutionSnapshotViewModel
                {
                    Timestamp = s.Timestamp,
                    DominantArchetype = s.DominantArchetype,
                    DominantArchetypeName = s.DominantArchetype.GetDisplayName(),
                    DominantArchetypeIcon = s.DominantArchetype.GetIcon(),
                    TopGenres = s.TopGenres
                });

                EvolutionHistory = new ObservableCollection<DnaEvolutionSnapshotViewModel>(viewModels);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evolution history");
        }
    }

    private void UpdateProfileDisplay()
    {
        if (CurrentProfile == null) return;

        PrimaryArchetype = CurrentProfile.PrimaryArchetype;
        PrimaryArchetypeName = PrimaryArchetype.GetDisplayName();
        PrimaryArchetypeDescription = PrimaryArchetype.GetDescription();
        PrimaryArchetypeIcon = PrimaryArchetype.GetIcon();
        PrimaryArchetypeColor = PrimaryArchetype.GetPrimaryColor();

        // Update archetype scores
        ArchetypeScores = new ObservableCollection<ArchetypeScoreViewModel>(
            CurrentProfile.ArchetypeScores.Select(s => new ArchetypeScoreViewModel
            {
                Archetype = s.Key,
                Name = s.Key.GetDisplayName(),
                Icon = s.Key.GetIcon(),
                Score = s.Value,
                Percentage = s.Value * 100,
                IsPrimary = s.Key == PrimaryArchetype,
                Color = s.Key.GetPrimaryColor()
            }).OrderByDescending(s => s.Score));

        // Update genre preferences
        GenrePreferences = new ObservableCollection<GenrePreferenceViewModel>(
            CurrentProfile.GenrePreferences.Select(g => new GenrePreferenceViewModel
            {
                Genre = g.Genre,
                PreferenceScore = g.PreferenceScore,
                HoursPlayed = g.HoursPlayed,
                GamesPlayed = g.GamesPlayed,
                Trend = g.Trend,
                TrendIcon = g.Trend.GetIcon(),
                TrendColor = g.Trend.GetColor(),
                BarWidth = g.PreferenceScore * 100
            }));

        // Update platform preferences
        PlatformPreferences = new ObservableCollection<PlatformPreferenceViewModel>(
            CurrentProfile.PlatformPreferences.Select(p => new PlatformPreferenceViewModel
            {
                Platform = p.Platform,
                PreferenceScore = p.PreferenceScore,
                HoursPlayed = p.HoursPlayed,
                GamesOwned = p.GamesOwned,
                BarWidth = p.PreferenceScore * 100
            }));

        // Update playstyle metrics
        PlaystyleMetrics = new PlaystyleMetricsViewModel
        {
            AverageSessionLength = FormatTimeSpan(CurrentProfile.Playstyle.AverageSessionLength),
            AverageTimeToComplete = FormatTimeSpan(CurrentProfile.Playstyle.AverageTimeToComplete),
            CompletionRate = CurrentProfile.Playstyle.CompletionRate,
            CompletionRatePercentage = CurrentProfile.Playstyle.CompletionRate * 100,
            AchievementHunterScore = CurrentProfile.Playstyle.AchievementHunterScore,
            ReplayabilityScore = CurrentProfile.Playstyle.ReplayabilityScore,
            ReplayabilityPercentage = CurrentProfile.Playstyle.ReplayabilityScore * 100,
            MostActiveDay = CurrentProfile.Playstyle.MostActiveDay.ToString(),
            MostActiveTime = CurrentProfile.Playstyle.MostActiveTime.ToString(),
            TotalGamesOwned = CurrentProfile.Playstyle.TotalGamesOwned,
            TotalGamesCompleted = CurrentProfile.Playstyle.TotalGamesCompleted,
            TotalAchievementsUnlocked = CurrentProfile.Playstyle.TotalAchievementsUnlocked,
            TotalPlaytimeHours = CurrentProfile.Playstyle.TotalPlaytimeHours,
            TotalPlaytimeFormatted = $"{CurrentProfile.Playstyle.TotalPlaytimeHours:F0}h"
        };
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return $"{timeSpan.TotalHours:F1}h";
        return $"{timeSpan.TotalMinutes:F0}m";
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

/// <summary>
/// ViewModel for displaying an archetype score.
/// </summary>
public partial class ArchetypeScoreViewModel : ObservableObject
{
    [ObservableProperty]
    private GamerArchetype _archetype;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _icon = "🎮";

    [ObservableProperty]
    private float _score;

    [ObservableProperty]
    private double _percentage;

    [ObservableProperty]
    private bool _isPrimary;

    [ObservableProperty]
    private string _color = "#808080";
}

/// <summary>
/// ViewModel for displaying genre preference.
/// </summary>
public partial class GenrePreferenceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _genre = string.Empty;

    [ObservableProperty]
    private float _preferenceScore;

    [ObservableProperty]
    private int _hoursPlayed;

    [ObservableProperty]
    private int _gamesPlayed;

    [ObservableProperty]
    private SaveState.Core.Analytics.Models.GamerProfile.TrendDirection _trend;

    [ObservableProperty]
    private string _trendIcon = "➡️";

    [ObservableProperty]
    private string _trendColor = "#808080";

    [ObservableProperty]
    private double _barWidth;
}

/// <summary>
/// ViewModel for displaying platform preference.
/// </summary>
public partial class PlatformPreferenceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _platform = string.Empty;

    [ObservableProperty]
    private float _preferenceScore;

    [ObservableProperty]
    private int _hoursPlayed;

    [ObservableProperty]
    private int _gamesOwned;

    [ObservableProperty]
    private double _barWidth;
}

/// <summary>
/// ViewModel for displaying playstyle metrics.
/// </summary>
public partial class PlaystyleMetricsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _averageSessionLength = "0m";

    [ObservableProperty]
    private string _averageTimeToComplete = "0h";

    [ObservableProperty]
    private float _completionRate;

    [ObservableProperty]
    private double _completionRatePercentage;

    [ObservableProperty]
    private float _achievementHunterScore;

    [ObservableProperty]
    private float _replayabilityScore;

    [ObservableProperty]
    private double _replayabilityPercentage;

    [ObservableProperty]
    private string _mostActiveDay = "Unknown";

    [ObservableProperty]
    private string _mostActiveTime = "Unknown";

    [ObservableProperty]
    private int _totalGamesOwned;

    [ObservableProperty]
    private int _totalGamesCompleted;

    [ObservableProperty]
    private int _totalAchievementsUnlocked;

    [ObservableProperty]
    private float _totalPlaytimeHours;

    [ObservableProperty]
    private string _totalPlaytimeFormatted = "0h";
}

/// <summary>
/// ViewModel for displaying evolution snapshot.
/// </summary>
public partial class DnaEvolutionSnapshotViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime _timestamp;

    [ObservableProperty]
    private GamerArchetype _dominantArchetype;

    [ObservableProperty]
    private string _dominantArchetypeName = string.Empty;

    [ObservableProperty]
    private string _dominantArchetypeIcon = "🎮";

    [ObservableProperty]
    private IReadOnlyDictionary<string, float> _topGenres = new Dictionary<string, float>();
}
