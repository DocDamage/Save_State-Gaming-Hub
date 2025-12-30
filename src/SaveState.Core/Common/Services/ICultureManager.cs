using System.Globalization;

namespace SaveState.Core.Common.Services;

/// <summary>
/// Interface for managing application culture and localization settings.
/// </summary>
public interface ICultureManager
{
    /// <summary>
    /// Gets the current culture.
    /// </summary>
    CultureInfo CurrentCulture { get; }

    /// <summary>
    /// Gets the current UI culture.
    /// </summary>
    CultureInfo CurrentUICulture { get; }

    /// <summary>
    /// Gets all supported cultures.
    /// </summary>
    IReadOnlyList<CultureInfo> SupportedCultures { get; }

    /// <summary>
    /// Sets the current culture for the application.
    /// </summary>
    /// <param name="cultureName">The culture name (e.g., "en-US", "es-ES").</param>
    /// <returns>True if the culture was set successfully, false otherwise.</returns>
    Task<bool> SetCultureAsync(string cultureName);

    /// <summary>
    /// Gets the display name for a culture.
    /// </summary>
    /// <param name="culture">The culture to get the display name for.</param>
    /// <returns>The display name in the current UI culture.</returns>
    string GetCultureDisplayName(CultureInfo culture);

    /// <summary>
    /// Gets the native name for a culture.
    /// </summary>
    /// <param name="culture">The culture to get the native name for.</param>
    /// <returns>The native name of the culture.</returns>
    string GetCultureNativeName(CultureInfo culture);

    /// <summary>
    /// Checks if a culture is right-to-left.
    /// </summary>
    /// <param name="culture">The culture to check.</param>
    /// <returns>True if the culture uses right-to-left text direction.</returns>
    bool IsRightToLeft(CultureInfo culture);

    /// <summary>
    /// Formats a date using the current culture.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <param name="format">Optional format string.</param>
    /// <returns>The formatted date string.</returns>
    string FormatDate(DateTime date, string? format = null);

    /// <summary>
    /// Formats a number using the current culture.
    /// </summary>
    /// <param name="number">The number to format.</param>
    /// <param name="format">Optional format string.</param>
    /// <returns>The formatted number string.</returns>
    string FormatNumber(double number, string? format = null);

    /// <summary>
    /// Formats currency using the current culture.
    /// </summary>
    /// <param name="amount">The amount to format.</param>
    /// <param name="currencyCode">Optional currency code (uses culture default if null).</param>
    /// <returns>The formatted currency string.</returns>
    string FormatCurrency(decimal amount, string? currencyCode = null);

    /// <summary>
    /// Event raised when the culture changes.
    /// </summary>
    event EventHandler<CultureChangedEventArgs>? CultureChanged;
}

/// <summary>
/// Event arguments for culture change events.
/// </summary>
public class CultureChangedEventArgs : EventArgs
{
    public CultureInfo OldCulture { get; }
    public CultureInfo NewCulture { get; }

    public CultureChangedEventArgs(CultureInfo oldCulture, CultureInfo newCulture)
    {
        OldCulture = oldCulture;
        NewCulture = newCulture;
    }
}
