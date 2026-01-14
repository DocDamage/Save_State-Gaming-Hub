using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Enums;
using System;
using System.Collections.Generic;

namespace SaveState.Application.Performance.Queries;

public record GetMemoryWatchesQuery(Guid GameId) : IRequest<Result<IReadOnlyList<MemoryWatchDto>>>;

public record MemoryWatchDto(Guid Id, string Label, long Address, string? CurrentValue, MemoryDataType DataType, bool IsFrozen);
