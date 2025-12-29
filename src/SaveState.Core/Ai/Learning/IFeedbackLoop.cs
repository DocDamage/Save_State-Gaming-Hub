namespace SaveState.Core.Ai.Learning;

public interface IFeedbackLoop
{
    Task SubmitFeedbackAsync(string messageId, FeedbackType type, string comment, CancellationToken ct = default);
    Task MaintainContextQualityAsync(CancellationToken ct = default);
}

public enum FeedbackType { Helpful, Inaccurate, Harmful, Hallucination }
