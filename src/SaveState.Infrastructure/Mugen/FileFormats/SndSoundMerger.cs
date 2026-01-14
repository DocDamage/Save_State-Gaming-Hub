using SaveState.Core.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Mugen.FileFormats;

public class SndSoundMerger
{
    private readonly ILogger<SndSoundMerger> _logger;

    public SndSoundMerger(ILogger<SndSoundMerger> logger)
    {
        _logger = logger;
    }

    public Task<Result> MergeSoundFilesAsync(IEnumerable<string> sourceFiles, string outputPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulating sound merge into {Path}", outputPath);
        return Task.FromResult(Result.Success());
    }
}
