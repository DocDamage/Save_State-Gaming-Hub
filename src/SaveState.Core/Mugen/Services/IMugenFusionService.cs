using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Mugen.Services;

public interface IMugenFusionService
{
    Task<Result<FusionResult>> FuseCharactersAsync(
        IEnumerable<Guid> characterIds,
        string newName,
        FusionType type,
        FusionOptions options,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<FusionMetadata>>> GetFusionsAsync(CancellationToken cancellationToken = default);

    Task<Result> DeleteFusionAsync(Guid fusionId, CancellationToken cancellationToken = default);
}
