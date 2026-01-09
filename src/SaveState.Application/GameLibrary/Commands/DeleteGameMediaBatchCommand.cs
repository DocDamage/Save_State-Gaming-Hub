using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.GameLibrary.Commands;

public record DeleteGameMediaBatchCommand(IEnumerable<Guid> MediaIds) : IRequest<Result>;
