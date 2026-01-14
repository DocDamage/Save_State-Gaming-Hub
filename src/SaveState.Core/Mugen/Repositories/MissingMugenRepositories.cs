using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Repositories;

public interface IMatchDataRepository
{
    Task<Result> SaveMatchAsync(object matchData); // Use object or specific DTO if known
    Task<Result<IEnumerable<object>>> GetMatchesAsync();
}

public interface ICharacterDataRepository
{
    Task<Result> SaveCharacterTwistAsync(string charName, object twistData);
    Task<Result<IEnumerable<object>>> GetCharacterStatsAsync(string charName);
}


