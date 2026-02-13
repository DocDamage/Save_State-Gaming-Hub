using Microsoft.Extensions.Logging;
using OBSWebsocketDotNet;
using SaveState.Core.Common;
using SaveState.Core.Plugins;
using System.Text.Json;

namespace SaveState.Plugins.OBSIntegration;

public sealed class OBSIntegrationPlugin : IPlugin
{
    private IPluginContext? _context;
    private OBSWebsocket? _obs;
    private OBSSettings _settings = new();

    public string Id => "obs-integration";
    public string Name => "OBS Integration";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Auto-switch scenes and start recording when games launch.";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension;

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _context.Logger.LogInformation("OBS Integration initialized");

        LoadSettings();
        if (_settings.Enabled)
        {
             ConnectToOBS();
        }

        _context.EventReceived += OnEventReceived;
        return Task.CompletedTask;
    }

    private void ConnectToOBS()
    {
        _obs = new OBSWebsocket();
        _obs.Connected += (s, e) => _context?.Logger.LogInformation("Connected to OBS");
        _obs.Disconnected += (s, e) => _context?.Logger.LogWarning("Disconnected from OBS");

        try
        {
            if (!string.IsNullOrEmpty(_settings.Host))
                _obs.ConnectAsync(_settings.Host, _settings.Password);
        }
        catch (Exception ex)
        {
            _context?.Logger.LogError(ex, "Failed to connect to OBS");
        }
    }

    private void OnEventReceived(object? sender, PluginEventArgs e)
    {
        if (_obs == null || !_obs.IsConnected) return;

        try
        {
            if (e.EventType == PluginEventType.GameLaunched)
            {
                 if (_settings.AutoRecord)
                 {
                     _obs.StartRecord();
                     _context?.Logger.LogInformation("OBS Recording Started");
                 }
                 if (!string.IsNullOrEmpty(_settings.GamingScene))
                 {
                     _obs.SetCurrentProgramScene(_settings.GamingScene);
                 }
            }
            else if (e.EventType == PluginEventType.GameClosed)
            {
                 if (_settings.AutoRecord)
                 {
                     _obs.StopRecord();
                 }
            }
        }
        catch (Exception ex)
        {
             _context?.Logger.LogError(ex, "Error sending OBS command");
        }
    }

    private void LoadSettings()
    {
        /* Load from json */
        _settings.Host = "ws://localhost:4455";
        _settings.Password = ""; // Default empty
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _obs?.Disconnect();
        return Task.CompletedTask;
    }
}

public class OBSSettings
{
    public bool Enabled { get; set; } = true;
    public string Host { get; set; } = "ws://localhost:4455";
    public string Password { get; set; } = "";
    public bool AutoRecord { get; set; } = false;
    public string GamingScene { get; set; } = "";
}
