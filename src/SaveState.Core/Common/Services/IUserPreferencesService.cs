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
}
