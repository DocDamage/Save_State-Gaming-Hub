using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

namespace SaveState.Infrastructure.Mugen.ReplayAnalysis.Managers;

public class ReplayParsingManager
{
    private readonly ILogger<ReplayParsingManager> _logger;
    private readonly IReplayParsingEngine _parsingEngine;

    public ReplayParsingManager(
        ILogger<ReplayParsingManager> logger,
        IReplayParsingEngine parsingEngine)
    {
        _logger = logger;
        _parsingEngine = parsingEngine;
    }

    public async Task<string> CalculateFileHashAsync(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    public async Task<(ReplayMetadata Metadata, List<ReplayEvent> Events)> ParseReplayFileAsync(
        string filePath,
        CancellationToken ct)
    {
        var metadata = new ReplayMetadata();
        var events = new List<ReplayEvent>();

        var content = await File.ReadAllTextAsync(filePath, ct);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension == ".json" || content.TrimStart().StartsWith("{"))
        {
            _parsingEngine.ParseJsonReplay(content, metadata, events);
        }
        else
        {
            _parsingEngine.ParseTextReplay(content, metadata, events);
        }

        return (metadata, events);
    }

    public static int ParseWinner(string? winner)
    {
        if (string.IsNullOrEmpty(winner)) return 0;
        if (winner.Contains("1") || winner.Equals("p1", StringComparison.OrdinalIgnoreCase)) return 1;
        if (winner.Contains("2") || winner.Equals("p2", StringComparison.OrdinalIgnoreCase)) return 2;
        return 0;
    }
}
