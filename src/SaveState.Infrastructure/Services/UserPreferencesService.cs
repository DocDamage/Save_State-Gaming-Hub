using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using System.Security.Cryptography;
using System.Text;

namespace SaveState.Infrastructure.Services;

/// <summary>
/// Simple file-based implementation of user preferences service.
/// Stores preferences in a local JSON file.
/// </summary>
public class UserPreferencesService : SaveState.Core.Common.Services.IUserPreferencesService
{
    private const int MinBackgroundAlertCooldownSeconds = 15;
    private const int MaxBackgroundAlertCooldownSeconds = 600;

    private readonly ILogger<UserPreferencesService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly string _preferencesFilePath;
    private UserPreferences? _cachedPreferences;

    public UserPreferencesService(ILogger<UserPreferencesService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "SaveStateReborn");
        Directory.CreateDirectory(appFolder);
        _preferencesFilePath = Path.Combine(appFolder, "preferences.json");
    }

    public async Task<bool> ShouldShowOnboardingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var preferences = await LoadPreferencesAsync(cancellationToken).ConfigureAwait(false);
            return !preferences.OnboardingCompleted;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load user preferences, defaulting to show onboarding");
            return true; // Default to showing onboarding if we can't load preferences
        }
    }

    public async Task CompleteOnboardingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var preferences = await LoadPreferencesAsync(cancellationToken).ConfigureAwait(false);
            preferences.OnboardingCompleted = true;
            preferences.OnboardingCompletedAt = _timeProvider.UtcNow;
            await SavePreferencesAsync(preferences, cancellationToken).ConfigureAwait(false);
            _cachedPreferences = preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save onboarding completion status");
            throw;
        }
    }

    public async Task<string> GetPreferredAiProviderAsync(CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        return prefs.PreferredAiProvider;
    }

    public async Task SetPreferredAiProviderAsync(string provider, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        prefs.PreferredAiProvider = provider;
        await SavePreferencesAsync(prefs, ct);
    }

    public async Task<string> GetPreferredAiModelAsync(CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        return prefs.PreferredAiModel;
    }

    public async Task SetPreferredAiModelAsync(string model, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        prefs.PreferredAiModel = model;
        await SavePreferencesAsync(prefs, ct);
    }

    public async Task<string> GetAiApiKeyAsync(string provider, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        var encrypted = provider.ToLowerInvariant() switch
        {
            "openai" => prefs.EncryptedOpenAiApiKey,
            "groq" => prefs.EncryptedGroqApiKey,
            _ => null
        };

        if (string.IsNullOrEmpty(encrypted)) return string.Empty;

        try
        {
            return Decrypt(encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt API key for {Provider}", provider);
            return string.Empty;
        }
    }

    public async Task SetAiApiKeyAsync(string provider, string apiKey, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        var encrypted = string.IsNullOrEmpty(apiKey) ? string.Empty : Encrypt(apiKey);

        switch (provider.ToLowerInvariant())
        {
            case "openai":
                prefs.EncryptedOpenAiApiKey = encrypted;
                break;
            case "groq":
                prefs.EncryptedGroqApiKey = encrypted;
                break;
        }

        await SavePreferencesAsync(prefs, ct);
    }

    public async Task<string> GetPreferredCloudProviderAsync(CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        return prefs.PreferredCloudProvider;
    }

    public async Task SetPreferredCloudProviderAsync(string provider, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        prefs.PreferredCloudProvider = provider;
        await SavePreferencesAsync(prefs, ct);
    }

    public async Task<bool> GetAutoSyncOnExitAsync(CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        return prefs.AutoSyncOnExit;
    }

    public async Task SetAutoSyncOnExitAsync(bool enabled, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        prefs.AutoSyncOnExit = enabled;
        await SavePreferencesAsync(prefs, ct);
    }

    public async Task<string> GetCloudClientIdAsync(string provider, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        var providerKey = provider.ToLowerInvariant().Replace(" ", string.Empty);
        var encrypted = providerKey switch
        {
            "onedrive" => prefs.EncryptedOneDriveClientId,
            "googledrive" => prefs.EncryptedGoogleDriveClientId,
            _ => null
        };

        if (string.IsNullOrEmpty(encrypted)) return string.Empty;
        try
        {
            return Decrypt(encrypted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt cloud client ID for {Provider}", provider);
            return string.Empty;
        }
    }

    public async Task SetCloudClientIdAsync(string provider, string clientId, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        var encrypted = string.IsNullOrEmpty(clientId) ? string.Empty : Encrypt(clientId);

        switch (provider.ToLowerInvariant().Replace(" ", ""))
        {
            case "onedrive":
                prefs.EncryptedOneDriveClientId = encrypted;
                break;
            case "googledrive":
                prefs.EncryptedGoogleDriveClientId = encrypted;
                break;
        }

        await SavePreferencesAsync(prefs, ct);
    }

    public async Task<bool> GetBackgroundSyncFailureAlertsEnabledAsync(CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        return prefs.BackgroundSyncFailureAlertsEnabled;
    }

    public async Task SetBackgroundSyncFailureAlertsEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        prefs.BackgroundSyncFailureAlertsEnabled = enabled;
        await SavePreferencesAsync(prefs, ct);
    }

    public async Task<bool> GetBackgroundSyncConflictAlertsEnabledAsync(CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        return prefs.BackgroundSyncConflictAlertsEnabled;
    }

    public async Task SetBackgroundSyncConflictAlertsEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        prefs.BackgroundSyncConflictAlertsEnabled = enabled;
        await SavePreferencesAsync(prefs, ct);
    }

    public async Task<int> GetBackgroundSyncAlertCooldownSecondsAsync(CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        return ClampBackgroundAlertCooldownSeconds(prefs.BackgroundSyncAlertCooldownSeconds);
    }

    public async Task SetBackgroundSyncAlertCooldownSecondsAsync(int cooldownSeconds, CancellationToken ct = default)
    {
        var prefs = await LoadPreferencesAsync(ct);
        prefs.BackgroundSyncAlertCooldownSeconds = ClampBackgroundAlertCooldownSeconds(cooldownSeconds);
        await SavePreferencesAsync(prefs, ct);
    }

    private string Encrypt(string clearText)
    {
        if (string.IsNullOrEmpty(clearText)) return string.Empty;
        var data = Encoding.UTF8.GetBytes(clearText);
        var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    private string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return string.Empty;
        var data = Convert.FromBase64String(encryptedText);
        var decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }

    private async Task<UserPreferences> LoadPreferencesAsync(CancellationToken cancellationToken)
    {
        if (_cachedPreferences != null)
        {
            return _cachedPreferences;
        }

        if (!File.Exists(_preferencesFilePath))
        {
            return new UserPreferences();
        }

        var json = await File.ReadAllTextAsync(_preferencesFilePath, cancellationToken).ConfigureAwait(false);
        var preferences = System.Text.Json.JsonSerializer.Deserialize<UserPreferences>(json) ?? new UserPreferences();
        _cachedPreferences = preferences;
        return preferences;
    }

    private async Task SavePreferencesAsync(UserPreferences preferences, CancellationToken cancellationToken)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(preferences, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(_preferencesFilePath, json, cancellationToken).ConfigureAwait(false);
    }

    private class UserPreferences
    {
        public bool OnboardingCompleted { get; set; }
        public DateTime? OnboardingCompletedAt { get; set; }
        public string PreferredAiProvider { get; set; } = "OpenAI";
        public string PreferredAiModel { get; set; } = "gpt-4";
        public string? EncryptedOpenAiApiKey { get; set; }
        public string? EncryptedGroqApiKey { get; set; }
        public string PreferredCloudProvider { get; set; } = string.Empty;
        public bool AutoSyncOnExit { get; set; } = true;
        public string? EncryptedOneDriveClientId { get; set; }
        public string? EncryptedGoogleDriveClientId { get; set; }
        public bool BackgroundSyncFailureAlertsEnabled { get; set; } = true;
        public bool BackgroundSyncConflictAlertsEnabled { get; set; } = true;
        public int BackgroundSyncAlertCooldownSeconds { get; set; } = 60;
    }

    private static int ClampBackgroundAlertCooldownSeconds(int cooldownSeconds)
    {
        if (cooldownSeconds < MinBackgroundAlertCooldownSeconds)
        {
            return MinBackgroundAlertCooldownSeconds;
        }

        if (cooldownSeconds > MaxBackgroundAlertCooldownSeconds)
        {
            return MaxBackgroundAlertCooldownSeconds;
        }

        return cooldownSeconds;
    }
}
