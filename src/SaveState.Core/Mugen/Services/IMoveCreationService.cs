using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Mugen.Services;

public interface IMoveCreationService
{
    Task<Result<IReadOnlyList<MoveTemplate>>> GetMoveTemplatesAsync(MoveCategory? category = null, CancellationToken cancellationToken = default);
    Task<Result<MoveDefinition>> CreateMoveFromTemplateAsync(MoveTemplate template, string name, string command, CancellationToken cancellationToken = default);
    Task<Result<ValidationResult>> ValidateMoveAsync(MoveDefinition move, ValidationOptions options, CancellationToken cancellationToken = default);
    Task<Result<TestResult>> TestMoveAsync(MoveDefinition move, TestParameters parameters, CancellationToken cancellationToken = default);
    Task<Result<MoveExportResult>> ExportMoveAsync(MoveDefinition move, ExportOptions options, CancellationToken cancellationToken = default);
    
    // Additional methods for UI integration
    Task<Result<IReadOnlyList<MugenCharacterSummary>>> GetAvailableCharactersAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<MugenMoveEntryDto>>> GetCharacterMovesAsync(Guid characterId, CancellationToken cancellationToken = default);
    Task<Result<MoveDefinition>> CreateMoveAsync(Guid characterId, MoveData moveData, CancellationToken cancellationToken = default);
    Task<Result<MoveDefinition>> UpdateMoveAsync(Guid characterId, string moveName, MoveData moveData, CancellationToken cancellationToken = default);
    Task<Result> DeleteMoveAsync(Guid characterId, string moveName, CancellationToken cancellationToken = default);
    Task<Result<string>> TestMoveAsync(Guid characterId, MoveData moveData, CancellationToken cancellationToken = default);
    Task<Result<string>> ExportMoveAsync(Guid characterId, string moveName, CancellationToken cancellationToken = default);
}
