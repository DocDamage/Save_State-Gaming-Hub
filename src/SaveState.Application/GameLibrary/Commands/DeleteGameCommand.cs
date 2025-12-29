using MediatR;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.GameLibrary.Commands;

public record DeleteGameCommand(GameId GameId) : IRequest<Unit>;