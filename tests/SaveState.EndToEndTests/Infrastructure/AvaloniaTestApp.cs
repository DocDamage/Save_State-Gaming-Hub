using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Presentation;

namespace SaveState.EndToEndTests.Infrastructure;

/// <summary>
/// Test application builder for Avalonia headless E2E tests.
/// </summary>
public static class AvaloniaTestApp
{
    private static AppBuilder? _appBuilder;
    private static bool _initialized;
    private static readonly object _initLock = new();

    /// <summary>
    /// Ensures the Avalonia headless platform is initialized.
    /// </summary>
    public static void EnsureInitialized()
    {
        if (_initialized) return;

        lock (_initLock)
        {
            if (_initialized) return;

            _appBuilder = AppBuilder.Configure<App>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions
                {
                    UseHeadlessDrawing = true
                });

            _appBuilder.SetupWithLifetime(new ClassicDesktopStyleApplicationLifetime());
            _initialized = true;
        }
    }
}

/// <summary>
/// Test host for running Avalonia UI automation tests.
/// </summary>
public class AvaloniaTestHost : IAsyncDisposable
{
    private Window? _mainWindow;
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    public AvaloniaTestHost(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets the main window of the application.
    /// </summary>
    public Window MainWindow => _mainWindow ?? throw new InvalidOperationException("TestApp has not been started. Call StartAsync first.");

    /// <summary>
    /// Starts the test application and shows the main window.
    /// </summary>
    public async Task StartAsync<TWindow>(CancellationToken cancellationToken = default) 
        where TWindow : Window, new()
    {
        AvaloniaTestApp.EnsureInitialized();

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            _mainWindow = new TWindow();
            _mainWindow.Show();
        });

        // Wait for layout pass
        await Task.Delay(100, cancellationToken);
    }

    /// <summary>
    /// Starts the test application with a window resolved from DI.
    /// </summary>
    public async Task StartAsync(Func<IServiceProvider, Window> windowFactory, CancellationToken cancellationToken = default)
    {
        AvaloniaTestApp.EnsureInitialized();

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            _mainWindow = windowFactory(_serviceProvider);
            _mainWindow.Show();
        });

        // Wait for layout pass
        await Task.Delay(100, cancellationToken);
    }

    /// <summary>
    /// Captures a screenshot of the current window state.
    /// </summary>
    public async Task<byte[]> CaptureScreenshotAsync(CancellationToken cancellationToken = default)
    {
        if (_mainWindow is null)
            throw new InvalidOperationException("Main window is not available");

        return await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            var pixelSize = PixelSize.FromSize(_mainWindow.Bounds.Size, 1);
            if (pixelSize.Width <= 0 || pixelSize.Height <= 0)
                return Array.Empty<byte>();

            var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(pixelSize);
            bitmap.Render(_mainWindow);
            
            using var stream = new MemoryStream();
            bitmap.Save(stream);
            return stream.ToArray();
        });
    }

    /// <summary>
    /// Performs a mouse click on a control.
    /// </summary>
    public async Task ClickAsync(Button button, CancellationToken cancellationToken = default)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        });
        
        await Task.Delay(50, cancellationToken);
    }

    /// <summary>
    /// Sets text on a TextBox.
    /// </summary>
    public async Task SetTextAsync(TextBox textBox, string text, CancellationToken cancellationToken = default)
    {
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            textBox.Text = text;
            textBox.RaiseEvent(new TextInputEventArgs
            {
                RoutedEvent = InputElement.TextInputEvent,
                Text = text
            });
        });
        
        await Task.Delay(50, cancellationToken);
    }

    /// <summary>
    /// Waits for a condition to be true within a timeout.
    /// </summary>
    public async Task WaitForAsync(Func<bool> condition, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(5);
        var startTime = DateTime.UtcNow;

        while (!condition() && DateTime.UtcNow - startTime < maxWait)
        {
            await Task.Delay(50, cancellationToken);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            _mainWindow?.Close();
            _mainWindow = null;
        });

        _disposed = true;
    }
}
