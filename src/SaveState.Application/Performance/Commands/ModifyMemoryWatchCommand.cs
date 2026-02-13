using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Performance.Commands;

public record ModifyMemoryWatchCommand(
    Guid WatchId,
    string? Label = null,
    bool? IsFrozen = null,
    string? Description = null) : IRequest<Result>;
