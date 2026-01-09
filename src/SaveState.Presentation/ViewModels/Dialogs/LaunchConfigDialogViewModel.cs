using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for configuring game launch options.
/// </summary>
public partial class LaunchConfigDialogViewModel : ObservableObject
{
    private readonly ILogger<LaunchConfigDialogViewModel> _logger;

    [ObservableProperty]
    private Guid _gameId;

    [ObservableProperty]
    private string _launchArguments = string.Empty;

    [ObservableProperty]
    private bool _useCustomResolution;

    [ObservableProperty]
    private int _width = 1920;

    [ObservableProperty]
    private int _height = 1080;

    [ObservableProperty]
    private bool _startInFullScreen;

    [ObservableProperty]
    private bool _enableVsync = true;

    [ObservableProperty]
    private bool _skipIntro;

    [ObservableProperty]
    private string _workingDirectory = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _commonResolutions = new()
    {
        "1920x1080 (Full HD)",
        "2560x1440 (2K)",
        "3840x2160 (4K)",
        "1280x720 (HD)",
        "1600x900 (HD+)"
    };

    [ObservableProperty]
    private string? _selectedResolution;

    public LaunchConfigDialogViewModel(ILogger<LaunchConfigDialogViewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the dialog with current launch configuration.
    /// </summary>
    public void Initialize(Guid gameId, string? currentArguments = null)
    {
        GameId = gameId;
        LaunchArguments = currentArguments ?? string.Empty;

        // Parse existing arguments if any
        ParseArguments(currentArguments);
    }

    private void ParseArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
            return;

        // Simple parsing of common arguments
        if (arguments.Contains("-fullscreen"))
            StartInFullScreen = true;

        if (arguments.Contains("-skipintro"))
            SkipIntro = true;

        if (arguments.Contains("-novsynch"))
            EnableVsync = false;

        // Parse resolution if present
        var resMatch = System.Text.RegularExpressions.Regex.Match(arguments, @"-width\s+(\d+).*-height\s+(\d+)");
        if (resMatch.Success)
        {
            UseCustomResolution = true;
            if (int.TryParse(resMatch.Groups[1].Value, out var w))
                Width = w;
            if (int.TryParse(resMatch.Groups[2].Value, out var h))
                Height = h;
        }
    }

    partial void OnSelectedResolutionChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        // Parse resolution string like "1920x1080 (Full HD)"
        var match = System.Text.RegularExpressions.Regex.Match(value, @"(\d+)x(\d+)");
        if (match.Success)
        {
            if (int.TryParse(match.Groups[1].Value, out var w))
                Width = w;
            if (int.TryParse(match.Groups[2].Value, out var h))
                Height = h;
            UseCustomResolution = true;
        }
    }

    partial void OnUseCustomResolutionChanged(bool value)
    {
        if (!value)
        {
            SelectedResolution = null;
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        LaunchArguments = string.Empty;
        UseCustomResolution = false;
        Width = 1920;
        Height = 1080;
        StartInFullScreen = false;
        EnableVsync = true;
        SkipIntro = false;
        WorkingDirectory = string.Empty;
        SelectedResolution = null;

        _logger.LogInformation("Launch configuration reset to defaults for game {GameId}", GameId);
    }

    [RelayCommand]
    private void Save()
    {
        // Build final launch arguments string
        var args = LaunchArguments;

        if (UseCustomResolution)
        {
            if (!args.Contains("-width"))
                args += $" -width {Width} -height {Height}";
        }

        if (StartInFullScreen)
        {
            if (!args.Contains("-fullscreen"))
                args += " -fullscreen";
        }

        if (!EnableVsync)
        {
             if (!args.Contains("-novsync"))
                args += " -novsync";
        }

        if (SkipIntro)
        {
            if (!args.Contains("-skipintro"))
                args += " -skipintro";
        }

        LaunchArguments = args.Trim();

        _logger.LogInformation(
            "Saving launch configuration for game {GameId}: {Arguments}",
            GameId,
            LaunchArguments);

        var result = new LaunchConfigResult(
            LaunchArguments,
            UseCustomResolution,
            UseCustomResolution ? Width : null,
            UseCustomResolution ? Height : null,
            StartInFullScreen);

        // Close dialog with result
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _logger.LogDebug("Launch configuration cancelled");

        // Close dialog without result
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(null);
        }
    }
}
