namespace SaveState.Application.Mugen.Models.ContentMarketplace;

/// <summary>
/// Content review with detailed information.
/// </summary>
public class ContentReview
{
    public string ReviewId { get; set; } = string.Empty;
    public string ContentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public int HelpfulVotes { get; set; }
}

/// <summary>
/// Content rating summary.
/// </summary>
public class ContentRating
{
    public string ContentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public DateTime RatedAt { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
}
