using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Performance.Commands;

public record UpdateMemoryWatchesCommand(Guid GameId, int ProcessId) : IRequest<Result<int>>;
