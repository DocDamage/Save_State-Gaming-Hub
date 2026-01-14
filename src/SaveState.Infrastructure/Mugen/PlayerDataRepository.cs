using SaveState.Core.Common;
using SaveState.Core.Mugen.Repositories;
using SaveState.Core.Mugen.ValueObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Mugen;

public class PlayerDataRepository : IPlayerDataRepository
{
    public Task<Result<PlayerSkill>> GetPlayerSkillAsync(string playerId, CancellationToken cancellationToken = default)
    {
        var skill = new PlayerSkill(playerId, 1500, 0, new Dictionary<string, double>(), DateTime.UtcNow);
        return Task.FromResult(Result.Success(skill));
    }
}
