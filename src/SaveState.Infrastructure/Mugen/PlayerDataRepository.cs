using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Repositories;
using SaveState.Core.Mugen.ValueObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Mugen;

public class PlayerDataRepository : IPlayerDataRepository
{
    private readonly ITimeProvider _timeProvider;

    public PlayerDataRepository(ITimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task<Result<PlayerSkill>> GetPlayerSkillAsync(string playerId, CancellationToken cancellationToken = default)
    {
        var skill = new PlayerSkill(playerId, 1500, 0, new Dictionary<string, double>(), _timeProvider.UtcNow);
        return Task.FromResult(Result.Success(skill));
    }
}
