using SaveState.Core.Common;

namespace SaveState.Infrastructure.WebBrowser.ExtensionSupport;

/// <summary>
/// Manages browser extensions for the integrated web browser.
/// </summary>
public interface IExtensionManager
{
    /// <summary>
    /// Gets all installed extensions.
    /// </summary>
    Task<IReadOnlyList<BrowserExtension>> GetInstalledExtensionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Loads an unpacked extension from the specified directory.
    /// </summary>
    Task<Result<BrowserExtension>> LoadUnpackedExtensionAsync(string directoryPath, CancellationToken ct = default);

    /// <summary>
    /// Unloads an extension.
    /// </summary>
    Task<Result> UnloadExtensionAsync(string extensionId, CancellationToken ct = default);

    /// <summary>
    /// Enables or disables an extension.
    /// </summary>
    Task<Result> SetExtensionEnabledAsync(string extensionId, bool enabled, CancellationToken ct = default);

    /// <summary>
    /// Gets extension content scripts for a specific URL.
    /// </summary>
    Task<IReadOnlyList<ContentScript>> GetContentScriptsForUrlAsync(string url, CancellationToken ct = default);

    /// <summary>
    /// Injects content scripts into a page.
    /// </summary>
    Task<Result> InjectContentScriptsAsync(string pageUrl, IList<ContentScript> scripts, CancellationToken ct = default);

    /// <summary>
    /// Event raised when an extension is loaded.
    /// </summary>
    event EventHandler<ExtensionLoadedEventArgs>? ExtensionLoaded;

    /// <summary>
    /// Event raised when an extension is unloaded.
    /// </summary>
    event EventHandler<ExtensionUnloadedEventArgs>? ExtensionUnloaded;
}

/// <summary>
/// Represents a browser extension.
/// </summary>
public class BrowserExtension
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsPacked { get; set; }
    public ExtensionManifest Manifest { get; set; } = new();
    public List<ContentScript> ContentScripts { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public DateTime InstalledAt { get; set; }
    public ExtensionIcon? Icon { get; set; }
}

/// <summary>
/// Extension manifest data.
/// </summary>
public class ExtensionManifest
{
    public int ManifestVersion { get; set; } = 2;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public List<ContentScriptDefinition> ContentScripts { get; set; } = new();
    public Dictionary<string, string> Icons { get; set; } = new();
    public BackgroundScript? Background { get; set; }
    public BrowserAction? BrowserAction { get; set; }
    public List<string> HostPermissions { get; set; } = new();
}

/// <summary>
/// Content script definition from manifest.
/// </summary>
public class ContentScriptDefinition
{
    public List<string> Matches { get; set; } = new();
    public List<string> ExcludeMatches { get; set; } = new();
    public List<string> Js { get; set; } = new();
    public List<string> Css { get; set; } = new();
    public string RunAt { get; set; } = "document_idle";
    public bool AllFrames { get; set; }
}

/// <summary>
/// A content script ready for injection.
/// </summary>
public class ContentScript
{
    public string ExtensionId { get; set; } = string.Empty;
    public string ExtensionName { get; set; } = string.Empty;
    public string? JavaScript { get; set; }
    public string? Css { get; set; }
    public string RunAt { get; set; } = "document_idle";
    public bool AllFrames { get; set; }
    public List<string> Matches { get; set; } = new();
}

/// <summary>
/// Background script definition.
/// </summary>
public class BackgroundScript
{
    public List<string> Scripts { get; set; } = new();
    public bool Persistent { get; set; } = true;
}

/// <summary>
/// Browser action definition.
/// </summary>
public class BrowserAction
{
    public string DefaultIcon { get; set; } = string.Empty;
    public string DefaultTitle { get; set; } = string.Empty;
    public string? DefaultPopup { get; set; }
}

/// <summary>
/// Extension icon information.
/// </summary>
public class ExtensionIcon
{
    public string Path16 { get; set; } = string.Empty;
    public string Path32 { get; set; } = string.Empty;
    public string Path48 { get; set; } = string.Empty;
    public string Path128 { get; set; } = string.Empty;
}

/// <summary>
/// Event args for extension loaded event.
/// </summary>
public class ExtensionLoadedEventArgs : EventArgs
{
    public required BrowserExtension Extension { get; init; }
}

/// <summary>
/// Event args for extension unloaded event.
/// </summary>
public class ExtensionUnloadedEventArgs : EventArgs
{
    public required string ExtensionId { get; init; }
}
