namespace SaveState.Infrastructure.Mugen;

using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Implementation of the MUGEN coaching service.
/// Provides AI-powered advice, character guides, and matchup analysis.
/// </summary>
public class MugenCoachService : IMugenCoachService
{
    private readonly IAiOrchestrator _aiOrchestrator;
    private readonly SaveState.Core.Mugen.IMugenCharacterRepository _characterRepository;
    private readonly IMugenStatsService _statsService;
    private readonly ILogger<MugenCoachService> _logger;

    public MugenCoachService(
        IAiOrchestrator aiOrchestrator,
        SaveState.Core.Mugen.IMugenCharacterRepository characterRepository,
        IMugenStatsService statsService,
        ILogger<MugenCoachService> logger)
    {
        _aiOrchestrator = aiOrchestrator;
        _characterRepository = characterRepository;
        _statsService = statsService;
        _logger = logger;
    }

    public async Task<string?> GetCoachingAdviceAsync(Guid characterId, CancellationToken ct = default)
    {
        var result = await GetCharacterGuideAsync(characterId, ct);
        return result.IsSuccess ? result.Value?.Overview : "Keep practicing! Every match is a learning opportunity.";
    }

    public async Task<Result<MatchupAdvice>> GetMatchupAdviceAsync(
        Guid characterId,
        Guid opponentId,
        CancellationToken ct = default)
    {
        try
        {
            // Get character entities
            var yourCharResult = await _characterRepository.GetByIdAsync(characterId, ct);
            var opponentResult = await _characterRepository.GetByIdAsync(opponentId, ct);

            if (yourCharResult.IsFailure || opponentResult.IsFailure)
            {
                return Result.Failure<MatchupAdvice>("Characters not found");
            }

            var yourChar = yourCharResult.Value!;
            var opponent = opponentResult.Value!;

            // Get statistics
            var yourStatsTask = _statsService.GetCharacterStatsAsync(characterId, ct);
            var opponentStatsTask = _statsService.GetCharacterStatsAsync(opponentId, ct);
            var matchupStatsTask = _statsService.GetMatchupStatsAsync(characterId, ct);

            await Task.WhenAll(yourStatsTask, opponentStatsTask, matchupStatsTask);

            var yourStatsResult = await yourStatsTask;
            var opponentStatsResult = await opponentStatsTask;
            var matchupStatsResult = await matchupStatsTask;

            var yourStats = yourStatsResult.IsSuccess ? yourStatsResult.Value : null;
            var opponentStats = opponentStatsResult.IsSuccess ? opponentStatsResult.Value : null;
            var matchupStats = matchupStatsResult.IsSuccess ? matchupStatsResult.Value : new List<MatchupStats>();

            // Find specific matchup
            var specificMatchup = matchupStats.FirstOrDefault(m => m.OpponentId == opponentId);
            var predictedWinRate = specificMatchup?.WinRate ?? 0.5f;

            // Generate AI advice
            var aiAdvice = await GenerateAiMatchupAdviceAsync(yourChar, opponent, predictedWinRate, ct);

            var advice = new MatchupAdvice(
                characterId,
                opponentId,
                predictedWinRate,
                aiAdvice.Tips,
                aiAdvice.MovesToAvoid,
                aiAdvice.KeyMoves);

            return Result.Success<MatchupAdvice>(advice);
        }
        catch (Exception ex)
        {
            return Result.Failure<MatchupAdvice>($"Failed to get matchup advice: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<Guid>>> GetCounterPicksAsync(
        Guid opponentId,
        CancellationToken ct = default)
    {
        try
        {
            // Get all characters and their matchup stats against the opponent
            var allCharacters = await _characterRepository.GetAllAsync(ct);

            var counterPicks = new List<(Guid CharacterId, float WinRate)>();

            foreach (var character in allCharacters)
            {
                if (character.Id == opponentId) continue;

                var matchupStatsResult = await _statsService.GetMatchupStatsAsync(character.Id, ct);
                if (!matchupStatsResult.IsSuccess) continue;

                var matchup = matchupStatsResult.Value!.FirstOrDefault(m => m.OpponentId == opponentId);
                if (matchup is not null)
                {
                    counterPicks.Add((character.Id, matchup.WinRate));
                }
            }

            // Return top 5 counter picks
            var topCounters = counterPicks
                .OrderByDescending(x => x.WinRate)
                .Take(5)
                .Select(x => x.CharacterId)
                .ToList();

            return Result.Success<IReadOnlyList<Guid>>(topCounters);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<Guid>>($"Failed to get counter picks: {ex.Message}");
        }
    }

    public async Task<Result<CharacterGuide>> GetCharacterGuideAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        try
        {
            var characterResult = await _characterRepository.GetByIdAsync(characterId, ct);
            if (characterResult.IsFailure)
                return Result.Failure<CharacterGuide>("Character not found");

            var character = characterResult.Value!;

            // Generate AI-powered character guide
            var guide = await GenerateAiCharacterGuideAsync(character, ct);

            return Result.Success<CharacterGuide>(guide);
        }
        catch (Exception ex)
        {
            return Result.Failure<CharacterGuide>($"Failed to get character guide: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyList<string>>> AnalyzeReplayAsync(
        string replayPath,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(replayPath))
        {
            return Result.Failure<IReadOnlyList<string>>("Replay path is required.");
        }

        try
        {
            var resolvedPath = ResolveReplayPath(replayPath);
            if (resolvedPath is null)
            {
                return Result.Failure<IReadOnlyList<string>>("Replay file not found.");
            }

            var analysis = await AnalyzeReplayInternalAsync(resolvedPath, ct);
            var suggestions = BuildCoachingSuggestions(analysis);

            if (analysis.Events.Count >= 10)
            {
                var aiSuggestions = await GetAiReplaySuggestionsAsync(analysis, ct);
                suggestions.AddRange(aiSuggestions);
            }

            var deduped = DeduplicateSuggestions(suggestions);
            if (deduped.Count == 0)
            {
                deduped.Add("Replay data was limited; focus on spacing, confirms, and consistent anti-airs.");
            }

            return Result.Success<IReadOnlyList<string>>(deduped);
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<string>>($"Failed to analyze replay: {ex.Message}");
        }
    }

    private static string? ResolveReplayPath(string replayPath)
    {
        if (File.Exists(replayPath))
        {
            return replayPath;
        }

        if (Directory.Exists(replayPath))
        {
            return Directory
                .GetFiles(replayPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path => IsReplayExtension(Path.GetExtension(path)))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        var directory = Path.GetDirectoryName(replayPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = Directory.GetCurrentDirectory();
        }

        var baseName = Path.GetFileNameWithoutExtension(replayPath);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            return null;
        }

        foreach (var extension in ReplayExtensions)
        {
            var candidate = Path.Combine(directory, baseName + extension);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task<ReplayAnalysis> AnalyzeReplayInternalAsync(string replayPath, CancellationToken ct)
    {
        var metadata = new ReplayMetadata
        {
            Source = replayPath,
            RecordedAt = GetFileTimestamp(replayPath)
        };

        var events = new List<ReplayEvent>();
        var content = await File.ReadAllTextAsync(replayPath, ct);

        if (!string.IsNullOrWhiteSpace(content))
        {
            if (IsJsonPayload(content))
            {
                ParseJsonReplay(content, metadata, events);
            }

            if (events.Count == 0)
            {
                ParseTextReplay(content, metadata, events);
            }
        }

        var sequences = BuildSequences(events);
        var players = BuildPlayerSummaries(events, metadata, sequences);
        var outcome = DetermineOutcome(metadata);

        return new ReplayAnalysis(metadata, events, players, sequences, outcome);
    }

    private static List<string> BuildCoachingSuggestions(ReplayAnalysis analysis)
    {
        var suggestions = new List<string>();
        var player = analysis.Players.FirstOrDefault(p => p.PlayerIndex == 1);
        var opponent = analysis.Players.FirstOrDefault(p => p.PlayerIndex == 2);

        if (player is null)
        {
            return suggestions;
        }

        if (player.TotalMoves > 0)
        {
            if (player.HitRate < 0.25f)
            {
                suggestions.Add("Your move success rate is low; tighten spacing and confirm before committing.");
            }

            if (player.WhiffRate > 0.35f)
            {
                suggestions.Add("High whiff rate detected; use faster pokes and check ranges before big swings.");
            }

            if (player.DamageTaken > player.DamageDealt * 1.2f && player.Blocks < Math.Max(3, player.Hits / 2))
            {
                suggestions.Add("You are taking more damage than you deal; slow the pace, block more, and punish unsafe strings.");
            }

            if (player.Throws == 0 && player.Blocks >= 6)
            {
                suggestions.Add("Opponent is blocking often; add throws to open them up.");
            }

            if (player.Projectiles >= 4 && player.HitRate < 0.2f)
            {
                suggestions.Add("Projectiles are getting avoided; vary timing and advance behind them.");
            }
        }

        if (opponent is not null)
        {
            var opponentAirHits = analysis.Events.Count(ev =>
                ev.PlayerIndex == opponent.PlayerIndex &&
                (ev.Type == ReplayEventType.Movement || ev.Type == ReplayEventType.AntiAir || IsAirMove(ev.Move)));

            if (opponentAirHits >= 3 && player.AntiAirs < 2)
            {
                suggestions.Add("Opponent landed several jump-ins; prioritize anti-airs and pre-emptive spacing.");
            }
        }

        var bestSequence = analysis.Sequences
            .Where(sequence => sequence.PlayerIndex == 1 && sequence.Moves.Count >= 2)
            .OrderByDescending(sequence => sequence.Damage)
            .ThenByDescending(sequence => sequence.Hits)
            .FirstOrDefault();

        if (bestSequence is null)
        {
            bestSequence = FindMostCommonTransition(analysis.Events, 1);
        }

        if (bestSequence is not null && bestSequence.Moves.Count >= 2)
        {
            suggestions.Add(FormatSequenceSuggestion("Better sequence", bestSequence));
        }

        var dropSequence = analysis.Sequences
            .Where(sequence => sequence.PlayerIndex == 1 && sequence.Drops > 0 && sequence.Moves.Count >= 2)
            .OrderByDescending(sequence => sequence.Drops)
            .FirstOrDefault();

        if (dropSequence is not null)
        {
            suggestions.Add($"Sequence cleanup: {string.Join(" -> ", dropSequence.Moves)} - tighten timing to avoid drops.");
        }

        if (!suggestions.Any())
        {
            suggestions.Add("Look for safer confirms into short combos, then reset to neutral.");
        }

        return suggestions;
    }

    private async Task<IReadOnlyList<string>> GetAiReplaySuggestionsAsync(ReplayAnalysis analysis, CancellationToken ct)
    {
        try
        {
            var prompt = BuildReplayCoachPrompt(analysis);
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return Array.Empty<string>();
            }

            var request = new AiRequest(
                AiRequestType.Chat,
                Prompt: prompt,
                MaxTokens: 260,
                Temperature: 0.4f);

            var response = await _aiOrchestrator.ProcessRequestAsync(request, ct);
            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
            {
                return Array.Empty<string>();
            }

            return ExtractAiSuggestions(response.Content);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string BuildReplayCoachPrompt(ReplayAnalysis analysis)
    {
        var player = analysis.Players.FirstOrDefault(p => p.PlayerIndex == 1);
        var opponent = analysis.Players.FirstOrDefault(p => p.PlayerIndex == 2);

        if (player is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("You are a fighting game coach. Provide 4-6 short bullet tips.");
        sb.AppendLine("Focus on improving move sequences, hit confirms, spacing, and defense.");
        sb.AppendLine();
        sb.AppendLine($"Player1: {player.Name ?? "Player1"}");
        sb.AppendLine($"Damage dealt: {player.DamageDealt}, damage taken: {player.DamageTaken}");
        sb.AppendLine($"Hits: {player.Hits}, blocks: {player.Blocks}, whiffs: {player.Whiffs}");
        sb.AppendLine($"Throws: {player.Throws}, projectiles: {player.Projectiles}, anti-airs: {player.AntiAirs}");

        if (opponent is not null)
        {
            sb.AppendLine($"Opponent: {opponent.Name ?? "Player2"}");
            sb.AppendLine($"Opponent damage dealt: {opponent.DamageDealt}, damage taken: {opponent.DamageTaken}");
        }

        if (!string.IsNullOrWhiteSpace(analysis.Metadata.Winner))
        {
            sb.AppendLine($"Winner: {analysis.Metadata.Winner}");
        }

        var topSequences = analysis.Sequences
            .Where(sequence => sequence.PlayerIndex == 1 && sequence.Moves.Count >= 2)
            .OrderByDescending(sequence => sequence.Damage)
            .ThenByDescending(sequence => sequence.Hits)
            .Take(3)
            .ToList();

        if (topSequences.Count > 0)
        {
            sb.AppendLine("Top sequences used by Player1:");
            foreach (var sequence in topSequences)
            {
                sb.AppendLine($"- {string.Join(" -> ", sequence.Moves)} (hits {sequence.Hits}, dmg {sequence.Damage}, uses {sequence.Occurrences})");
            }
        }

        sb.AppendLine("Return only bullet points.");
        return sb.ToString();
    }

    private static List<string> ExtractAiSuggestions(string content)
    {
        var suggestions = new List<string>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            line = Regex.Replace(line, @"^\d+[\).]\s*", string.Empty);
            line = line.TrimStart('-', '*').Trim();

            if (!string.IsNullOrWhiteSpace(line))
            {
                suggestions.Add(line);
            }
        }

        return suggestions;
    }

    private static List<string> DeduplicateSuggestions(IEnumerable<string> suggestions)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var suggestion in suggestions)
        {
            var trimmed = suggestion.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }

            if (seen.Add(trimmed))
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    private static IReadOnlyList<MoveSequenceSummary> BuildSequences(IReadOnlyList<ReplayEvent> events)
    {
        var ordered = events
            .Select((ev, index) => new { Event = ev, Index = index })
            .OrderBy(item => item.Event.TimeSeconds ?? (item.Event.Frame.HasValue ? item.Event.Frame.Value / 60d : item.Index))
            .ThenBy(item => item.Index)
            .Select(item => item.Event)
            .ToList();

        var rawSequences = new List<MoveSequenceSummary>();

        foreach (var group in ordered.GroupBy(ev => ev.PlayerIndex).Where(group => group.Key > 0))
        {
            SequenceBuilder? builder = null;
            ReplayEvent? last = null;

            foreach (var ev in group)
            {
                if (builder is null)
                {
                    builder = new SequenceBuilder(ev.PlayerIndex);
                }

                if (last is not null && IsSequenceBreak(last, ev))
                {
                    if (builder.HasContent)
                    {
                        rawSequences.Add(builder.Build());
                    }

                    builder = new SequenceBuilder(ev.PlayerIndex);
                }

                builder.Add(ev);
                last = ev;
            }

            if (builder?.HasContent == true)
            {
                rawSequences.Add(builder.Build());
            }
        }

        var grouped = rawSequences
            .Where(sequence => sequence.Moves.Count > 0)
            .GroupBy(sequence => (sequence.PlayerIndex, Key: string.Join(" -> ", sequence.Moves)))
            .Select(group => new MoveSequenceSummary(
                group.Key.PlayerIndex,
                group.First().Moves,
                group.Sum(item => item.Hits),
                group.Sum(item => item.Damage),
                group.Count(),
                group.Sum(item => item.Drops)))
            .ToList();

        return grouped;
    }

    private static bool IsSequenceBreak(ReplayEvent last, ReplayEvent current)
    {
        if (last.TimeSeconds.HasValue && current.TimeSeconds.HasValue)
        {
            if (current.TimeSeconds.Value - last.TimeSeconds.Value > 2.5d)
            {
                return true;
            }
        }

        if (last.Frame.HasValue && current.Frame.HasValue)
        {
            if (current.Frame.Value - last.Frame.Value > 120)
            {
                return true;
            }
        }

        return false;
    }

    private static PlayerReplaySummary[] BuildPlayerSummaries(
        IReadOnlyList<ReplayEvent> events,
        ReplayMetadata metadata,
        IReadOnlyList<MoveSequenceSummary> sequences)
    {
        var players = new[]
        {
            new PlayerReplaySummary(1) { Name = metadata.Player1 },
            new PlayerReplaySummary(2) { Name = metadata.Player2 }
        };

        foreach (var ev in events)
        {
            if (ev.PlayerIndex != 1 && ev.PlayerIndex != 2)
            {
                continue;
            }

            var player = players[ev.PlayerIndex - 1];
            var opponent = players[ev.PlayerIndex == 1 ? 1 : 0];

            if (IsCountingMove(ev.Type))
            {
                player.TotalMoves++;
            }

            switch (ev.Type)
            {
                case ReplayEventType.Hit:
                    player.Hits++;
                    if (ev.Damage.HasValue)
                    {
                        player.DamageDealt += ev.Damage.Value;
                        opponent.DamageTaken += ev.Damage.Value;
                    }
                    break;
                case ReplayEventType.Block:
                    player.Blocks++;
                    break;
                case ReplayEventType.Whiff:
                    player.Whiffs++;
                    break;
                case ReplayEventType.Throw:
                    player.Throws++;
                    break;
                case ReplayEventType.Projectile:
                    player.Projectiles++;
                    break;
                case ReplayEventType.AntiAir:
                    player.AntiAirs++;
                    break;
                case ReplayEventType.Knockdown:
                    player.Knockdowns++;
                    break;
            }
        }

        foreach (var sequence in sequences)
        {
            if (sequence.PlayerIndex != 1 && sequence.PlayerIndex != 2)
            {
                continue;
            }

            var player = players[sequence.PlayerIndex - 1];
            if (sequence.Hits >= 2)
            {
                player.Combos += sequence.Occurrences;
            }

            if (sequence.Drops > 0)
            {
                player.ComboDrops += sequence.Drops;
            }
        }

        return players;
    }

    private static ReplayOutcome DetermineOutcome(ReplayMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.Winner))
        {
            return ReplayOutcome.Unknown;
        }

        if (string.Equals(metadata.Winner, metadata.Player1, StringComparison.OrdinalIgnoreCase) ||
            metadata.Winner.Contains("p1", StringComparison.OrdinalIgnoreCase))
        {
            return ReplayOutcome.Player1Win;
        }

        if (string.Equals(metadata.Winner, metadata.Player2, StringComparison.OrdinalIgnoreCase) ||
            metadata.Winner.Contains("p2", StringComparison.OrdinalIgnoreCase))
        {
            return ReplayOutcome.Player2Win;
        }

        return ReplayOutcome.Unknown;
    }

    private static bool IsJsonPayload(string content)
    {
        var trimmed = content.TrimStart();
        return trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal);
    }

    private static void ParseJsonReplay(string json, ReplayMetadata metadata, List<ReplayEvent> events)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true });
        var root = doc.RootElement;

        var metaElement = root.TryGetProperty("metadata", out var meta) ? meta : root;
        metadata.Player1 ??= GetJsonString(metaElement, "player1", "p1", "player_one", "playerOne");
        metadata.Player2 ??= GetJsonString(metaElement, "player2", "p2", "player_two", "playerTwo");
        metadata.Winner ??= GetJsonString(metaElement, "winner", "victor");
        metadata.Stage ??= GetJsonString(metaElement, "stage", "arena");
        metadata.Game ??= GetJsonString(metaElement, "game", "title");
        metadata.Duration ??= GetJsonDuration(metaElement, "duration", "length");
        metadata.RecordedAt ??= GetJsonTimestamp(metaElement, "recordedAt", "timestamp", "date");

        TryParsePlayers(root, metadata);

        if (root.TryGetProperty("events", out var eventsElement))
        {
            ParseJsonEvents(eventsElement, events);
        }
        else if (root.TryGetProperty("timeline", out var timelineElement))
        {
            ParseJsonEvents(timelineElement, events);
        }
        else if (root.TryGetProperty("actions", out var actionsElement))
        {
            ParseJsonEvents(actionsElement, events);
        }
    }

    private static void ParseJsonEvents(JsonElement eventsElement, List<ReplayEvent> events)
    {
        if (eventsElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var element in eventsElement.EnumerateArray())
        {
            var playerIndex = ParsePlayerIndex(element);
            var type = ParseEventType(GetJsonString(element, "type", "event", "action", "state"));
            var move = GetJsonString(element, "move", "name", "command", "input");
            var command = GetJsonString(element, "command", "input");
            var damage = GetJsonInt(element, "damage", "dmg");
            var frame = GetJsonInt(element, "frame", "tick");
            var timeSeconds = GetJsonDouble(element, "time", "t");
            var raw = element.ToString();

            if (type == ReplayEventType.Unknown &&
                string.IsNullOrWhiteSpace(move) &&
                string.IsNullOrWhiteSpace(command) &&
                !damage.HasValue)
            {
                continue;
            }

            events.Add(new ReplayEvent(
                playerIndex,
                type == ReplayEventType.Unknown && !string.IsNullOrWhiteSpace(move) ? ReplayEventType.Move : type,
                CleanMoveName(move),
                CleanMoveName(command),
                damage,
                frame,
                timeSeconds,
                raw));
        }
    }

    private static void ParseTextReplay(string text, ReplayMetadata metadata, List<ReplayEvent> events)
    {
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            TryParseMetadataLine(line, metadata);

            var ev = TryParseEventFromLine(line);
            if (ev is not null)
            {
                events.Add(ev);
            }
        }
    }

    private static void TryParseMetadataLine(string line, ReplayMetadata metadata)
    {
        metadata.Player1 ??= ExtractMetadataValue(line, "player1", "player 1", "p1");
        metadata.Player2 ??= ExtractMetadataValue(line, "player2", "player 2", "p2");
        metadata.Winner ??= ExtractMetadataValue(line, "winner", "victor");
        metadata.Stage ??= ExtractMetadataValue(line, "stage", "arena");
        metadata.Game ??= ExtractMetadataValue(line, "game", "title");

        if (!metadata.RecordedAt.HasValue)
        {
            var dateValue = ExtractMetadataValue(line, "date", "recordedAt", "timestamp");
            if (!string.IsNullOrWhiteSpace(dateValue) &&
                DateTimeOffset.TryParse(dateValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                metadata.RecordedAt = parsed;
            }
        }
    }

    private static string? ExtractMetadataValue(string line, params string[] keys)
    {
        foreach (var key in keys)
        {
            var match = Regex.Match(
                line,
                $@"\b{Regex.Escape(key)}\b\s*[:=]\s*(?<value>.+)$",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups["value"].Value.Trim();
            }
        }

        return null;
    }

    private static ReplayEvent? TryParseEventFromLine(string line)
    {
        var playerIndex = ParsePlayerIndex(line);
        if (playerIndex == 0)
        {
            return null;
        }

        var type = ParseEventTypeFromLine(line);
        var move = ExtractMove(line);
        var command = ExtractCommand(line);
        var damage = ExtractInt(line, DamageRegex);
        var frame = ExtractInt(line, FrameRegex);
        var timeSeconds = ExtractDouble(line, TimeRegex);

        if (type == ReplayEventType.Unknown &&
            string.IsNullOrWhiteSpace(move) &&
            string.IsNullOrWhiteSpace(command) &&
            !damage.HasValue)
        {
            return null;
        }

        if (type == ReplayEventType.Unknown && !string.IsNullOrWhiteSpace(move))
        {
            type = ReplayEventType.Move;
        }

        return new ReplayEvent(
            playerIndex,
            type,
            CleanMoveName(move),
            CleanMoveName(command),
            damage,
            frame,
            timeSeconds,
            line.Trim());
    }

    private static ReplayEventType ParseEventType(string? typeValue)
    {
        if (string.IsNullOrWhiteSpace(typeValue))
        {
            return ReplayEventType.Unknown;
        }

        return ParseEventTypeFromLine(typeValue);
    }

    private static ReplayEventType ParseEventTypeFromLine(string line)
    {
        var lower = line.ToLowerInvariant();

        if (lower.Contains("anti-air") || lower.Contains("anti air") || lower.Contains("antiair"))
        {
            return ReplayEventType.AntiAir;
        }

        if (lower.Contains("projectile"))
        {
            return ReplayEventType.Projectile;
        }

        if (lower.Contains("knockdown") || lower.Contains("kd"))
        {
            return ReplayEventType.Knockdown;
        }

        if (lower.Contains("throw"))
        {
            return ReplayEventType.Throw;
        }

        if (lower.Contains("whiff"))
        {
            return ReplayEventType.Whiff;
        }

        if (lower.Contains("block"))
        {
            return ReplayEventType.Block;
        }

        if (lower.Contains("hit"))
        {
            return ReplayEventType.Hit;
        }

        if (lower.Contains("dash") || lower.Contains("jump") || lower.Contains("walk"))
        {
            return ReplayEventType.Movement;
        }

        return ReplayEventType.Unknown;
    }

    private static string? ExtractMove(string line)
    {
        var match = MoveRegex.Match(line);
        if (match.Success)
        {
            return match.Groups["value"].Value.Trim();
        }

        var uses = UsesRegex.Match(line);
        if (uses.Success)
        {
            return uses.Groups["value"].Value.Trim();
        }

        return null;
    }

    private static string? ExtractCommand(string line)
    {
        var match = CommandRegex.Match(line);
        if (match.Success)
        {
            return match.Groups["value"].Value.Trim();
        }

        return null;
    }

    private static int? ExtractInt(string line, Regex regex)
    {
        var match = regex.Match(line);
        if (!match.Success)
        {
            return null;
        }

        if (int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return null;
    }

    private static double? ExtractDouble(string line, Regex regex)
    {
        var match = regex.Match(line);
        if (!match.Success)
        {
            return null;
        }

        if (double.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        return null;
    }

    private static int ParsePlayerIndex(string line)
    {
        if (Regex.IsMatch(line, @"\b(p1|player\s*1|side\s*1|team\s*1)\b", RegexOptions.IgnoreCase))
        {
            return 1;
        }

        if (Regex.IsMatch(line, @"\b(p2|player\s*2|side\s*2|team\s*2)\b", RegexOptions.IgnoreCase))
        {
            return 2;
        }

        return 0;
    }

    private static int ParsePlayerIndex(JsonElement element)
    {
        var direct = GetJsonInt(element, "player", "p", "side", "slot");
        if (direct.HasValue && direct.Value is >= 1 and <= 2)
        {
            return direct.Value;
        }

        var text = GetJsonString(element, "player", "side", "slot", "source", "actor");
        if (!string.IsNullOrWhiteSpace(text))
        {
            return ParsePlayerIndex(text);
        }

        if (element.TryGetProperty("actor", out var actor) && actor.ValueKind == JsonValueKind.Object)
        {
            var actorIndex = GetJsonInt(actor, "index", "player", "slot");
            if (actorIndex.HasValue && actorIndex.Value is >= 1 and <= 2)
            {
                return actorIndex.Value;
            }
        }

        return 0;
    }

    private static string? GetJsonString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }

                if (value.ValueKind == JsonValueKind.Number)
                {
                    return value.ToString();
                }
            }
        }

        return null;
    }

    private static int? GetJsonInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result))
                {
                    return result;
                }

                if (value.ValueKind == JsonValueKind.String &&
                    int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
            }
        }

        return null;
    }

    private static double? GetJsonDouble(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var result))
                {
                    return result;
                }

                if (value.ValueKind == JsonValueKind.String &&
                    double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result))
                {
                    return result;
                }
            }
        }

        return null;
    }

    private static DateTimeOffset? GetJsonTimestamp(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var numeric))
            {
                var seconds = numeric > 100000000000 ? numeric / 1000d : numeric;
                return DateTimeOffset.FromUnixTimeSeconds((long)seconds);
            }
        }

        return null;
    }

    private static TimeSpan? GetJsonDuration(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (!element.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String &&
                TimeSpan.TryParse(value.GetString(), CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var numeric))
            {
                return TimeSpan.FromSeconds(numeric);
            }
        }

        return null;
    }

    private static void TryParsePlayers(JsonElement root, ReplayMetadata metadata)
    {
        if (!root.TryGetProperty("players", out var playersElement) ||
            playersElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var players = playersElement.EnumerateArray().ToList();
        if (players.Count >= 1)
        {
            metadata.Player1 ??= GetJsonString(players[0], "name", "player", "id", "character");
        }

        if (players.Count >= 2)
        {
            metadata.Player2 ??= GetJsonString(players[1], "name", "player", "id", "character");
        }
    }

    private static DateTimeOffset? GetFileTimestamp(string replayPath)
    {
        if (!File.Exists(replayPath))
        {
            return null;
        }

        return new DateTimeOffset(File.GetLastWriteTimeUtc(replayPath), TimeSpan.Zero);
    }

    private static bool IsCountingMove(ReplayEventType type)
    {
        return type is ReplayEventType.Move or ReplayEventType.Hit or ReplayEventType.Block or ReplayEventType.Whiff
            or ReplayEventType.Throw or ReplayEventType.Projectile or ReplayEventType.AntiAir or ReplayEventType.Movement;
    }

    private static bool IsReplayExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return ReplayExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase));
    }

    private static string? CleanMoveName(string? move)
    {
        if (string.IsNullOrWhiteSpace(move))
        {
            return null;
        }

        var trimmed = move.Trim();
        if (trimmed.Length > 64)
        {
            trimmed = trimmed.Substring(0, 64);
        }

        return trimmed;
    }

    private static bool IsAirMove(string? move)
    {
        if (string.IsNullOrWhiteSpace(move))
        {
            return false;
        }

        var lower = move.ToLowerInvariant();
        return lower.Contains("j.") || lower.Contains("jump") || lower.Contains("air");
    }

    private static string FormatSequenceSuggestion(string label, MoveSequenceSummary sequence)
    {
        var moveText = string.Join(" -> ", sequence.Moves);
        var detail = sequence.Damage > 0 ? $" (approx {sequence.Damage} dmg)" : string.Empty;
        return $"{label}: {moveText}{detail}";
    }

    private static MoveSequenceSummary? FindMostCommonTransition(IReadOnlyList<ReplayEvent> events, int playerIndex)
    {
        var moves = events
            .Where(ev => ev.PlayerIndex == playerIndex)
            .Select(ev => CleanMoveName(ev.Move) ?? CleanMoveName(ev.Command))
            .Where(move => !string.IsNullOrWhiteSpace(move))
            .ToList();

        if (moves.Count < 2)
        {
            return null;
        }

        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < moves.Count - 1; i++)
        {
            var current = moves[i]!;
            var next = moves[i + 1]!;
            if (string.Equals(current, next, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var key = $"{current} -> {next}";
            counts[key] = counts.TryGetValue(key, out var value) ? value + 1 : 1;
        }

        var best = counts.OrderByDescending(pair => pair.Value).FirstOrDefault();
        if (best.Value < 2)
        {
            return null;
        }

        var parts = best.Key.Split(" -> ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        return new MoveSequenceSummary(playerIndex, parts.ToList(), 0, 0, best.Value, 0);
    }

    private sealed class SequenceBuilder
    {
        private readonly int _playerIndex;
        private readonly List<string> _moves = new();
        private int _hits;
        private int _damage;
        private int _drops;
        private string? _lastMove;

        public SequenceBuilder(int playerIndex)
        {
            _playerIndex = playerIndex;
        }

        public bool HasContent => _moves.Count > 0 || _hits > 0;

        public void Add(ReplayEvent ev)
        {
            var move = CleanMoveName(ev.Move) ?? CleanMoveName(ev.Command);
            if (!string.IsNullOrWhiteSpace(move) &&
                !string.Equals(move, _lastMove, StringComparison.OrdinalIgnoreCase))
            {
                if (_moves.Count < 6)
                {
                    _moves.Add(move);
                }

                _lastMove = move;
            }

            if (ev.Type == ReplayEventType.Hit)
            {
                _hits++;
                if (ev.Damage.HasValue)
                {
                    _damage += ev.Damage.Value;
                }
            }

            if (ev.Type == ReplayEventType.Whiff && _hits > 0)
            {
                _drops++;
            }
        }

        public MoveSequenceSummary Build()
        {
            return new MoveSequenceSummary(_playerIndex, _moves.ToList(), _hits, _damage, 1, _drops);
        }
    }

    private static readonly string[] ReplayExtensions = { ".json", ".log", ".txt", ".replay" };
    private static readonly Regex DamageRegex = new(@"\b(?:dmg|damage)\s*[:=]?\s*(?<value>\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex FrameRegex = new(@"\b(?:frame|f)\s*[:=]?\s*(?<value>\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex TimeRegex = new(@"\b(?:t|time|timestamp)\s*[:=]?\s*(?<value>\d+(\.\d+)?)", RegexOptions.IgnoreCase);
    private static readonly Regex MoveRegex = new(@"\b(?:move|action|command|input)\s*[:=]\s*(?<value>[^,;]+)", RegexOptions.IgnoreCase);
    private static readonly Regex CommandRegex = new(@"\b(?:input|command)\s*[:=]\s*(?<value>[^,;]+)", RegexOptions.IgnoreCase);
    private static readonly Regex UsesRegex = new(@"\buses\s+(?<value>[A-Za-z0-9\.\+\-\s]+)", RegexOptions.IgnoreCase);

    private sealed class ReplayMetadata
    {
        public string? Player1 { get; set; }
        public string? Player2 { get; set; }
        public string? Winner { get; set; }
        public string? Stage { get; set; }
        public string? Game { get; set; }
        public DateTimeOffset? RecordedAt { get; set; }
        public TimeSpan? Duration { get; set; }
        public string? Source { get; set; }
    }

    private sealed record ReplayAnalysis(
        ReplayMetadata Metadata,
        IReadOnlyList<ReplayEvent> Events,
        PlayerReplaySummary[] Players,
        IReadOnlyList<MoveSequenceSummary> Sequences,
        ReplayOutcome Outcome);

    private sealed record ReplayEvent(
        int PlayerIndex,
        ReplayEventType Type,
        string? Move,
        string? Command,
        int? Damage,
        int? Frame,
        double? TimeSeconds,
        string? Raw);

    private sealed record MoveSequenceSummary(
        int PlayerIndex,
        IReadOnlyList<string> Moves,
        int Hits,
        int Damage,
        int Occurrences,
        int Drops);

    private sealed class PlayerReplaySummary
    {
        public PlayerReplaySummary(int playerIndex)
        {
            PlayerIndex = playerIndex;
        }

        public int PlayerIndex { get; }
        public string? Name { get; set; }
        public int TotalMoves { get; set; }
        public int Hits { get; set; }
        public int Blocks { get; set; }
        public int Whiffs { get; set; }
        public int DamageDealt { get; set; }
        public int DamageTaken { get; set; }
        public int Throws { get; set; }
        public int Projectiles { get; set; }
        public int AntiAirs { get; set; }
        public int Knockdowns { get; set; }
        public int Combos { get; set; }
        public int ComboDrops { get; set; }
        public float HitRate => TotalMoves == 0 ? 0f : (float)Hits / TotalMoves;
        public float WhiffRate => TotalMoves == 0 ? 0f : (float)Whiffs / TotalMoves;
    }

    private enum ReplayOutcome
    {
        Unknown,
        Player1Win,
        Player2Win,
        Draw
    }

    private enum ReplayEventType
    {
        Unknown,
        Move,
        Hit,
        Block,
        Whiff,
        Throw,
        Projectile,
        AntiAir,
        Knockdown,
        Movement
    }

    private async Task<AiMatchupAdvice> GenerateAiMatchupAdviceAsync(
        MugenCharacter yourChar,
        MugenCharacter opponent,
        float predictedWinRate,
        CancellationToken ct)
    {
        var prompt = $@"Provide fighting game matchup advice for {yourChar.Name} vs {opponent.Name}.

Your Character: {yourChar.Name}
- Author: {yourChar.Author}
- Description: {yourChar.DisplayName}

Opponent: {opponent.Name}
- Author: {opponent.Author}
- Description: {opponent.DisplayName}

Predicted win rate: {predictedWinRate:P1}

Provide:
1. 3-5 specific tips for playing this matchup
2. 2-3 moves or strategies to avoid
3. 2-3 key moves or tech to use

Focus on fighting game fundamentals like spacing, pressure, and matchup knowledge.";

        var request = new AiRequest(AiRequestType.Chat, Prompt: prompt);
        var response = await _aiOrchestrator.ProcessRequestAsync(request, ct);

        if (!response.IsSuccessful)
        {
            // Fallback advice
            return new AiMatchupAdvice(
                new[] { "Focus on spacing and conditioning", "Use safe pokes to control the neutral", "Look for punish opportunities" },
                new[] { "Don't get predictable with movement", "Avoid mashing during pressure" },
                new[] { "Use your best normals", "Mix up high/low attacks", "Condition with projectiles if available" });
        }

        // Parse AI response (simplified parsing)
        var aiResponse = response.Content;
        var tips = ExtractSection(aiResponse, "tips") ?? new[] { "Study the matchup fundamentals", "Practice neutral game" };
        var movesToAvoid = ExtractSection(aiResponse, "avoid") ?? new[] { "Don't get hit by unsafe moves" };
        var keyMoves = ExtractSection(aiResponse, "key") ?? new[] { "Use safe, rewarding moves" };

        return new AiMatchupAdvice(tips, movesToAvoid, keyMoves);
    }

    private async Task<CharacterGuide> GenerateAiCharacterGuideAsync(
        MugenCharacter character,
        CancellationToken ct)
    {
        var prompt = $@"Create a fighting game character guide for {character.Name}.

Character: {character.Name}
Author: {character.Author}
Version: {character.Version}
Description: {character.DisplayName}

Provide:
1. Overview paragraph
2. 3-4 key strengths
3. 3-4 key weaknesses
4. 3-5 basic combos with input notation
5. 2-3 advanced tips

Use standard fighting game notation (e.g., 5LP, 2MK, 214HK for special moves).";

        var request = new AiRequest(AiRequestType.Chat, Prompt: prompt);
        var response = await _aiOrchestrator.ProcessRequestAsync(request, ct);

        if (!response.IsSuccessful)
        {
            // Fallback guide
            return new CharacterGuide(
                character.Id,
                character.Name,
                $"Guide for {character.Name}",
                new[] { "Strong normals", "Good combos", "Solid fundamentals" },
                new[] { "Limited mixups", "Weak anti-airs", "Slow movement" },
                new[]
                {
                    new ComboInfo("Basic Link Combo", "5LP > 5MP > 5HP", 1200, "Easy"),
                    new ComboInfo("Corner Combo", "2MK > 5HK > 214HK", 1800, "Medium")
                },
                new[] { "Master the neutral game", "Learn frame data", "Condition opponents" });
        }

        // Parse AI response
        var aiResponse = response.Content;

        var overview = ExtractOverview(aiResponse) ?? $"Comprehensive guide for {character.Name}";
        var strengths = ExtractList(aiResponse, "strengths") ?? new[] { "Solid fundamentals" };
        var weaknesses = ExtractList(aiResponse, "weaknesses") ?? new[] { "Learn to improve" };
        var combos = ExtractCombos(aiResponse) ?? new[]
        {
            new ComboInfo("Basic Combo", "5LP > 5MP", 800, "Easy")
        };
        var tips = ExtractList(aiResponse, "tips") ?? new[] { "Practice regularly" };

        return new CharacterGuide(
            character.Id,
            character.Name,
            overview,
            strengths,
            weaknesses,
            combos,
            tips);
    }

    private static IReadOnlyList<string>? ExtractSection(string text, string sectionName)
    {
        // Simplified parsing - real implementation would use better NLP
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var sectionLines = new List<string>();
        var inSection = false;

        foreach (var line in lines)
        {
            if (line.Contains(sectionName, StringComparison.OrdinalIgnoreCase))
            {
                inSection = true;
                continue;
            }

            if (inSection && (line.StartsWith("1.") || line.StartsWith("2.") || line.StartsWith("3.") ||
                            line.StartsWith("-") || line.StartsWith("•")))
            {
                sectionLines.Add(line.TrimStart('1', '2', '3', '.', '-', '•', ' ').Trim());
            }
            else if (inSection && string.IsNullOrWhiteSpace(line) == false &&
                    !line.StartsWith(char.ToUpper(line[0]).ToString()))
            {
                // End of section
                break;
            }
        }

        return sectionLines.Any() ? sectionLines : null;
    }

    private static string? ExtractOverview(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return lines.FirstOrDefault(line =>
            line.Length > 50 && !line.Contains("1.") && !line.Contains("2.") && !line.Contains("3."));
    }

    private static IReadOnlyList<string>? ExtractList(string text, string sectionName)
    {
        return ExtractSection(text, sectionName);
    }

    private static IReadOnlyList<ComboInfo>? ExtractCombos(string text)
    {
        var comboSection = ExtractSection(text, "combo");
        if (comboSection is null) return null;

        var combos = new List<ComboInfo>();
        foreach (var comboText in comboSection)
        {
            // Parse combo format: "Name: Input (damage) - difficulty"
            var parts = comboText.Split(':');
            if (parts.Length >= 2)
            {
                var name = parts[0].Trim();
                var rest = parts[1].Trim();
                var input = rest.Split('(').First().Trim();
                var damage = 1000; // Default
                var difficulty = "Medium"; // Default

                if (rest.Contains('(') && rest.Contains(')'))
                {
                    var damageStr = rest.Split('(')[1].Split(')')[0];
                    if (int.TryParse(damageStr, out var d)) damage = d;
                }

                combos.Add(new ComboInfo(name, input, damage, difficulty));
            }
        }

        return combos.Any() ? combos : null;
    }

    private sealed record AiMatchupAdvice(
        IReadOnlyList<string> Tips,
        IReadOnlyList<string> MovesToAvoid,
        IReadOnlyList<string> KeyMoves);

    public async Task<Result<string>> SendChatMessageAsync(string userMessage, string? context = null, CancellationToken ct = default)
    {
        try
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("You are an expert fighting game coach providing guidance to MUGEN players.");
            promptBuilder.AppendLine("Answer the user's question with concise, actionable fighting game advice.");

            if (!string.IsNullOrWhiteSpace(context))
            {
                promptBuilder.AppendLine($"Context: {context}");
            }

            promptBuilder.AppendLine($"User Question: {userMessage}");
            promptBuilder.AppendLine("Provide a brief, helpful response (2-4 sentences).");

            var request = new AiRequest(
                AiRequestType.Chat,
                Prompt: promptBuilder.ToString(),
                MaxTokens: 200,
                Temperature: 0.7f);

            var response = await _aiOrchestrator.ProcessRequestAsync(request, ct);

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
            {
                return Result.Failure<string>("AI coach is currently unavailable. Please try again later.");
            }

            return Result.Success(response.Content.Trim());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chat message: {Message}", userMessage);
            return Result.Failure<string>("Failed to get response from AI coach.");
        }
    }
}
