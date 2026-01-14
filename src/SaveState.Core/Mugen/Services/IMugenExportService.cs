using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Mugen.Services;

public interface IMugenExportService
{
    Task<Result<MoveExportResult>> ExportMoveAsync(
        MugenMoveDefinition move,
        ExportOptions options,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ExportCharacterAsync(Guid characterId, string outputDirectory, CancellationToken cancellationToken = default);

    Task<Result<bool>> ExportCommandsAsync(Guid characterId, string outputPath, CancellationToken cancellationToken = default);

    Task<Result<bool>> ExportSpritesAsync(Guid characterId, string outputPath, CancellationToken cancellationToken = default);
}
