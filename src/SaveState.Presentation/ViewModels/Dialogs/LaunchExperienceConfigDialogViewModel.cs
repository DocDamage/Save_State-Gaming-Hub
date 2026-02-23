using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.GameLibrary.Services.DTOs;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for configuring the launch experience settings with cinematic animations.
/// </summary>
public sealed partial class LaunchExperienceConfigDialogViewModel : ObservableObject
{
    #region Observable Properties

    [ObservableProperty]
    private bool _enableCinematicLaunch = true;

    [ObservableProperty]
    private AnimationDuration _selectedDuration = AnimationDuration.Medium;

    [ObservableProperty]
    private bool _showAiBriefing = true;

    [ObservableProperty]
    private bool _showTips = true;

    [ObservableProperty]
    private bool _showLastSessionSummary = true;

    [ObservableProperty]
    private bool _showAchievements = true;

    [ObservableProperty]
    private bool _showPlaytime = true;

    [ObservableProperty]
    private bool _allowSkip = true;

    [ObservableProperty]
    private BackgroundStyle _selectedBackgroundStyle = BackgroundStyle.GameArt;

    /// <summary>
    /// Design-time constructor.
    /// </summary>
    public LaunchExperienceConfigDialogViewModel()
    {
        // Design-time initialization with default values
    }

    #endregion

    #region Collections

    /// <summary>
    /// Available animation duration options.
    /// </summary>
    public ObservableCollection<AnimationDurationOption> DurationOptions { get; } = new()
    {
        new AnimationDurationOption(AnimationDuration.Short, "Short (5 seconds)", 5),
        new AnimationDurationOption(AnimationDuration.Medium, "Medium (10 seconds)", 10),
        new AnimationDurationOption(AnimationDuration.Long, "Long (15 seconds)", 15),
        new AnimationDurationOption(AnimationDuration.Manual, "Manual (press key to dismiss)", 0)
    };

    /// <summary>
    /// Available background style options.
    /// </summary>
    public ObservableCollection<BackgroundStyleOption> BackgroundStyleOptions { get; } = new()
    {
        new BackgroundStyleOption(BackgroundStyle.GameArt, "Game Art", "Use the game's cover art as background"),
        new BackgroundStyleOption(BackgroundStyle.SolidColor, "Solid Color", "Use a dark solid color background"),
        new BackgroundStyleOption(BackgroundStyle.Animated, "Animated", "Use an animated gradient background")
    };

    #endregion

    #region Commands

    /// <summary>
    /// Saves the configuration settings.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        var settings = CreateSettings();
        OnSaveRequested(settings);
    }

    /// <summary>
    /// Cancels the configuration dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        OnCancelRequested();
    }

    /// <summary>
    /// Resets all settings to defaults.
    /// </summary>
    [RelayCommand]
    private void ResetToDefaults()
    {
        EnableCinematicLaunch = true;
        SelectedDuration = AnimationDuration.Medium;
        ShowAiBriefing = true;
        ShowTips = true;
        ShowLastSessionSummary = true;
        ShowAchievements = true;
        ShowPlaytime = true;
        AllowSkip = true;
        SelectedBackgroundStyle = BackgroundStyle.GameArt;
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when save is requested with the configured settings.
    /// </summary>
    public event EventHandler<LaunchExperienceSettings>? SaveRequested;

    /// <summary>
    /// Raised when cancel is requested.
    /// </summary>
    public event EventHandler? CancelRequested;

    private void OnSaveRequested(LaunchExperienceSettings settings) 
        => SaveRequested?.Invoke(this, settings);
    
    private void OnCancelRequested() 
        => CancelRequested?.Invoke(this, EventArgs.Empty);

    #endregion

    #region Public Methods

    /// <summary>
    /// Creates a LaunchExperienceSettings object from the current view model state.
    /// </summary>
    public LaunchExperienceSettings CreateSettings()
    {
        return new LaunchExperienceSettings
        {
            IsEnabled = EnableCinematicLaunch,
            Duration = SelectedDuration,
            ShowAiBriefing = ShowAiBriefing,
            ShowTips = ShowTips,
            ShowLastSession = ShowLastSessionSummary,
            ShowAchievements = ShowAchievements,
            ShowPlaytime = ShowPlaytime,
            AllowSkip = AllowSkip,
            BackgroundStyle = SelectedBackgroundStyle
        };
    }

    /// <summary>
    /// Loads settings into the view model.
    /// </summary>
    public void LoadSettings(LaunchExperienceSettings settings)
    {
        EnableCinematicLaunch = settings.IsEnabled;
        SelectedDuration = settings.Duration;
        ShowAiBriefing = settings.ShowAiBriefing;
        ShowTips = settings.ShowTips;
        ShowLastSessionSummary = settings.ShowLastSession;
        ShowAchievements = settings.ShowAchievements;
        ShowPlaytime = settings.ShowPlaytime;
        AllowSkip = settings.AllowSkip;
        SelectedBackgroundStyle = settings.BackgroundStyle;
    }

    #endregion
}

/// <summary>
/// Represents an animation duration option for the UI.
/// </summary>
public sealed record AnimationDurationOption
{
    public AnimationDuration Value { get; }
    public string DisplayName { get; }
    public int Seconds { get; }

    public AnimationDurationOption(AnimationDuration value, string displayName, int seconds)
    {
        Value = value;
        DisplayName = displayName;
        Seconds = seconds;
    }

    public override string ToString() => DisplayName;
}

/// <summary>
/// Represents a background style option for the UI.
/// </summary>
public sealed record BackgroundStyleOption
{
    public BackgroundStyle Value { get; }
    public string DisplayName { get; }
    public string Description { get; }

    public BackgroundStyleOption(BackgroundStyle value, string displayName, string description)
    {
        Value = value;
        DisplayName = displayName;
        Description = description;
    }

    public override string ToString() => DisplayName;
}
