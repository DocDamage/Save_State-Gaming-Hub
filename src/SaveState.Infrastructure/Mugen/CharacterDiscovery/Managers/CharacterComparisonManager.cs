using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.CharacterDiscovery.Managers;

/// <summary>
/// Manages character comparisons, compatibility matrices, and roster suggestions.
/// </summary>
public sealed class CharacterComparisonManager
{
    private readonly ILogger<CharacterComparisonManager> _logger;

    public CharacterComparisonManager(ILogger<CharacterComparisonManager> logger)
    {
        _logger = logger;
    }

    public async Task<Result<CharacterComparison>> CompareCharactersAsync(
        IReadOnlyList<Guid> characterIds,
        ComparisonOptions options,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            var selectedCharacters = characterIds
                .Select(id => characters.TryGetValue(id, out var c) ? c : null)
                .Where(c => c != null)
                .ToList();

            var compared = selectedCharacters.Select(c => new ComparedCharacter(c!.Id, c.Name, c.ThumbnailUrl)).ToList();

            var categories = new List<ComparisonCategory>
            {
                new("Rating", selectedCharacters.Select(c => new ComparisonValue(c!.Id, c.Rating.ToString("F1"), c.Rating >= 4.0)).ToList()),
                new("Downloads", selectedCharacters.Select(c => new ComparisonValue(c!.Id, c.DownloadCount.ToString(), c.DownloadCount > 1000)).ToList()),
                new("Reviews", selectedCharacters.Select(c => new ComparisonValue(c!.Id, c.ReviewCount.ToString(), c.ReviewCount > 10)).ToList())
            };

            var comparison = new CharacterComparison(compared, categories);
            return Result<CharacterComparison>.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare characters");
            return Result<CharacterComparison>.Failure(
                $"Comparison failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<CompatibilityMatrix>> GetCompatibilityMatrixAsync(
        IReadOnlyList<Guid> characterIds,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            var matrixChars = characterIds
                .Select(id => characters.TryGetValue(id, out var c) ? new MatrixCharacter(c.Id, c.Name) : null)
                .Where(c => c != null)
                .ToList();

            var scores = new List<IReadOnlyList<CompatibilityScore>>();
            var random = new Random();

            foreach (var char1 in matrixChars)
            {
                var row = new List<CompatibilityScore>();
                foreach (var char2 in matrixChars)
                {
                    var score = random.NextDouble() * 100;
                    var level = score switch
                    {
                        > 80 => CompatibilityLevel.Excellent,
                        > 60 => CompatibilityLevel.Good,
                        > 40 => CompatibilityLevel.Fair,
                        > 20 => CompatibilityLevel.Poor,
                        _ => CompatibilityLevel.Incompatible
                    };
                    row.Add(new CompatibilityScore(score, level));
                }
                scores.Add(row);
            }

            var matrix = new CompatibilityMatrix(matrixChars!, scores);
            return Result<CompatibilityMatrix>.Success(matrix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get compatibility matrix");
            return Result<CompatibilityMatrix>.Failure(
                $"Get matrix failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> SuggestRosterCompletionAsync(
        IReadOnlyList<Guid> currentRoster,
        RosterPreferences preferences,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            var needed = preferences.TargetSize - currentRoster.Count;
            if (needed <= 0)
            {
                return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Success(new List<DiscoveredCharacterRecommendation>());
            }

            var suggestions = characters.Values
                .Where(c => !currentRoster.Contains(c.Id))
                .Take(needed)
                .Select(c => new DiscoveredCharacterRecommendation(
                    c,
                    85.0,
                    $"Suggested for {preferences.Balance} roster",
                    preferences.RequiredTags.Intersect(c.Tags).ToList()))
                .ToList();

            return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Success(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to suggest roster completion");
            return Result<IReadOnlyList<DiscoveredCharacterRecommendation>>.Failure(
                $"Suggestion failed: {ex.Message}", ErrorType.Internal);
        }
    }
}
