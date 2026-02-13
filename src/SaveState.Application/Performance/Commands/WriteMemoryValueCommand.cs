using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Performance.Commands;

public record WriteMemoryValueCommand(Guid WatchId, int ProcessId, string NewValue) : IRequest<Result>;
