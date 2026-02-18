using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.NarrativeMemory;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.NarrativeMemory.Engines;

/// <summary>
/// Engine for managing alternate timelines.
/// </summary>
public class TimelineEngine
{
    private readonly ILogger<TimelineEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, AlternateTimeline> _timelines;
    private readonly ConcurrentDictionary<string, TimelineReplay> _replays;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimelineEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    public TimelineEngine(ILogger<TimelineEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _timelines = new ConcurrentDictionary<string, AlternateTimeline>();
        _replays = new ConcurrentDictionary<string, TimelineReplay>();
    }

    /// <summary>
    /// Creates an alternate timeline by forking from a branch point.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="request">The timeline fork request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created alternate timeline.</returns>
    public Task<AlternateTimeline> CreateAlternateTimelineAsync(
        string userId,
        TimelineForkRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating alternate timeline for user {UserId} at branch point {BranchPoint}",
            userId,
            request.BranchPoint);

        var timelineId = Guid.NewGuid().ToString();
        var now = _timeProvider.UtcNow;

        // Generate alternate events based on the desired outcome
        var alternateEvents = GenerateAlternateEvents(request);

        // Calculate probability and stability
        var probability = CalculateProbability(request);
        var stability = CalculateStability(request, probability);

        var timeline = new AlternateTimeline
        {
            TimelineId = timelineId,
            CreatorId = userId,
            SourceCrystalId = request.PlayerId, // Using PlayerId as source reference
            BranchPoint = request.BranchPoint,
            AlternateEvents = alternateEvents,
            Probability = probability,
            Stability = stability,
            CreatedAt = now,
            Explored = false
        };

        _timelines[timelineId] = timeline;

        _logger.LogInformation(
            "Created alternate timeline {TimelineId} for user {UserId} with stability {Stability:P}",
            timelineId,
            userId,
            stability);

        return Task.FromResult(timeline);
    }

    /// <summary>
    /// Replays a timeline to show what would have happened.
    /// </summary>
    /// <param name="timelineId">The timeline ID.</param>
    /// <param name="options">The replay options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The timeline replay result.</returns>
    public Task<TimelineReplay> ReplayTimelineAsync(
        string timelineId,
        ReplayOptions options,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Replaying timeline {TimelineId} for user {UserId} with commentary: {IncludeCommentary}",
            timelineId,
            options.PlayerId,
            options.IncludeCommentary);

        if (!_timelines.TryGetValue(timelineId, out var timeline))
        {
            _logger.LogWarning("Timeline {TimelineId} not found for replay", timelineId);
            throw new KeyNotFoundException($"Timeline {timelineId} not found");
        }

        var replayId = Guid.NewGuid().ToString();
        var now = _timeProvider.UtcNow;

        // Calculate replay duration based on playback speed
        var baseDuration = TimeSpan.FromMinutes(5);
        var duration = TimeSpan.FromTicks((long)(baseDuration.Ticks / options.PlaybackSpeed));

        // Generate key differences
        var keyDifferences = GenerateKeyDifferences(timeline);

        // Generate insights
        var insights = GenerateInsights(timeline, options);

        // Determine outcome based on timeline
        var outcome = DetermineOutcome(timeline);

        var replay = new TimelineReplay
        {
            ReplayId = replayId,
            TimelineId = timelineId,
            Duration = duration,
            KeyDifferences = keyDifferences,
            Outcome = outcome,
            Insights = insights,
            ReplayedAt = now
        };

        _replays[replayId] = replay;

        // Mark timeline as explored
        timeline.Explored = true;
        _timelines[timelineId] = timeline;

        _logger.LogInformation(
            "Completed replay {ReplayId} for timeline {TimelineId} with outcome: {Outcome}",
            replayId,
            timelineId,
            outcome);

        return Task.FromResult(replay);
    }

    /// <summary>
    /// Gets a timeline by ID.
    /// </summary>
    /// <param name="timelineId">The timeline ID.</param>
    /// <returns>The timeline if found; otherwise, null.</returns>
    public AlternateTimeline? GetTimeline(string timelineId)
    {
        _timelines.TryGetValue(timelineId, out var timeline);
        return timeline;
    }

    /// <summary>
    /// Gets all timelines for a creator.
    /// </summary>
    /// <param name="creatorId">The creator ID.</param>
    /// <returns>The collection of timelines.</returns>
    public IEnumerable<AlternateTimeline> GetCreatorTimelines(string creatorId)
    {
        return _timelines.Values.Where(t => t.CreatorId == creatorId);
    }

    /// <summary>
    /// Gets a replay by ID.
    /// </summary>
    /// <param name="replayId">The replay ID.</param>
    /// <returns>The replay if found; otherwise, null.</returns>
    public TimelineReplay? GetReplay(string replayId)
    {
        _replays.TryGetValue(replayId, out var replay);
        return replay;
    }

    /// <summary>
    /// Removes a timeline and all its replays.
    /// </summary>
    /// <param name="timelineId">The timeline ID.</param>
    /// <returns>True if removed; otherwise, false.</returns>
    public bool RemoveTimeline(string timelineId)
    {
        // Remove associated replays
        var replaysToRemove = _replays.Values.Where(r => r.TimelineId == timelineId).ToList();
        foreach (var replay in replaysToRemove)
        {
            _replays.TryRemove(replay.ReplayId, out _);
        }

        return _timelines.TryRemove(timelineId, out _);
    }

    private static List<string> GenerateAlternateEvents(TimelineForkRequest request)
    {
        var events = new List<string>
        {
            $"Timeline forked at: {request.BranchPoint}",
            $"Desired outcome: {request.DesiredOutcome}",
            $"Initial probability: {request.Probability:P}"
        };

        // Add contextual events based on desired outcome
        if (request.DesiredOutcome.Contains("Victory", StringComparison.OrdinalIgnoreCase))
        {
            events.Add("Critical hit lands instead of missing");
            events.Add("Opponent's combo is interrupted");
            events.Add("Health regenerates at crucial moment");
        }
        else if (request.DesiredOutcome.Contains("Defeat", StringComparison.OrdinalIgnoreCase))
        {
            events.Add("Dodge fails at critical moment");
            events.Add("Opponent executes perfect combo");
            events.Add("Special move cooldown extended");
        }

        return events;
    }

    private static float CalculateProbability(TimelineForkRequest request)
    {
        // Base probability from request
        var baseProbability = request.Probability;

        // Adjust based on desired outcome complexity
        var complexityFactor = request.DesiredOutcome.Length > 20 ? 0.9f : 1.0f;

        return Math.Min(1.0f, baseProbability * complexityFactor);
    }

    private static float CalculateStability(TimelineForkRequest request, float probability)
    {
        // Stability is inversely related to how far from the original timeline
        var baseStability = 1.0f - Math.Abs(0.5f - probability);

        // Longer desired outcomes are less stable
        var lengthFactor = 1.0f - (request.DesiredOutcome.Length / 100.0f);

        return Math.Max(0.1f, baseStability * lengthFactor);
    }

    private static List<string> GenerateKeyDifferences(AlternateTimeline timeline)
    {
        var differences = new List<string>
        {
            $"Branch point: {timeline.BranchPoint}",
            $"Probability difference: {Math.Abs(timeline.Probability - 0.5f):P}",
            $"Stability rating: {timeline.Stability:P}"
        };

        // Add differences based on alternate events
        foreach (var evt in timeline.AlternateEvents.Take(3))
        {
            differences.Add($"Event: {evt}");
        }

        return differences;
    }

    private static List<string> GenerateInsights(AlternateTimeline timeline, ReplayOptions options)
    {
        var insights = new List<string>
        {
            $"This timeline has {timeline.Stability:P} stability",
            $"The probability of this outcome was {timeline.Probability:P}"
        };

        if (options.IncludeCommentary)
        {
            if (timeline.Stability < 0.3f)
            {
                insights.Add("Warning: This is a highly unstable timeline with many variables");
            }
            else if (timeline.Stability > 0.8f)
            {
                insights.Add("This timeline represents a highly probable alternate outcome");
            }

            if (timeline.Probability > 0.7f)
            {
                insights.Add("The desired outcome was more likely than the actual outcome");
            }
            else
            {
                insights.Add("The desired outcome would have required significant changes to events");
            }
        }

        return insights;
    }

    private static string DetermineOutcome(AlternateTimeline timeline)
    {
        // Analyze the alternate events to determine the likely outcome
        var events = string.Join(" ", timeline.AlternateEvents);

        if (events.Contains("Victory", StringComparison.OrdinalIgnoreCase) ||
            timeline.Probability > 0.6f)
        {
            return "Victory in alternate timeline";
        }

        if (events.Contains("Defeat", StringComparison.OrdinalIgnoreCase) ||
            timeline.Probability < 0.4f)
        {
            return "Defeat in alternate timeline";
        }

        return "Uncertain outcome";
    }
}
