namespace SaveState.Core.Plugins;

/// <summary>
/// Specifies that this plugin depends on another plugin.
/// The dependent plugin will be loaded first.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class DependsOnPluginAttribute : Attribute
{
    /// <summary>
    /// Gets the ID of the plugin this plugin depends on.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Gets whether this is an optional dependency.
    /// If true, the plugin will still load even if the dependency is missing.
    /// </summary>
    public bool Optional { get; }

    /// <summary>
    /// Gets the minimum required version of the dependency.
    /// </summary>
    public string? MinVersion { get; }

    /// <summary>
    /// Creates a new plugin dependency declaration.
    /// </summary>
    /// <param name="pluginId">The ID of the required plugin.</param>
    /// <param name="optional">Whether this dependency is optional.</param>
    /// <param name="minVersion">Minimum required version (null = any version).</param>
    public DependsOnPluginAttribute(string pluginId, bool optional = false, string? minVersion = null)
    {
        PluginId = pluginId ?? throw new ArgumentNullException(nameof(pluginId));
        Optional = optional;
        MinVersion = minVersion;
    }
}

/// <summary>
/// Specifies that this plugin conflicts with another plugin.
/// Both plugins cannot be loaded simultaneously.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ConflictsWithPluginAttribute : Attribute
{
    /// <summary>
    /// Gets the ID of the conflicting plugin.
    /// </summary>
    public string PluginId { get; }

    /// <summary>
    /// Gets a human-readable reason for the conflict.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    /// Creates a new plugin conflict declaration.
    /// </summary>
    /// <param name="pluginId">The ID of the conflicting plugin.</param>
    /// <param name="reason">Human-readable reason for the conflict.</param>
    public ConflictsWithPluginAttribute(string pluginId, string? reason = null)
    {
        PluginId = pluginId ?? throw new ArgumentNullException(nameof(pluginId));
        Reason = reason;
    }
}

/// <summary>
/// Metadata about a plugin's dependency.
/// </summary>
public sealed record PluginDependency(
    string PluginId,
    bool IsOptional,
    string? MinVersion,
    bool IsSatisfied = false,
    string? ResolvedVersion = null);

/// <summary>
/// Metadata about a plugin conflict.
/// </summary>
public sealed record PluginConflict(
    string PluginId,
    string? Reason,
    bool IsActive = false);
