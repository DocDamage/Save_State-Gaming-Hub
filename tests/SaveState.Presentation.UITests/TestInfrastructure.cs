using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using SaveState.Presentation;

[assembly: AvaloniaTestApplication(typeof(SaveState.Presentation.UITests.TestAppBuilder))]

namespace SaveState.Presentation.UITests;

/// <summary>
/// Application builder for headless Avalonia tests.
/// </summary>
public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApp>()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

/// <summary>
/// Application class for headless tests.
/// </summary>
public class TestApp : App
{
    public override void OnFrameworkInitializationCompleted()
    {
        // Skip desktop shell bootstrapping for isolated view tests.
    }
}

/// <summary>
/// Base class for Avalonia headless tests.
/// Provides common test infrastructure.
/// </summary>
public abstract class HeadlessTestBase
{
    // Base class can be extended with common test utilities
}
