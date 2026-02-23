using Microsoft.Extensions.DependencyInjection;
using SaveState.Presentation;
using SaveState.Presentation.ViewModels;
using SaveState.Presentation.ViewModels.Library;

namespace SaveState.Presentation.Tests;

/// <summary>
/// Provides a minimal DI container for presentation-layer tests that rely on <see cref="Locator"/>.
/// </summary>
internal static class PresentationTestLocator
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Ensures at least the game library view model is registered so navigation tests can resolve it.
    /// </summary>
    public static void EnsureGameLibraryRegistered()
    {
        if (_serviceProvider == null)
        {
            var services = new ServiceCollection();
            services.AddTransient<GameLibraryViewModel>(_ =>
                (GameLibraryViewModel)Activator.CreateInstance(typeof(GameLibraryViewModel), nonPublic: true)!);
            _serviceProvider = services.BuildServiceProvider();
        }

        // Reset Locator for each call to avoid cross-test contamination from other suites.
        Locator.Current.SetServices(_serviceProvider);
    }
}
