using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Timeline
{
    /// <summary>
    /// Alternate timeline management.
    /// - Fork timelines at decision points
    /// - "What-if" simulation queries
    /// - AI can reference alternate outcomes
    /// </summary>
    public class Timeline
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string? ParentTimelineId { get; set; }
        public int ForkSequence { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }
        public StateSnapshot? CurrentState { get; set; }
    }

    public class WhatIfResult
    {
        public string Query { get; set; } = string.Empty;
        public Timeline HypotheticalTimeline { get; set; } = null!;
        public StateSnapshot ResultState { get; set; } = null!;
        public List<string> KeyDifferences { get; set; } = new();
        public string NarrativeSummary { get; set; } = string.Empty;
    }

    public interface ITimelineService
    {
        string CurrentTimelineId { get; }
        Timeline GetCurrentTimeline();
        Timeline ForkTimeline(string name, string? description = null);
        bool SwitchTimeline(string timelineId);
        IEnumerable<Timeline> GetAllTimelines();
        IEnumerable<Timeline> GetTimelineHistory();
        Task<WhatIfResult> SimulateWhatIf(string scenario, List<StateDelta> hypotheticalDeltas);
        List<string> CompareTimelines(string timelineId1, string timelineId2);
    }

    public class TimelineService : ITimelineService
    {
        private readonly Dictionary<string, Timeline> _timelines = new();
        private readonly IStateDeltaService _deltaService;
        private Timeline _currentTimeline;

        public string CurrentTimelineId => _currentTimeline.Id;

        public TimelineService(IStateDeltaService? deltaService = null)
        {
            _deltaService = deltaService ?? new StateDeltaService();
            
            // Initialize main timeline
            _currentTimeline = new Timeline
            {
                Id = "main",
                Name = "Main Timeline",
                Description = "The primary timeline of events"
            };
            _timelines[_currentTimeline.Id] = _currentTimeline;
        }

        public Timeline GetCurrentTimeline() => _currentTimeline;

        public Timeline ForkTimeline(string name, string? description = null)
        {
            // Mark current position as branch point
            _deltaService.MarkBranchPoint($"Fork: {name}");
            
            // Create new timeline
            var fork = new Timeline
            {
                Name = name,
                ParentTimelineId = _currentTimeline.Id,
                ForkSequence = _deltaService.CreateSnapshot().DeltaSequence,
                Description = description,
                CurrentState = _deltaService.CreateSnapshot()
            };

            _timelines[fork.Id] = fork;
            return fork;
        }

        public bool SwitchTimeline(string timelineId)
        {
            if (!_timelines.TryGetValue(timelineId, out var timeline))
                return false;

            // Save current state
            _currentTimeline.CurrentState = _deltaService.CreateSnapshot();
            
            // Switch to new timeline
            _currentTimeline = timeline;
            
            // Restore state from new timeline
            if (timeline.CurrentState != null)
            {
                _deltaService.RevertToSequence(timeline.CurrentState.DeltaSequence);
            }

            return true;
        }

        public IEnumerable<Timeline> GetAllTimelines() => _timelines.Values;

        public IEnumerable<Timeline> GetTimelineHistory()
        {
            var history = new List<Timeline>();
            var current = _currentTimeline;

            while (current != null)
            {
                history.Add(current);
                if (current.ParentTimelineId != null && _timelines.TryGetValue(current.ParentTimelineId, out var parent))
                {
                    current = parent;
                }
                else
                {
                    break;
                }
            }

            history.Reverse();
            return history;
        }

        public async Task<WhatIfResult> SimulateWhatIf(string scenario, List<StateDelta> hypotheticalDeltas)
        {
            // Create a hypothetical timeline without committing
            var hypothetical = new Timeline
            {
                Name = $"What-If: {scenario}",
                ParentTimelineId = _currentTimeline.Id,
                ForkSequence = _deltaService.CreateSnapshot().DeltaSequence,
                Description = scenario,
                IsActive = false
            };

            // Apply hypothetical deltas to a copy of current state
            var baseSnapshot = _deltaService.CreateSnapshot();
            var resultState = _deltaService.ApplyDeltas(baseSnapshot, hypotheticalDeltas);

            // Calculate differences
            var differences = new List<string>();
            
            foreach (var flag in resultState.Flags)
            {
                if (!baseSnapshot.Flags.TryGetValue(flag.Key, out var oldVal) || oldVal != flag.Value)
                {
                    differences.Add($"Flag '{flag.Key}': {oldVal} → {flag.Value}");
                }
            }

            foreach (var counter in resultState.Counters)
            {
                if (!baseSnapshot.Counters.TryGetValue(counter.Key, out var oldVal) || oldVal != counter.Value)
                {
                    differences.Add($"Counter '{counter.Key}': {oldVal} → {counter.Value}");
                }
            }

            return await Task.FromResult(new WhatIfResult
            {
                Query = scenario,
                HypotheticalTimeline = hypothetical,
                ResultState = resultState,
                KeyDifferences = differences,
                NarrativeSummary = GenerateNarrativeSummary(differences)
            });
        }

        public List<string> CompareTimelines(string timelineId1, string timelineId2)
        {
            var differences = new List<string>();

            if (!_timelines.TryGetValue(timelineId1, out var t1) || t1.CurrentState == null)
                return differences;
            if (!_timelines.TryGetValue(timelineId2, out var t2) || t2.CurrentState == null)
                return differences;

            var state1 = t1.CurrentState;
            var state2 = t2.CurrentState;

            // Compare flags
            var allFlags = state1.Flags.Keys.Union(state2.Flags.Keys);
            foreach (var key in allFlags)
            {
                var val1 = state1.Flags.TryGetValue(key, out var v1) ? v1 : (bool?)null;
                var val2 = state2.Flags.TryGetValue(key, out var v2) ? v2 : (bool?)null;
                if (val1 != val2)
                {
                    differences.Add($"[{t1.Name}] {key}={val1} vs [{t2.Name}] {key}={val2}");
                }
            }

            // Compare counters
            var allCounters = state1.Counters.Keys.Union(state2.Counters.Keys);
            foreach (var key in allCounters)
            {
                var val1 = state1.Counters.TryGetValue(key, out var v1) ? v1 : (int?)null;
                var val2 = state2.Counters.TryGetValue(key, out var v2) ? v2 : (int?)null;
                if (val1 != val2)
                {
                    differences.Add($"[{t1.Name}] {key}={val1} vs [{t2.Name}] {key}={val2}");
                }
            }

            return differences;
        }

        private string GenerateNarrativeSummary(List<string> differences)
        {
            if (differences.Count == 0)
                return "No significant changes would occur.";

            return $"This scenario would result in {differences.Count} changes: " +
                   string.Join(", ", differences.Take(3)) +
                   (differences.Count > 3 ? $" and {differences.Count - 3} more..." : "");
        }
    }
}
