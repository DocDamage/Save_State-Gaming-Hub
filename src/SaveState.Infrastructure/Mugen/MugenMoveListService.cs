namespace SaveState.Infrastructure.Mugen;

using System.Linq;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Extracts move lists from MUGEN command files.
/// </summary>
public class MugenMoveListService : IMugenMoveListService
{
    public async Task<Result<IReadOnlyList<MugenMoveEntry>>> GetMoveListAsync(
        MugenCharacter character,
        CancellationToken ct = default)
    {
        if (character == null)
            return Result.Failure<IReadOnlyList<MugenMoveEntry>>("Character is required.");

        var commandFile = ResolveCommandFile(character);
        if (string.IsNullOrWhiteSpace(commandFile) || !File.Exists(commandFile))
            return Result.Failure<IReadOnlyList<MugenMoveEntry>>("Command file not found.");

        var lines = await File.ReadAllLinesAsync(commandFile, ct);
        var moves = ParseCommandFile(lines);

        return Result.Success<IReadOnlyList<MugenMoveEntry>>(moves);
    }

    private static string? ResolveCommandFile(MugenCharacter character)
    {
        var directory = character.CharacterDirectory;
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return null;

        if (!string.IsNullOrWhiteSpace(character.CommandFile))
        {
            var resolved = ResolvePath(directory, character.CommandFile);
            if (File.Exists(resolved))
                return resolved;
        }

        var defName = Path.GetFileNameWithoutExtension(character.DefinitionFilePath);
        if (!string.IsNullOrWhiteSpace(defName))
        {
            var candidate = Path.Combine(directory, $"{defName}.cmd");
            if (File.Exists(candidate))
                return candidate;
        }

        return Directory.EnumerateFiles(directory, "*.cmd", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();
    }

    private static string ResolvePath(string root, string relativeOrAbsolute)
    {
        var trimmed = relativeOrAbsolute.Trim().Trim('"');
        if (Path.IsPathRooted(trimmed))
            return trimmed;

        return Path.GetFullPath(Path.Combine(root, trimmed));
    }

    private static IReadOnlyList<MugenMoveEntry> ParseCommandFile(IEnumerable<string> lines)
    {
        var entries = new List<MugenMoveEntry>();
        var inCommand = false;
        var currentName = string.Empty;
        var currentCommand = string.Empty;
        string? currentComment = null;

        void Flush()
        {
            if (!string.IsNullOrWhiteSpace(currentName) && !string.IsNullOrWhiteSpace(currentCommand))
            {
                entries.Add(new MugenMoveEntry(currentName, currentCommand, currentComment));
            }

            currentName = string.Empty;
            currentCommand = string.Empty;
            currentComment = null;
        }

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith(";") || line.StartsWith("//"))
            {
                var comment = line.TrimStart(';', '/').Trim();
                if (!string.IsNullOrWhiteSpace(comment))
                    currentComment = comment;
                continue;
            }

            if (line.StartsWith("[", StringComparison.OrdinalIgnoreCase) && line.EndsWith("]"))
            {
                var isCommand = line.Equals("[Command]", StringComparison.OrdinalIgnoreCase);
                if (inCommand && !isCommand)
                {
                    Flush();
                }

                if (isCommand)
                {
                    if (inCommand)
                        Flush();
                    inCommand = true;
                }
                else
                {
                    inCommand = false;
                }

                continue;
            }

            if (!inCommand || !line.Contains('='))
                continue;

            var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                continue;

            var key = parts[0];
            var value = parts[1].Trim().Trim('"');
            if (key.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                currentName = value;
                continue;
            }

            if (key.Equals("command", StringComparison.OrdinalIgnoreCase))
            {
                currentCommand = value;
            }
        }

        if (inCommand)
        {
            Flush();
        }

        return entries;
    }
}
