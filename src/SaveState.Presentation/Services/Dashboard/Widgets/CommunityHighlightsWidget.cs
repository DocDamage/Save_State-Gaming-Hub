using MediatR;
using SaveState.Application.Social.Queries;
using SaveState.Core.Social.Entities;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Dashboard widget displaying community challenges and leaderboards.
/// </summary>
public class CommunityHighlightsWidget : WidgetBase
{
    private readonly IMediator _mediator;
    private Challenge? _topChallenge;
    private LeaderboardRanking? _topLeader;

    public CommunityHighlightsWidget(IMediator mediator, Microsoft.Extensions.Logging.ILogger<CommunityHighlightsWidget> logger)
        : base(logger)
    {
        _mediator = mediator;
    }

    public override string Id => "community-highlights";
    public override string Title => "Community Highlights";
    public override string Icon => "👥";
    public override WidgetSize DefaultSize => WidgetSize.Medium;

    public Challenge? TopChallenge
    {
        get => _topChallenge;
        private set => SetProperty(ref _topChallenge, value);
    }

    public LeaderboardRanking? TopLeader
    {
        get => _topLeader;
        private set => SetProperty(ref _topLeader, value);
    }

    public override async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            // Get latest active challenge
            var challengesResult = await _mediator.Send(new GetActiveChallengesQuery());
            if (challengesResult.IsSuccess && challengesResult.Value != null && challengesResult.Value.Any())
            {
                TopChallenge = challengesResult.Value.First();
            }

            // Get top player from global leaderboard
            var leaderboardResult = await _mediator.Send(new GetLeaderboardByCategoryQuery(LeaderboardCategory.Global));
            if (leaderboardResult.IsSuccess && leaderboardResult.Value != null && leaderboardResult.Value.Entries.Any())
            {
                TopLeader = leaderboardResult.Value.Entries.OrderBy(e => e.Rank).First();
            }
        }
        catch (Exception)
        {
            // Silently fail for dashboard widgets
        }
        finally
        {
            IsLoading = false;
        }
    }
}
