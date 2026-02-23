using Avalonia;
using SaveState.Presentation.Models.QuickActions;

namespace SaveState.Presentation.Services.QuickActions;

/// <summary>
/// Service for managing and executing quick actions throughout the application.
/// </summary>
public interface IQuickActionService
{
    /// <summary>
    /// Registers a context menu for a specific context.
    /// </summary>
    /// <param name="context">The context identifier (e.g., "GameCard", "SaveState", "Library").</param>
    /// <param name="actions">The actions to include in the context menu.</param>
    void RegisterContextMenu(string context, List<QuickAction> actions);

    /// <summary>
    /// Unregisters a context menu.
    /// </summary>
    /// <param name="context">The context identifier to unregister.</param>
    void UnregisterContextMenu(string context);

    /// <summary>
    /// Shows a context menu for the specified context.
    /// </summary>
    /// <param name="context">The context identifier.</param>
    /// <param name="data">The context data for the menu.</param>
    /// <param name="position">The screen position to show the menu.</param>
    Task ShowContextMenuAsync(string context, QuickActionContext data, Point position);

    /// <summary>
    /// Executes a specific action by ID.
    /// </summary>
    /// <param name="actionId">The ID of the action to execute.</param>
    /// <param name="context">The context for execution.</param>
    Task ExecuteActionAsync(string actionId, QuickActionContext context);

    /// <summary>
    /// Gets all available actions for a given context.
    /// </summary>
    /// <param name="context">The context to get actions for.</param>
    /// <returns>List of available actions.</returns>
    List<QuickAction> GetAvailableActions(QuickActionContext context);

    /// <summary>
    /// Searches for actions matching the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="context">The current context.</param>
    /// <returns>Matching actions.</returns>
    List<QuickAction> SearchActions(string query, QuickActionContext context);

    /// <summary>
    /// Registers a single global action.
    /// </summary>
    /// <param name="action">The action to register.</param>
    void RegisterAction(QuickAction action);

    /// <summary>
    /// Unregisters a global action.
    /// </summary>
    /// <param name="actionId">The ID of the action to unregister.</param>
    void UnregisterAction(string actionId);

    /// <summary>
    /// Gets an action by ID.
    /// </summary>
    /// <param name="actionId">The action ID.</param>
    /// <returns>The action, or null if not found.</returns>
    QuickAction? GetAction(string actionId);

    /// <summary>
    /// Event raised when an action is executed.
    /// </summary>
    event EventHandler<QuickActionExecutedEventArgs>? ActionExecuted;

    /// <summary>
    /// Event raised when the context menu is opened.
    /// </summary>
    event EventHandler<QuickActionContext>? ContextMenuOpened;

    /// <summary>
    /// Event raised when the context menu is closed.
    /// </summary>
    event EventHandler? ContextMenuClosed;
}
