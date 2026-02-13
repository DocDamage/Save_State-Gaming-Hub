namespace SaveState.Core.Common.Constants;

/// <summary>
/// Configuration section keys used in appsettings.json and IConfiguration.
/// </summary>
public static class ConfigurationKeys
{
    // Root Sections
    public const string ConnectionStrings = "ConnectionStrings";
    public const string Logging = "Logging";
    public const string AllowedHosts = "AllowedHosts";

    // Application Settings
    public const string Application = "Application";
    public const string ApplicationName = "Application:Name";
    public const string ApplicationVersion = "Application:Version";
    public const string ApplicationEnvironment = "Application:Environment";

    // Database
    public const string Database = "Database";
    public const string DefaultConnection = "ConnectionStrings:DefaultConnection";
    public const string DatabaseProvider = "Database:Provider";
    public const string EnableSensitiveDataLogging = "Database:EnableSensitiveDataLogging";
    public const string EnableDetailedErrors = "Database:EnableDetailedErrors";

    // Authentication & Security
    public const string Jwt = "Jwt";
    public const string JwtSecret = "Jwt:Secret";
    public const string JwtIssuer = "Jwt:Issuer";
    public const string JwtAudience = "Jwt:Audience";
    public const string JwtExpirationHours = "Jwt:ExpirationHours";
    public const string JwtRefreshTokenExpirationDays = "Jwt:RefreshTokenExpirationDays";

    // AI Configuration
    public const string Ai = "Ai";
    public const string AiProvider = "Ai:Provider";
    public const string AiModel = "Ai:Model";
    public const string AiMaxTokens = "Ai:MaxTokens";
    public const string AiTemperature = "Ai:Temperature";
    public const string OpenAi = "OpenAi";
    public const string OpenAiApiKey = "OpenAi:ApiKey";
    public const string Groq = "Groq";
    public const string GroqApiKey = "Groq:ApiKey";
    public const string LocalAi = "LocalAi";
    public const string LocalAiEndpoint = "LocalAi:Endpoint";

    // Cloud Sync
    public const string CloudSync = "CloudSync";
    public const string CloudSyncEnabled = "CloudSync:Enabled";
    public const string CloudSyncProvider = "CloudSync:Provider";
    public const string CloudSyncIntervalMinutes = "CloudSync:IntervalMinutes";

    // Google Drive
    public const string GoogleDrive = "GoogleDrive";
    public const string GoogleDriveEnabled = "GoogleDrive:Enabled";
    public const string GoogleDriveClientId = "GoogleDrive:ClientId";
    public const string GoogleDriveClientSecret = "GoogleDrive:ClientSecret";
    public const string GoogleDriveRefreshToken = "GoogleDrive:RefreshToken";

    // OneDrive
    public const string OneDrive = "OneDrive";
    public const string OneDriveEnabled = "OneDrive:Enabled";
    public const string OneDriveClientId = "OneDrive:ClientId";

    // AWS S3
    public const string AwsS3 = "AwsS3";
    public const string AwsS3Enabled = "AwsS3:Enabled";
    public const string AwsS3AccessKey = "AwsS3:AccessKey";
    public const string AwsS3SecretKey = "AwsS3:SecretKey";
    public const string AwsS3BucketName = "AwsS3:BucketName";
    public const string AwsS3Region = "AwsS3:Region";

    // Azure Blob
    public const string AzureBlob = "AzureBlob";
    public const string AzureBlobEnabled = "AzureBlob:Enabled";
    public const string AzureBlobConnectionString = "AzureBlob:ConnectionString";
    public const string AzureBlobContainerName = "AzureBlob:ContainerName";

    // Cloud Gaming
    public const string CloudGaming = "CloudGaming";
    public const string CloudGamingEnabled = "CloudGaming:Enabled";

    // GeForce NOW
    public const string GeForceNow = "CloudGaming:GeForceNow";
    public const string GeForceNowEnabled = "CloudGaming:GeForceNow:Enabled";

    // Xbox Cloud
    public const string XboxCloud = "CloudGaming:XboxCloud";
    public const string XboxCloudEnabled = "CloudGaming:XboxCloud:Enabled";
    public const string XboxCloudAccountEmail = "CloudGaming:XboxCloud:AccountEmail";

    // Amazon Luna
    public const string AmazonLuna = "CloudGaming:AmazonLuna";
    public const string AmazonLunaEnabled = "CloudGaming:AmazonLuna:Enabled";
    public const string AmazonLunaApiKey = "CloudGaming:AmazonLuna:ApiKey";

    // PlayStation Now
    public const string PlayStationNow = "CloudGaming:PlayStationNow";
    public const string PlayStationNowEnabled = "CloudGaming:PlayStationNow:Enabled";
    public const string PlayStationNowAccountId = "CloudGaming:PlayStationNow:AccountId";

    // External Services
    public const string Steam = "Steam";
    public const string SteamApiKey = "Steam:ApiKey";
    public const string SteamUserId = "Steam:UserId";

    public const string Discord = "Discord";
    public const string DiscordBotToken = "Discord:BotToken";
    public const string DiscordClientId = "Discord:ClientId";

    public const string RetroAchievements = "RetroAchievements";
    public const string RetroAchievementsUsername = "RetroAchievements:Username";
    public const string RetroAchievementsApiKey = "RetroAchievements:ApiKey";

    // MUGEN Configuration
    public const string Mugen = "Mugen";
    public const string MugenPath = "Mugen:Path";
    public const string MugenCharsPath = "Mugen:CharsPath";
    public const string MugenStagesPath = "Mugen:StagesPath";
    public const string MugenDataPath = "Mugen:DataPath";

    public const string MugenNetwork = "MugenNetwork";
    public const string MugenNetworkEnabled = "MugenNetwork:Enabled";
    public const string MugenNetworkPort = "MugenNetwork:Port";

    // ROM Management
    public const string RomScanning = "RomScanning";
    public const string RomScanningEnabled = "RomScanning:Enabled";
    public const string RomScanningPaths = "RomScanning:Paths";
    public const string RomScanningExtensions = "RomScanning:Extensions";

    public const string EmulatorScanning = "EmulatorScanning";
    public const string EmulatorScanningEnabled = "EmulatorScanning:Enabled";
    public const string EmulatorScanningPaths = "EmulatorScanning:Paths";

    // RetroArch
    public const string RetroArch = "RetroArch";
    public const string RetroArchPath = "RetroArch:Path";
    public const string RetroArchCoresPath = "RetroArch:CoresPath";
    public const string RetroArchSavesPath = "RetroArch:SavesPath";
    public const string RetroArchConfigPath = "RetroArch:ConfigPath";

    // Caching
    public const string Caching = "Caching";
    public const string CachingEnabled = "Caching:Enabled";
    public const string CachingDefaultExpirationMinutes = "Caching:DefaultExpirationMinutes";

    // Resilience
    public const string Resilience = "Resilience";
    public const string ResilienceRetryCount = "Resilience:RetryCount";
    public const string ResilienceRetryDelayMs = "Resilience:RetryDelayMs";
    public const string ResilienceCircuitBreakerThreshold = "Resilience:CircuitBreakerThreshold";
    public const string ResilienceCircuitBreakerDurationSeconds = "Resilience:CircuitBreakerDurationSeconds";

    // Rate Limiting
    public const string RateLimiting = "RateLimiting";
    public const string RateLimitingEnabled = "RateLimiting:Enabled";
    public const string RateLimitingRequestsPerSecond = "RateLimiting:RequestsPerSecond";

    // Memory
    public const string Memory = "Memory";
    public const string MemoryCacheSizeMB = "Memory:CacheSizeMB";
    public const string MemoryPressureThresholdMB = "Memory:PressureThresholdMB";

    // Localization
    public const string Localization = "Localization";
    public const string LocalizationDefaultCulture = "Localization:DefaultCulture";
    public const string LocalizationSupportedCultures = "Localization:SupportedCultures";

    // Feature Flags
    public const string Features = "Features";
    public const string FeatureEnableAnalytics = "Features:EnableAnalytics";
    public const string FeatureEnableSocialFeatures = "Features:EnableSocialFeatures";
    public const string FeatureEnableCloudSync = "Features:EnableCloudSync";
    public const string FeatureEnableAiAssistant = "Features:EnableAiAssistant";
    public const string FeatureEnableStreaming = "Features:EnableStreaming";
    public const string FeatureEnableMugen = "Features:EnableMugen";
}
