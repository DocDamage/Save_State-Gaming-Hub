using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common.Services;
using SaveState.Core.Configuration;

namespace SaveState.Infrastructure.Services;

/// <summary>
/// Implementation of ICultureManager for managing application culture and localization.
/// </summary>
public class CultureManager : ICultureManager
{
    private readonly ILogger<CultureManager> _logger;
    private readonly LocalizationOptions _options;
    private CultureInfo _currentCulture;
    private CultureInfo _currentUICulture;

    public CultureInfo CurrentCulture => _currentCulture;
    public CultureInfo CurrentUICulture => _currentUICulture;

    public IReadOnlyList<CultureInfo> SupportedCultures { get; }

    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    public CultureManager(
        ILogger<CultureManager> logger,
        IOptions<LocalizationOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        // Initialize supported cultures
        SupportedCultures = _options.SupportedCultures
            .Select(cultureName =>
            {
                try
                {
                    return new CultureInfo(cultureName);
                }
                catch (CultureNotFoundException ex)
                {
                    _logger.LogWarning(ex, "Invalid culture name: {CultureName}", cultureName);
                    return null;
                }
            })
            .Where(c => c != null)
            .Cast<CultureInfo>()
            .ToList()
            .AsReadOnly();

        // Set default culture
        try
        {
            _currentCulture = new CultureInfo(_options.DefaultCulture);
            _currentUICulture = new CultureInfo(_options.DefaultCulture);
        }
        catch (CultureNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invalid default culture: {DefaultCulture}, falling back to invariant culture", _options.DefaultCulture);
            _currentCulture = CultureInfo.InvariantCulture;
            _currentUICulture = CultureInfo.InvariantCulture;
        }

        // Apply the culture to the current thread
        ApplyCulture(_currentCulture, _currentUICulture);
    }

    public async Task<bool> SetCultureAsync(string cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            _logger.LogWarning("Attempted to set null or empty culture");
            return false;
        }

        CultureInfo? newCulture;
        try
        {
            newCulture = new CultureInfo(cultureName);
        }
        catch (CultureNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invalid culture name: {CultureName}", cultureName);
            return false;
        }

        // Check if the culture is supported
        if (!SupportedCultures.Any(c => c.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Culture {CultureName} is not in the list of supported cultures", cultureName);
            return false;
        }

        var oldCulture = _currentCulture;
        var oldUICulture = _currentUICulture;

        _currentCulture = newCulture;
        _currentUICulture = newCulture;

        // Apply the culture to the current thread
        ApplyCulture(_currentCulture, _currentUICulture);

        // Persist the culture setting (this could be saved to user preferences)
        // For now, we'll just log it
        _logger.LogInformation("Culture changed from {OldCulture} to {NewCulture}",
            oldCulture.Name, newCulture.Name);

        // Raise the culture changed event
        CultureChanged?.Invoke(this, new CultureChangedEventArgs(oldCulture, newCulture));

        return true;
    }

    public string GetCultureDisplayName(CultureInfo culture)
    {
        return culture.DisplayName;
    }

    public string GetCultureNativeName(CultureInfo culture)
    {
        return culture.NativeName;
    }

    public bool IsRightToLeft(CultureInfo culture)
    {
        return culture.TextInfo.IsRightToLeft;
    }

    public string FormatDate(DateTime date, string? format = null)
    {
        return date.ToString(format ?? "d", _currentCulture);
    }

    public string FormatNumber(double number, string? format = null)
    {
        return number.ToString(format ?? "N2", _currentCulture);
    }

    public string FormatCurrency(decimal amount, string? currencyCode = null)
    {
        if (!string.IsNullOrEmpty(currencyCode))
        {
            // For custom currency codes, we'd need additional logic
            // For now, use the culture's default currency formatting
            return amount.ToString("C", _currentCulture);
        }

        return amount.ToString("C", _currentCulture);
    }

    private void ApplyCulture(CultureInfo culture, CultureInfo uiCulture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = uiCulture;

        // Also set the default thread culture for new threads
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = uiCulture;

        _logger.LogDebug("Applied culture {Culture} and UI culture {UICulture}",
            culture.Name, uiCulture.Name);
    }
}
