namespace SaveState.Application.Mugen.Services.AdvancedCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.AdvancedCombat;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.Concurrent;

/// <summary>
/// Input buffer engine for forgiving input systems.
/// </summary>
public class InputBufferEngine
{
    private readonly ILogger<InputBufferEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, List<InputBufferResult>> _buffers = new();

    public InputBufferEngine(ILogger<InputBufferEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Processes an input buffer request.
    /// </summary>
    public Task<Result<InputBufferResult>> ProcessBufferAsync(AdvancedCombatSession session, InputBufferRequest request, CancellationToken ct = default)
    {
        var similarity = CalculateInputSimilarity(request.Input, request.ExpectedInput);
        var success = similarity >= 0.7f;

        var result = new InputBufferResult
        {
            ProcessedInput = request.Input,
            ExpectedInput = request.ExpectedInput,
            Success = success,
            BufferedFrames = request.BufferSize,
            TimingOffset = 0,
            ProcessedAt = _timeProvider.UtcNow
        };

        var list = _buffers.GetOrAdd(session.SessionId, _ => new List<InputBufferResult>());
        lock (list)
        {
            list.Add(result);
        }

        session.LastBufferedInput = request.Input;

        _logger.LogDebug("Input buffer processed for session {SessionId}: Success={Success}", session.SessionId, success);
        return Task.FromResult(Result.Success(result));
    }

    /// <summary>
    /// Gets buffer statistics for a session.
    /// </summary>
    public Task<Result<InputBufferStats>> GetBufferStatsAsync(AdvancedCombatSession session, CancellationToken ct = default)
    {
        var buffers = GetBuffersForSession(session.SessionId);
        var successful = buffers.Count(b => b.Success);

        var stats = new InputBufferStats
        {
            TotalInputs = buffers.Count,
            SuccessfulBuffers = successful,
            AverageBufferSize = buffers.Any() ? (float)buffers.Average(b => b.BufferedFrames) : 0f,
            ForgivenessRate = buffers.Any() ? (float)successful / buffers.Count : 0,
            CommonMistakes = new[] { "Quarter-circle", "Dragon punch", "Double tap" },
            MeasuredAt = _timeProvider.UtcNow
        };

        return Task.FromResult(Result.Success(stats));
    }

    /// <summary>
    /// Gets all buffer results for a session (used for analysis).
    /// </summary>
    public IReadOnlyList<InputBufferResult> GetBuffersForSession(string sessionId)
    {
        return _buffers.TryGetValue(sessionId, out var buffers) ? buffers : new List<InputBufferResult>();
    }

    /// <summary>
    /// Calculates input accuracy for a list of buffers.
    /// </summary>
    public float CalculateInputAccuracy(List<InputBufferResult> buffers)
    {
        if (buffers.Count == 0) return 0f;
        var successful = buffers.Count(b => b.Success);
        return (float)successful / buffers.Count;
    }

    private static float CalculateInputSimilarity(string input, string expected)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(expected)) return 0f;
        if (input == expected) return 1f;

        var distance = LevenshteinDistance(input, expected);
        var maxLen = Math.Max(input.Length, expected.Length);
        return 1.0f - ((float)distance / maxLen);
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }
}
