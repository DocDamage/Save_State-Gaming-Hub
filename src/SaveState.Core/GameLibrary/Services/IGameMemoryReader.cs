using SaveState.Core.Common;

namespace SaveState.Core.GameLibrary.Services;

public interface IGameMemoryReader
{
    Task<Result> AttachToProcessAsync(int processId, CancellationToken ct = default);
    Task<Result> DetachAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<MemoryPattern>>> DetectPatternsAsync(CancellationToken ct = default);
    bool IsAttached { get; }
    event EventHandler<GameStateChangedEventArgs>? StateChanged;

    // Memory writing methods
    Task<Result> WriteMemoryAsync(IntPtr address, int value, CancellationToken ct = default);
    Task<Result> WriteMemoryAsync(IntPtr address, float value, CancellationToken ct = default);
    Task<Result> FreezeValueAsync(IntPtr address, object value, CancellationToken ct = default);
    Task<Result> UnfreezeValueAsync(IntPtr address, CancellationToken ct = default);

    // Memory reading methods for hex view
    Task<Result<byte[]>> ReadMemoryBytesAsync(IntPtr address, int length, CancellationToken ct = default);
    Task<Result<long>> GetModuleBaseAddressAsync(string? moduleName = null, CancellationToken ct = default);
}

public sealed record MemoryPattern(
    string Name,
    IntPtr Address,
    string ValueType,
    object CurrentValue);

public sealed class GameStateChangedEventArgs : EventArgs
{
    public GameStateType StateType { get; init; }
    public object? Data { get; init; }
}

public enum GameStateType
{
    Unknown,
    MainMenu,
    InGame,
    Paused,
    Loading,
    Cutscene,
    BossFight,
    LevelComplete
}
