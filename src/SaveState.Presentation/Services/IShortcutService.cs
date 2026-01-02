using Avalonia.Input;

namespace SaveState.Presentation.Services;

/// <summary>
/// Service for managing keyboard shortcuts throughout the application.
/// </summary>
public interface IShortcutService
{
    /// <summary>
    /// Registers a global keyboard shortcut.
    /// </summary>
    /// <param name="gesture">The key gesture.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="description">Description of the shortcut for help/documentation.</param>
    void RegisterGlobal(KeyGesture gesture, Action action, string description);

    /// <summary>
    /// Registers a contextual keyboard shortcut that only works in specific contexts.
    /// </summary>
    /// <param name="context">The context identifier (e.g., "Library", "MUGEN").</param>
    /// <param name="gesture">The key gesture.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="description">Description of the shortcut for help/documentation.</param>
    void RegisterContextual(string context, KeyGesture gesture, Action action, string description);

    /// <summary>
    /// Unregisters a keyboard shortcut.
    /// </summary>
    /// <param name="gesture">The key gesture to unregister.</param>
    void Unregister(KeyGesture gesture);

    /// <summary>
    /// Gets all registered shortcut bindings.
    /// </summary>
    /// <returns>A read-only list of all shortcut bindings.</returns>
    IReadOnlyList<ShortcutBinding> GetAllBindings();

    /// <summary>
    /// Loads user-customized shortcuts from storage.
    /// </summary>
    Task LoadUserCustomizations();

    /// <summary>
    /// Saves user-customized shortcuts to storage.
    /// </summary>
    Task SaveUserCustomizations();

    /// <summary>
    /// Gets the context-specific shortcuts for the given context.
    /// </summary>
    /// <param name="context">The context identifier.</param>
    /// <returns>A list of shortcuts for the context.</returns>
    IReadOnlyList<ShortcutBinding> GetContextualShortcuts(string context);

    /// <summary>
    /// Executes a shortcut if it's registered.
    /// </summary>
    /// <param name="gesture">The key gesture that was pressed.</param>
    /// <param name="context">The current context (optional).</param>
    /// <returns>True if a shortcut was executed, false otherwise.</returns>
    bool ExecuteShortcut(KeyGesture gesture, string? context = null);
}

/// <summary>
/// Represents a keyboard shortcut binding.
/// </summary>
public record ShortcutBinding(
    KeyGesture Gesture,
    string Description,
    string Context,
    bool IsCustomized,
    Action Action);