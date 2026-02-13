namespace SaveState.Core.Common.Services;

/// <summary>
/// Service for managing user preferences and settings.
/// </summary>
public interface IUserPreferencesService
{
    /// <summary>
    /// Gets whether the onboarding flow should be shown to the user.
    /// </summary>
    /// <returns>True if onboarding should be shown, false otherwise.</returns>
    Task<bool> ShouldShowOnboardingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the onboarding flow as completed.
    /// </summary>
    Task CompleteOnboardingAsync(CancellationToken cancellationToken = default);

    Task<string> GetPreferredAiProviderAsync(CancellationToken ct = default);
    Task SetPreferredAiProviderAsync(string provider, CancellationToken ct = default);

    Task<string> GetPreferredAiModelAsync(CancellationToken ct = default);
    Task SetPreferredAiModelAsync(string model, CancellationToken ct = default);

    Task<string> GetAiApiKeyAsync(string provider, CancellationToken ct = default);
    Task SetAiApiKeyAsync(string provider, string apiKey, CancellationToken ct = default);

    Task<string> GetPreferredCloudProviderAsync(CancellationToken ct = default);
    Task SetPreferredCloudProviderAsync(string provider, CancellationToken ct = default);

    Task<bool> GetAutoSyncOnExitAsync(CancellationToken ct = default);
    Task SetAutoSyncOnExitAsync(bool enabled, CancellationToken ct = default);

    Task<string> GetCloudClientIdAsync(string provider, CancellationToken ct = default);
    Task SetCloudClientIdAsync(string provider, string clientId, CancellationToken ct = default);

    Task<bool> GetBackgroundSyncFailureAlertsEnabledAsync(CancellationToken ct = default);
    Task SetBackgroundSyncFailureAlertsEnabledAsync(bool enabled, CancellationToken ct = default);

    Task<bool> GetBackgroundSyncConflictAlertsEnabledAsync(CancellationToken ct = default);
    Task SetBackgroundSyncConflictAlertsEnabledAsync(bool enabled, CancellationToken ct = default);

    Task<int> GetBackgroundSyncAlertCooldownSecondsAsync(CancellationToken ct = default);
    Task SetBackgroundSyncAlertCooldownSecondsAsync(int cooldownSeconds, CancellationToken ct = default);
}
