using SaveState.Core.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Mugen.FileFormats;

public class SffSpriteMerger
{
    private readonly ILogger<SffSpriteMerger> _logger;

    public SffSpriteMerger(ILogger<SffSpriteMerger> logger)
    {
        _logger = logger;
    }

    public Task<Result> MergeSpriteFilesAsync(IEnumerable<string> sourcePaths, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulating sprite merge into {Path}", outputPath);
        return Task.FromResult(Result.Success());
    }
}
