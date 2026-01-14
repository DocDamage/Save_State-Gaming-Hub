using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Core.Mugen.DTOs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Mugen.Services;

public interface IMachineLearningService
{
    Task<Result<MatchupPrediction>> AnalyzeCharacterMatchupAsync(string characterA, string characterB, CancellationToken cancellationToken = default);
    Task<Result<MatchupAnalysis>> AnalyzeCharacterBalanceAsync(string character, CancellationToken cancellationToken = default);
    Task<Result<ProceduralMove>> GenerateProceduralMoveAsync(MoveGenerationParameters parameters, CancellationToken cancellationToken = default);
    
    // Additional methods for model management
    Task<Result<IReadOnlyList<TrainingModel>>> GetTrainedModelsAsync(CancellationToken cancellationToken = default);
    Task<Result<TrainingModel>> TrainModelAsync(TrainingConfiguration configuration, IProgress<TrainingProgress> progress, CancellationToken cancellationToken = default);
    Task<Result<CharacterPerformanceAnalysis>> AnalyzeCharacterPerformanceAsync(Guid characterId, CancellationToken cancellationToken = default);
    Task<Result> DeleteModelAsync(Guid modelId, CancellationToken cancellationToken = default);
    Task<Result<string>> ExportModelAsync(Guid modelId, CancellationToken cancellationToken = default);
}
