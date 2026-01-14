using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Enums;
using System;

namespace SaveState.Application.Performance.Commands;

public record AddMemoryWatchCommand(Guid GameId, string Label, long Address, MemoryDataType DataType) : IRequest<Result<Guid>>;

public record ModifyMemoryWatchCommand(Guid WatchId, bool IsFrozen) : IRequest<Result>;

public record WriteMemoryValueCommand(Guid WatchId, int ProcessId, string Value) : IRequest<Result>;

public record UpdateMemoryWatchesCommand(Guid GameId, int ProcessId) : IRequest<Result>;
