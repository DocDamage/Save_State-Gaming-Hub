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
        if (_serviceProvider != null)
        {
            return;
        }

        var services = new ServiceCollection();
        // Note: GameLibraryViewModel has complex dependencies and required members.
        // Tests that need it should provide their own mock or use the full DI container.
        _serviceProvider = services.BuildServiceProvider();
        Locator.Current.SetServices(_serviceProvider);
    }
}
