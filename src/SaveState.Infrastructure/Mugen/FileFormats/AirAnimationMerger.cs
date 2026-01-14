using SaveState.Core.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Mugen.FileFormats;

public class AirAnimationMerger
{
    private readonly ILogger<AirAnimationMerger> _logger;

    public AirAnimationMerger(ILogger<AirAnimationMerger> logger)
    {
        _logger = logger;
    }

    public Task<Result> MergeAnimationFilesAsync(IEnumerable<string> sourcePaths, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulating animation merge into {Path}", outputPath);
        return Task.FromResult(Result.Success());
    }
}
