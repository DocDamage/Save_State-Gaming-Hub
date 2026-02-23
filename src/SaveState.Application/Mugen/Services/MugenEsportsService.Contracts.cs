using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// MUGEN Esports service interface.
/// </summary>
public interface MugenEsportsServiceIMugenEsportsService
{
    Task<Result<MugenEsportsServiceEsportsLeague>> CreateLeagueAsync(MugenEsportsServiceLeagueCreationRequest request, CancellationToken ct = default);
    Task<Result> RegisterTeamForLeagueAsync(string leagueId, string teamId, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceProfessionalPlayer>> RegisterProfessionalPlayerAsync(MugenEsportsServicePlayerRegistrationRequest request, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceTeam>> CreateTeamAsync(MugenEsportsServiceTeamCreationRequest request, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceLeagueRankings>> GetLeagueRankingsAsync(string leagueId, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceGlobalRankings>> GetGlobalRankingsAsync(MugenEsportsServiceRankingPeriod period, CancellationToken ct = default);
    Task<Result> UpdatePlayerStatisticsAsync(string playerId, MatchResult result, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceSponsorshipDeal>> CreateSponsorshipAsync(MugenEsportsServiceSponsorshipRequest request, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceEsportsEvent>> CreateEsportsEventAsync(MugenEsportsServiceEventCreationRequest request, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceProfessionalPlayer>> GetPlayerProfileAsync(string playerId, CancellationToken ct = default);
    Task<Result<MugenEsportsServiceTeam>> GetTeamProfileAsync(string teamId, CancellationToken ct = default);
}
