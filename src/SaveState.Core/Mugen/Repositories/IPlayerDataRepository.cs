using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Mugen.Repositories;

public interface IPlayerDataRepository
{
    Task<Result<PlayerSkill>> GetPlayerSkillAsync(string playerId, CancellationToken cancellationToken = default);
}
