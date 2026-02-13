namespace SaveState.Core.Common.Constants;

/// <summary>
/// Standardized error messages used throughout the application.
/// These messages are returned in Result.Failure() calls and exceptions.
/// </summary>
public static class ErrorMessages
{
    // Not Found Errors
    public const string NotFound = "Not found";
    public const string GameNotFound = "Game not found";
    public const string UserNotFound = "User not found";
    public const string TournamentNotFound = "Tournament not found";
    public const string ParticipantNotFound = "Participant not found";
    public const string CharacterNotFound = "Character not found";
    public const string SaveStateNotFound = "Save state not found";
    public const string RomNotFound = "ROM not found";
    public const string EmulatorNotFound = "Emulator not found";
    public const string ProfileNotFound = "Profile not found";
    public const string CollectionNotFound = "Collection not found";
    public const string ItemNotFound = "Item not found";
    public const string ReviewNotFound = "Review not found";
    public const string PostNotFound = "Post not found";
    public const string FriendNotFound = "Friend not found";
    public const string StreamNotFound = "Stream not found";
    public const string MatchNotFound = "Match not found";
    public const string PluginNotFound = "Plugin not found";
    public const string ApiKeyNotFound = "API key not found";
    public const string ThemeNotFound = "Theme not found";
    public const string FileNotFound = "File not found";
    public const string DirectoryNotFound = "Directory not found";
    public const string ResourceNotFound = "Resource not found";

    // Authentication & Authorization Errors
    public const string InvalidCredentials = "Invalid username or password";
    public const string InvalidToken = "Invalid token";
    public const string InvalidRefreshToken = "Invalid refresh token";
    public const string InvalidApiKey = "Invalid API key";
    public const string TokenExpired = "Token has expired";
    public const string AccessDenied = "Access denied";
    public const string Unauthorized = "Unauthorized";
    public const string Forbidden = "Forbidden";
    public const string AuthenticationFailed = "Authentication failed due to an internal error";
    public const string TokenRefreshFailed = "Token refresh failed due to an internal error";
    public const string InvalidTokenType = "Invalid token type";
    public const string UserInactive = "User not found or inactive";
    public const string InsufficientPermissions = "Insufficient permissions";

    // Configuration Errors
    public const string NotConfigured = "Not configured";
    public const string NotEnabled = "Not enabled";
    public const string ConfigurationMissing = "Configuration missing";
    public const string CredentialsMissing = "Credentials missing";
    public const string PathNotConfigured = "Path not configured";
    public const string FeatureNotConfigured = "Feature not configured";
    public const string FeatureNotEnabled = "Feature is not enabled";
    public const string EnvironmentVariableMissing = "Environment variable not set";
    public const string RequiredSettingMissing = "Required setting is missing";

    // Validation Errors
    public const string ValidationFailed = "Validation failed";
    public const string InvalidInput = "Invalid input";
    public const string InvalidFormat = "Invalid format";
    public const string InvalidId = "Invalid identifier";
    public const string AlreadyExists = "Already exists";
    public const string DuplicateEntry = "Duplicate entry";
    public const string ValueRequired = "Value is required";
    public const string ValueOutOfRange = "Value is out of range";
    public const string InvalidEmail = "Invalid email address";
    public const string InvalidUrl = "Invalid URL";
    public const string InvalidDate = "Invalid date";

    // Operation Errors
    public const string OperationFailed = "Operation failed";
    public const string OperationCancelled = "Operation cancelled";
    public const string OperationTimeout = "Operation timed out";
    public const string OperationNotSupported = "Operation not supported";
    public const string CreateFailed = "Failed to create";
    public const string UpdateFailed = "Failed to update";
    public const string DeleteFailed = "Failed to delete";
    public const string SaveFailed = "Failed to save";
    public const string LoadFailed = "Failed to load";
    public const string ImportFailed = "Failed to import";
    public const string ExportFailed = "Failed to export";
    public const string SyncFailed = "Failed to synchronize";

    // External Service Errors
    public const string ExternalServiceError = "External service error";
    public const string NetworkError = "Network error";
    public const string ServiceUnavailable = "Service unavailable";
    public const string RateLimitExceeded = "Rate limit exceeded";
    public const string ApiError = "API error";
    public const string CloudServiceError = "Cloud service error";
    public const string DiscordNotConfigured = "Discord integration not configured";
    public const string SteamNotConfigured = "Steam integration not configured";

    // File/IO Errors
    public const string FileAccessDenied = "File access denied";
    public const string FileInUse = "File is in use";
    public const string InvalidPath = "Invalid path";
    public const string PathTooLong = "Path too long";
    public const string InsufficientDiskSpace = "Insufficient disk space";
    public const string ReadError = "Read error";
    public const string WriteError = "Write error";

    // Game/MUGEN Specific
    public const string CharacterAlreadyInRoster = "Character is already in roster";
    public const string StageNotFound = "Stage not found";
    public const string ModNotFound = "Mod not found";
    public const string TournamentFull = "Tournament is full";
    public const string TournamentInProgress = "Tournament is already in progress";
    public const string TournamentCompleted = "Tournament has already completed";
    public const string InvalidGameState = "Invalid game state";
    public const string ProcessNotFound = "Process not found";

    // Cloud Gaming Specific
    public const string CloudGaming = "Cloud gaming";
    public const string CloudGamingNotConfigured = "Cloud gaming not configured";

    // Cloud Sync Specific
    public const string CloudSyncNotConfigured = "Cloud sync not configured";
    public const string CloudSyncFailed = "Cloud synchronization failed";
    public const string CloudProviderNotConfigured = "Cloud provider not configured";
    public const string CloudProviderNotEnabled = "Cloud provider not enabled";
    public const string NotAuthenticated = "Not authenticated";
    public const string AuthenticationRequired = "Authentication required";
    public const string SyncConflict = "Synchronization conflict detected";
    public const string UploadFailed = "Upload failed";
    public const string DownloadFailed = "Download failed";

    // Generic
    public const string InvalidValue = "Invalid value";
    public const string UnknownError = "An unknown error occurred";
    public const string InternalError = "An internal error occurred";
    public const string UnexpectedError = "An unexpected error occurred";
    public const string RequestFailed = "Request failed";
}
