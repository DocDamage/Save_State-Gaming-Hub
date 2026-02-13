namespace SaveState.Core.Common.Constants;

/// <summary>
/// Environment variable names used throughout the application.
/// Centralizes access to environment-based configuration.
/// </summary>
public static class EnvironmentVariables
{
    // Application
    public const string AspNetCoreEnvironment = "ASPNETCORE_ENVIRONMENT";
    public const string DotNetEnvironment = "DOTNET_ENVIRONMENT";

    // Database
    public const string DatabaseConnectionString = "DATABASE_CONNECTION_STRING";

    // Security
    public const string JwtSecret = "JWT_SECRET";
    public const string JwtIssuer = "JWT_ISSUER";
    public const string JwtAudience = "JWT_AUDIENCE";

    // AI Providers
    public const string OpenAiApiKey = "OPENAI_API_KEY";
    public const string GroqApiKey = "GROQ_API_KEY";
    public const string AnthropicApiKey = "ANTHROPIC_API_KEY";
    public const string GoogleAiApiKey = "GOOGLE_AI_API_KEY";

    // Cloud Sync - Google Drive
    public const string GoogleDriveClientId = "GOOGLE_DRIVE_CLIENT_ID";
    public const string GoogleDriveClientSecret = "GOOGLE_DRIVE_CLIENT_SECRET";
    public const string GoogleDriveRefreshToken = "GOOGLE_DRIVE_REFRESH_TOKEN";

    // Cloud Sync - AWS S3
    public const string AwsAccessKeyId = "AWS_ACCESS_KEY_ID";
    public const string AwsSecretAccessKey = "AWS_SECRET_ACCESS_KEY";
    public const string AwsRegion = "AWS_REGION";
    public const string AwsS3BucketName = "AWS_S3_BUCKET_NAME";

    // Cloud Sync - Azure
    public const string AzureStorageConnectionString = "AZURE_STORAGE_CONNECTION_STRING";
    public const string AzureBlobContainerName = "AZURE_BLOB_CONTAINER_NAME";

    // External Services - Steam
    public const string SteamApiKey = "STEAM_API_KEY";
    public const string UserSteamId = "USER_STEAM_ID";

    // External Services - Discord
    public const string DiscordBotToken = "DISCORD_BOT_TOKEN";
    public const string DiscordClientId = "DISCORD_CLIENT_ID";
    public const string DiscordClientSecret = "DISCORD_CLIENT_SECRET";

    // External Services - RetroAchievements
    public const string RetroAchievementsUsername = "RETRO_ACHIEVEMENTS_USERNAME";
    public const string RetroAchievementsApiKey = "RETRO_ACHIEVEMENTS_API_KEY";

    // External Services - Twitch
    public const string TwitchClientId = "TWITCH_CLIENT_ID";
    public const string TwitchClientSecret = "TWITCH_CLIENT_SECRET";
    public const string TwitchAccessToken = "TWITCH_ACCESS_TOKEN";

    // Cloud Gaming
    public const string AmazonLunaApiKey = "AMAZON_LUNA_API_KEY";
    public const string PlayStationNowAccountId = "PLAYSTATION_NOW_ACCOUNT_ID";
    public const string XboxCloudAccountEmail = "XBOX_CLOUD_ACCOUNT_EMAIL";
    public const string GeForceNowApiKey = "GEFORCE_NOW_API_KEY";

    // Feature Flags
    public const string EnableAnalytics = "ENABLE_ANALYTICS";
    public const string EnableSocialFeatures = "ENABLE_SOCIAL_FEATURES";
    public const string EnableCloudSync = "ENABLE_CLOUD_SYNC";
    public const string EnableAiAssistant = "ENABLE_AI_ASSISTANT";
    public const string EnableStreaming = "ENABLE_STREAMING";

    // Logging
    public const string LogLevel = "LOG_LEVEL";
    public const string LogOutputPath = "LOG_OUTPUT_PATH";

    // Performance
    public const string MemoryLimitMB = "MEMORY_LIMIT_MB";
    public const string EnablePerformanceMonitoring = "ENABLE_PERFORMANCE_MONITORING";

    // Development
    public const string DevelopmentMode = "DEVELOPMENT_MODE";
    public const string SkipMigrations = "SKIP_MIGRATIONS";
    public const string SeedData = "SEED_DATA";
}
