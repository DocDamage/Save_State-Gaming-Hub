using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.LiveStatsWidget;

public sealed class LiveStatsWidgetPlugin : IPlugin, IUIPanel
{
    private IPluginContext? _context;

    public string Id => "live-stats-widget";
    public string Name => "Live Game Stats Widget";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Transparent overlay with real-time statistics for streaming.";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    // IUIPanel Implementation
    public string PanelName => "Live Stats";
    public string DisplayName => "Live Stats Overlay";
    public string? Icon => "Stats";

    private UserControl? _control;

    public object GetControl()
    {
        // Lazy load the control
        if (_control == null)
        {
            _control = new LiveStatsView();
        }
        return _control;
    }

    public Task OnActivatedAsync()
    {
        _context?.Logger.LogInformation("Live Stats Widget Activated");
        return Task.CompletedTask;
    }

    public Task OnDeactivatedAsync()
    {
        return Task.CompletedTask;
    }

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("Live Stats Widget initialized");
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public partial class LiveStatsView : UserControl
{
    public LiveStatsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
