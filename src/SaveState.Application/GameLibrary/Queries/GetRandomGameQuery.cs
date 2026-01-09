using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Application.GameLibrary.Queries;

public record GetRandomGameQuery : IRequest<Result<Game>>;
