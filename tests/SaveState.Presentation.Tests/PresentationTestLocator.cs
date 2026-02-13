using Microsoft.Extensions.DependencyInjection;
using SaveState.Presentation;
using SaveState.Presentation.ViewModels;

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
        if (_serviceProvider != null)
        {
            return;
        }

        var services = new ServiceCollection();
        services.AddTransient<GameLibraryViewModel>(_ => new GameLibraryViewModel());
        _serviceProvider = services.BuildServiceProvider();
        Locator.Current.SetServices(_serviceProvider);
    }
}
