using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.CharacterDiscovery.Managers;

/// <summary>
/// Manages character details, reviews, matchups, and showcases.
/// </summary>
public sealed class CharacterDetailsManager
{
    private readonly ILogger<CharacterDetailsManager> _logger;
    private readonly ITimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="CharacterDetailsManager"/> class.
    /// </summary>
    public CharacterDetailsManager(
        ILogger<CharacterDetailsManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets detailed information about a character.
    /// </summary>
    public Task<Result<CharacterDetail>> GetCharacterDetailsAsync(
        Guid characterId,
        ConcurrentDictionary<Guid, CharacterDetail> characterDetails,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        ConcurrentDictionary<string, List<Guid>> recentlyViewed,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting character details for {CharacterId}", characterId);

            ct.ThrowIfCancellationRequested();

            // Check if character exists in the characters dictionary
            if (!characters.TryGetValue(characterId, out var character))
            {
                _logger.LogWarning("Character {CharacterId} not found", characterId);
                return Task.FromResult(Result<CharacterDetail>.Failure(
                    $"Character {characterId} not found",
                    ErrorType.NotFound));
            }

            // Try to get existing detailed info or create from basic character info
            if (!characterDetails.TryGetValue(characterId, out var detail))
            {
                // Create detail from basic character info
                detail = new CharacterDetail(
                    character.Id,
                    character.Name,
                    character.Author,
                    character.Description,
                    null, // Story
                    character.Tags,
                    character.Categories,
                    new List<string>(), // GameplayStyles
                    character.Rating,
                    character.ReviewCount,
                    character.DownloadCount,
                    character.ThumbnailUrl,
                    new List<string>(), // ScreenshotUrls
                    null, // VideoUrl
                    new List<string>(), // DownloadUrls
                    character.AddedDate,
                    character.LastUpdated,
                    character.Stats,
                    new CharacterMoveList(0, new List<string>(), new List<string>()),
                    new CharacterPaletteInfo(0, new List<string>(), false),
                    new List<CharacterCompatibility>());

                characterDetails[characterId] = detail;
            }

            // Track the view in recently viewed dictionary
            var today = _timeProvider.Today.ToString("yyyy-MM-dd");
            var viewedList = recentlyViewed.GetOrAdd(today, _ => new List<Guid>());
            lock (viewedList)
            {
                if (!viewedList.Contains(characterId))
                {
                    viewedList.Add(characterId);
                }
            }

            return Task.FromResult(Result<CharacterDetail>.Success(detail));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character details for {CharacterId}", characterId);
            return Task.FromResult(Result<CharacterDetail>.Failure(
                $"Failed to get character details: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets character reviews and ratings.
    /// </summary>
    public Task<Result<CharacterReviews>> GetCharacterReviewsAsync(
        Guid characterId,
        int? page,
        int? pageSize,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting character reviews for {CharacterId}", characterId);

            ct.ThrowIfCancellationRequested();

            var actualPage = page ?? 1;
            var actualPageSize = pageSize ?? 10;

            // Generate sample review data
            var reviews = new List<CharacterReview>
            {
                new(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "MugenFan123",
                    5,
                    "Amazing character!",
                    "One of the best characters I've played with. Great animations and balanced gameplay.",
                    _timeProvider.Now.AddDays(-5),
                    12,
                    1),
                new(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "FightingGamePro",
                    4,
                    "Solid character",
                    "Good moveset and combos. Could use some polish on the AI.",
                    _timeProvider.Now.AddDays(-12),
                    8,
                    0),
                new(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "RetroGamer",
                    5,
                    "Must download!",
                    "Perfect recreation of the original. Love the special moves!",
                    _timeProvider.Now.AddDays(-20),
                    15,
                    2)
            };

            var distribution = new RatingDistribution(
                FiveStars: 45,
                FourStars: 23,
                ThreeStars: 8,
                TwoStars: 3,
                OneStar: 1);

            var result = new CharacterReviews(
                reviews,
                reviews.Count + 76, // Total count including non-visible reviews
                4.5,
                distribution,
                actualPage,
                actualPageSize);

            return Task.FromResult(Result<CharacterReviews>.Success(result));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character reviews for {CharacterId}", characterId);
            return Task.FromResult(Result<CharacterReviews>.Failure(
                $"Failed to get character reviews: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets character matchups and compatibility.
    /// </summary>
    public Task<Result<IReadOnlyList<CharacterMatchup>>> GetCharacterMatchupsAsync(
        Guid characterId,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting character matchups for {CharacterId}", characterId);

            ct.ThrowIfCancellationRequested();

            // Check if character exists
            if (!characters.ContainsKey(characterId))
            {
                _logger.LogWarning("Character {CharacterId} not found", characterId);
                return Task.FromResult(Result<IReadOnlyList<CharacterMatchup>>.Failure(
                    $"Character {characterId} not found",
                    ErrorType.NotFound));
            }

            // Generate sample matchup data from other characters
            var matchups = new List<CharacterMatchup>();
            var random = new Random(characterId.GetHashCode());

            foreach (var kvp in characters.Take(10))
            {
                if (kvp.Key == characterId) continue;

                var wins = random.Next(10, 100);
                var losses = random.Next(10, 100);
                var winRate = (double)wins / (wins + losses) * 100;

                matchups.Add(new CharacterMatchup(
                    kvp.Key,
                    kvp.Value.Name,
                    wins,
                    losses,
                    Math.Round(winRate, 1)));
            }

            return Task.FromResult(Result<IReadOnlyList<CharacterMatchup>>.Success(matchups));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character matchups for {CharacterId}", characterId);
            return Task.FromResult(Result<IReadOnlyList<CharacterMatchup>>.Failure(
                $"Failed to get character matchups: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets character showcase videos.
    /// </summary>
    public Task<Result<IReadOnlyList<CharacterShowcase>>> GetShowcasesAsync(
        Guid characterId,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting character showcases for {CharacterId}", characterId);

            ct.ThrowIfCancellationRequested();

            // Generate sample showcase data
            var showcases = new List<CharacterShowcase>
            {
                new(
                    "Combo Exhibition - Max Damage",
                    "https://example.com/showcase1",
                    "https://example.com/thumb1.jpg",
                    "Showcasing the most powerful combos and setups",
                    "ComboMaster",
                    _timeProvider.Now.AddDays(-30),
                    15420),
                new(
                    "Gameplay Tutorial - Beginner Friendly",
                    "https://example.com/showcase2",
                    "https://example.com/thumb2.jpg",
                    "Learn the basics of playing this character",
                    "TutorialKing",
                    _timeProvider.Now.AddDays(-45),
                    8930),
                new(
                    "AI Battle Showcase",
                    "https://example.com/showcase3",
                    "https://example.com/thumb3.jpg",
                    "Watch the AI battle against various opponents",
                    "AI_Watcher",
                    _timeProvider.Now.AddDays(-60),
                    22150)
            };

            return Task.FromResult(Result<IReadOnlyList<CharacterShowcase>>.Success(showcases));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character showcases for {CharacterId}", characterId);
            return Task.FromResult(Result<IReadOnlyList<CharacterShowcase>>.Failure(
                $"Failed to get character showcases: {ex.Message}",
                ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets download history for a character.
    /// </summary>
    public Task<Result<DownloadHistory>> GetDownloadHistoryAsync(
        Guid characterId,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Getting download history for {CharacterId}", characterId);

            ct.ThrowIfCancellationRequested();

            // Generate sample download history
            var recentDownloads = new List<DownloadEntry>
            {
                new(_timeProvider.Today.AddDays(-1), "2.1.0", 45),
                new(_timeProvider.Today.AddDays(-2), "2.1.0", 52),
                new(_timeProvider.Today.AddDays(-3), "2.0.5", 38),
                new(_timeProvider.Today.AddDays(-4), "2.0.5", 41),
                new(_timeProvider.Today.AddDays(-5), "2.0.5", 35),
                new(_timeProvider.Today.AddDays(-6), "2.0.0", 29),
                new(_timeProvider.Today.AddDays(-7), "2.0.0", 33)
            };

            var history = new DownloadHistory(
                characterId,
                15420, // Total downloads
                recentDownloads);

            return Task.FromResult(Result<DownloadHistory>.Success(history));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get download history for {CharacterId}", characterId);
            return Task.FromResult(Result<DownloadHistory>.Failure(
                $"Failed to get download history: {ex.Message}",
                ErrorType.Internal));
        }
    }
}
