using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;

namespace SaveState.Application.Mugen.Queries;

public record GetMugenTournamentsQuery : IRequest<Result<IReadOnlyList<MugenTournament>>>;
