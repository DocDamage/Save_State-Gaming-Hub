using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SaveState.Infrastructure.Mugen.Coaching.ReplayAnalysis;

/// <summary>
/// Parses replay files in JSON and text formats.
/// </summary>
public sealed partial class ReplayParsingEngine : IReplayParsingEngine
{
    private static readonly Regex DamageRegex = new(@"\b(?:dmg|damage)\s*[:=]?\s*(?<value>\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex FrameRegex = new(@"\b(?:frame|f)\s*[:=]?\s*(?<value>\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex TimeRegex = new(@"\b(?:t|time|timestamp)\s*[:=]?\s*(?<value>\d+(\.\d+)?)", RegexOptions.IgnoreCase);
    private static readonly Regex MoveRegex = new(@"\b(?:move|action|command|input)\s*[:=]\s*(?<value>[^,;]+)", RegexOptions.IgnoreCase);
    private static readonly Regex CommandRegex = new(@"\b(?:input|command)\s*[:=]\s*(?<value>[^,;]+)", RegexOptions.IgnoreCase);
    private static readonly Regex UsesRegex = new(@"\buses\s+(?<value>[A-Za-z0-9\.\+\-\s]+)", RegexOptions.IgnoreCase);

    /// <inheritdoc />
    public void ParseJsonReplay(string json, ReplayMetadata metadata, List<ReplayEvent> events)
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

    /// <inheritdoc />
    public void ParseTextReplay(string text, ReplayMetadata metadata, List<ReplayEvent> events)
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
                $"\\b{Regex.Escape(key)}\\b\\s*[:=]\\s*(?<value>.+)$",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                return match.Groups["value"].Value.Trim();
            }
        }

        return null;
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
}
