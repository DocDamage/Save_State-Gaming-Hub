using SaveState.Presentation.Models.QuickActions;

namespace SaveState.Presentation.Services.QuickActions;

/// <summary>
/// Event arguments for when a quick action is executed.
/// </summary>
public class QuickActionExecutedEventArgs : EventArgs
{
    /// <summary>
    /// The quick action that was executed.
    /// </summary>
    public QuickAction Action { get; }

    /// <summary>
    /// The result of the execution.
    /// </summary>
    public QuickActionResult Result { get; }

    /// <summary>
    /// When the action was executed.
    /// </summary>
    public DateTime ExecutedAt { get; }

    /// <summary>
    /// Creates a new instance of QuickActionExecutedEventArgs.
    /// </summary>
    public QuickActionExecutedEventArgs(QuickAction action, QuickActionResult result, DateTime? executedAt = null)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Result = result ?? throw new ArgumentNullException(nameof(result));
        ExecutedAt = executedAt ?? DateTime.Now;
    }
}

/// <summary>
/// Result of a quick action execution.
/// </summary>
public class QuickActionResult
{
    /// <summary>
    /// Whether the action was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Optional error message if the action failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Optional result data from the action.
    /// </summary>
    public object? Data { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static QuickActionResult Success(object? data = null)
    {
        return new QuickActionResult { IsSuccess = true, Data = data };
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static QuickActionResult Failure(string errorMessage)
    {
        return new QuickActionResult { IsSuccess = false, ErrorMessage = errorMessage };
    }
}
