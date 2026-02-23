using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.QuickActions;
using SaveState.Presentation.ViewModels.QuickActions;
using SaveState.Presentation.Views.QuickActions;

namespace SaveState.Presentation.Services.QuickActions;

/// <summary>
/// Implementation of the quick action service.
/// </summary>
public sealed class QuickActionService : IQuickActionService
{
    private readonly ILogger<QuickActionService> _logger;
    private readonly ConcurrentDictionary<string, List<QuickAction>> _contextMenus = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, QuickAction> _globalActions = new(StringComparer.OrdinalIgnoreCase);
    private readonly IOverlayService _overlayService;
    private QuickActionMenuWindow? _currentMenuWindow;
    private readonly object _menuLock = new();

    public QuickActionService(
        ILogger<QuickActionService> logger,
        IOverlayService overlayService)
    {
        _logger = logger;
        _overlayService = overlayService;
    }

    /// <inheritdoc />
    public event EventHandler<QuickActionExecutedEventArgs>? ActionExecuted;

    /// <inheritdoc />
    public event EventHandler<QuickActionContext>? ContextMenuOpened;

    /// <inheritdoc />
    public event EventHandler? ContextMenuClosed;

    /// <inheritdoc />
    public void RegisterContextMenu(string context, List<QuickAction> actions)
    {
        ArgumentException.ThrowIfNullOrEmpty(context);
        ArgumentNullException.ThrowIfNull(actions);

        _contextMenus[context] = actions;
        _logger.LogDebug("Registered context menu '{Context}' with {Count} actions", context, actions.Count);
    }

    /// <inheritdoc />
    public void UnregisterContextMenu(string context)
    {
        _contextMenus.TryRemove(context, out _);
        _logger.LogDebug("Unregistered context menu '{Context}'", context);
    }

    /// <inheritdoc />
    public async Task ShowContextMenuAsync(string context, QuickActionContext data, Point position)
    {
        lock (_menuLock)
        {
            // Close existing menu if open
            CloseCurrentMenu();

            // Get actions for context
            var actions = GetActionsForContext(context, data);
            if (actions.Count == 0)
            {
                _logger.LogWarning("No actions available for context '{Context}'", context);
                return;
            }

            // Create and show menu window
            var viewModel = new QuickActionMenuViewModel(this, _overlayService)
            {
                CurrentContext = data,
                OpenPosition = position
            };

            // Organize actions into groups
            viewModel.InitializeWithActions(actions);

            _currentMenuWindow = new QuickActionMenuWindow
            {
                DataContext = viewModel,
                Position = new PixelPoint((int)position.X, (int)position.Y)
            };

            _currentMenuWindow.Closed += OnMenuWindowClosed;
            ContextMenuOpened?.Invoke(this, data);

            _currentMenuWindow.Show();
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ExecuteActionAsync(string actionId, QuickActionContext context)
    {
        var action = GetAction(actionId);
        if (action == null)
        {
            _logger.LogWarning("Action '{ActionId}' not found", actionId);
            var unknownAction = new QuickAction { Id = actionId, Label = "Unknown" };
            var notFoundResult = QuickActionResult.Failure($"Action '{actionId}' not found");
            OnActionExecuted(new QuickActionExecutedEventArgs(unknownAction, notFoundResult));
            return;
        }

        if (!action.IsEnabled)
        {
            _logger.LogWarning("Action '{ActionId}' is disabled", actionId);
            var disabledResult = QuickActionResult.Failure("Action is disabled");
            OnActionExecuted(new QuickActionExecutedEventArgs(action, disabledResult));
            return;
        }

        // Show confirmation if required
        if (!string.IsNullOrEmpty(action.ConfirmationMessage))
        {
            var result = await ShowConfirmationAsync(action.ConfirmationMessage);
            if (!result)
            {
                return;
            }
        }

        try
        {
            _logger.LogInformation("Executing action '{ActionId}'", actionId);

            if (action.ExecuteAsync != null)
            {
                await action.ExecuteAsync();
            }

            var successResult = QuickActionResult.Success();
            OnActionExecuted(new QuickActionExecutedEventArgs(action, successResult));

            // Close menu after successful execution
            CloseCurrentMenu();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute action '{ActionId}'", actionId);
            var errorResult = QuickActionResult.Failure(ex.Message);
            OnActionExecuted(new QuickActionExecutedEventArgs(action, errorResult));
        }
    }

    /// <inheritdoc />
    public List<QuickAction> GetAvailableActions(QuickActionContext context)
    {
        var allActions = _globalActions.Values.ToList();

        // Filter by context
        if (!string.IsNullOrEmpty(context.CurrentView))
        {
            if (_contextMenus.TryGetValue(context.CurrentView, out var viewActions))
            {
                allActions.AddRange(viewActions);
            }
        }

        // Filter by game context
        if (context.SelectedGame != null)
        {
            if (_contextMenus.TryGetValue("Game", out var gameActions))
            {
                allActions.AddRange(gameActions);
            }
        }

        // Filter by save state context
        if (context.SelectedSaveState != null)
        {
            if (_contextMenus.TryGetValue("SaveState", out var saveStateActions))
            {
                allActions.AddRange(saveStateActions);
            }
        }

        return allActions
            .Where(a => a.IsVisible)
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.Label)
            .ToList();
    }

    /// <inheritdoc />
    public List<QuickAction> SearchActions(string query, QuickActionContext context)
    {
        var actions = GetAvailableActions(context);

        if (string.IsNullOrWhiteSpace(query))
        {
            return actions;
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        return actions
            .Select(a => new { Action = a, Score = CalculateSearchScore(a, normalizedQuery) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Action.Priority)
            .Select(x => x.Action)
            .ToList();
    }

    /// <inheritdoc />
    public void RegisterAction(QuickAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentException.ThrowIfNullOrEmpty(action.Id);

        _globalActions[action.Id] = action;
        _logger.LogDebug("Registered global action '{ActionId}'", action.Id);
    }

    /// <inheritdoc />
    public void UnregisterAction(string actionId)
    {
        _globalActions.TryRemove(actionId, out _);
        _logger.LogDebug("Unregistered global action '{ActionId}'", actionId);
    }

    /// <inheritdoc />
    public QuickAction? GetAction(string actionId)
    {
        _globalActions.TryGetValue(actionId, out var action);
        return action;
    }

    private List<QuickAction> GetActionsForContext(string context, QuickActionContext data)
    {
        var actions = new List<QuickAction>();

        // Get context-specific actions
        if (_contextMenus.TryGetValue(context, out var contextActions))
        {
            actions.AddRange(contextActions);
        }

        // Add global actions that apply to this context
        actions.AddRange(_globalActions.Values.Where(a =>
            a.Tags.Contains(context, StringComparer.OrdinalIgnoreCase) ||
            a.Tags.Contains("Global", StringComparer.OrdinalIgnoreCase)));

        // Filter by visibility
        actions = actions.Where(a => a.IsVisible).ToList();

        // Apply context-specific filtering
        if (data.SelectedGame == null)
        {
            // Remove game-specific actions if no game selected
            actions.RemoveAll(a => a.Tags.Contains("RequiresGame"));
        }

        if (data.SelectedSaveState == null)
        {
            // Remove save state actions if no save state selected
            actions.RemoveAll(a => a.Tags.Contains("RequiresSaveState"));
        }

        return actions
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.Label)
            .ToList();
    }

    private static double CalculateSearchScore(QuickAction action, string query)
    {
        var score = 0.0;

        // Exact match on label
        if (action.Label.Equals(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 100;
        }
        // Starts with query
        else if (action.Label.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 80;
        }
        // Contains query
        else if (action.Label.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 60;
        }

        // Match on ID
        if (action.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 40;
        }

        // Match on description
        if (!string.IsNullOrEmpty(action.Description) &&
            action.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            score += 30;
        }

        // Match on tags
        foreach (var tag in action.Tags)
        {
            if (tag.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                score += 20;
            }
        }

        // Boost by priority
        score += action.Priority * 0.1;

        return score;
    }

    private void OnMenuWindowClosed(object? sender, EventArgs e)
    {
        lock (_menuLock)
        {
            if (_currentMenuWindow != null)
            {
                _currentMenuWindow.Closed -= OnMenuWindowClosed;
                _currentMenuWindow = null;
            }
            ContextMenuClosed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void CloseCurrentMenu()
    {
        lock (_menuLock)
        {
            if (_currentMenuWindow != null)
            {
                _currentMenuWindow.Close();
                _currentMenuWindow = null;
            }
        }
    }

    private void OnActionExecuted(QuickActionExecutedEventArgs args)
    {
        ActionExecuted?.Invoke(this, args);
    }

    private static async Task<bool> ShowConfirmationAsync(string message)
    {
        // This would typically use a dialog service
        // For now, return true (confirmed)
        await Task.CompletedTask;
        return true;
    }
}
