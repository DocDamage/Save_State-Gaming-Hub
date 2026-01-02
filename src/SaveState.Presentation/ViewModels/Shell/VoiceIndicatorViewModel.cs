using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the voice indicator overlay.
/// </summary>
public partial class VoiceIndicatorViewModel : ObservableObject
{
    private string _statusText = "Listening...";
    private double _volumeLevel;

    /// <summary>
    /// Gets the status text.
    /// </summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>
    /// Gets the current volume level (0.0 to 1.0).
    /// </summary>
    public double VolumeLevel
    {
        get => _volumeLevel;
        private set => SetProperty(ref _volumeLevel, value);
    }

    /// <summary>
    /// Updates the voice indicator status.
    /// </summary>
    /// <param name="isListening">Whether voice recognition is actively listening.</param>
    /// <param name="volumeLevel">The current input volume level.</param>
    /// <param name="lastCommand">The last recognized command (optional).</param>
    public void UpdateStatus(bool isListening, double volumeLevel, string? lastCommand = null)
    {
        VolumeLevel = Math.Clamp(volumeLevel, 0.0, 1.0);

        if (!isListening)
        {
            StatusText = "Voice recognition stopped";
        }
        else if (!string.IsNullOrEmpty(lastCommand))
        {
            StatusText = $"Recognized: {lastCommand}";
        }
        else
        {
            StatusText = "Listening...";
        }
    }
}