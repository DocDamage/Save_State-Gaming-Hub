using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of IRemoteCommandExecutor for integration testing.
/// </summary>
public class FakeRemoteCommandExecutor : IRemoteCommandExecutor
{
    private readonly Queue<RemoteCommandMessage> _commandQueue = new();

    public event EventHandler<CommandExecutedEventArgs>? OnCommandExecuted;
    public event EventHandler<CommandFailedEventArgs>? OnCommandFailed;

    public Task<Result> ExecuteCommandAsync(RemoteCommandMessage command)
    {
        _commandQueue.Enqueue(command);

        OnCommandExecuted?.Invoke(this, new CommandExecutedEventArgs
        {
            CommandId = command.Id,
            Command = command.Command,
            ExecutedAt = DateTime.UtcNow,
            ExecutionTime = TimeSpan.FromMilliseconds(10)
        });

        return Task.FromResult(Result.Success());
    }

    public Task<Result> ExecuteGamepadInputAsync(GamepadInput input)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ExecuteTouchpadInputAsync(TouchpadInput input)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result> ExecuteKeyboardInputAsync(KeyboardInput input)
    {
        return Task.FromResult(Result.Success());
    }

    public int GetQueueLength()
    {
        return _commandQueue.Count;
    }

    public void ClearQueue()
    {
        _commandQueue.Clear();
    }
}
