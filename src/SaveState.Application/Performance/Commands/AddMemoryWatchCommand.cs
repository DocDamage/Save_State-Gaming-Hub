using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Performance.Entities;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Application.Performance.Commands;

/// <summary>
/// Command to create a new memory watch.
/// </summary>
public sealed record AddMemoryWatchCommand(
    Guid GameId,
    string Label,
    long Address,
    MemoryDataType DataType,
    int[]? Offsets = null,
    string? Description = null) : IRequest<Result<MemoryWatch>>;
