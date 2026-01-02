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
        // TODO: Load user customizations from storage
        // For now, this is a placeholder
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveUserCustomizations()
    {
        // TODO: Save user customizations to storage
        // For now, this is a placeholder
        await Task.CompletedTask;
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