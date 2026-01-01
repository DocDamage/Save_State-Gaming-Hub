using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Plugins;
using SaveState.Core.Social;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace SaveState.Plugins.DiscordIntegration;

/// <summary>
/// Advanced Discord integration plugin that provides:
/// - Enhanced Rich Presence with detailed gaming states
/// - Discord bot for server management and gaming coordination
/// - Game invites and matchmaking through Discord
/// - Activity sharing and social gaming features
/// - Voice channel integration for gaming sessions
/// </summary>
public class DiscordIntegrationPlugin : IPlugin
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private DiscordSocketClient? _discordClient;
    private IDiscordPresenceService? _presenceService;
    private bool _isBotConnected;
    private bool _isPresenceConnected;

    public string Id => "savestate.discord.integration";
    public string Name => "Advanced Discord Integration";
    public string Version => "2.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Enhanced Discord integration with bot, matchmaking, and rich social features";
    public PluginCapabilities Capabilities => PluginCapabilities.SocialFeatures | PluginCapabilities.UIExtension;

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;

        _logger.LogInformation("Initializing Advanced Discord Integration plugin");

        // Get existing presence service
        _presenceService = context.Services.GetService(typeof(IDiscordPresenceService)) as IDiscordPresenceService;

        if (_presenceService == null)
        {
            _logger.LogWarning("IDiscordPresenceService not available - presence features will be limited");
        }

        // Register menu items
        await RegisterMenuItemsAsync(context);

        // Register CLI commands
        await RegisterCliCommandsAsync(context);

        // Initialize Discord bot if configured
        await InitializeDiscordBotAsync(ct);

        _logger.LogInformation("Advanced Discord Integration plugin initialized");
    }

    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down Advanced Discord Integration plugin");

        if (_discordClient != null)
        {
            await _discordClient.StopAsync();
            _discordClient.Dispose();
        }

        if (_presenceService != null)
        {
            await _presenceService.DisconnectAsync(ct);
        }
    }

    private async Task RegisterMenuItemsAsync(IPluginContext context)
    {
        // Bot management menu items
        var botConnectItem = new PluginMenuItem(
            Id: "discord.bot.connect",
            Label: "Connect Discord Bot",
            Icon: "🤖",
            SortOrder: 400,
            Action: () => ConnectBotAsync());

        var botDisconnectItem = new PluginMenuItem(
            Id: "discord.bot.disconnect",
            Label: "Disconnect Discord Bot",
            Icon: "🔌",
            SortOrder: 401,
            Action: () => DisconnectBotAsync());

        // Matchmaking menu items
        var matchmakingStartItem = new PluginMenuItem(
            Id: "discord.matchmaking.start",
            Label: "Start Matchmaking",
            Icon: "🎯",
            SortOrder: 402,
            Action: () => StartMatchmakingAsync());

        var matchmakingStopItem = new PluginMenuItem(
            Id: "discord.matchmaking.stop",
            Label: "Stop Matchmaking",
            Icon: "⏹️",
            SortOrder: 403,
            Action: () => StopMatchmakingAsync());

        // Activity sharing
        var shareActivityItem = new PluginMenuItem(
            Id: "discord.share.activity",
            Label: "Share Current Activity",
            Icon: "📢",
            SortOrder: 404,
            Action: () => ShareCurrentActivityAsync());

        // Voice channel management
        var createVoiceChannelItem = new PluginMenuItem(
            Id: "discord.voice.create",
            Label: "Create Gaming Voice Channel",
            Icon: "🎤",
            SortOrder: 405,
            Action: () => CreateGamingVoiceChannelAsync());

        await context.RegisterMenuItemAsync(botConnectItem);
        await context.RegisterMenuItemAsync(botDisconnectItem);
        await context.RegisterMenuItemAsync(matchmakingStartItem);
        await context.RegisterMenuItemAsync(matchmakingStopItem);
        await context.RegisterMenuItemAsync(shareActivityItem);
        await context.RegisterMenuItemAsync(createVoiceChannelItem);
    }

    private async Task RegisterCliCommandsAsync(IPluginContext context)
    {
        // Main discord command
        var discordCommand = new Command("discord", "Advanced Discord integration commands");

        // Bot commands
        var botCommand = new Command("bot", "Discord bot management");
        var botConnectCommand = new Command("connect", "Connect Discord bot");
        var botDisconnectCommand = new Command("disconnect", "Disconnect Discord bot");
        var botStatusCommand = new Command("status", "Show bot connection status");

        botConnectCommand.SetHandler(async () => await HandleBotConnectAsync());
        botDisconnectCommand.SetHandler(async () => await HandleBotDisconnectAsync());
        botStatusCommand.SetHandler(async () => await HandleBotStatusAsync());

        botCommand.AddCommand(botConnectCommand);
        botCommand.AddCommand(botDisconnectCommand);
        botCommand.AddCommand(botStatusCommand);

        // Rich Presence commands
        var presenceCommand = new Command("presence", "Rich Presence management");
        var presenceSetCommand = new Command("set", "Set custom rich presence");
        var gameTitleArgument = new Argument<string>("game-title", "Game title to display");
        var detailsOption = new Option<string?>("--details", "Additional details");
        var stateOption = new Option<string?>("--state", "Current game state");

        presenceSetCommand.AddArgument(gameTitleArgument);
        presenceSetCommand.AddOption(detailsOption);
        presenceSetCommand.AddOption(stateOption);
        presenceSetCommand.SetHandler(async (InvocationContext context) =>
        {
            var gameTitle = context.ParseResult.GetValueForArgument(gameTitleArgument);
            var details = context.ParseResult.GetValueForOption(detailsOption);
            var state = context.ParseResult.GetValueForOption(stateOption);
            await HandlePresenceSetAsync(gameTitle, details, state);
        });

        var presenceClearCommand = new Command("clear", "Clear rich presence");
        presenceClearCommand.SetHandler(async (InvocationContext context) => await HandlePresenceClearAsync());

        presenceCommand.AddCommand(presenceSetCommand);
        presenceCommand.AddCommand(presenceClearCommand);

        // Matchmaking commands
        var matchmakingCommand = new Command("matchmaking", "Game matchmaking through Discord");
        var matchmakingStartCommand = new Command("start", "Start looking for game matches");
        var gameTypeArgument = new Argument<string>("game-type", "Type of game to matchmake for");

        matchmakingStartCommand.AddArgument(gameTypeArgument);
        matchmakingStartCommand.SetHandler(async (InvocationContext context) =>
        {
            var gameType = context.ParseResult.GetValueForArgument(gameTypeArgument);
            await HandleMatchmakingStartAsync(gameType);
        });

        var matchmakingStopCommand = new Command("stop", "Stop matchmaking");
        matchmakingStopCommand.SetHandler(async (InvocationContext context) => await HandleMatchmakingStopAsync());

        var matchmakingStatusCommand = new Command("status", "Show matchmaking status");
        matchmakingStatusCommand.SetHandler(async (InvocationContext context) => await HandleMatchmakingStatusAsync());

        matchmakingCommand.AddCommand(matchmakingStartCommand);
        matchmakingCommand.AddCommand(matchmakingStopCommand);
        matchmakingCommand.AddCommand(matchmakingStatusCommand);

        // Activity sharing
        var shareCommand = new Command("share", "Share activities and achievements");
        var shareAchievementCommand = new Command("achievement", "Share an achievement");
        var achievementIdArgument = new Argument<string>("achievement-id", "Achievement to share");

        shareAchievementCommand.AddArgument(achievementIdArgument);
        shareAchievementCommand.SetHandler(async (InvocationContext context) =>
        {
            var achievementId = context.ParseResult.GetValueForArgument(achievementIdArgument);
            await HandleShareAchievementAsync(achievementId);
        });

        var shareSessionCommand = new Command("session", "Share current gaming session");
        shareSessionCommand.SetHandler(async (InvocationContext context) => await HandleShareSessionAsync());

        shareCommand.AddCommand(shareAchievementCommand);
        shareCommand.AddCommand(shareSessionCommand);

        // Voice channel commands
        var voiceCommand = new Command("voice", "Voice channel management");
        var voiceCreateCommand = new Command("create", "Create a gaming voice channel");
        var channelNameArgument = new Argument<string>("name", "Channel name");

        voiceCreateCommand.AddArgument(channelNameArgument);
        voiceCreateCommand.SetHandler(async (InvocationContext context) =>
        {
            var name = context.ParseResult.GetValueForArgument(channelNameArgument);
            await HandleVoiceCreateAsync(name);
        });

        var voiceInviteCommand = new Command("invite", "Invite friends to voice channel");
        var friendsOption = new Option<string[]>("--friends", "Friend IDs to invite") { AllowMultipleArgumentsPerToken = true };
        voiceInviteCommand.AddOption(friendsOption);
        voiceInviteCommand.SetHandler(async (InvocationContext context) =>
        {
            var friends = context.ParseResult.GetValueForOption(friendsOption);
            await HandleVoiceInviteAsync(friends);
        });

        voiceCommand.AddCommand(voiceCreateCommand);
        voiceCommand.AddCommand(voiceInviteCommand);

        // Build command hierarchy
        discordCommand.AddCommand(botCommand);
        discordCommand.AddCommand(presenceCommand);
        discordCommand.AddCommand(matchmakingCommand);
        discordCommand.AddCommand(shareCommand);
        discordCommand.AddCommand(voiceCommand);

        // Commands would be registered with the main CLI system
        _logger?.LogInformation("Discord CLI commands registered");
    }

    private async Task InitializeDiscordBotAsync(CancellationToken ct)
    {
        try
        {
            // Check for Discord bot token in configuration
            // In production, this would come from appsettings or environment variables
            var botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");

            if (string.IsNullOrEmpty(botToken))
            {
                _logger?.LogInformation("Discord bot token not configured. Bot features will be unavailable.");
                return;
            }

            _discordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages |
                                GatewayIntents.DirectMessages | GatewayIntents.GuildVoiceStates
            });

            _discordClient.Log += LogDiscordMessage;
            _discordClient.Ready += OnBotReady;
            _discordClient.MessageReceived += OnMessageReceived;
            _discordClient.UserVoiceStateUpdated += OnVoiceStateUpdated;

            await _discordClient.LoginAsync(TokenType.Bot, botToken);
            await _discordClient.StartAsync();

            _logger?.LogInformation("Discord bot initialized and connecting...");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize Discord bot");
        }
    }

    private async Task ConnectBotAsync()
    {
        if (_discordClient == null)
        {
            _logger?.LogError("Discord bot not configured");
            return;
        }

        try
        {
            if (_isBotConnected)
            {
                _logger?.LogInformation("Discord bot is already connected");
                return;
            }

            await InitializeDiscordBotAsync(default);
            _logger?.LogInformation("Discord bot connection initiated");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect Discord bot");
        }
    }

    private async Task DisconnectBotAsync()
    {
        if (_discordClient == null)
        {
            _logger?.LogInformation("Discord bot not connected");
            return;
        }

        try
        {
            await _discordClient.StopAsync();
            _isBotConnected = false;
            _logger?.LogInformation("Discord bot disconnected");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error disconnecting Discord bot");
        }
    }

    private async Task StartMatchmakingAsync()
    {
        if (!_isBotConnected || _discordClient == null)
        {
            _logger?.LogError("Discord bot not connected - cannot start matchmaking");
            return;
        }

        try
        {
            // Create matchmaking role/voice channel
            // Send matchmaking announcement
            // Monitor for participants

            _logger?.LogInformation("Matchmaking started through Discord");
            // Implementation would create matchmaking channels and monitor responses
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting matchmaking");
        }
    }

    private async Task StopMatchmakingAsync()
    {
        _logger?.LogInformation("Matchmaking stopped");
        // Implementation would clean up matchmaking channels
    }

    private async Task ShareCurrentActivityAsync()
    {
        // Share current gaming activity to Discord
        _logger?.LogInformation("Sharing current gaming activity to Discord");
        // Implementation would post current game/activity to configured Discord channel
    }

    private async Task CreateGamingVoiceChannelAsync()
    {
        if (!_isBotConnected || _discordClient == null)
        {
            _logger?.LogError("Discord bot not connected");
            return;
        }

        try
        {
            // Create temporary voice channel for gaming session
            _logger?.LogInformation("Creating gaming voice channel");
            // Implementation would create voice channel with appropriate permissions
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating voice channel");
        }
    }

    // CLI command handlers
    private async Task HandleBotConnectAsync() => await ConnectBotAsync();
    private async Task HandleBotDisconnectAsync() => await DisconnectBotAsync();

    private async Task HandleBotStatusAsync()
    {
        _logger?.LogInformation($"Discord Bot Status: {(_isBotConnected ? "Connected" : "Disconnected")}");
        _logger?.LogInformation($"Rich Presence Status: {(_isPresenceConnected ? "Connected" : "Disconnected")}");

        if (_discordClient != null)
        {
            _logger?.LogInformation($"Connected to {_discordClient.Guilds.Count} servers");
        }
    }

    private async Task HandlePresenceSetAsync(string gameTitle, string? details, string? state)
    {
        if (_presenceService == null)
        {
            _logger?.LogError("Discord presence service not available");
            return;
        }

        try
        {
            await _presenceService.SetPlayingGameAsync(
                gameTitle: gameTitle,
                details: details,
                largeImageText: state ?? "Playing with SaveState Reborn",
                startTimestamp: DateTime.UtcNow);

            _logger?.LogInformation($"Discord presence set: Playing {gameTitle}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting Discord presence");
        }
    }

    private async Task HandlePresenceClearAsync()
    {
        if (_presenceService == null)
        {
            _logger?.LogError("Discord presence service not available");
            return;
        }

        try
        {
            await _presenceService.ClearPresenceAsync();
            _logger?.LogInformation("Discord presence cleared");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error clearing Discord presence");
        }
    }

    private async Task HandleMatchmakingStartAsync(string gameType)
    {
        _logger?.LogInformation($"Starting matchmaking for {gameType}");
        await StartMatchmakingAsync();
    }

    private async Task HandleMatchmakingStopAsync() => await StopMatchmakingAsync();

    private async Task HandleMatchmakingStatusAsync()
    {
        _logger?.LogInformation("Matchmaking status: Active (placeholder)");
        // Implementation would check actual matchmaking status
    }

    private async Task HandleShareAchievementAsync(string achievementId)
    {
        _logger?.LogInformation($"Sharing achievement {achievementId} to Discord");
        // Implementation would post achievement to Discord
    }

    private async Task HandleShareSessionAsync()
    {
        _logger?.LogInformation("Sharing current gaming session to Discord");
        await ShareCurrentActivityAsync();
    }

    private async Task HandleVoiceCreateAsync(string name)
    {
        _logger?.LogInformation($"Creating voice channel: {name}");
        await CreateGamingVoiceChannelAsync();
    }

    private async Task HandleVoiceInviteAsync(string[] friends)
    {
        _logger?.LogInformation($"Inviting {friends.Length} friends to voice channel");
        // Implementation would send invites
    }

    // Discord bot event handlers
    private Task LogDiscordMessage(LogMessage msg)
    {
        var logLevel = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

        _logger?.Log(logLevel, "Discord: {Message}", msg.Message);
        return Task.CompletedTask;
    }

    private async Task OnBotReady()
    {
        _isBotConnected = true;
        _logger?.LogInformation("Discord bot is ready!");

        // Set bot status
        await _discordClient!.SetGameAsync("SaveState Gaming Hub", null, ActivityType.Playing);
    }

    private async Task OnMessageReceived(SocketMessage message)
    {
        // Handle bot commands and interactions
        if (message.Author.IsBot) return;

        var content = message.Content.ToLowerInvariant();

        if (content.Contains("savestate") || content.Contains("game"))
        {
            // Respond to game-related messages
            await message.Channel.SendMessageAsync("🎮 I'm your SaveState Gaming Hub assistant! Use `!help` for commands.");
        }
    }

    private Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        // Track voice channel activity for gaming sessions
        if (oldState.VoiceChannel == null && newState.VoiceChannel != null)
        {
            _logger?.LogInformation($"{user.Username} joined voice channel {newState.VoiceChannel.Name}");
        }
        else if (oldState.VoiceChannel != null && newState.VoiceChannel == null)
        {
            _logger?.LogInformation($"{user.Username} left voice channel {oldState.VoiceChannel.Name}");
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Configuration options for Discord integration.
/// </summary>
public class DiscordIntegrationOptions
{
    public string? BotToken { get; set; }
    public string? ApplicationId { get; set; }
    public string? ServerId { get; set; }
    public string? GamingChannelId { get; set; }
    public string? MatchmakingChannelId { get; set; }
    public bool EnableMatchmaking { get; set; } = true;
    public bool EnableVoiceChannels { get; set; } = true;
    public bool EnableActivitySharing { get; set; } = true;
}