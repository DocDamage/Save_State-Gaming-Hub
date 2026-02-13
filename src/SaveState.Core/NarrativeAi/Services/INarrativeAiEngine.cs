using SaveState.Core.Common;
using SaveState.Core.NarrativeAi.Models;

namespace SaveState.Core.NarrativeAi.Services;

/// <summary>
/// Engine that provides AI-powered narrative generation including quests, dialogue, and story branching.
/// </summary>
public interface INarrativeAiEngine
{
    /// <summary>
    /// Generates a procedural quest based on game context and player preferences.
    /// </summary>
    /// <param name="request">The quest generation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the generated quest.</returns>
    Task<Result<GeneratedQuest>> GenerateQuestAsync(QuestGenerationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Generates multiple procedural quests.
    /// </summary>
    /// <param name="request">The quest generation request.</param>
    /// <param name="count">Number of quests to generate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the generated quests.</returns>
    Task<Result<IReadOnlyList<GeneratedQuest>>> GenerateQuestsAsync(QuestGenerationRequest request, int count, CancellationToken ct = default);

    /// <summary>
    /// Generates a dialogue node with player response options.
    /// </summary>
    /// <param name="request">The dialogue generation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the generated dialogue node.</returns>
    Task<Result<DialogueNode>> GenerateDialogueAsync(DialogueGenerationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Continues an existing dialogue from a player selection.
    /// </summary>
    /// <param name="currentNodeId">The current dialogue node ID.</param>
    /// <param name="selectedOptionId">The selected option ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the next dialogue node.</returns>
    Task<Result<DialogueNode>> ContinueDialogueAsync(string currentNodeId, string selectedOptionId, CancellationToken ct = default);

    /// <summary>
    /// Generates a new story branch based on player choices.
    /// </summary>
    /// <param name="parentBranchId">The parent branch ID.</param>
    /// <param name="playerChoice">The player choice that triggered the branch.</param>
    /// <param name="narrativeContext">Additional narrative context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the new story branch.</returns>
    Task<Result<StoryBranch>> GenerateStoryBranchAsync(string parentBranchId, string playerChoice, string narrativeContext, CancellationToken ct = default);

    /// <summary>
    /// Gets all available story branches.
    /// </summary>
    /// <param name="storyId">The story identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing all story branches.</returns>
    Task<Result<IReadOnlyList<StoryBranch>>> GetStoryBranchesAsync(string storyId, CancellationToken ct = default);

    /// <summary>
    /// Updates the narrative state based on game events.
    /// </summary>
    /// <param name="state">The current narrative state.</param>
    /// <param name="eventType">Type of event.</param>
    /// <param name="eventData">Event data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the updated narrative state.</returns>
    Task<Result<NarrativeState>> UpdateNarrativeStateAsync(NarrativeState state, string eventType, IReadOnlyDictionary<string, object> eventData, CancellationToken ct = default);

    /// <summary>
    /// Analyzes player choices and suggests story adaptations.
    /// </summary>
    /// <param name="storyId">The story identifier.</param>
    /// <param name="recentChoices">Recent player choices.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing suggested story adaptations.</returns>
    Task<Result<IReadOnlyList<string>>> AnalyzePlayerChoicesAsync(string storyId, IReadOnlyList<string> recentChoices, CancellationToken ct = default);

    /// <summary>
    /// Saves a dialogue tree for later use.
    /// </summary>
    /// <param name="rootNode">The root dialogue node.</param>
    /// <param name="conversationId">Unique conversation identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SaveDialogueTreeAsync(DialogueNode rootNode, string conversationId, CancellationToken ct = default);

    /// <summary>
    /// Loads a saved dialogue tree.
    /// </summary>
    /// <param name="conversationId">The conversation identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the root dialogue node.</returns>
    Task<Result<DialogueNode>> LoadDialogueTreeAsync(string conversationId, CancellationToken ct = default);

    /// <summary>
    /// Validates that a quest is completable given current game state.
    /// </summary>
    /// <param name="quest">The quest to validate.</param>
    /// <param name="gameState">Current game state data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing validation information.</returns>
    Task<Result<QuestValidationResult>> ValidateQuestAsync(GeneratedQuest quest, IReadOnlyDictionary<string, object> gameState, CancellationToken ct = default);

    /// <summary>
    /// Generates narrative summary of player journey so far.
    /// </summary>
    /// <param name="state">The narrative state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result containing the narrative summary.</returns>
    Task<Result<string>> GenerateNarrativeSummaryAsync(NarrativeState state, CancellationToken ct = default);
}

/// <summary>
/// Result of quest validation.
/// </summary>
public record QuestValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Suggestions { get; init; } = Array.Empty<string>();
}
