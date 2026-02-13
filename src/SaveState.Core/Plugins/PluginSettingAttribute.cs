using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Plugins;

/// <summary>
/// Marks a property as a plugin setting that can be automatically rendered in the UI.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PluginSettingAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the display name for this setting.
    /// If null, the property name is used.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets a description/tooltip for this setting.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category for grouping settings.
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Gets or sets the display order within the category.
    /// Lower numbers appear first.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets whether this setting requires a restart to take effect.
    /// </summary>
    public bool RequiresRestart { get; set; }

    /// <summary>
    /// Gets or sets whether this setting is advanced (hidden by default).
    /// </summary>
    public bool IsAdvanced { get; set; }
}

/// <summary>
/// Specifies valid range for numeric settings.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PluginSettingRangeAttribute : Attribute
{
    public double Minimum { get; }
    public double Maximum { get; }
    public double Step { get; }

    public PluginSettingRangeAttribute(double minimum, double maximum, double step = 1.0)
    {
        Minimum = minimum;
        Maximum = maximum;
        Step = step;
    }
}

/// <summary>
/// Specifies options for string settings (dropdown).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PluginSettingOptionsAttribute : Attribute
{
    public string[] Options { get; }

    public PluginSettingOptionsAttribute(params string[] options)
    {
        Options = options ?? Array.Empty<string>();
    }
}

/// <summary>
/// Specifies that a string setting is a file path.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PluginSettingFilePathAttribute : Attribute
{
    public string? Filter { get; set; }
    public bool MustExist { get; set; } = true;
    public bool IsDirectory { get; set; } = false;
}

/// <summary>
/// Specifies that a string setting is a color (hex format).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PluginSettingColorAttribute : Attribute
{
    public bool AllowAlpha { get; set; } = false;
}

/// <summary>
/// Specifies that a string setting should use a multiline text editor.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PluginSettingMultilineAttribute : Attribute
{
    public int MinLines { get; set; } = 3;
    public int MaxLines { get; set; } = 10;
}

/// <summary>
/// Specifies that a string setting contains sensitive data (password field).
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PluginSettingSecretAttribute : Attribute
{
}

/// <summary>
/// Metadata about a discovered plugin setting.
/// </summary>
public sealed class PluginSettingMetadata
{
    public required string PropertyName { get; init; }
    public required string DisplayName { get; init; }
    public required string? Description { get; init; }
    public required string Category { get; init; }
    public required int Order { get; init; }
    public required Type PropertyType { get; init; }
    public required PluginSettingType SettingType { get; init; }
    public required bool RequiresRestart { get; init; }
    public required bool IsAdvanced { get; init; }
    public object? CurrentValue { get; set; }
    public object? DefaultValue { get; init; }

    // Range settings (for numeric types)
    public double? Minimum { get; init; }
    public double? Maximum { get; init; }
    public double? Step { get; init; }

    // Options (for dropdowns)
    public string[]? Options { get; init; }

    // File path settings
    public string? FileFilter { get; init; }
    public bool FileMustExist { get; init; }
    public bool IsDirectory { get; init; }

    // Color settings
    public bool AllowAlpha { get; init; }

    // Multiline settings
    public int MultilineMinLines { get; init; }
    public int MultilineMaxLines { get; init; }

    // Secret (password) setting
    public bool IsSecret { get; init; }
}

/// <summary>
/// Types of plugin settings for UI rendering.
/// </summary>
public enum PluginSettingType
{
    /// <summary>Boolean toggle</summary>
    Toggle,

    /// <summary>Single-line text input</summary>
    Text,

    /// <summary>Multi-line text input</summary>
    MultilineText,

    /// <summary>Password/secret input</summary>
    Password,

    /// <summary>Whole number input</summary>
    Integer,

    /// <summary>Decimal number input</summary>
    Decimal,

    /// <summary>Slider for numeric values</summary>
    Slider,

    /// <summary>Dropdown selection</summary>
    Dropdown,

    /// <summary>Enum selection</summary>
    EnumDropdown,

    /// <summary>File path picker</summary>
    FilePath,

    /// <summary>Directory path picker</summary>
    DirectoryPath,

    /// <summary>Color picker</summary>
    Color,

    /// <summary>Date picker</summary>
    Date,

    /// <summary>Time span input</summary>
    TimeSpan,

    /// <summary>List of strings</summary>
    StringList,

    /// <summary>Unknown/unsupported type</summary>
    Unknown
}
