using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Progress evaluator for assessing learning progress.
/// </summary>
public class BpProgressEvaluator
{
    private readonly ILogger<BpProgressEvaluator> _logger;

    public BpProgressEvaluator(ILogger<BpProgressEvaluator> logger)
    {
        _logger = logger;
    }

    public double EvaluateProgress(BpUserPathProgress progress)
    {
        if (progress.CompletedLessons.Count == 0) return 0;
        var totalLessons = progress.TotalLessons;
        return totalLessons > 0 ? (double)progress.CompletedLessons.Count / totalLessons : 0;
    }
}
