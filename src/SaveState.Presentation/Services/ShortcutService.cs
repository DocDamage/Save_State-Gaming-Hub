using Avalonia.Input;
using Microsoft.Extensions.Logging;

namespace SaveState.Presentation.Services;

/// <summary>
/// Implementation of the shortcut service.
/// </summary>
public class ShortcutService : IShortcutService
{
    private readonly ILogger<ShortcutService> _logger;
    private readonly Dictionary<KeyGesture, ShortcutBinding> _globalShortcuts = new();
    private readonly Dictionary<string, Dictionary<KeyGesture, ShortcutBinding>> _contextualShortcuts = new();

    public ShortcutService(ILogger<ShortcutService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RegisterGlobal(KeyGesture gesture, Action action, string description)
    {
        var binding = new ShortcutBinding(gesture, description, "Global", false, action);

        if (_globalShortcuts.ContainsKey(gesture))
        {
            _logger.LogWarning("Global shortcut already registered: {Gesture}", gesture);
            return;
        }

        _globalShortcuts[gesture] = binding;
        _logger.LogDebug("Registered global shortcut: {Gesture} - {Description}", gesture, description);
    }

    /// <inheritdoc />
    public void RegisterContextual(string context, KeyGesture gesture, Action action, string description)
    {
        var binding = new ShortcutBinding(gesture, description, context, false, action);

        if (!_contextualShortcuts.ContainsKey(context))
        {
            _contextualShortcuts[context] = new Dictionary<KeyGesture, ShortcutBinding>();
        }

        if (_contextualShortcuts[context].ContainsKey(gesture))
        {
            _logger.LogWarning("Contextual shortcut already registered for context {Context}: {Gesture}", context, gesture);
            return;
        }

        _contextualShortcuts[context][gesture] = binding;
        _logger.LogDebug("Registered contextual shortcut for {Context}: {Gesture} - {Description}", context, gesture, description);
    }

    /// <inheritdoc />
    public void Unregister(KeyGesture gesture)
    {
        if (_globalShortcuts.Remove(gesture))
        {
            _logger.LogDebug("Unregistered global shortcut: {Gesture}", gesture);
            return;
        }

        foreach (var contextShortcuts in _contextualShortcuts.Values)
        {
            if (contextShortcuts.Remove(gesture))
            {
                _logger.LogDebug("Unregistered contextual shortcut: {Gesture}", gesture);
                return;
            }
        }

        _logger.LogWarning("Attempted to unregister unknown shortcut: {Gesture}", gesture);
    }

    /// <inheritdoc />
    public IReadOnlyList<ShortcutBinding> GetAllBindings()
    {
        var allBindings = new List<ShortcutBinding>(_globalShortcuts.Values);

        foreach (var contextShortcuts in _contextualShortcuts.Values)
        {
            allBindings.AddRange(contextShortcuts.Values);
        }

        return allBindings.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task LoadUserCustomizations()
    {
        try
        {
            var settingsPath = GetShortcutsFilePath();

            if (!File.Exists(settingsPath))
            {
                _logger.LogInformation("No user shortcut customizations found, using defaults");
                return;
            }

            var json = await File.ReadAllTextAsync(settingsPath);
            var customizations = System.Text.Json.JsonSerializer.Deserialize<ShortcutCustomizations>(json);

            if (customizations == null)
            {
                _logger.LogWarning("Failed to deserialize shortcut customizations");
                return;
            }

            // Apply customizations
            foreach (var custom in customizations.GlobalShortcuts)
            {
                var gesture = ParseKeyGesture(custom.KeyGesture);
                if (gesture != null && _globalShortcuts.ContainsKey(gesture))
                {
                    // Update existing binding with custom gesture
                    var existing = _globalShortcuts[gesture];
                    _globalShortcuts.Remove(gesture);

                    var newGesture = ParseKeyGesture(custom.CustomKeyGesture ?? custom.KeyGesture);
                    if (newGesture != null)
                    {
                        _globalShortcuts[newGesture] = existing with { Gesture = newGesture };
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} shortcut customizations", customizations.GlobalShortcuts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load user shortcut customizations");
        }
    }

    /// <inheritdoc />
    public async Task SaveUserCustomizations()
    {
        try
        {
            var customizations = new ShortcutCustomizations
            {
                GlobalShortcuts = _globalShortcuts.Select(kvp => new ShortcutCustomization
                {
                    KeyGesture = kvp.Key.ToString() ?? string.Empty,
                    Description = kvp.Value.Description,
                    Context = kvp.Value.Context,
                    CustomKeyGesture = kvp.Key.ToString() // Store current gesture
                }).ToList(),
                LastModified = DateTime.UtcNow
            };

            var json = System.Text.Json.JsonSerializer.Serialize(customizations, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            var settingsPath = GetShortcutsFilePath();
            var directory = Path.GetDirectoryName(settingsPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(settingsPath, json);
            _logger.LogInformation("Saved {Count} shortcut customizations", customizations.GlobalShortcuts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user shortcut customizations");
        }
    }

    private static string GetShortcutsFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "SaveStateReborn", "shortcuts.json");
    }

    private static KeyGesture? ParseKeyGesture(string gestureString)
    {
        try
        {
            return KeyGesture.Parse(gestureString);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ShortcutBinding> GetContextualShortcuts(string context)
    {
        return _contextualShortcuts.TryGetValue(context, out var shortcuts)
            ? shortcuts.Values.ToList().AsReadOnly()
            : Array.Empty<ShortcutBinding>();
    }

    /// <inheritdoc />
    public bool ExecuteShortcut(KeyGesture gesture, string? context = null)
    {
        // Try global shortcuts first
        if (_globalShortcuts.TryGetValue(gesture, out var globalBinding))
        {
            try
            {
                globalBinding.Action();
                _logger.LogDebug("Executed global shortcut: {Gesture}", gesture);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing global shortcut: {Gesture}", gesture);
                return false;
            }
        }

        // Try contextual shortcuts if context is provided
        if (!string.IsNullOrEmpty(context) &&
            _contextualShortcuts.TryGetValue(context, out var contextShortcuts) &&
            contextShortcuts.TryGetValue(gesture, out var contextualBinding))
        {
            try
            {
                contextualBinding.Action();
                _logger.LogDebug("Executed contextual shortcut for {Context}: {Gesture}", context, gesture);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing contextual shortcut for {Context}: {Gesture}", context, gesture);
                return false;
            }
        }

        return false;
    }
}

/// <summary>
/// Container for shortcut customizations.
/// </summary>
internal class ShortcutCustomizations
{
    public List<ShortcutCustomization> GlobalShortcuts { get; set; } = new();
    public DateTime LastModified { get; set; }
}

/// <summary>
/// Individual shortcut customization.
/// </summary>
internal class ShortcutCustomization
{
    public string KeyGesture { get; set; } = string.Empty;
    public string? CustomKeyGesture { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
}
