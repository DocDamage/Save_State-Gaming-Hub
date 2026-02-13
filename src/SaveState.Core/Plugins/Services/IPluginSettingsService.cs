using System.Reflection;
using System.Text.Json;

namespace SaveState.Core.Plugins.Services;

/// <summary>
/// Service for managing plugin settings discovery and persistence.
/// </summary>
public interface IPluginSettingsService
{
    /// <summary>
    /// Discovers all settings from a plugin's settings class.
    /// </summary>
    IReadOnlyList<PluginSettingMetadata> DiscoverSettings<TSettings>() where TSettings : class, new();

    /// <summary>
    /// Discovers all settings from a plugin's settings class (non-generic).
    /// </summary>
    IReadOnlyList<PluginSettingMetadata> DiscoverSettings(Type settingsType);

    /// <summary>
    /// Loads settings from the plugin's data directory.
    /// </summary>
    Task<TSettings> LoadSettingsAsync<TSettings>(string pluginDataDirectory, CancellationToken ct = default)
        where TSettings : class, new();

    /// <summary>
    /// Saves settings to the plugin's data directory.
    /// </summary>
    Task SaveSettingsAsync<TSettings>(TSettings settings, string pluginDataDirectory, CancellationToken ct = default)
        where TSettings : class;

    /// <summary>
    /// Updates a single setting value.
    /// </summary>
    void UpdateSetting<TSettings>(TSettings settings, string propertyName, object? value) where TSettings : class;
}

/// <summary>
/// Default implementation of IPluginSettingsService.
/// </summary>
public sealed class PluginSettingsService : IPluginSettingsService
{
    private const string SettingsFileName = "settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<PluginSettingMetadata> DiscoverSettings<TSettings>() where TSettings : class, new()
    {
        return DiscoverSettings(typeof(TSettings));
    }

    public IReadOnlyList<PluginSettingMetadata> DiscoverSettings(Type settingsType)
    {
        var settings = new List<PluginSettingMetadata>();
        var properties = settingsType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var settingAttr = property.GetCustomAttribute<PluginSettingAttribute>();
            if (settingAttr == null)
            {
                // Auto-include public properties with getters and setters
                if (property.CanRead && property.CanWrite)
                {
                    settingAttr = new PluginSettingAttribute();
                }
                else
                {
                    continue;
                }
            }

            var metadata = CreateMetadata(property, settingAttr);
            settings.Add(metadata);
        }

        return settings
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Order)
            .ThenBy(s => s.DisplayName)
            .ToList();
    }

    public async Task<TSettings> LoadSettingsAsync<TSettings>(string pluginDataDirectory, CancellationToken ct = default)
        where TSettings : class, new()
    {
        var settingsPath = Path.Combine(pluginDataDirectory, SettingsFileName);

        if (!File.Exists(settingsPath))
        {
            return new TSettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(settingsPath, ct).ConfigureAwait(false);
            return JsonSerializer.Deserialize<TSettings>(json, JsonOptions) ?? new TSettings();
        }
        catch
        {
            return new TSettings();
        }
    }

    public async Task SaveSettingsAsync<TSettings>(TSettings settings, string pluginDataDirectory, CancellationToken ct = default)
        where TSettings : class
    {
        Directory.CreateDirectory(pluginDataDirectory);
        var settingsPath = Path.Combine(pluginDataDirectory, SettingsFileName);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(settingsPath, json, ct).ConfigureAwait(false);
    }

    public void UpdateSetting<TSettings>(TSettings settings, string propertyName, object? value)
        where TSettings : class
    {
        var property = typeof(TSettings).GetProperty(propertyName);
        if (property == null || !property.CanWrite)
            return;

        // Handle type conversions
        var targetType = property.PropertyType;
        var convertedValue = ConvertValue(value, targetType);
        property.SetValue(settings, convertedValue);
    }

    private PluginSettingMetadata CreateMetadata(PropertyInfo property, PluginSettingAttribute attr)
    {
        var settingType = DetermineSettingType(property);
        var rangeAttr = property.GetCustomAttribute<PluginSettingRangeAttribute>();
        var optionsAttr = property.GetCustomAttribute<PluginSettingOptionsAttribute>();
        var filePathAttr = property.GetCustomAttribute<PluginSettingFilePathAttribute>();
        var colorAttr = property.GetCustomAttribute<PluginSettingColorAttribute>();
        var multilineAttr = property.GetCustomAttribute<PluginSettingMultilineAttribute>();
        var secretAttr = property.GetCustomAttribute<PluginSettingSecretAttribute>();

        // Determine setting type based on attributes
        if (secretAttr != null)
            settingType = PluginSettingType.Password;
        else if (colorAttr != null)
            settingType = PluginSettingType.Color;
        else if (filePathAttr != null)
            settingType = filePathAttr.IsDirectory ? PluginSettingType.DirectoryPath : PluginSettingType.FilePath;
        else if (optionsAttr != null)
            settingType = PluginSettingType.Dropdown;
        else if (multilineAttr != null)
            settingType = PluginSettingType.MultilineText;
        else if (rangeAttr != null && IsNumericType(property.PropertyType))
            settingType = PluginSettingType.Slider;

        return new PluginSettingMetadata
        {
            PropertyName = property.Name,
            DisplayName = attr.DisplayName ?? FormatPropertyName(property.Name),
            Description = attr.Description,
            Category = attr.Category,
            Order = attr.Order,
            PropertyType = property.PropertyType,
            SettingType = settingType,
            RequiresRestart = attr.RequiresRestart,
            IsAdvanced = attr.IsAdvanced,
            DefaultValue = GetDefaultValue(property.PropertyType),
            Minimum = rangeAttr?.Minimum,
            Maximum = rangeAttr?.Maximum,
            Step = rangeAttr?.Step,
            Options = optionsAttr?.Options,
            FileFilter = filePathAttr?.Filter,
            FileMustExist = filePathAttr?.MustExist ?? true,
            IsDirectory = filePathAttr?.IsDirectory ?? false,
            AllowAlpha = colorAttr?.AllowAlpha ?? false,
            MultilineMinLines = multilineAttr?.MinLines ?? 3,
            MultilineMaxLines = multilineAttr?.MaxLines ?? 10,
            IsSecret = secretAttr != null
        };
    }

    private static PluginSettingType DetermineSettingType(PropertyInfo property)
    {
        var type = property.PropertyType;

        // Handle nullable types
        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
            type = underlyingType;

        if (type == typeof(bool))
            return PluginSettingType.Toggle;
        if (type == typeof(string))
            return PluginSettingType.Text;
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            return PluginSettingType.Integer;
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return PluginSettingType.Decimal;
        if (type.IsEnum)
            return PluginSettingType.EnumDropdown;
        if (type == typeof(DateTime) || type == typeof(DateOnly))
            return PluginSettingType.Date;
        if (type == typeof(TimeSpan))
            return PluginSettingType.TimeSpan;
        if (type == typeof(List<string>) || type == typeof(string[]))
            return PluginSettingType.StringList;

        return PluginSettingType.Unknown;
    }

    private static bool IsNumericType(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
            type = underlyingType;

        return type == typeof(int) || type == typeof(long) || type == typeof(short) ||
               type == typeof(byte) || type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    private static string FormatPropertyName(string name)
    {
        // Convert PascalCase to "Title Case" with spaces
        var result = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c) && result.Length > 0)
                result.Append(' ');
            result.Append(c);
        }
        return result.ToString();
    }

    private static object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        if (type == typeof(string))
            return string.Empty;
        return null;
    }

    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        var valueType = value.GetType();
        if (targetType.IsAssignableFrom(valueType))
            return value;

        // Handle nullable target types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
            targetType = underlyingType;

        // Handle enum conversion from string
        if (targetType.IsEnum && value is string stringValue)
            return Enum.Parse(targetType, stringValue, ignoreCase: true);

        // Handle TimeSpan from various formats
        if (targetType == typeof(TimeSpan))
        {
            if (value is string tsString && TimeSpan.TryParse(tsString, out var ts))
                return ts;
            if (value is double hours)
                return TimeSpan.FromHours(hours);
        }

        // Use Convert for common types
        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }
}
