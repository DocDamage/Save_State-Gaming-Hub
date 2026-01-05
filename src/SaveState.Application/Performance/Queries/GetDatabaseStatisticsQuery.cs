using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Performance.Queries;

public record GetDatabaseStatisticsQuery : IRequest<Result<DatabaseStatistics>>;

public record DatabaseStatistics(
    string Status,
    string Size,
    int TotalGames,
    int TotalSessions,
    DateTime LastCompacted);
