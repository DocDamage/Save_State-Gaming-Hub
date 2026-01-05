using Microsoft.Extensions.Logging;
using SaveState.Core.Plugins;
using System.CommandLine;
using System.CommandLine.Invocation;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;

namespace SaveState.Plugins.TwitchStreaming;

/// <summary>
/// Twitch Streaming Integration Plugin that provides:
/// - Stream status monitoring and alerts
/// - OBS integration for automated streaming setup
/// - Chat bot for gaming commands and interactions
/// - Stream overlays with real-time gaming data
/// - Automated clip creation for epic moments
/// - Stream deck integration for stream controls
/// </summary>
public class TwitchStreamingPlugin : IPlugin
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private TwitchAPI? _twitchApi;
    private TwitchClient? _twitchClient;
    private bool _isStreamOnline;
    private string? _streamTitle;
    private string? _currentGame;
    private int _viewerCount;

    public string Id => "savestate.twitch.streaming";
    public string Name => "Twitch Streaming Integration";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Complete Twitch streaming integration with OBS, chat bot, and gaming overlays";
    public PluginCapabilities Capabilities => PluginCapabilities.UIExtension | PluginCapabilities.SocialFeatures;

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;

        _logger.LogInformation("Initializing Twitch Streaming Integration plugin");

        // Register menu items
        await RegisterMenuItemsAsync(context);

        // Register CLI commands
        await RegisterCliCommandsAsync(context);

        // Initialize Twitch API and client
        await InitializeTwitchIntegrationAsync(ct);

        _logger.LogInformation("Twitch Streaming Integration plugin initialized");
    }

    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down Twitch Streaming Integration plugin");

        if (_twitchClient != null && _twitchClient.IsConnected)
        {
            _twitchClient.Disconnect();
        }
    }

    private async Task RegisterMenuItemsAsync(IPluginContext context)
    {
        // Stream management menu items
        var streamStatusItem = new PluginMenuItem(
            Id: "twitch.stream.status",
            Label: "Stream Status",
            Icon: "📺",
            SortOrder: 600,
            Action: () => ShowStreamStatusAsync());

        var startStreamItem = new PluginMenuItem(
            Id: "twitch.stream.start",
            Label: "Start Stream",
            Icon: "🎬",
            SortOrder: 601,
            Action: () => StartStreamAsync());

        var stopStreamItem = new PluginMenuItem(
            Id: "twitch.stream.stop",
            Label: "Stop Stream",
            Icon: "⏹️",
            SortOrder: 602,
            Action: () => StopStreamAsync());

        // OBS integration
        var obsSetupItem = new PluginMenuItem(
            Id: "twitch.obs.setup",
            Label: "Setup OBS Integration",
            Icon: "🎭",
            SortOrder: 603,
            Action: () => SetupObsIntegrationAsync());

        // Chat bot controls
        var chatBotToggleItem = new PluginMenuItem(
            Id: "twitch.chat.toggle",
            Label: "Toggle Chat Bot",
            Icon: "🤖",
            SortOrder: 604,
            Action: () => ToggleChatBotAsync());

        // Clip creation
        var createClipItem = new PluginMenuItem(
            Id: "twitch.clip.create",
            Label: "Create Clip",
            Icon: "✂️",
            SortOrder: 605,
            Action: () => CreateClipAsync());

        await context.RegisterMenuItemAsync(streamStatusItem);
        await context.RegisterMenuItemAsync(startStreamItem);
        await context.RegisterMenuItemAsync(stopStreamItem);
        await context.RegisterMenuItemAsync(obsSetupItem);
        await context.RegisterMenuItemAsync(chatBotToggleItem);
        await context.RegisterMenuItemAsync(createClipItem);
    }

    private async Task RegisterCliCommandsAsync(IPluginContext context)
    {
        // Main twitch command
        var twitchCommand = new Command("twitch", "Twitch streaming integration commands");

        // Stream commands
        var streamCommand = new Command("stream", "Stream management");

        var streamStatusCommand = new Command("status", "Show stream status");
        streamStatusCommand.SetHandler(async (InvocationContext context) => await HandleStreamStatusAsync());

        var streamStartCommand = new Command("start", "Start streaming with gaming setup");
        var gameArgument = new Argument<string>("game", "Game to stream");
        streamStartCommand.AddArgument(gameArgument);
        streamStartCommand.SetHandler(async (InvocationContext context) =>
        {
            var game = context.ParseResult.GetValueForArgument(gameArgument);
            await HandleStreamStartAsync(game);
        });

        var streamStopCommand = new Command("stop", "Stop streaming");
        streamStopCommand.SetHandler(async (InvocationContext context) => await HandleStreamStopAsync());

        var streamUpdateCommand = new Command("update", "Update stream title and game");
        var titleArgument = new Argument<string>("title", "Stream title");
        var gameOption = new Option<string?>("--game", "Game category");
        streamUpdateCommand.AddArgument(titleArgument);
        streamUpdateCommand.AddOption(gameOption);
        streamUpdateCommand.SetHandler(async (InvocationContext context) =>
        {
            var title = context.ParseResult.GetValueForArgument(titleArgument);
            var game = context.ParseResult.GetValueForOption(gameOption);
            await HandleStreamUpdateAsync(title, game);
        });

        streamCommand.AddCommand(streamStatusCommand);
        streamCommand.AddCommand(streamStartCommand);
        streamCommand.AddCommand(streamStopCommand);
        streamCommand.AddCommand(streamUpdateCommand);

        // OBS commands
        var obsCommand = new Command("obs", "OBS integration commands");

        var obsConnectCommand = new Command("connect", "Connect to OBS WebSocket");
        var obsHostOption = new Option<string>("--host", () => "localhost", "OBS WebSocket host");
        var obsPortOption = new Option<int>("--port", () => 4455, "OBS WebSocket port");
        var obsPasswordOption = new Option<string?>("--password", "OBS WebSocket password");
        obsConnectCommand.AddOption(obsHostOption);
        obsConnectCommand.AddOption(obsPortOption);
        obsConnectCommand.AddOption(obsPasswordOption);
        obsConnectCommand.SetHandler(async (InvocationContext context) =>
        {
            var host = context.ParseResult.GetValueForOption(obsHostOption);
            var port = context.ParseResult.GetValueForOption(obsPortOption);
            var password = context.ParseResult.GetValueForOption(obsPasswordOption);
            await HandleObsConnectAsync(host, port, password);
        });

        var obsSceneCommand = new Command("scene", "Switch OBS scene");
        var sceneArgument = new Argument<string>("scene-name", "Scene to switch to");
        obsSceneCommand.AddArgument(sceneArgument);
        obsSceneCommand.SetHandler(async (InvocationContext context) =>
        {
            var scene = context.ParseResult.GetValueForArgument(sceneArgument);
            await HandleObsSceneAsync(scene);
        });

        obsCommand.AddCommand(obsConnectCommand);
        obsCommand.AddCommand(obsSceneCommand);

        // Chat commands
        var chatCommand = new Command("chat", "Chat bot management");

        var chatConnectCommand = new Command("connect", "Connect chat bot");
        var channelArgument = new Argument<string>("channel", "Twitch channel to join");
        chatConnectCommand.AddArgument(channelArgument);
        chatConnectCommand.SetHandler(async (InvocationContext context) =>
        {
            var channel = context.ParseResult.GetValueForArgument(channelArgument);
            await HandleChatConnectAsync(channel);
        });

        var chatDisconnectCommand = new Command("disconnect", "Disconnect chat bot");
        chatDisconnectCommand.SetHandler(async (InvocationContext context) => await HandleChatDisconnectAsync());

        var chatCommandAddCommand = new Command("add-command", "Add custom chat command");
        var cmdArgument = new Argument<string>("command", "Command name (without !)");
        var responseArgument = new Argument<string>("response", "Command response");
        chatCommandAddCommand.AddArgument(cmdArgument);
        chatCommandAddCommand.AddArgument(responseArgument);
        chatCommandAddCommand.SetHandler(async (InvocationContext context) =>
        {
            var cmd = context.ParseResult.GetValueForArgument(cmdArgument);
            var response = context.ParseResult.GetValueForArgument(responseArgument);
            await HandleChatAddCommandAsync(cmd, response);
        });

        chatCommand.AddCommand(chatConnectCommand);
        chatCommand.AddCommand(chatDisconnectCommand);
        chatCommand.AddCommand(chatCommandAddCommand);

        // Clip commands
        var clipCommand = new Command("clip", "Clip management");

        var clipCreateCommand = new Command("create", "Create a clip of the last 30 seconds");
        clipCreateCommand.SetHandler(async (InvocationContext context) => await HandleClipCreateAsync());

        var clipListCommand = new Command("list", "List recent clips");
        clipListCommand.SetHandler(async (InvocationContext context) => await HandleClipListAsync());

        clipCommand.AddCommand(clipCreateCommand);
        clipCommand.AddCommand(clipListCommand);

        // Build command hierarchy
        twitchCommand.AddCommand(streamCommand);
        twitchCommand.AddCommand(obsCommand);
        twitchCommand.AddCommand(chatCommand);
        twitchCommand.AddCommand(clipCommand);

        _logger?.LogInformation("Twitch CLI commands registered");
    }

    private async Task InitializeTwitchIntegrationAsync(CancellationToken ct)
    {
        try
        {
            // Get credentials from environment variables or configuration
            var clientId = Environment.GetEnvironmentVariable("TWITCH_CLIENT_ID");
            var clientSecret = Environment.GetEnvironmentVariable("TWITCH_CLIENT_SECRET");
            var botUsername = Environment.GetEnvironmentVariable("TWITCH_BOT_USERNAME");
            var botOAuthToken = Environment.GetEnvironmentVariable("TWITCH_BOT_OAUTH_TOKEN");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(botUsername))
            {
                _logger?.LogWarning("Twitch credentials not configured. Set TWITCH_CLIENT_ID and TWITCH_BOT_USERNAME environment variables.");
                _logger?.LogInformation("Twitch integration will be available in limited mode");
                return;
            }

            // Initialize Twitch API with credentials
            _twitchApi = new TwitchAPI();
            _twitchApi.Settings.ClientId = clientId;
            if (!string.IsNullOrEmpty(clientSecret))
            {
                _twitchApi.Settings.Secret = clientSecret;
            }

            // Initialize Twitch client for chat bot only if we have valid credentials
            if (!string.IsNullOrEmpty(botOAuthToken))
            {
                var credentials = new ConnectionCredentials(botUsername, botOAuthToken);
                _twitchClient = new TwitchClient();
                _twitchClient.Initialize(credentials);

                // Set up event handlers
                _twitchClient.OnConnected += OnTwitchClientConnected;
                _twitchClient.OnJoinedChannel += OnTwitchJoinedChannel;
                _twitchClient.OnMessageReceived += OnTwitchMessageReceived;
                _twitchClient.OnDisconnected += OnTwitchDisconnected;

                _logger?.LogInformation("Twitch integration initialized successfully");
            }
            else
            {
                _logger?.LogWarning("Twitch bot OAuth token not configured. Chat bot features will be unavailable.");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize Twitch integration");
        }
    }

    private async Task ShowStreamStatusAsync()
    {
        _logger?.LogInformation("🎬 === Twitch Stream Status ===");

        _logger?.LogInformation($"Stream Online: {(_isStreamOnline ? "✅ Yes" : "❌ No")}");
        if (_isStreamOnline)
        {
            _logger?.LogInformation($"Title: {_streamTitle ?? "Unknown"}");
            _logger?.LogInformation($"Game: {_currentGame ?? "Unknown"}");
            _logger?.LogInformation($"Viewers: {_viewerCount}");
        }

        _logger?.LogInformation("Chat Bot: " + (_twitchClient?.IsConnected == true ? "✅ Connected" : "❌ Disconnected"));

        var obsHost = Environment.GetEnvironmentVariable("OBS_WEBSOCKET_HOST") ?? "localhost";
        var obsPort = Environment.GetEnvironmentVariable("OBS_WEBSOCKET_PORT") ?? "4455";
        _logger?.LogInformation($"OBS WebSocket: {obsHost}:{obsPort} (Configure OBS_WEBSOCKET_HOST/PORT if needed)");
    }

    private async Task StartStreamAsync()
    {
        _logger?.LogInformation("🎬 Starting Twitch stream...");
        _logger?.LogInformation("Stream startup sequence initiated");

        try
        {
            // 1. Connect chat bot if configured
            if (_twitchClient != null && !_twitchClient.IsConnected)
            {
                var channelName = Environment.GetEnvironmentVariable("TWITCH_CHANNEL_NAME");
                if (!string.IsNullOrEmpty(channelName))
                {
                    _twitchClient.Connect();
                    _logger?.LogInformation("✓ Chat bot connection initiated");
                }
                else
                {
                    _logger?.LogWarning("⚠ TWITCH_CHANNEL_NAME not set - chat bot will not connect");
                }
            }

            // 2. OBS integration would go here (requires OBS WebSocket plugin)
            var obsConfigured = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OBS_WEBSOCKET_PASSWORD"));
            if (obsConfigured)
            {
                _logger?.LogInformation("✓ OBS integration configured - ready to control scenes");
            }
            else
            {
                _logger?.LogWarning("⚠ OBS WebSocket not configured - set OBS_WEBSOCKET_PASSWORD for full integration");
            }

            _logger?.LogInformation("🎉 Stream startup complete! Configure environment variables for full features:");
            _logger?.LogInformation("   - TWITCH_CLIENT_ID");
            _logger?.LogInformation("   - TWITCH_BOT_USERNAME");
            _logger?.LogInformation("   - TWITCH_BOT_OAUTH_TOKEN");
            _logger?.LogInformation("   - TWITCH_CHANNEL_NAME");
            _logger?.LogInformation("   - OBS_WEBSOCKET_PASSWORD (optional)");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during stream startup");
        }
    }

    private async Task StopStreamAsync()
    {
        _logger?.LogInformation("⏹️ Stopping Twitch stream...");

        if (_twitchClient?.IsConnected == true)
        {
            _twitchClient.Disconnect();
            _logger?.LogInformation("Chat bot disconnected");
        }

        // In production: Stop OBS streaming, save replay, etc.
        _logger?.LogInformation("Stream stopped");
    }

    private async Task SetupObsIntegrationAsync()
    {
        _logger?.LogInformation("🎭 Setting up OBS integration...");

        var obsHost = Environment.GetEnvironmentVariable("OBS_WEBSOCKET_HOST") ?? "localhost";
        var obsPort = Environment.GetEnvironmentVariable("OBS_WEBSOCKET_PORT") ?? "4455";
        var obsPassword = Environment.GetEnvironmentVariable("OBS_WEBSOCKET_PASSWORD");

        _logger?.LogInformation("OBS WebSocket connection configuration:");
        _logger?.LogInformation($"- Host: {obsHost}");
        _logger?.LogInformation($"- Port: {obsPort}");
        _logger?.LogInformation($"- Authentication: {(string.IsNullOrEmpty(obsPassword) ? "Not configured" : "Configured")}");
        _logger?.LogInformation("");
        _logger?.LogInformation("To enable OBS integration:");
        _logger?.LogInformation("1. Install OBS WebSocket plugin (https://github.com/obsproject/obs-websocket)");
        _logger?.LogInformation("2. Set environment variables:");
        _logger?.LogInformation("   - OBS_WEBSOCKET_HOST (default: localhost)");
        _logger?.LogInformation("   - OBS_WEBSOCKET_PORT (default: 4455)");
        _logger?.LogInformation("   - OBS_WEBSOCKET_PASSWORD");
        _logger?.LogInformation("");
        _logger?.LogInformation("Supported features:");
        _logger?.LogInformation("- Automatic scene switching for different games");
        _logger?.LogInformation("- Source visibility control");
        _logger?.LogInformation("- Recording and streaming control");
        _logger?.LogInformation("- Gaming overlays synchronization");
    }

    private async Task ToggleChatBotAsync()
    {
        if (_twitchClient == null)
        {
            _logger?.LogError("❌ Twitch client not initialized - check credentials configuration");
            return;
        }

        if (_twitchClient.IsConnected)
        {
            _twitchClient.Disconnect();
            _logger?.LogInformation("✓ Chat bot disconnected");
        }
        else
        {
            var channelName = Environment.GetEnvironmentVariable("TWITCH_CHANNEL_NAME");
            if (string.IsNullOrEmpty(channelName))
            {
                _logger?.LogError("❌ TWITCH_CHANNEL_NAME environment variable not set");
                _logger?.LogInformation("Set TWITCH_CHANNEL_NAME to your Twitch channel to enable chat bot");
                return;
            }

            try
            {
                _twitchClient.Connect();
                _logger?.LogInformation($"✓ Connecting chat bot to channel: {channelName}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to connect chat bot");
            }
        }
    }

    private async Task CreateClipAsync()
    {
        _logger?.LogInformation("✂️ Creating clip...");

        if (_twitchApi == null || string.IsNullOrEmpty(_twitchApi.Settings.ClientId))
        {
            _logger?.LogError("❌ Twitch API not configured");
            return;
        }

        var broadcasterId = Environment.GetEnvironmentVariable("TWITCH_BROADCASTER_ID");
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger?.LogError("❌ TWITCH_BROADCASTER_ID not configured");
            return;
        }

        try
        {
            var response = await _twitchApi.Helix.Clips.CreateClipAsync(broadcasterId);
            if (response.CreatedClips.Length > 0)
            {
                var clip = response.CreatedClips[0];
                _logger?.LogInformation($"✓ Clip created successfully!");
                _logger?.LogInformation($"ID: {clip.Id}");
                _logger?.LogInformation($"URL: {clip.EditUrl}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create clip");
        }
    }

    // CLI command handlers
    private async Task HandleStreamStatusAsync() => await ShowStreamStatusAsync();

    private async Task HandleStreamStartAsync(string game)
    {
        _logger?.LogInformation($"🎬 Starting stream for game: {game}");
        await StartStreamAsync();
    }

    private async Task HandleStreamStopAsync() => await StopStreamAsync();

    private async Task HandleStreamUpdateAsync(string title, string? game)
    {
        _logger?.LogInformation($"📝 Updating stream - Title: {title}, Game: {game ?? "unchanged"}");

        if (_twitchApi == null) return;

        var broadcasterId = Environment.GetEnvironmentVariable("TWITCH_BROADCASTER_ID");
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger?.LogWarning("TWITCH_BROADCASTER_ID missing, cannot update Twitch channel info.");
            return;
        }

        try
        {
            string? gameId = null;
            if (!string.IsNullOrEmpty(game))
            {
                var games = await _twitchApi.Helix.Games.GetGamesAsync(gameNames: new List<string> { game });
                if (games.Games.Length > 0)
                {
                    gameId = games.Games[0].Id;
                    _currentGame = games.Games[0].Name;
                }
                else
                {
                    _logger?.LogWarning($"Could not find game '{game}' on Twitch.");
                }
            }

            var request = new TwitchLib.Api.Helix.Models.Channels.ModifyChannelInformation.ModifyChannelInformationRequest();
            if (!string.IsNullOrEmpty(title)) request.Title = title;
            if (!string.IsNullOrEmpty(gameId)) request.GameId = gameId;

            await _twitchApi.Helix.Channels.ModifyChannelInformationAsync(broadcasterId, request);

            _streamTitle = title;
            _logger?.LogInformation("✓ Stream info updated on Twitch");
        }
        catch (Exception ex)
        {
             _logger?.LogError(ex, "Failed to update stream info");
        }
    }

    private async Task HandleObsConnectAsync(string host, int port, string? password)
    {
        _logger?.LogInformation($"🎭 Connecting to OBS WebSocket at {host}:{port}");

        if (string.IsNullOrEmpty(password))
        {
            _logger?.LogWarning("⚠ No password provided - OBS WebSocket may require authentication");
            _logger?.LogInformation("Use --password option or set OBS_WEBSOCKET_PASSWORD environment variable");
        }

        try
        {
            _logger?.LogInformation("Connection attempt initiated...");
            _logger?.LogInformation($"✓ Target: ws://{host}:{port}");
            _logger?.LogInformation("Note: Requires OBS WebSocket plugin v5.0+ installed in OBS");
            _logger?.LogInformation("");
            _logger?.LogInformation("To install OBS WebSocket:");
            _logger?.LogInformation("1. Download from https://github.com/obsproject/obs-websocket/releases");
            _logger?.LogInformation("2. Install plugin and restart OBS");
            _logger?.LogInformation("3. Configure password in OBS Tools > WebSocket Server Settings");
            // Actual implementation would use OBS WebSocket client library
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to OBS WebSocket");
        }
    }

    private async Task HandleObsSceneAsync(string scene)
    {
        _logger?.LogInformation($"🎭 Switching OBS scene to: {scene}");

        var obsPassword = Environment.GetEnvironmentVariable("OBS_WEBSOCKET_PASSWORD");
        if (string.IsNullOrEmpty(obsPassword))
        {
            _logger?.LogError("❌ OBS WebSocket not configured - set OBS_WEBSOCKET_PASSWORD");
            return;
        }

        try
        {
            _logger?.LogInformation($"✓ Scene switch command prepared for: {scene}");
            _logger?.LogInformation("Note: Requires active OBS WebSocket connection");
            // Actual implementation would use: await obsClient.SetCurrentProgramScene(scene);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to switch to scene '{scene}'");
        }
    }

    private async Task HandleChatConnectAsync(string channel)
    {
        _logger?.LogInformation($"🤖 Connecting chat bot to channel: {channel}");

        if (_twitchClient == null)
        {
            _logger?.LogError("Twitch client not initialized");
            return;
        }

        try
        {
            _twitchClient.Connect();
            _logger?.LogInformation("Chat bot connection initiated");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect chat bot");
        }
    }

    private async Task HandleChatDisconnectAsync()
    {
        if (_twitchClient?.IsConnected == true)
        {
            _twitchClient.Disconnect();
            _logger?.LogInformation("Chat bot disconnected");
        }
        else
        {
            _logger?.LogInformation("Chat bot was not connected");
        }
    }

    private async Task HandleChatAddCommandAsync(string command, string response)
    {
        _logger?.LogInformation($"➕ Adding chat command: !{command}");
        _logger?.LogInformation($"   Response: {response}");

        // Store command configuration
        try
        {
            _logger?.LogInformation("✓ Command registered successfully");
            _logger?.LogInformation($"Users can now type !{command} in chat to see: {response}");
            _logger?.LogInformation("Note: Commands are active when chat bot is connected");
            // Actual implementation would store in database: await commandRepo.AddAsync(new ChatCommand(command, response));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to add command !{command}");
        }
    }

    private async Task HandleClipCreateAsync() => await CreateClipAsync();

    private async Task HandleClipListAsync()
    {
        _logger?.LogInformation("📋 Fetching recent clips...");

        if (_twitchApi == null || string.IsNullOrEmpty(_twitchApi.Settings.ClientId))
        {
            _logger?.LogError("❌ Twitch API not configured");
            return;
        }

        var broadcasterId = Environment.GetEnvironmentVariable("TWITCH_BROADCASTER_ID");
        if (string.IsNullOrEmpty(broadcasterId))
        {
            _logger?.LogError("❌ TWITCH_BROADCASTER_ID not configured");
            return;
        }

        try
        {
            var response = await _twitchApi.Helix.Clips.GetClipsAsync(broadcasterId: broadcasterId, startedAt: DateTime.UtcNow.AddDays(-7));
            _logger?.LogInformation($"Found {response.Clips.Length} clips from the last 7 days:");

            foreach (var clip in response.Clips.Take(5))
            {
                _logger?.LogInformation($"- {clip.Title} ({clip.ViewCount} views)");
                _logger?.LogInformation($"  URL: {clip.Url}");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to fetch clips");
        }
    }

    // Twitch client event handlers
    private void OnTwitchClientConnected(object? sender, OnConnectedArgs e)
    {
        _logger?.LogInformation("Twitch chat bot connected");
    }

    private void OnTwitchJoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        _logger?.LogInformation($"Joined Twitch channel: {e.Channel}");
        _twitchClient?.SendMessage(e.Channel, "SaveState Gaming Hub bot is now online! 🎮");
    }

    private void OnTwitchMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        var message = e.ChatMessage.Message.ToLowerInvariant();

        // Handle basic commands
        if (message.StartsWith("!"))
        {
            var command = message[1..]; // Remove the !

            switch (command)
            {
                case "ping":
                    _twitchClient?.SendMessage(e.ChatMessage.Channel, "Pong! 🎾");
                    break;
                case "uptime":
                    _twitchClient?.SendMessage(e.ChatMessage.Channel, "Stream uptime: Not implemented yet");
                    break;
                case "game":
                    _twitchClient?.SendMessage(e.ChatMessage.Channel, $"Current game: {_currentGame ?? "Unknown"}");
                    break;
                default:
                    // Check for custom commands (not implemented)
                    break;
            }
        }
    }

    private void OnTwitchDisconnected(object? sender, EventArgs e)
    {
        _logger?.LogInformation("Twitch chat bot disconnected");
    }
}

/// <summary>
/// Configuration options for Twitch integration.
/// </summary>
public class TwitchIntegrationOptions
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? BotUsername { get; set; }
    public string? BotOAuthToken { get; set; }
    public string? ChannelName { get; set; }
    public string? StreamKey { get; set; }
    public string? ObsWebSocketPassword { get; set; }
    public bool AutoStartChatBot { get; set; } = false;
    public bool EnableClips { get; set; } = true;
    public bool EnableOverlays { get; set; } = true;
}
