using SaveState.Core.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Infrastructure.Mugen.FileFormats;

public class CnsStateMerger
{
    private readonly ILogger<CnsStateMerger> _logger;

    public CnsStateMerger(ILogger<CnsStateMerger> logger)
    {
        _logger = logger;
    }

    public Task<Result> MergeStateFilesAsync(IEnumerable<string> stateFiles, string outputPath, string characterName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Simulating CNS merge for {CharacterName} into {OutputPath}", characterName, outputPath);
        return Task.FromResult(Result.Success());
    }
}
