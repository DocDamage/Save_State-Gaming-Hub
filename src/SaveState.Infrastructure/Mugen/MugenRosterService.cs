namespace SaveState.Infrastructure.Mugen;

using Microsoft.Extensions.Options;
using SaveState.Core.Common;
using SaveState.Core.Configuration;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Parses and persists MUGEN select.def rosters.
/// </summary>
public class MugenRosterService : IMugenRosterService
{
    private readonly MugenOptions _options;

    public MugenRosterService(IOptions<MugenOptions> options)
    {
        _options = options.Value;
    }

    public string? GetSelectDefPath()
    {
        if (!string.IsNullOrWhiteSpace(_options.SelectDefPath) && File.Exists(_options.SelectDefPath))
            return _options.SelectDefPath;

        var candidates = new[]
        {
            Path.Combine(_options.DataDirectory, "select.def"),
            Path.Combine("engines", "ikemen", "data", "select.def")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    public Task<Result<MugenRoster>> LoadRosterAsync(CancellationToken ct = default)
    {
        var path = GetSelectDefPath();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return Task.FromResult(Result.Failure<MugenRoster>("select.def not found"));

        var lines = File.ReadAllLines(path);
        var headerLines = new List<string>();
        var footerLines = new List<string>();
        var entries = new List<MugenRosterEntry>();

        var inCharacters = false;
        var currentCategory = (string?)null;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("[", StringComparison.OrdinalIgnoreCase))
            {
                if (line.Equals("[Characters]", StringComparison.OrdinalIgnoreCase))
                {
                    inCharacters = true;
                    continue;
                }

                if (inCharacters)
                {
                    inCharacters = false;
                    footerLines.Add(raw);
                }
                else
                {
                    headerLines.Add(raw);
                }

                continue;
            }

            if (!inCharacters)
            {
                headerLines.Add(raw);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                entries.Add(new MugenRosterEntry(MugenRosterEntryType.Comment, null, null, currentCategory, raw));
                continue;
            }

            if (line.StartsWith(";") || line.StartsWith("//"))
            {
                var comment = line.TrimStart(';', '/').Trim();
                if (!string.IsNullOrWhiteSpace(comment) && comment.Any(char.IsLetterOrDigit))
                {
                    currentCategory = comment;
                    entries.Add(new MugenRosterEntry(MugenRosterEntryType.Category, null, null, comment, raw));
                }
                else
                {
                    entries.Add(new MugenRosterEntry(MugenRosterEntryType.Comment, null, null, currentCategory, raw));
                }

                continue;
            }

            var parts = line.Split(',', 2, StringSplitOptions.TrimEntries);
            var characterPath = parts.Length > 0 ? parts[0] : string.Empty;
            var stagePath = parts.Length > 1 ? parts[1] : null;

            entries.Add(new MugenRosterEntry(
                MugenRosterEntryType.Character,
                characterPath,
                string.IsNullOrWhiteSpace(stagePath) ? null : stagePath,
                currentCategory,
                raw));
        }

        var roster = new MugenRoster(entries, headerLines, footerLines);
        return Task.FromResult(Result.Success(roster));
    }

    public Task<Result> SaveRosterAsync(MugenRoster roster, CancellationToken ct = default)
    {
        var path = GetSelectDefPath();
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult(Result.Failure("select.def not found"));

        var output = new List<string>();
        if (roster.HeaderLines.Count > 0)
        {
            output.AddRange(roster.HeaderLines);
        }

        output.Add("[Characters]");

        foreach (var entry in roster.Entries)
        {
            switch (entry.EntryType)
            {
                case MugenRosterEntryType.Category:
                    if (!string.IsNullOrWhiteSpace(entry.Category))
                        output.Add($"; {entry.Category}");
                    break;
                case MugenRosterEntryType.Comment:
                    output.Add(entry.RawLine ?? string.Empty);
                    break;
                case MugenRosterEntryType.Character:
                    if (string.IsNullOrWhiteSpace(entry.CharacterPath))
                        break;
                    if (!string.IsNullOrWhiteSpace(entry.StagePath))
                        output.Add($"{entry.CharacterPath}, {entry.StagePath}");
                    else
                        output.Add(entry.CharacterPath);
                    break;
            }
        }

        if (roster.FooterLines.Count > 0)
        {
            output.AddRange(roster.FooterLines);
        }

        File.WriteAllLines(path, output);
        return Task.FromResult(Result.Success());
    }

    public async Task<Result<IReadOnlyList<CharacterInfo>>> GetAllCharactersAsync(CancellationToken ct = default)
    {
        var result = await LoadRosterAsync(ct);
        if (!result.IsSuccess)
            return Result.Failure<IReadOnlyList<CharacterInfo>>(result.Error);

        var characters = result.Value.Entries
            .Where(e => e.EntryType == MugenRosterEntryType.Character)
            .Select(MapToCharacterInfo)
            .ToList();

        return Result.Success<IReadOnlyList<CharacterInfo>>(characters);
    }

    public async Task<Result<IReadOnlyList<CharacterInfo>>> SearchCharactersAsync(string searchTerm, CancellationToken ct = default)
    {
        var result = await GetAllCharactersAsync(ct);
        if (!result.IsSuccess)
            return result;

        var filtered = result.Value
            .Where(c => c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Result.Success<IReadOnlyList<CharacterInfo>>(filtered);
    }

    public async Task<Result<IReadOnlyList<CharacterInfo>>> GetCharactersByCategoryAsync(string category, CancellationToken ct = default)
    {
        var result = await GetAllCharactersAsync(ct);
        if (!result.IsSuccess)
            return result;

        var filtered = result.Value
            .Where(c => string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Result.Success<IReadOnlyList<CharacterInfo>>(filtered);
    }

    public async Task<Result> AddCharacterAsync(CharacterInfo character, CancellationToken ct = default)
    {
        var rosterResult = await LoadRosterAsync(ct);
        if (!rosterResult.IsSuccess)
            return Result.Failure(rosterResult.Error);

        var roster = rosterResult.Value;
        var entries = roster.Entries.ToList();

        // Add to matching category or default
        var newEntry = new MugenRosterEntry(
            MugenRosterEntryType.Character,
            character.Id, // Assuming Id is the path
            null,
            character.Category,
            character.Id);

        entries.Add(newEntry);

        var newRoster = new MugenRoster(entries, roster.HeaderLines, roster.FooterLines);
        return await SaveRosterAsync(newRoster, ct);
    }

    public async Task<Result> RemoveCharacterAsync(string characterId, CancellationToken ct = default)
    {
        var rosterResult = await LoadRosterAsync(ct);
        if (!rosterResult.IsSuccess)
            return Result.Failure(rosterResult.Error);

        var roster = rosterResult.Value;
        var entries = roster.Entries.ToList();

        var removed = entries.RemoveAll(e =>
            e.EntryType == MugenRosterEntryType.Character &&
            (string.Equals(e.CharacterPath, characterId, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(Path.GetFileName(e.CharacterPath), characterId, StringComparison.OrdinalIgnoreCase)));

        if (removed == 0)
            return Result.Failure($"Character '{characterId}' not found in roster");

        var newRoster = new MugenRoster(entries, roster.HeaderLines, roster.FooterLines);
        return await SaveRosterAsync(newRoster, ct);
    }

    private static CharacterInfo MapToCharacterInfo(MugenRosterEntry entry)
    {
        var name = Path.GetFileNameWithoutExtension(entry.CharacterPath) ?? "Unknown";
        return new CharacterInfo(
            Id: entry.CharacterPath ?? string.Empty,
            Name: name,
            DisplayName: name,
            Category: entry.Category,
            Author: null,
            Version: null,
            ThumbnailPath: null);
    }
}
