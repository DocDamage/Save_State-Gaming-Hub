using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;

namespace SaveState.Core.MobileCompanion.Services;

/// <summary>
/// Interface for executing remote commands received from mobile companion devices.
/// </summary>
public interface IRemoteCommandExecutor
{
    /// <summary>
    /// Executes a remote command.
    /// </summary>
    /// <param name="command">The command message to execute.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExecuteCommandAsync(RemoteCommandMessage command);

    /// <summary>
    /// Executes a gamepad input command.
    /// </summary>
    /// <param name="input">The gamepad input to execute.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExecuteGamepadInputAsync(GamepadInput input);

    /// <summary>
    /// Executes a touchpad input command.
    /// </summary>
    /// <param name="input">The touchpad input to execute.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExecuteTouchpadInputAsync(TouchpadInput input);

    /// <summary>
    /// Executes a keyboard input command.
    /// </summary>
    /// <param name="input">The keyboard input to execute.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> ExecuteKeyboardInputAsync(KeyboardInput input);

    /// <summary>
    /// Gets the current command queue status.
    /// </summary>
    /// <returns>The number of commands in the queue.</returns>
    int GetQueueLength();

    /// <summary>
    /// Clears all pending commands from the queue.
    /// </summary>
    void ClearQueue();

    /// <summary>
    /// Event raised when a command is executed.
    /// </summary>
    event EventHandler<CommandExecutedEventArgs>? OnCommandExecuted;

    /// <summary>
    /// Event raised when a command fails to execute.
    /// </summary>
    event EventHandler<CommandFailedEventArgs>? OnCommandFailed;
}

/// <summary>
/// Event arguments for successful command execution.
/// </summary>
public class CommandExecutedEventArgs : EventArgs
{
    public Guid CommandId { get; set; }
    public RemoteControlCommand Command { get; set; }
    public DateTime ExecutedAt { get; set; }
    public TimeSpan ExecutionTime { get; set; }
}

/// <summary>
/// Event arguments for failed command execution.
/// </summary>
public class CommandFailedEventArgs : EventArgs
{
    public Guid CommandId { get; set; }
    public RemoteControlCommand Command { get; set; }
    public string Error { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}

/// <summary>
/// Configuration options for remote command execution.
/// </summary>
public class RemoteCommandExecutorOptions
{
    /// <summary>
    /// The maximum number of commands to queue.
    /// </summary>
    public int MaxQueueSize { get; set; } = 100;

    /// <summary>
    /// The timeout in seconds for command execution.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable command logging.
    /// </summary>
    public bool EnableCommandLogging { get; set; } = true;

    /// <summary>
    /// Commands that require confirmation before execution.
    /// </summary>
    public List<RemoteControlCommand> RequireConfirmation { get; set; } = new()
    {
        RemoteControlCommand.CloseGame,
        RemoteControlCommand.DeleteSaveState
    };
}
