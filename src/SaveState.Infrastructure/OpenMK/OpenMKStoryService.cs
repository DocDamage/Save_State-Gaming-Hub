using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Services;
using SaveState.Core.OpenMK.ValueObjects;

namespace SaveState.Infrastructure.OpenMK;

/// <summary>
/// Implementation of OpenMK story service for arcade mode progression.
/// </summary>
public partial class OpenMKStoryService : IOpenMKStoryService
{
    private readonly ILogger<OpenMKStoryService> _logger;

    public OpenMKStoryService(ILogger<OpenMKStoryService> logger)
    {
        _logger = logger;
    }

    public async Task<Result<OpenMKStoryCampaign>> StartCampaignAsync(Guid userId, Guid selectedCharacterId, CancellationToken ct = default)
    {
        try
        {
            var campaign = new OpenMKStoryCampaign(
                Id: Guid.NewGuid(),
                UserId: userId,
                SelectedCharacterId: selectedCharacterId,
                CharacterName: "Liu Kang", // Would be fetched from character service
                Difficulty: OpenMKCampaignDifficulty.Medium,
                StartedAt: DateTime.UtcNow,
                CurrentChapter: 1,
                TotalChapters: 10,
                ProgressPercentage: 0,
                ChoicesMade: new List<OpenMKStoryChoice>(),
                CompletedChapters: new List<OpenMKChapterCompletion>());

            LogCampaignStarted(_logger, userId, selectedCharacterId, campaign.Id);
            return Result.Success(campaign);
        }
        catch (Exception ex)
        {
            LogStartCampaignFailed(_logger, userId, selectedCharacterId, ex);
            return Result.Failure<OpenMKStoryCampaign>($"Failed to start campaign: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKStoryCampaign>> GetCurrentCampaignAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            // In a real implementation, this would fetch from database
            // For now, return a sample campaign
            var campaign = new OpenMKStoryCampaign(
                Id: Guid.NewGuid(),
                UserId: userId,
                SelectedCharacterId: Guid.NewGuid(),
                CharacterName: "Liu Kang",
                Difficulty: OpenMKCampaignDifficulty.Medium,
                StartedAt: DateTime.UtcNow.AddDays(-2),
                CurrentChapter: 3,
                TotalChapters: 10,
                ProgressPercentage: 30,
                ChoicesMade: new List<OpenMKStoryChoice>(),
                CompletedChapters: new List<OpenMKChapterCompletion>());

            return Result.Success(campaign);
        }
        catch (Exception ex)
        {
            LogGetCampaignFailed(_logger, userId, ex);
            return Result.Failure<OpenMKStoryCampaign>($"Failed to get current campaign: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKStoryChapter>> AdvanceToNextChapterAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var campaign = await GetCurrentCampaignAsync(userId, ct);
            if (!campaign.IsSuccess)
            {
                return Result.Failure<OpenMKStoryChapter>("No active campaign found", ErrorType.NotFound);
            }

            var nextChapterNumber = campaign.Value.CurrentChapter + 1;
            var chapter = new OpenMKStoryChapter(
                Id: Guid.NewGuid(),
                Name: $"Chapter {nextChapterNumber}: The Next Challenge",
                Description: "Face a new opponent in the tournament.",
                ChapterNumber: nextChapterNumber,
                Type: OpenMKChapterType.Fight,
                OpponentCharacter: "Johnny Cage",
                Location: "Wu Shi Academy",
                AvailableChoices: new List<OpenMKStoryChoice>(),
                Objectives: new List<string> { "Defeat Johnny Cage", "Perform a fatality" });

            LogAdvancedToChapter(_logger, userId, nextChapterNumber);
            return Result.Success(chapter);
        }
        catch (Exception ex)
        {
            LogAdvanceChapterFailed(_logger, userId, ex);
            return Result.Failure<OpenMKStoryChapter>($"Failed to advance to next chapter: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<OpenMKStoryChapter>>> GetAvailableChaptersAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var chapters = new List<OpenMKStoryChapter>
            {
                new OpenMKStoryChapter(
                    Id: Guid.NewGuid(),
                    Name: "Chapter 1: The Beginning",
                    Description: "Your journey begins at the Shaolin Temple.",
                    ChapterNumber: 1,
                    Type: OpenMKChapterType.Fight,
                    OpponentCharacter: "Kung Lao",
                    Location: "Shaolin Temple",
                    AvailableChoices: new List<OpenMKStoryChoice>(),
                    Objectives: new List<string> { "Learn basic moves", "Defeat Kung Lao" }),

                new OpenMKStoryChapter(
                    Id: Guid.NewGuid(),
                    Name: "Chapter 2: City of Neon",
                    Description: "Travel to the modern world to face a Hollywood star.",
                    ChapterNumber: 2,
                    Type: OpenMKChapterType.Fight,
                    OpponentCharacter: "Johnny Cage",
                    Location: "Los Angeles",
                    AvailableChoices: new List<OpenMKStoryChoice>(),
                    Objectives: new List<string> { "Defeat Johnny Cage", "Discover his true motives" })
            };

            return Result.Success<IReadOnlyList<OpenMKStoryChapter>>(chapters);
        }
        catch (Exception ex)
        {
            LogGetChaptersFailed(_logger, userId, ex);
            return Result.Failure<IReadOnlyList<OpenMKStoryChapter>>($"Failed to get available chapters: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKStoryMatch>> StartChapterMatchAsync(Guid userId, Guid chapterId, CancellationToken ct = default)
    {
        try
        {
            var match = new OpenMKStoryMatch(
                Id: Guid.NewGuid(),
                ChapterId: chapterId,
                OpponentCharacter: "Johnny Cage",
                Location: "Wu Shi Academy",
                MatchType: OpenMKMatchType.Story,
                SpecialRules: new List<string> { "Fatality Required", "Time Limit: 99 seconds" });

            LogChapterMatchStarted(_logger, userId, chapterId, match.Id);
            return Result.Success(match);
        }
        catch (Exception ex)
        {
            LogStartChapterMatchFailed(_logger, userId, chapterId, ex);
            return Result.Failure<OpenMKStoryMatch>($"Failed to start chapter match: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKChapterCompletion>> CompleteChapterAsync(
        Guid userId,
        Guid chapterId,
        OpenMKMatchResult matchResult,
        CancellationToken ct = default)
    {
        try
        {
            var completion = new OpenMKChapterCompletion(
                ChapterId: chapterId,
                ChapterNumber: 2,
                Success: true,
                CompletionTime: TimeSpan.FromMinutes(5),
                Score: 1500,
                Rewards: new List<OpenMKChapterReward>
                {
                    new OpenMKChapterReward(OpenMKChapterRewardType.Experience, "Chapter completion bonus", 200, null),
                    new OpenMKChapterReward(OpenMKChapterRewardType.Koins, "Match victory bonus", 100, null)
                });

            LogChapterCompleted(_logger, userId, chapterId, completion.Score);
            return Result.Success(completion);
        }
        catch (Exception ex)
        {
            LogCompleteChapterFailed(_logger, userId, chapterId, ex);
            return Result.Failure<OpenMKChapterCompletion>($"Failed to complete chapter: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKStoryChoiceResult>> MakeStoryChoiceAsync(
        Guid userId,
        Guid choiceId,
        CancellationToken ct = default)
    {
        try
        {
            var result = new OpenMKStoryChoiceResult(
                ChoiceId: choiceId,
                Effects: new List<OpenMKChoiceEffect>
                {
                    new OpenMKChoiceEffect(OpenMKChoiceEffectType.RelationshipChange, "Johnny Cage", 10)
                },
                NarrativeResult: "Your choice has strengthened your alliance with Johnny Cage.",
                UnlockedChapters: new List<OpenMKStoryChapter>());

            LogStoryChoiceMade(_logger, userId, choiceId);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogMakeChoiceFailed(_logger, userId, choiceId, ex);
            return Result.Failure<OpenMKStoryChoiceResult>($"Failed to make story choice: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKCharacterEnding>> GetCharacterEndingAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var ending = new OpenMKCharacterEnding(
                Title: "Champion of Earthrealm",
                Description: "You have proven yourself as the ultimate warrior and savior of Earthrealm.",
                Type: OpenMKEndingType.Heroic,
                KeyChoices: new List<string> { "Helped Kung Lao", "Spared Johnny Cage", "Defeated Shao Kahn" },
                FullNarrative: "Liu Kang's journey reached its climax as he faced Shao Kahn in the final battle. " +
                              "With the power of his Shaolin training and the help of his allies, Liu Kang emerged victorious, " +
                              "restoring balance to the realms and earning his place as the eternal Champion of Earthrealm.");

            return Result.Success(ending);
        }
        catch (Exception ex)
        {
            LogGetEndingFailed(_logger, userId, ex);
            return Result.Failure<OpenMKCharacterEnding>($"Failed to get character ending: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> ResetCampaignAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            // In a real implementation, this would reset campaign progress in database
            LogCampaignReset(_logger, userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogResetCampaignFailed(_logger, userId, ex);
            return Result.Failure($"Failed to reset campaign: {ex.Message}", ErrorType.Internal);
        }
    }

    #region LoggerMessage Definitions

    [LoggerMessage(Level = LogLevel.Information, Message = "Started OpenMK campaign {CampaignId} for user {UserId} with character {CharacterId}")]
    private static partial void LogCampaignStarted(ILogger logger, Guid userId, Guid characterId, Guid campaignId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to start campaign for user {UserId} with character {CharacterId}")]
    private static partial void LogStartCampaignFailed(ILogger logger, Guid userId, Guid characterId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get current campaign for user {UserId}")]
    private static partial void LogGetCampaignFailed(ILogger logger, Guid userId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} advanced to chapter {ChapterNumber}")]
    private static partial void LogAdvancedToChapter(ILogger logger, Guid userId, int chapterNumber);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to advance chapter for user {UserId}")]
    private static partial void LogAdvanceChapterFailed(ILogger logger, Guid userId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get available chapters for user {UserId}")]
    private static partial void LogGetChaptersFailed(ILogger logger, Guid userId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Started chapter match {MatchId} for user {UserId} in chapter {ChapterId}")]
    private static partial void LogChapterMatchStarted(ILogger logger, Guid userId, Guid chapterId, Guid matchId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to start chapter match for user {UserId} in chapter {ChapterId}")]
    private static partial void LogStartChapterMatchFailed(ILogger logger, Guid userId, Guid chapterId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} completed chapter {ChapterId} with score {Score}")]
    private static partial void LogChapterCompleted(ILogger logger, Guid userId, Guid chapterId, int score);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to complete chapter {ChapterId} for user {UserId}")]
    private static partial void LogCompleteChapterFailed(ILogger logger, Guid userId, Guid chapterId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "User {UserId} made story choice {ChoiceId}")]
    private static partial void LogStoryChoiceMade(ILogger logger, Guid userId, Guid choiceId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to make story choice {ChoiceId} for user {UserId}")]
    private static partial void LogMakeChoiceFailed(ILogger logger, Guid userId, Guid choiceId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get character ending for user {UserId}")]
    private static partial void LogGetEndingFailed(ILogger logger, Guid userId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reset campaign progress for user {UserId}")]
    private static partial void LogCampaignReset(ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to reset campaign for user {UserId}")]
    private static partial void LogResetCampaignFailed(ILogger logger, Guid userId, Exception ex);

    #endregion
}