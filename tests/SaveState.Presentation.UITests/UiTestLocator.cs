namespace SaveState.Presentation.UITests;

using Microsoft.Extensions.DependencyInjection;
using SaveState.Presentation;
using SaveState.Presentation.ViewModels;

/// <summary>
/// Provides minimal DI setup for UI tests that construct MainViewModel.
/// </summary>
internal static class UiTestLocator
{
    private static IServiceProvider? _serviceProvider;

    public static void EnsureInitialized()
    {
        if (_serviceProvider is not null)
        {
            return;
        }

        var services = new ServiceCollection();
        services.AddTransient<GameLibraryViewModel>(_ =>
            (GameLibraryViewModel)Activator.CreateInstance(typeof(GameLibraryViewModel), nonPublic: true)!);

        _serviceProvider = services.BuildServiceProvider();
        Locator.Current.SetServices(_serviceProvider);
    }
}
