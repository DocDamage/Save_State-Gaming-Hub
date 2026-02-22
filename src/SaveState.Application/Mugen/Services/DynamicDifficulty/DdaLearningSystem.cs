using SaveState.Core.Common;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Learning system for continuous improvement.
/// </summary>
public class DdaLearningSystem
{
    private readonly ILogger<DdaLearningSystem> _logger;

    public DdaLearningSystem(ILogger<DdaLearningSystem> logger)
    {
        _logger = logger;
    }

    public async Task TrainModelAsync(IReadOnlyList<DdaTrainingMatch> trainingMatches, CancellationToken ct = default)
    {
        await Task.Delay(2000, ct);
    }
}
