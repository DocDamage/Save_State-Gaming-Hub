namespace SaveState.Application.Mugen.Models.ContentMarketplace;

/// <summary>
/// Review model for content ratings.
/// </summary>
public class Review
{
    public string ReviewId { get; set; } = default!;
    public string ContentId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public int Rating { get; set; } = default!;
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime? UpdatedAt { get; set; }
    public bool IsVerifiedPurchase { get; set; } = default!;
    public int HelpfulVotes { get; set; } = default!;
}

/// <summary>
/// Rating information.
/// </summary>
public class Rating
{
    public string ContentId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public int Score { get; set; } = default!;
    public DateTime RatedAt { get; set; } = default!;
}

/// <summary>
/// Review summary statistics.
/// </summary>
public class ReviewSummary
{
    public string ContentId { get; set; } = default!;
    public float AverageRating { get; set; } = default!;
    public int TotalReviews { get; set; } = default!;
    public int FiveStarCount { get; set; } = default!;
    public int FourStarCount { get; set; } = default!;
    public int ThreeStarCount { get; set; } = default!;
    public int TwoStarCount { get; set; } = default!;
    public int OneStarCount { get; set; } = default!;
    public IReadOnlyDictionary<int, int> Distribution => new Dictionary<int, int>
    {
        [5] = FiveStarCount,
        [4] = FourStarCount,
        [3] = ThreeStarCount,
        [2] = TwoStarCount,
        [1] = OneStarCount
    };
}

/// <summary>
/// Review submission request.
/// </summary>
public class ReviewRequest
{
    public string ContentId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public int Rating { get; set; } = default!;
    public string? Comment { get; set; }
}
