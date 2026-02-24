namespace SaveState.Presentation;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.Views;
using System.Linq;

/// <summary>
/// Main Avalonia application class for SaveState.
/// Handles application lifecycle, UI initialization, and service setup.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the Avalonia application and loads XAML resources.
    /// Called during application startup before framework initialization.
    /// </summary>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Called when Avalonia framework initialization is complete.
    /// Sets up the main window, view models, and disables duplicate data validation.
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        Dispatcher.UIThread.UnhandledException += (_, eventArgs) =>
        {
            Log.Error(eventArgs.Exception, "Unhandled UI thread exception");
            eventArgs.Handled = true;
        };

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            var viewModel = Locator.Current.GetService<SaveState.Presentation.ViewModels.Shell.MainShellViewModel>()!;

            var mainShell = new Views.Shell.MainShell
            {
                DataContext = viewModel
            };

            desktop.MainWindow = mainShell;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
