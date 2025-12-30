using Microsoft.Extensions.Logging;

namespace SaveState.Infrastructure.Services;

/// <summary>
/// Simple file-based implementation of user preferences service.
/// Stores preferences in a local JSON file.
/// </summary>
public class UserPreferencesService : SaveState.Core.Common.Services.IUserPreferencesService
{
    private readonly ILogger<UserPreferencesService> _logger;
    private readonly string _preferencesFilePath;
    private UserPreferences? _cachedPreferences;

    public UserPreferencesService(ILogger<UserPreferencesService> logger)
    {
        _logger = logger;
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
            preferences.OnboardingCompletedAt = DateTime.UtcNow;
            await SavePreferencesAsync(preferences, cancellationToken).ConfigureAwait(false);
            _cachedPreferences = preferences;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save onboarding completion status");
            throw;
        }
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
    }
}
