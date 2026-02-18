namespace SaveState.Plugins.TwitchStreaming;

/// <summary>
/// String constants for Twitch Streaming Plugin.
/// </summary>
public static class TwitchStreamingStrings
{
    // Plugin Info
    public const string PluginId = "savestate.twitch.streaming";
    public const string PluginName = "Twitch Streaming Integration";
    public const string PluginVersion = "1.0.0";
    public const string PluginAuthor = "SaveState Team";
    public const string PluginDescription = "Complete Twitch streaming integration with OBS, chat bot, and gaming overlays";

    // Log Messages
    public const string LogInitializing = "Initializing Twitch Streaming Integration plugin";
    public const string LogInitialized = "Twitch Streaming Integration plugin initialized";
    public const string LogShuttingDown = "Shutting down Twitch Streaming Integration plugin";
    public const string LogCliCommandsRegistered = "Twitch Streaming CLI commands registered";

    // Menu Items
    public const string MenuStreamStatusId = "twitch.stream.status";
    public const string MenuStreamStatusLabel = "Stream Status";
    public const string MenuStreamStatusIcon = "📺";
    public const string MenuChatBotId = "twitch.chat.bot";
    public const string MenuChatBotLabel = "Chat Bot";
    public const string MenuChatBotIcon = "💬";
    public const string MenuObsIntegrationId = "twitch.obs.integration";
    public const string MenuObsIntegrationLabel = "OBS Integration";
    public const string MenuObsIntegrationIcon = "🎥";

    // CLI Commands
    public const string CliTwitchDescription = "Twitch streaming integration commands";
    public const string CliStatusDescription = "Show Twitch stream status";
    public const string CliStartDescription = "Start Twitch stream";
    public const string CliStopDescription = "Stop Twitch stream";
    public const string CliChatDescription = "Chat bot commands";
    public const string CliChatSendDescription = "Send message to chat";
    public const string CliChatMessageDescription = "Message to send";
    public const string CliObsDescription = "OBS integration commands";
}
