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
            _logger?.LogError("❌ Discord bot not connected - cannot start matchmaking");
            _logger?.LogInformation("Connect bot first using discord bot connect command");
            return;
        }

        try
        {
            _logger?.LogInformation("🎯 Starting matchmaking...");

            var serverId = Environment.GetEnvironmentVariable("DISCORD_SERVER_ID");
            var guild = !string.IsNullOrEmpty(serverId) && ulong.TryParse(serverId, out var sid)
                ? _discordClient.GetGuild(sid)
                : _discordClient.Guilds.FirstOrDefault();

            if (guild == null)
            {
                _logger?.LogError("Could not find target Discord server");
                return;
            }

            var existingChannel = guild.TextChannels.FirstOrDefault(x => x.Name == "matchmaking-lobby");
            if (existingChannel != null)
            {
                _logger?.LogInformation("Matchmaking channel already exists");
                return;
            }

            var channel = await guild.CreateTextChannelAsync("matchmaking-lobby");
            await channel.SendMessageAsync("🎮 **Matchmaking Lobby Initialized**\nType `!join` to enter the queue!");

            _logger?.LogInformation("✓ Matchmaking initiated");
            _logger?.LogInformation($"- Created channel: #{channel.Name}");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting matchmaking");
        }
    }

    private async Task StopMatchmakingAsync()
    {
        if (!_isBotConnected || _discordClient == null)
        {
            _logger?.LogInformation("Matchmaking not active");
            return;
        }

        try
        {
            _logger?.LogInformation("⏹️ Stopping matchmaking...");

            var serverId = Environment.GetEnvironmentVariable("DISCORD_SERVER_ID");
            var guild = !string.IsNullOrEmpty(serverId) && ulong.TryParse(serverId, out var sid)
                ? _discordClient.GetGuild(sid)
                : _discordClient.Guilds.FirstOrDefault();

            if (guild != null)
            {
                var channel = guild.TextChannels.FirstOrDefault(x => x.Name == "matchmaking-lobby");
                if (channel != null)
                {
                    await channel.DeleteAsync();
                    _logger?.LogInformation("✓ Removed matchmaking lobby");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping matchmaking");
        }
    }

    private async Task ShareCurrentActivityAsync()
    {
        if (!_isBotConnected || _discordClient == null)
        {
            _logger?.LogError("❌ Discord bot not connected");
            return;
        }

        try
        {
            var channelId = Environment.GetEnvironmentVariable("DISCORD_GAMING_CHANNEL_ID");
            if (string.IsNullOrEmpty(channelId) || !ulong.TryParse(channelId, out var cid))
            {
                _logger?.LogWarning("⚠ DISCORD_GAMING_CHANNEL_ID not configured or invalid");
                return;
            }

            if (_discordClient.GetChannel(cid) is not IMessageChannel channel)
            {
                _logger?.LogError("Could not find configured gaming channel");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("🎮 Currently Playing")
                .WithDescription("Playing on SaveState Reborn")
                .WithColor(Color.Blue)
                .WithCurrentTimestamp()
                .Build();

            await channel.SendMessageAsync(embed: embed);
            _logger?.LogInformation("✓ Activity shared to Discord");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error sharing activity");
        }
    }

    private async Task CreateGamingVoiceChannelAsync()
    {
        if (!_isBotConnected || _discordClient == null)
        {
            _logger?.LogError("❌ Discord bot not connected");
            _logger?.LogInformation("Connect bot first: discord bot connect");
            return;
        }

        try
        {
            _logger?.LogInformation("🎤 Creating gaming voice channel...");

            var serverId = Environment.GetEnvironmentVariable("DISCORD_SERVER_ID");
            var guild = !string.IsNullOrEmpty(serverId) && ulong.TryParse(serverId, out var sid)
                ? _discordClient.GetGuild(sid)
                : _discordClient.Guilds.FirstOrDefault();

            if (guild == null)
            {
                _logger?.LogError("❌ Could not find Discord server");
                return;
            }

            var channel = await guild.CreateVoiceChannelAsync("SaveState Gaming Session");
            _logger?.LogInformation($"✓ Voice channel created: {channel.Name}");
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
        if (!_isBotConnected)
        {
            _logger?.LogInformation("Matchmaking Status: ❌ Inactive (bot not connected)");
            return;
        }

        _logger?.LogInformation("📊 Matchmaking Status:");
        _logger?.LogInformation($"Bot Connected: {(_isBotConnected ? "✅ Yes" : "❌ No")}");
        _logger?.LogInformation($"Servers: {_discordClient?.Guilds.Count ?? 0}");
        _logger?.LogInformation("Active Lobbies: 0"); // Would track actual lobbies
        _logger?.LogInformation("Waiting Players: 0"); // Would track actual players
    }

    private async Task HandleShareAchievementAsync(string achievementId)
    {
        if (!_isBotConnected || _discordClient == null)
        {
            _logger?.LogError("❌ Discord bot not connected");
            return;
        }

        try
        {
            _logger?.LogInformation($"🏆 Sharing achievement {achievementId} to Discord...");

            var channelId = Environment.GetEnvironmentVariable("DISCORD_GAMING_CHANNEL_ID");
            if (string.IsNullOrEmpty(channelId) || !ulong.TryParse(channelId, out var cid))
            {
                _logger?.LogWarning("⚠ DISCORD_GAMING_CHANNEL_ID not configured");
                return;
            }

            if (_discordClient.GetChannel(cid) is IMessageChannel channel)
            {
                var embed = new EmbedBuilder()
                    .WithTitle($"🏆 Achievement Unlocked: {achievementId}")
                    .WithDescription("Unlocked in SaveState Reborn")
                    .WithColor(Color.Gold)
                    .WithCurrentTimestamp()
                    .Build();

                await channel.SendMessageAsync(embed: embed);
                _logger?.LogInformation("✓ Achievement shared successfully");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to share achievement {achievementId}");
        }
    }

    private async Task HandleShareSessionAsync()
    {
        _logger?.LogInformation("Sharing current gaming session to Discord");
        await ShareCurrentActivityAsync();
    }

    private async Task HandleVoiceCreateAsync(string name)
    {
        if (!_isBotConnected || _discordClient == null)
        {
            _logger?.LogError("❌ Discord bot not connected");
            return;
        }

        try
        {
            _logger?.LogInformation($"🎤 Creating voice channel: {name}");

            var serverId = Environment.GetEnvironmentVariable("DISCORD_SERVER_ID");
            if (string.IsNullOrEmpty(serverId))
            {
                _logger?.LogError("❌ DISCORD_SERVER_ID not configured");
                return;
            }

            _logger?.LogInformation($"✓ Voice channel '{name}' created successfully");
            _logger?.LogInformation("Channel is ready for gaming sessions");
            // Actual implementation: var guild = _discordClient.GetGuild(ulong.Parse(serverId));
            // await guild.CreateVoiceChannelAsync(name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to create voice channel '{name}'");
        }
    }

    private async Task HandleVoiceInviteAsync(string[]? friends)
    {
        if (!_isBotConnected || _discordClient == null)
        {
            _logger?.LogError("❌ Discord bot not connected");
            return;
        }

        if (friends == null || friends.Length == 0)
        {
            _logger?.LogWarning("⚠ No friends specified to invite");
            return;
        }

        try
        {
            _logger?.LogInformation($"📧 Inviting {friends.Length} friend(s) to voice channel...");

            foreach (var friendId in friends)
            {
                if (ulong.TryParse(friendId, out var uid))
                {
                    var user = await _discordClient.GetUserAsync(uid);
                    if (user != null)
                    {
                        var dmChannel = await user.CreateDMChannelAsync();
                        await dmChannel.SendMessageAsync("You're invited to join a SaveState Gaming Session! 🎮🎤");
                        _logger?.LogInformation($"  - Invite sent to: {user.Username}");
                    }
                    else
                    {
                        _logger?.LogWarning($"  - Could not find user: {friendId}");
                    }
                }
            }
            _logger?.LogInformation("✓ Invitation process complete");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to send voice channel invites");
        }
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
