using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Statistics and data loading partial class for MugenHubViewModel.
/// </summary>
public partial class MugenHubViewModel
{
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            await Task.WhenAll(
                LoadCharactersAsync(),
                LoadRecentMatchesAsync(),
                LoadStagesAsync(),
                LoadReplaysAsync(),
                LoadNetplayAsync()
            );

            await Task.WhenAll(
                LoadTierListAsync(),
                LoadEloRatingsAsync()
            );
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadRecentMatchesAsync()
    {
        try
        {
            var result = await _statsService.GetRecentMatchesAsync(10);
            RecentMatches.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                var nameMap = await BuildRosterNameMapAsync();
                foreach (var match in result.Value)
                {
                    var p1Name = nameMap.TryGetValue(match.Player1CharacterId, out var n1) ? n1 : match.Player1CharacterId.ToString();
                    var p2Name = nameMap.TryGetValue(match.Player2CharacterId, out var n2) ? n2 : match.Player2CharacterId.ToString();

                    RecentMatches.Add(new MugenMatchSummary(
                        p1Name,
                        p2Name,
                        match.Result,
                        match.MatchDuration,
                        match.PlayedAt));
                }
            }

            _logger.LogInformation("Loaded {Count} recent matches", RecentMatches.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent matches");
        }
    }

    private async Task<Dictionary<Guid, string>> BuildRosterNameMapAsync()
    {
        var map = new Dictionary<Guid, string>();

        foreach (var character in Characters)
        {
            map[character.Id] = character.DisplayName;
        }

        if (map.Count > 0)
            return map;

        var roster = await _collectionService.GetRosterAsync();
        if (roster.IsSuccess && roster.Value != null)
        {
            foreach (var character in roster.Value)
            {
                map[character.Id] = character.DisplayName;
            }
        }

        return map;
    }

    private async Task LoadStagesAsync()
    {
        try
        {
            var stageFiles = FindStageDefinitionFiles(_mugenOptions.StageDirectories)
                .Distinct(StringComparer.OrdinalIgnoreCase);
            var stageItems = new List<MugenStageSummary>();

            foreach (var stagePath in stageFiles)
            {
                var name = await ExtractStageNameAsync(stagePath);
                stageItems.Add(new MugenStageSummary(name, stagePath));
            }

            Stages.Clear();
            foreach (var stage in stageItems.OrderBy(s => s.Name))
            {
                Stages.Add(stage);
            }

            SelectedStage ??= Stages.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load stages");
        }
    }

    private async Task LoadReplaysAsync()
    {
        try
        {
            var replays = new List<MugenReplaySummary>();
            var nameMap = await BuildRosterNameMapAsync();

            var histories = await _matchHistoryRepository.GetMatchHistoriesAsync(pageNumber: 1, pageSize: 100, ct: default);
            foreach (var match in histories.Items)
            {
                if (string.IsNullOrWhiteSpace(match.ReplayPath))
                    continue;

                var p1Name = nameMap.TryGetValue(match.Player1CharacterId, out var n1) ? n1 : match.Player1CharacterId.ToString();
                var p2Name = nameMap.TryGetValue(match.Player2CharacterId, out var n2) ? n2 : match.Player2CharacterId.ToString();

                replays.Add(new MugenReplaySummary(
                    $"{p1Name} vs {p2Name}",
                    match.ReplayPath,
                    match.PlayedAt,
                    match.MatchDuration));
            }

            var replayFiles = FindReplayFiles(_mugenOptions.SaveDirectory);
            foreach (var replay in replayFiles)
            {
                replays.Add(replay);
            }

            Replays.Clear();
            foreach (var replay in replays
                .GroupBy(r => r.Path, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.PlayedAt).First())
                .OrderByDescending(r => r.PlayedAt)
                .Take(50))
            {
                Replays.Add(replay);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load replays");
        }
    }

    private async Task LoadTierListAsync()
    {
        try
        {
            var matches = await _matchHistoryRepository.GetMatchHistoriesAsync(pageNumber: 1, pageSize: 1000, ct: default);
            var stats = new Dictionary<Guid, CharacterRecord>();

            foreach (var match in matches.Items)
            {
                UpdateRecord(stats, match.Player1CharacterId, match.Result == MatchResult.Player1Win);
                UpdateRecord(stats, match.Player2CharacterId, match.Result == MatchResult.Player2Win);
            }

            if (stats.Count == 0)
            {
                TierList.Clear();
                return;
            }

            if (Characters.Count == 0)
            {
                var roster = await _collectionService.GetRosterAsync();
                if (roster.IsSuccess && roster.Value != null)
                {
                    Characters.Clear();
                    foreach (var character in roster.Value)
                    {
                        Characters.Add(character);
                    }
                }
            }

            var nameMap = Characters.ToDictionary(c => c.Id, c => c.DisplayName);
            var tiers = stats
                .Select(kvp =>
                {
                    var total = kvp.Value.Wins + kvp.Value.Losses;
                    var winRate = total > 0 ? (double)kvp.Value.Wins / total : 0;
                    return new MugenTierEntry(
                        kvp.Key,
                        nameMap.TryGetValue(kvp.Key, out var name) ? name : kvp.Key.ToString(),
                        kvp.Value.Wins,
                        kvp.Value.Losses,
                        winRate,
                        GetTierLabel(winRate, total));
                })
                .OrderByDescending(t => t.WinRate)
                .ThenByDescending(t => t.Matches)
                .Take(20)
                .ToList();

            TierList.Clear();
            foreach (var entry in tiers)
            {
                TierList.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load tier list");
        }
    }

    private static IReadOnlyDictionary<Guid, string> BuildCharacterNameMap(MugenTournament tournament)
    {
        var map = new Dictionary<Guid, string>();

        foreach (var participant in tournament.Participants)
        {
            var name = participant.Character?.DisplayName;
            if (string.IsNullOrWhiteSpace(name))
                name = participant.CharacterId.ToString();

            map[participant.CharacterId] = name;
        }

        return map;
    }

    private static void UpdateRecord(Dictionary<Guid, CharacterRecord> stats, Guid characterId, bool isWin)
    {
        if (!stats.TryGetValue(characterId, out var record))
        {
            record = new CharacterRecord();
            stats[characterId] = record;
        }

        if (isWin)
            record.Wins++;
        else
            record.Losses++;
    }

    private static string GetTierLabel(double winRate, int matches)
    {
        if (matches < 5) return "C";
        if (winRate >= 0.7) return "S";
        if (winRate >= 0.6) return "A";
        if (winRate >= 0.5) return "B";
        return "C";
    }

    private static IEnumerable<string> FindStageDefinitionFiles(IEnumerable<string> directories)
    {
        var results = new List<string>();

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir)) continue;
            try
            {
                results.AddRange(Directory.EnumerateFiles(dir, "*.def", SearchOption.AllDirectories));
            }
            catch
            {
                // Ignore invalid paths
            }
        }

        return results;
    }

    private static async Task<string> ExtractStageNameAsync(string stagePath)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(stagePath);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.StartsWith("name", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                        return parts[1].Trim().Trim('"');
                }
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return Path.GetFileNameWithoutExtension(stagePath);
    }

    private static IEnumerable<MugenReplaySummary> FindReplayFiles(string baseDirectory)
    {
        var results = new List<MugenReplaySummary>();

        if (string.IsNullOrWhiteSpace(baseDirectory))
            return results;

        var root = Path.GetFullPath(baseDirectory);
        if (!Directory.Exists(root))
            return results;

        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".rep",
            ".replay",
            ".rpl"
        };

        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (!extensions.Contains(Path.GetExtension(file)))
                    continue;

                var info = new FileInfo(file);
                results.Add(new MugenReplaySummary(
                    Path.GetFileNameWithoutExtension(file),
                    file,
                    info.LastWriteTimeUtc,
                    TimeSpan.Zero));
            }
        }
        catch
        {
            return results;
        }

        return results;
    }

    private async Task LoadEloRatingsAsync()
    {
        try
        {
            var result = await _eloService.GetRatingsAsync();
            EloRatings.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var entry in result.Value)
                    EloRatings.Add(new MugenEloRating
                    {
                        CharacterId = entry.CharacterId,
                        CharacterName = entry.CharacterName,
                        Rating = (int)entry.Rating,
                        Wins = entry.Wins,
                        Losses = entry.Losses
                    });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ELO ratings");
        }
    }
}
