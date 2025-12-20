namespace SaveState.Core.Models;

public class AiChatMessage
{
    public string Role { get; set; } = "user"; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class GameRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public string[] SimilarTo { get; set; } = Array.Empty<string>();
    public string Genre { get; set; } = string.Empty;
}

public class AiAnalysisResult
{
    public string GameTitle { get; set; } = string.Empty;
    public string GeneratedDescription { get; set; } = string.Empty;
    public string[] SuggestedGenres { get; set; } = Array.Empty<string>();
    public string[] SuggestedTags { get; set; } = Array.Empty<string>();
    public string AgeRating { get; set; } = string.Empty;
    public double SentimentScore { get; set; }
}
