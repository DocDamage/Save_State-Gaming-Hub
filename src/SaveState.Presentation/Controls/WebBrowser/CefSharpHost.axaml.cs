using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CefSharp;
using CefSharp.Handler;
// NOTE: CefSharp.WinForms requires separate package. Using OffScreen for now.
// using CefSharp.WinForms;
using SaveState.Core.WebBrowser.Models;

namespace SaveState.Presentation.Controls.WebBrowser;

public partial class CefSharpHost : UserControl
{
    public static readonly StyledProperty<string> AddressProperty =
        AvaloniaProperty.Register<CefSharpHost, string>(nameof(Address));

    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<CefSharpHost, bool>(nameof(IsLoading));

    public string Address
    {
        get => GetValue(AddressProperty);
        set => SetValue(AddressProperty, value);
    }

    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        private set => SetValue(IsLoadingProperty, value);
    }

    public ChromiumWebBrowser? Browser { get; private set; }

    public event EventHandler<AddressChangedEventArgs>? AddressChanged;
    public event EventHandler<TitleChangedEventArgs>? TitleChanged;
    public event EventHandler<LoadingStateChangedEventArgs>? LoadingStateChanged;
    public event EventHandler<LoadingProgressChangedEventArgs>? LoadingProgressChanged;

    public CefSharpHost()
    {
        InitializeComponent();
        this.AttachedToVisualTree += OnAttachedToVisualTree;
        this.DetachedFromVisualTree += OnDetachedFromVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (!Cef.IsInitialized)
        {
            var settings = new CefSettings
            {
                CachePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SaveState", "CefCache"),
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 SaveState/2.5",
                LogSeverity = LogSeverity.Warning,
                WindowlessRenderingEnabled = true
            };
            Cef.Initialize(settings);
        }

        InitializeBrowser();
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Browser?.Dispose();
        Browser = null;
    }

    private void InitializeBrowser()
    {
        if (Browser != null) return;

        // NOTE: Using OffScreen rendering for Avalonia compatibility
        // For full browser integration, consider CefSharp.Avalonia or similar
        Browser = new ChromiumWebBrowser(Address ?? "about:blank", offscreenRenderer: true);

        Browser.AddressChanged += (s, e) =>
        {
            Address = e.Address;
            AddressChanged?.Invoke(this, e);
        };

        Browser.TitleChanged += (s, e) =>
        {
            TitleChanged?.Invoke(this, e);
        };

        Browser.LoadingStateChanged += (s, e) =>
        {
            IsLoading = e.IsLoading;
            LoadingStateChanged?.Invoke(this, e);
        };

        // FUTURE: Embed browser view in Avalonia using native host or CefSharp.Avalonia
        HostContainer.Child = hwndHost;
    }

    public void LoadUrl(string url)
    {
        Browser?.Load(url);
    }

    public Task<string> GetSourceAsync()
    {
        return Browser?.GetSourceAsync() ?? Task.FromResult(string.Empty);
    }

    public Task<string> GetTextAsync()
    {
        return Browser?.GetTextAsync() ?? Task.FromResult(string.Empty);
    }

    public Task<JavascriptResponse> EvaluateScriptAsync(string script)
    {
        return Browser?.EvaluateScriptAsync(script) ?? Task.FromResult(new JavascriptResponse { Success = false });
    }

    public void ShowDevTools()
    {
        Browser?.ShowDevTools();
    }

    public void CloseDevTools()
    {
        Browser?.CloseDevTools();
    }

    public Task<byte[]> CaptureScreenshotAsync()
    {
        return Browser?.CaptureScreenshotAsync() ?? Task.FromResult(Array.Empty<byte>());
    }
}

public class Win32HwndControl : NativeControlHost
{
    private readonly ChromiumWebBrowser _browser;

    public Win32HwndControl(ChromiumWebBrowser browser)
    {
        _browser = browser;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        return new PlatformHandle(_browser.Handle, "HWND");
    }
}

public class LoadingProgressChangedEventArgs : EventArgs
{
    public double Progress { get; set; }
}
