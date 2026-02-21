using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for configuring the launch experience settings.
/// </summary>
public partial class LaunchExperienceConfigDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _enableCinematicLaunch = true;

    [ObservableProperty]
    private int _animationDurationSeconds = 10;

    [ObservableProperty]
    private bool _showAiBriefing = true;

    [ObservableProperty]
    private bool _showTips = true;

    [ObservableProperty]
    private bool _showLastSessionSummary = true;

    [ObservableProperty]
    private string _backgroundStyle = "Game art";

    /// <summary>
    /// Available animation duration options in seconds.
    /// </summary>
    public List<int> DurationOptions { get; } = new() { 5, 10, 15 };

    /// <summary>
    /// Available background style options.
    /// </summary>
    public List<string> BackgroundStyleOptions { get; } = new()
    {
        "Game art",
        "Solid color",
        "Animated"
    };

    /// <summary>
    /// Saves the configuration settings.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        // Save to user preferences - implementation depends on settings service
        OnSaveRequested();
    }

    /// <summary>
    /// Cancels the configuration dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        // Close dialog without saving
        OnCancelRequested();
    }

    /// <summary>
    /// Raised when save is requested.
    /// </summary>
    public event EventHandler? SaveRequested;

    /// <summary>
    /// Raised when cancel is requested.
    /// </summary>
    public event EventHandler? CancelRequested;

    private void OnSaveRequested() => SaveRequested?.Invoke(this, EventArgs.Empty);
    private void OnCancelRequested() => CancelRequested?.Invoke(this, EventArgs.Empty);
}
