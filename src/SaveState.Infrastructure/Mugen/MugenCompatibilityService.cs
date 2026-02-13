namespace SaveState.Infrastructure.Mugen;

using System.Linq;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Analyzes and fixes common compatibility issues in MUGEN character definitions.
/// </summary>
public class MugenCompatibilityService : IMugenCompatibilityService
{
    private static readonly Dictionary<string, string[]> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cmd"] = new[] { ".cmd" },
        ["cns"] = new[] { ".cns" },
        ["st"] = new[] { ".st" },
        ["stcommon"] = new[] { ".cns" },
        ["state"] = new[] { ".st" },
        ["state2"] = new[] { ".st" },
        ["ai"] = new[] { ".cmd" },
        ["sprite"] = new[] { ".sff" },
        ["anim"] = new[] { ".air" },
        ["sound"] = new[] { ".snd" }
    };

    public async Task<Result<MugenCompatibilityReport>> AnalyzeAsync(MugenCharacter character, CancellationToken ct = default)
    {
        if (character == null)
            return Result.Failure<MugenCompatibilityReport>("Character is required.");

        var defPath = character.DefinitionFilePath;
        if (string.IsNullOrWhiteSpace(defPath) || !File.Exists(defPath))
            return Result.Failure<MugenCompatibilityReport>("Definition file not found.");

        var lines = await File.ReadAllLinesAsync(defPath, ct);
        var issues = AnalyzeDefinition(character, lines);

        return Result.Success(new MugenCompatibilityReport(issues, Array.Empty<MugenCompatibilityFix>()));
    }

    public async Task<Result<MugenCompatibilityReport>> FixAsync(MugenCharacter character, CancellationToken ct = default)
    {
        if (character == null)
            return Result.Failure<MugenCompatibilityReport>("Character is required.");

        var defPath = character.DefinitionFilePath;
        if (string.IsNullOrWhiteSpace(defPath) || !File.Exists(defPath))
            return Result.Failure<MugenCompatibilityReport>("Definition file not found.");

        var lines = await File.ReadAllLinesAsync(defPath, ct);
        var fixes = new List<MugenCompatibilityFix>();
        var updates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var issues = AnalyzeDefinition(character, lines);
        foreach (var issue in issues)
        {
            var parts = issue.Code.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                continue;

            var key = parts[1];
            if (!ExtensionMap.TryGetValue(key, out var extensions))
                continue;

            var candidate = FindCandidate(character.CharacterDirectory, extensions);
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            updates[key] = candidate;
            fixes.Add(new MugenCompatibilityFix("FIXED_MISSING", $"Updated {key} to {candidate}."));
        }

        if (updates.Count > 0)
        {
            ApplyUpdates(lines, updates);
            await File.WriteAllLinesAsync(defPath, lines, ct);
        }

        var remainingIssues = AnalyzeDefinition(character, lines);
        return Result.Success(new MugenCompatibilityReport(remainingIssues, fixes));
    }

    private static IReadOnlyList<MugenCompatibilityIssue> AnalyzeDefinition(
        MugenCharacter character,
        IReadOnlyList<string> lines)
    {
        var issues = new List<MugenCompatibilityIssue>();
        var directory = character.CharacterDirectory;

        if (!string.IsNullOrWhiteSpace(character.CommandFile))
        {
            var commandPath = ResolvePath(directory, character.CommandFile);
            if (!File.Exists(commandPath))
            {
                issues.Add(new MugenCompatibilityIssue("MISSING_FILE:cmd", $"Missing cmd file: {character.CommandFile}"));
            }
        }

        var inFilesSection = false;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("[", StringComparison.OrdinalIgnoreCase) && line.EndsWith("]"))
            {
                inFilesSection = line.Equals("[Files]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inFilesSection || string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith(";") || line.StartsWith("//"))
                continue;

            if (!line.Contains('='))
                continue;

            var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            if (!ExtensionMap.ContainsKey(key))
                continue;

            var valuePart = ExtractValue(parts[1]);
            if (string.IsNullOrWhiteSpace(valuePart))
                continue;

            var resolved = ResolvePath(directory, valuePart);
            if (!File.Exists(resolved))
            {
                issues.Add(new MugenCompatibilityIssue($"MISSING_FILE:{key}", $"Missing {key} file: {valuePart}"));
            }
        }

        return issues;
    }

    private static void ApplyUpdates(IList<string> lines, Dictionary<string, string> updates)
    {
        var inFilesSection = false;

        for (var i = 0; i < lines.Count; i++)
        {
            var raw = lines[i];
            var trimmed = raw.Trim();

            if (trimmed.StartsWith("[", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith("]"))
            {
                inFilesSection = trimmed.Equals("[Files]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inFilesSection || string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.StartsWith(";") || trimmed.StartsWith("//"))
                continue;

            if (!trimmed.Contains('='))
                continue;

            var parts = trimmed.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            if (!updates.TryGetValue(key, out var newValue))
                continue;

            var commentSplit = parts[1].Split(';', 2, StringSplitOptions.TrimEntries);
            var comment = commentSplit.Length == 2 ? $"; {commentSplit[1]}" : string.Empty;

            var prefix = raw.Substring(0, raw.IndexOf('=') + 1);
            lines[i] = $"{prefix} {newValue} {comment}".TrimEnd();
        }
    }

    private static string ExtractValue(string raw)
    {
        var trimmed = raw.Trim();
        var commentIndex = trimmed.IndexOf(';');
        if (commentIndex >= 0)
            trimmed = trimmed[..commentIndex];

        return trimmed.Trim().Trim('"');
    }

    private static string ResolvePath(string root, string relativeOrAbsolute)
    {
        var trimmed = relativeOrAbsolute.Trim().Trim('"');
        if (Path.IsPathRooted(trimmed))
            return trimmed;

        return Path.GetFullPath(Path.Combine(root, trimmed));
    }

    private static string? FindCandidate(string root, IEnumerable<string> extensions)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return null;

        foreach (var ext in extensions)
        {
            var match = Directory.EnumerateFiles(root, $"*{ext}", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(match))
                return Path.GetFileName(match);
        }

        return null;
    }
}
