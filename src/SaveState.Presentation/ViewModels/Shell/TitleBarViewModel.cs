using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reflection;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the title bar.
/// </summary>
public partial class TitleBarViewModel : ObservableObject
{
    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string AppName => "SaveState Reborn";

    /// <summary>
    /// Gets the application version.
    /// </summary>
    public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "2.0.0";

    /// <summary>
    /// Command to minimize the window.
    /// </summary>
    [RelayCommand]
    private void Minimize()
    {
        var window = Avalonia.Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;

        if (window?.MainWindow is { } mainWindow)
        {
            mainWindow.WindowState = Avalonia.Controls.WindowState.Minimized;
        }
    }

    /// <summary>
    /// Command to maximize or restore the window.
    /// </summary>
    [RelayCommand]
    private void Maximize()
    {
        var window = Avalonia.Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;

        if (window?.MainWindow is { } mainWindow)
        {
            mainWindow.WindowState = mainWindow.WindowState == Avalonia.Controls.WindowState.Maximized
                ? Avalonia.Controls.WindowState.Normal
                : Avalonia.Controls.WindowState.Maximized;
        }
    }

    /// <summary>
    /// Command to close the application.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        var window = Avalonia.Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;

        window?.Shutdown();
    }
}