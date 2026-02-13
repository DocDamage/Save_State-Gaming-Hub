namespace SaveState.Infrastructure.Mugen;

using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Core.Mugen;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Runs actual MUGEN engine matches and records results.
/// </summary>
public class MugenDeathMatchService : IMugenDeathMatchService
{
    private static readonly string[] ReplayExtensions = { ".replay", ".rep", ".rpl", ".log", ".txt", ".json" };
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(120);

    private readonly IMugenLauncher _launcher;
    private readonly IMugenCharacterRepository _characterRepository;
    private readonly IMugenMatchHistoryRepository _matchHistoryRepository;
    private readonly MugenOptions _options;
    private readonly ILogger<MugenDeathMatchService> _logger;

    public MugenDeathMatchService(
        IMugenLauncher launcher,
        IMugenCharacterRepository characterRepository,
        IMugenMatchHistoryRepository matchHistoryRepository,
        IOptions<MugenOptions> options,
        ILogger<MugenDeathMatchService> logger)
    {
        _launcher = launcher;
        _characterRepository = characterRepository;
        _matchHistoryRepository = matchHistoryRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<DeathMatchResult>> RunDeathMatchAsync(
        Guid character1Id,
        Guid character2Id,
        int matchCount = 3,
        CancellationToken ct = default)
    {
        if (matchCount <= 0)
            return Result.Failure<DeathMatchResult>("Match count must be greater than zero.");

        if (!_launcher.IsIkemenAvailable())
            return Result.Failure<DeathMatchResult>("IKEMEN executable not available.");

        var character1Result = await _characterRepository.GetByIdAsync(character1Id, ct);
        if (character1Result.IsFailure || character1Result.Value == null)
            return Result.Failure<DeathMatchResult>("Character 1 not found.");
        var character1 = character1Result.Value;

        var character2Result = await _characterRepository.GetByIdAsync(character2Id, ct);
        if (character2Result.IsFailure || character2Result.Value == null)
            return Result.Failure<DeathMatchResult>("Character 2 not found.");
        var character2 = character2Result.Value;

        var replayDirectory = ResolveReplayDirectory();
        if (!string.IsNullOrWhiteSpace(replayDirectory))
        {
            Directory.CreateDirectory(replayDirectory);
        }
        else
        {
            _logger.LogWarning("Replay directory could not be resolved; match results may be limited.");
        }

        var replayPaths = new List<string>();
        var p1Wins = 0;
        var p2Wins = 0;
        var draws = 0;
        var totalDuration = TimeSpan.Zero;

        for (var i = 0; i < matchCount; i++)
        {
            ct.ThrowIfCancellationRequested();

            var replaySnapshot = SnapshotReplays(replayDirectory);
            var timer = Stopwatch.StartNew();
            Process? process = null;
            string? replayPath = null;

            try
            {
                process = await _launcher.LaunchWatchAsync(character1.Name, character2.Name);
                replayPath = await WaitForReplayAsync(replayDirectory, replaySnapshot, MatchTimeout, ct);
                if (!string.IsNullOrWhiteSpace(replayPath))
                    replayPaths.Add(replayPath);

                var result = DetermineMatchResult(replayPath, character1, character2);
                switch (result)
                {
                    case MatchResult.Player1Win:
                        p1Wins++;
                        break;
                    case MatchResult.Player2Win:
                        p2Wins++;
                        break;
                    default:
                        draws++;
                        break;
                }

                var history = MugenMatchHistory.Create(
                    character1.Id,
                    character2.Id,
                    result,
                    roundsP1: 0,
                    roundsP2: 0,
                    duration: timer.Elapsed,
                    mode: GameMode.Watch);
                history.SetReplayPath(replayPath);

                var recordResult = await _matchHistoryRepository.RecordMatchAsync(history, ct);
                if (recordResult.IsFailure)
                {
                    _logger.LogWarning("Failed to record match history: {Error}", recordResult.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to run death match {MatchIndex}", i + 1);
                return Result.Failure<DeathMatchResult>($"Death match failed: {ex.Message}");
            }
            finally
            {
                if (process != null)
                    await ShutdownProcessAsync(process, CancellationToken.None);
                timer.Stop();
                totalDuration += timer.Elapsed;
                process?.Dispose();
            }

        }

        var summary = new DeathMatchResult(
            character1.Id,
            ResolveDisplayName(character1),
            character2.Id,
            ResolveDisplayName(character2),
            matchCount,
            p1Wins,
            p2Wins,
            draws,
            totalDuration,
            replayPaths);

        return Result.Success(summary);
    }

    private string? ResolveReplayDirectory()
    {
        var candidates = new List<string>();

        var exePath = _launcher.GetIkemenExecutablePath();
        if (!string.IsNullOrWhiteSpace(exePath))
        {
            var exeDir = Path.GetDirectoryName(exePath);
            if (!string.IsNullOrWhiteSpace(exeDir))
            {
                candidates.Add(Path.Combine(exeDir, "save", "replays"));
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.ReplaysFolder))
        {
            candidates.Add(Path.GetFullPath(_options.ReplaysFolder));
        }

        if (!string.IsNullOrWhiteSpace(_options.SaveDirectory))
        {
            candidates.Add(Path.GetFullPath(Path.Combine(_options.SaveDirectory, "replays")));
        }

        var existing = candidates.FirstOrDefault(Directory.Exists);
        return existing ?? candidates.FirstOrDefault();
    }

    private static HashSet<string> SnapshotReplays(string? replayDirectory)
    {
        if (string.IsNullOrWhiteSpace(replayDirectory) || !Directory.Exists(replayDirectory))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return Directory.EnumerateFiles(replayDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => ReplayExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<string?> WaitForReplayAsync(
        string? replayDirectory,
        HashSet<string> baseline,
        TimeSpan timeout,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(replayDirectory) || !Directory.Exists(replayDirectory))
            return null;

        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var candidate = FindNewestReplay(replayDirectory, baseline);
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;

            await Task.Delay(500, ct);
        }

        return FindNewestReplay(replayDirectory, baseline);
    }

    private static async Task ShutdownProcessAsync(Process process, CancellationToken ct)
    {
        if (process.HasExited)
            return;

        try
        {
            if (process.CloseMainWindow())
            {
                var exitTask = process.WaitForExitAsync(ct);
                var completed = await Task.WhenAny(exitTask, Task.Delay(TimeSpan.FromSeconds(5), ct));
                if (completed == exitTask)
                    return;
            }
        }
        catch
        {
            // Ignore close errors.
        }

        try
        {
            if (!process.HasExited)
                process.Kill(true);
        }
        catch
        {
            // Ignore kill errors.
        }
    }

    private static string? FindNewestReplay(string replayDirectory, HashSet<string> baseline)
    {
        return Directory.EnumerateFiles(replayDirectory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => ReplayExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .Where(file => !baseline.Contains(file))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static MatchResult DetermineMatchResult(
        string? replayPath,
        MugenCharacter character1,
        MugenCharacter character2)
    {
        if (string.IsNullOrWhiteSpace(replayPath) || !File.Exists(replayPath))
            return MatchResult.Draw;

        var content = ReadReplayContent(replayPath);
        if (string.IsNullOrWhiteSpace(content))
            return MatchResult.Draw;

        if (TryParseJsonWinner(content, out var winnerToken, out var outcome))
        {
            return ResolveWinner(outcome ?? winnerToken, character1, character2);
        }

        if (TryParseTextWinner(content, out winnerToken))
        {
            return ResolveWinner(winnerToken, character1, character2);
        }

        return MatchResult.Draw;
    }

    private static string ReadReplayContent(string path)
    {
        try
        {
            return File.ReadAllText(path, Encoding.UTF8);
        }
        catch
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    private static bool TryParseJsonWinner(string content, out string? winnerToken, out string? outcome)
    {
        winnerToken = null;
        outcome = null;
        var trimmed = content.TrimStart();
        if (!trimmed.StartsWith("{", StringComparison.Ordinal) && !trimmed.StartsWith("[", StringComparison.Ordinal))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(content, new JsonDocumentOptions { AllowTrailingCommas = true });
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                root = root[0];
            }

            if (root.TryGetProperty("metadata", out var metadata))
            {
                winnerToken = ReadJsonString(metadata, "winner", "victor");
                outcome = ReadJsonString(metadata, "outcome", "result");
            }

            winnerToken ??= ReadJsonString(root, "winner", "victor");
            outcome ??= ReadJsonString(root, "outcome", "result");

            return !string.IsNullOrWhiteSpace(winnerToken) || !string.IsNullOrWhiteSpace(outcome);
        }
        catch
        {
            return false;
        }
    }

    private static string? ReadJsonString(JsonElement element, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (element.TryGetProperty(key, out var value))
            {
                if (value.ValueKind == JsonValueKind.String)
                    return value.GetString();
                if (value.ValueKind == JsonValueKind.Number)
                    return value.GetRawText();
            }
        }

        return null;
    }

    private static bool TryParseTextWinner(string content, out string? winnerToken)
    {
        winnerToken = null;
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0)
                continue;

            var lower = line.ToLowerInvariant();
            if (lower.Contains("winner") || lower.Contains("victor") || lower.Contains("result"))
            {
                var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    winnerToken = parts[1];
                    return true;
                }
            }

            if (lower.Contains("p1 win") || lower.Contains("player 1 win"))
            {
                winnerToken = "p1";
                return true;
            }

            if (lower.Contains("p2 win") || lower.Contains("player 2 win"))
            {
                winnerToken = "p2";
                return true;
            }
        }

        return false;
    }

    private static MatchResult ResolveWinner(string? token, MugenCharacter character1, MugenCharacter character2)
    {
        if (string.IsNullOrWhiteSpace(token))
            return MatchResult.Draw;

        var normalized = token.Trim();
        var lower = normalized.ToLowerInvariant();

        if (lower.Contains("timeout"))
            return MatchResult.Timeout;

        if (lower.Contains("draw") || lower.Contains("tie"))
            return MatchResult.Draw;

        if (lower.Contains("p1") || lower.Contains("player1") || lower.Contains("player 1"))
            return MatchResult.Player1Win;

        if (lower.Contains("p2") || lower.Contains("player2") || lower.Contains("player 2"))
            return MatchResult.Player2Win;

        if (lower == "1")
            return MatchResult.Player1Win;

        if (lower == "2")
            return MatchResult.Player2Win;

        if (lower == "0")
            return MatchResult.Draw;

        if (string.Equals(normalized, character1.DisplayName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, character1.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MatchResult.Player1Win;
        }

        if (string.Equals(normalized, character2.DisplayName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, character2.Name, StringComparison.OrdinalIgnoreCase))
        {
            return MatchResult.Player2Win;
        }

        return MatchResult.Draw;
    }

    private static string ResolveDisplayName(MugenCharacter character)
    {
        return string.IsNullOrWhiteSpace(character.DisplayName) ? character.Name : character.DisplayName;
    }
}
