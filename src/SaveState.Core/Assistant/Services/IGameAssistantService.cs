using SaveState.Core.Common;

namespace SaveState.Core.Assistant.Services;

public interface IGameAssistantService
{
    Task<Result<AssistantResponse>> AskAsync(
        Guid gameId,
        string question,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<string>>> GetQuickTipsAsync(
        Guid gameId,
        CancellationToken ct = default);

    Task<Result<string>> GetWalkthroughHintAsync(
        Guid gameId,
        string currentLocation,
        bool avoidSpoilers = true,
        CancellationToken ct = default);

    Task<Result> ClearContextAsync(Guid gameId, CancellationToken ct = default);
}

public sealed record AssistantResponse(
    string Answer,
    IReadOnlyList<string> Sources,
    bool ContainsSpoilers,
    IReadOnlyList<string> RelatedQuestions,
    float Confidence);