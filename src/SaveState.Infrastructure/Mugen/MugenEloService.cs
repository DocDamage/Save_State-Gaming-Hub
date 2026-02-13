namespace SaveState.Infrastructure.Mugen;

using System.Linq;
using SaveState.Core.Common;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Computes ELO ratings from match history.
/// </summary>
public class MugenEloService : IMugenEloService
{
    private const double DefaultRating = 1000;
    private const double KFactor = 32;

    private readonly IMugenMatchHistoryRepository _matchHistoryRepository;
    private readonly IMugenCharacterRepository _characterRepository;

    public MugenEloService(
        IMugenMatchHistoryRepository matchHistoryRepository,
        IMugenCharacterRepository characterRepository)
    {
        _matchHistoryRepository = matchHistoryRepository;
        _characterRepository = characterRepository;
    }

    public async Task<Result<IReadOnlyList<MugenEloRating>>> GetRatingsAsync(CancellationToken ct = default)
    {
        try
        {
            var matches = await _matchHistoryRepository.GetMatchHistoriesAsync(pageNumber: 1, pageSize: 5000, ct: ct);
            var ordered = matches.Items.OrderBy(m => m.PlayedAt).ToList();

            var ratings = new Dictionary<Guid, double>();
            var wins = new Dictionary<Guid, int>();
            var losses = new Dictionary<Guid, int>();

            foreach (var match in ordered)
            {
                var p1 = match.Player1CharacterId;
                var p2 = match.Player2CharacterId;

                var rating1 = ratings.TryGetValue(p1, out var r1) ? r1 : DefaultRating;
                var rating2 = ratings.TryGetValue(p2, out var r2) ? r2 : DefaultRating;

                var expected1 = 1.0 / (1.0 + Math.Pow(10, (rating2 - rating1) / 400.0));
                var expected2 = 1.0 / (1.0 + Math.Pow(10, (rating1 - rating2) / 400.0));

                var score1 = GetScore(match.Result, true);
                var score2 = GetScore(match.Result, false);

                rating1 += KFactor * (score1 - expected1);
                rating2 += KFactor * (score2 - expected2);

                ratings[p1] = rating1;
                ratings[p2] = rating2;

                if (score1 == 1)
                {
                    wins[p1] = wins.GetValueOrDefault(p1) + 1;
                    losses[p2] = losses.GetValueOrDefault(p2) + 1;
                }
                else if (score2 == 1)
                {
                    wins[p2] = wins.GetValueOrDefault(p2) + 1;
                    losses[p1] = losses.GetValueOrDefault(p1) + 1;
                }
            }

            var characters = await _characterRepository.GetAllAsync(ct);
            var nameMap = characters.ToDictionary(
                c => c.Id,
                c => string.IsNullOrWhiteSpace(c.DisplayName) ? c.Name : c.DisplayName);

            var ratingList = ratings
                .Select(kvp =>
                {
                    var id = kvp.Key;
                    nameMap.TryGetValue(id, out var name);
                    return new MugenEloRating(
                        id,
                        name ?? id.ToString(),
                        kvp.Value,
                        wins.GetValueOrDefault(id),
                        losses.GetValueOrDefault(id));
                })
                .OrderByDescending(r => r.Rating)
                .ThenByDescending(r => r.Matches)
                .ToList();

            return Result.Success<IReadOnlyList<MugenEloRating>>(ratingList);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<MugenEloRating>>($"Failed to compute ELO: {ex.Message}");
        }
    }

    public async Task<Result<MugenEloRating>> GetPlayerRatingAsync(string playerId, CancellationToken ct = default)
    {
        try
        {
            if (Guid.TryParse(playerId, out var key))
            {
                var ratingsResult = await GetRatingsAsync(ct);
                if (!ratingsResult.IsSuccess)
                {
                    return Result.Failure<MugenEloRating>(ratingsResult.Error);
                }

                var rating = ratingsResult.Value.FirstOrDefault(r => r.CharacterId == key);
                if (rating != null)
                {
                    return Result.Success(rating);
                }
            }

            // Player not found - return failure instead of fake object with Guid.Empty
            return Result.Failure<MugenEloRating>($"Player not found: {playerId}", ErrorType.NotFound);
        }
        catch (Exception ex)
        {
            return Result.Failure<MugenEloRating>($"Failed to get player rating: {ex.Message}");
        }
    }

    private static double GetScore(MatchResult result, bool isPlayer1)
    {
        return result switch
        {
            MatchResult.Player1Win => isPlayer1 ? 1.0 : 0.0,
            MatchResult.Player2Win => isPlayer1 ? 0.0 : 1.0,
            MatchResult.Draw => 0.5,
            MatchResult.Timeout => 0.5,
            _ => 0.5
        };
    }
}
