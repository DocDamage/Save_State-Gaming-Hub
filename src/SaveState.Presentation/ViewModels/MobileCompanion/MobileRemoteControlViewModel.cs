using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.Mobile;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.MobileCompanion;

/// <summary>
/// ViewModel for the mobile remote control interface.
/// Supports multiple control modes: Gamepad, Touchpad, Media, and Keyboard.
/// </summary>
public partial class MobileRemoteControlViewModel : ObservableObject
{
    private readonly ILogger<MobileRemoteControlViewModel> _logger;
    private readonly IRemoteControlService? _remoteService;

    [ObservableProperty]
    private RemoteControlMode _currentMode = RemoteControlMode.Gamepad;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _currentGame = string.Empty;

    [ObservableProperty]
    private double _touchpadX;

    [ObservableProperty]
    private double _touchpadY;

    [ObservableProperty]
    private bool _isTouching;

    [ObservableProperty]
    private double _volumeLevel = 50;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private string _trackTitle = string.Empty;

    [ObservableProperty]
    private string _trackArtist = string.Empty;

    [ObservableProperty]
    private ObservableCollection<RemoteButton> _gamepadButtons = new();

    [ObservableProperty]
    private bool _isKeyboardVisible;

    [ObservableProperty]
    private string _keyboardInput = string.Empty;

    [ObservableProperty]
    private bool _hapticFeedbackEnabled = true;

    public MobileRemoteControlViewModel(
        ILogger<MobileRemoteControlViewModel> logger,
        IRemoteControlService? remoteService = null)
    {
        _logger = logger;
        _remoteService = remoteService;
        InitializeGamepadButtons();
    }

    /// <summary>
    /// Initializes the gamepad button layout
    /// </summary>
    private void InitializeGamepadButtons()
    {
        GamepadButtons = new ObservableCollection<RemoteButton>
        {
            new() { Label = "LB", Code = "LeftBumper", Position = ButtonPosition.LeftTrigger },
            new() { Label = "RB", Code = "RightBumper", Position = ButtonPosition.RightTrigger },
            new() { Label = "LT", Code = "LeftTrigger", Position = ButtonPosition.LeftTriggerLower },
            new() { Label = "RT", Code = "RightTrigger", Position = ButtonPosition.RightTriggerLower },
            new() { Label = "L-Stick", Code = "LeftStick", Position = ButtonPosition.LeftStick },
            new() { Label = "R-Stick", Code = "RightStick", Position = ButtonPosition.RightStick },
            new() { Label = "A", Code = "A", Position = ButtonPosition.ABXY },
            new() { Label = "B", Code = "B", Position = ButtonPosition.ABXY },
            new() { Label = "X", Code = "X", Position = ButtonPosition.ABXY },
            new() { Label = "Y", Code = "Y", Position = ButtonPosition.ABXY },
            new() { Label = "←", Code = "DpadLeft", Position = ButtonPosition.DPad },
            new() { Label = "→", Code = "DpadRight", Position = ButtonPosition.DPad },
            new() { Label = "↑", Code = "DpadUp", Position = ButtonPosition.DPad },
            new() { Label = "↓", Code = "DpadDown", Position = ButtonPosition.DPad },
            new() { Label = "Home", Code = "Home", Position = ButtonPosition.Center },
            new() { Label = "Menu", Code = "Menu", Position = ButtonPosition.Center },
            new() { Label = "View", Code = "View", Position = ButtonPosition.Center }
        };
    }

    /// <summary>
    /// Sends a button press event to the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task SendButtonPressAsync(string? button)
    {
        if (string.IsNullOrEmpty(button)) return;

        try
        {
            _logger.LogDebug("Button pressed: {Button}", button);

            if (HapticFeedbackEnabled)
            {
                TriggerHapticFeedback();
            }

            if (_remoteService is not null)
            {
                await _remoteService.SendButtonPressAsync(button);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send button press");
        }
    }

    /// <summary>
    /// Sends axis input (thumbstick) to the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task SendAxisInputAsync(string? axisInfo)
    {
        if (string.IsNullOrEmpty(axisInfo)) return;

        try
        {
            var parts = axisInfo.Split(',');
            if (parts.Length >= 3)
            {
                var axis = parts[0];
                var x = double.Parse(parts[1]);
                var y = double.Parse(parts[2]);

                _logger.LogDebug("Axis input: {Axis} ({X}, {Y})", axis, x, y);

                if (_remoteService is not null)
                {
                    await _remoteService.SendAxisInputAsync(axis, x, y);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send axis input");
        }
    }

    /// <summary>
    /// Sends touch input to the gaming hub (mouse emulation)
    /// </summary>
    [RelayCommand]
    private async Task SendTouchAsync(TouchpadInput? touch)
    {
        if (touch is null) return;

        try
        {
            TouchpadX = touch.X;
            TouchpadY = touch.Y;
            IsTouching = touch.IsPressed;

            if (_remoteService is not null)
            {
                await _remoteService.SendTouchInputAsync(touch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send touch input");
        }
    }

    /// <summary>
    /// Handles touchpad tap (mouse click)
    /// </summary>
    [RelayCommand]
    private async Task SendTouchTapAsync(string? button)
    {
        if (string.IsNullOrEmpty(button)) return;

        try
        {
            _logger.LogDebug("Touchpad tap: {Button}", button);

            if (HapticFeedbackEnabled)
            {
                TriggerHapticFeedback();
            }

            if (_remoteService is not null)
            {
                await _remoteService.SendMouseClickAsync(button);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send touch tap");
        }
    }

    /// <summary>
    /// Media play command
    /// </summary>
    [RelayCommand]
    private async Task MediaPlayAsync()
    {
        try
        {
            IsPlaying = true;
            _logger.LogDebug("Media play");

            if (_remoteService is not null)
            {
                await _remoteService.SendMediaCommandAsync("play");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send media play");
        }
    }

    /// <summary>
    /// Media pause command
    /// </summary>
    [RelayCommand]
    private async Task MediaPauseAsync()
    {
        try
        {
            IsPlaying = false;
            _logger.LogDebug("Media pause");

            if (_remoteService is not null)
            {
                await _remoteService.SendMediaCommandAsync("pause");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send media pause");
        }
    }

    /// <summary>
    /// Media previous track command
    /// </summary>
    [RelayCommand]
    private async Task MediaPreviousAsync()
    {
        try
        {
            _logger.LogDebug("Media previous");

            if (HapticFeedbackEnabled)
            {
                TriggerHapticFeedback();
            }

            if (_remoteService is not null)
            {
                await _remoteService.SendMediaCommandAsync("previous");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send media previous");
        }
    }

    /// <summary>
    /// Media next track command
    /// </summary>
    [RelayCommand]
    private async Task MediaNextAsync()
    {
        try
        {
            _logger.LogDebug("Media next");

            if (HapticFeedbackEnabled)
            {
                TriggerHapticFeedback();
            }

            if (_remoteService is not null)
            {
                await _remoteService.SendMediaCommandAsync("next");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send media next");
        }
    }

    /// <summary>
    /// Volume up command
    /// </summary>
    [RelayCommand]
    private async Task VolumeUpAsync()
    {
        try
        {
            VolumeLevel = Math.Min(100, VolumeLevel + 5);
            _logger.LogDebug("Volume up: {Volume}", VolumeLevel);

            if (_remoteService is not null)
            {
                await _remoteService.SetVolumeAsync(VolumeLevel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adjust volume");
        }
    }

    /// <summary>
    /// Volume down command
    /// </summary>
    [RelayCommand]
    private async Task VolumeDownAsync()
    {
        try
        {
            VolumeLevel = Math.Max(0, VolumeLevel - 5);
            _logger.LogDebug("Volume down: {Volume}", VolumeLevel);

            if (_remoteService is not null)
            {
                await _remoteService.SetVolumeAsync(VolumeLevel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to adjust volume");
        }
    }

    /// <summary>
    /// Mute toggle command
    /// </summary>
    [RelayCommand]
    private async Task MuteToggleAsync()
    {
        try
        {
            _logger.LogDebug("Mute toggle");

            if (HapticFeedbackEnabled)
            {
                TriggerHapticFeedback();
            }

            if (_remoteService is not null)
            {
                await _remoteService.SendMediaCommandAsync("mute");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle mute");
        }
    }

    /// <summary>
    /// Switches between remote control modes
    /// </summary>
    [RelayCommand]
    private void SwitchModeAsync(string? mode)
    {
        if (string.IsNullOrEmpty(mode)) return;

        try
        {
            if (Enum.TryParse<RemoteControlMode>(mode, out var newMode))
            {
                CurrentMode = newMode;
                _logger.LogInformation("Switched to {Mode} mode", newMode);

                if (HapticFeedbackEnabled)
                {
                    TriggerHapticFeedback();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch mode");
        }
    }

    /// <summary>
    /// Shows the on-screen keyboard
    /// </summary>
    [RelayCommand]
    private void ShowKeyboardAsync()
    {
        IsKeyboardVisible = true;
        _logger.LogDebug("Keyboard shown");
    }

    /// <summary>
    /// Hides the on-screen keyboard
    /// </summary>
    [RelayCommand]
    private void HideKeyboardAsync()
    {
        IsKeyboardVisible = false;
        KeyboardInput = string.Empty;
        _logger.LogDebug("Keyboard hidden");
    }

    /// <summary>
    /// Sends keyboard input to the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task SendKeyboardInputAsync()
    {
        try
        {
            if (!string.IsNullOrEmpty(KeyboardInput))
            {
                _logger.LogDebug("Sending keyboard input: {Input}", KeyboardInput);

                if (_remoteService is not null)
                {
                    await _remoteService.SendKeyboardInputAsync(KeyboardInput);
                }

                KeyboardInput = string.Empty;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send keyboard input");
        }
    }

    /// <summary>
    /// Takes a screenshot on the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task TakeScreenshotAsync()
    {
        try
        {
            _logger.LogDebug("Taking screenshot");

            if (HapticFeedbackEnabled)
            {
                TriggerHapticFeedback();
            }

            if (_remoteService is not null)
            {
                await _remoteService.SendCommandAsync("screenshot");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take screenshot");
        }
    }

    /// <summary>
    /// Starts/stops recording on the gaming hub
    /// </summary>
    [RelayCommand]
    private async Task ToggleRecordingAsync()
    {
        try
        {
            _logger.LogDebug("Toggling recording");

            if (HapticFeedbackEnabled)
            {
                TriggerHapticFeedback();
            }

            if (_remoteService is not null)
            {
                await _remoteService.SendCommandAsync("toggleRecording");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle recording");
        }
    }

    /// <summary>
    /// Activates voice command mode
    /// </summary>
    [RelayCommand]
    private async Task ActivateVoiceCommandAsync()
    {
        try
        {
            _logger.LogDebug("Activating voice command");

            if (HapticFeedbackEnabled)
            {
                TriggerHapticFeedback();
            }

            // FUTURE: Implement voice recognition when speech recognition service is available
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to activate voice command");
        }
    }

    /// <summary>
    /// Navigates back to the dashboard
    /// </summary>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        try
        {
            _logger.LogDebug("Navigating back to dashboard");
            // Navigation would happen here
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate back");
        }
    }

    /// <summary>
    /// Triggers haptic feedback on the mobile device
    /// </summary>
    private void TriggerHapticFeedback()
    {
        // FUTURE: Implement haptic feedback using platform-specific APIs
        // iOS: UIImpactFeedbackGenerator, Android: Vibrator when mobile native layer is added
    }
}

/// <summary>
/// Represents a button on the virtual gamepad
/// </summary>
public partial class RemoteButton : ObservableObject
{
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private ButtonPosition _position;
    [ObservableProperty] private bool _isPressed;
}

/// <summary>
/// Button position enum for layout purposes
/// </summary>
public enum ButtonPosition
{
    LeftTrigger,
    RightTrigger,
    LeftTriggerLower,
    RightTriggerLower,
    LeftStick,
    RightStick,
    ABXY,
    DPad,
    Center
}

/// <summary>
/// Service interface for remote control operations
/// </summary>
public interface IRemoteControlService
{
    Task SendButtonPressAsync(string button);
    Task SendAxisInputAsync(string axis, double x, double y);
    Task SendTouchInputAsync(TouchpadInput touch);
    Task SendMouseClickAsync(string button);
    Task SendMediaCommandAsync(string command);
    Task SetVolumeAsync(double level);
    Task SendKeyboardInputAsync(string text);
    Task SendCommandAsync(string command);
}
