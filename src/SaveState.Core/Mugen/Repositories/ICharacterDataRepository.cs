using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Mugen.Repositories;

/// <summary>
/// Repository for storing and retrieving character statistics for ML analysis.
/// </summary>
public interface ICharacterDataRepository
{
    /// <summary>
    /// Gets the statistics for a specific character.
    /// </summary>
    Task<Result<CharacterAttributes>> GetCharacterStatsAsync(string characterName, CancellationToken ct = default);

    /// <summary>
    /// Updates the statistics for a specific character.
    /// </summary>
    Task<Result> UpdateCharacterStatsAsync(string characterName, CharacterAttributes stats, CancellationToken ct = default);
}
