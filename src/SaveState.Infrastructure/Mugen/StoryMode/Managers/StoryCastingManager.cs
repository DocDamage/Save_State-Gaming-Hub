using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.StoryMode.Managers;

/// <summary>
/// Manages story character casting.
/// </summary>
public class StoryCastingManager
{
    private readonly ILogger<StoryCastingManager> _logger;
    private readonly ConcurrentDictionary<Guid, StoryCharacter> _cast;

    public StoryCastingManager(ILogger<StoryCastingManager> logger)
    {
        _logger = logger;
        _cast = new ConcurrentDictionary<Guid, StoryCharacter>();
    }

    public ConcurrentDictionary<Guid, StoryCharacter> Cast => _cast;

    /// <summary>
    /// Adds a character to the story cast.
    /// </summary>
    public Task<Result<StoryCharacter>> AddCastMemberAsync(
        Guid characterId,
        CastingOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adding cast member: {CharacterId}", characterId);

            var castMember = new StoryCharacter(
                Guid.NewGuid(),
                characterId,
                $"Character_{characterId.ToString()[..8]}",
                options.DefaultAppearance,
                options.DefaultDifficulty != StoryAiDifficulty.Normal ? new StoryAiSettings(
                    options.DefaultDifficulty,
                    50,
                    new List<string>(),
                    new List<string>()) : null,
                new Dictionary<string, object>());

            _cast[castMember.Id] = castMember;
            return Task.FromResult(Result<StoryCharacter>.Success(castMember));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add cast member");
            return Task.FromResult(Result<StoryCharacter>.Failure($"Add cast member failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Removes a cast member from the story.
    /// </summary>
    public Task<Result> RemoveCastMemberAsync(
        Guid castMemberId,
        CancellationToken ct = default)
    {
        _cast.TryRemove(castMemberId, out _);
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Gets all cast members for the story.
    /// </summary>
    public Task<Result<IReadOnlyList<StoryCharacter>>> GetCastAsync(
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<IReadOnlyList<StoryCharacter>>.Success(_cast.Values.ToList()));
    }

    /// <summary>
    /// Sets the appearance for a cast member.
    /// </summary>
    public Task<Result> SetCharacterAppearanceAsync(
        Guid castMemberId,
        CharacterAppearance appearance,
        CancellationToken ct = default)
    {
        try
        {
            if (!_cast.TryGetValue(castMemberId, out var character))
            {
                return Task.FromResult(Result.Failure("Cast member not found", ErrorType.NotFound));
            }

            _cast[castMemberId] = character with { Appearance = appearance };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set character appearance");
            return Task.FromResult(Result.Failure($"Set appearance failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Sets the AI settings for a cast member.
    /// </summary>
    public Task<Result> SetCharacterAiAsync(
        Guid castMemberId,
        StoryAiSettings aiSettings,
        CancellationToken ct = default)
    {
        try
        {
            if (!_cast.TryGetValue(castMemberId, out var character))
            {
                return Task.FromResult(Result.Failure("Cast member not found", ErrorType.NotFound));
            }

            _cast[castMemberId] = character with { AiSettings = aiSettings };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set character AI");
            return Task.FromResult(Result.Failure($"Set AI failed: {ex.Message}", ErrorType.Internal));
        }
    }
}
