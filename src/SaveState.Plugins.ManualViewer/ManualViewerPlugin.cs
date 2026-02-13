using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.ManualViewer;

public sealed class ManualViewerPlugin : IPlugin, IUIPanel
{
    private IPluginContext? _context;
    private ManualView? _control;

    public string Id => "manual-viewer";
    public string Name => "Manual Viewer";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "View game manuals inside SaveState.";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    // IUIPanel
    public string PanelName => "Manuals";
    public string DisplayName => "Game Manual";
    public string? Icon => "Book";

    public object GetControl()
    {
        if (_control == null) _control = new ManualView();
        return _control;
    }

    public Task OnActivatedAsync()
    {
        if (_context?.Logger.IsEnabled(LogLevel.Information) == true)
        {
             _context.Logger.LogInformation("Manual Viewer Activated");
        }
        return Task.CompletedTask;
    }

    public Task OnDeactivatedAsync() => Task.CompletedTask;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        if (_context.Logger.IsEnabled(LogLevel.Information))
        {
            _context.Logger.LogInformation("Manual Viewer Initialized");
        }
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public partial class ManualView : UserControl
{
    public ManualView()
    {
        // InitializeComponent(); // In a real app with XAML compilation
    }
}
