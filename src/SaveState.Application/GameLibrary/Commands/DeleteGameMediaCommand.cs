using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.GameLibrary.Commands;

public record DeleteGameMediaCommand(Guid MediaId) : IRequest<Result>;
