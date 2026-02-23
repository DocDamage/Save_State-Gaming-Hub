using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Presentation;
using SaveState.Presentation.ViewModels;

namespace SaveState.Presentation.UITests;

/// <summary>
/// Application builder for headless Avalonia tests.
/// Uses the main App class to ensure all XAML resources are available.
/// </summary>
public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = true
        });
}

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

/// <summary>
/// Base class for Avalonia headless tests.
/// Provides common test infrastructure.
/// </summary>
public abstract class HeadlessTestBase
{
    // Static constructor ensures Avalonia is initialized before any tests run
    static HeadlessTestBase()
    {
        // Initialization is handled by AvaloniaTestApplication attribute
    }
}
