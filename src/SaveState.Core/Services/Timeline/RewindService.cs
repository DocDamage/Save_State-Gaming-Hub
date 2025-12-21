using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveState.Core.Services.Timeline
{
    /// <summary>
    /// Player decision rewind capability.
    /// - Restore to any delta point
    /// - Diff visualization
    /// </summary>
    public class RewindPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int Sequence { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsAutoSave { get; set; }
        public StateSnapshot? Snapshot { get; set; }
    }

    public class StateDiff
    {
        public string Key { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public object? CurrentValue { get; set; }
        public object? TargetValue { get; set; }
        public bool WillChange => !Equals(CurrentValue, TargetValue);
    }

    public class RewindPreview
    {
        public RewindPoint Target { get; set; } = null!;
        public List<StateDiff> Changes { get; set; } = new();
        public int StepsBack { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<string> LostProgress { get; set; } = new(); // Things that will be undone
    }

    public interface IRewindService
    {
        void CreateRewindPoint(string name, string? description = null, bool isAutoSave = false);
        IEnumerable<RewindPoint> GetRewindPoints();
        RewindPoint? GetRewindPoint(string pointId);
        RewindPreview PreviewRewind(string pointId);
        StateSnapshot Rewind(string pointId);
        StateSnapshot RewindToSequence(int sequence);
        void ConfigureAutoSave(TimeSpan interval, int maxPoints);
    }

    public class RewindService : IRewindService
    {
        private readonly IStateDeltaService _deltaService;
        private readonly List<RewindPoint> _rewindPoints = new();
        private TimeSpan _autoSaveInterval = TimeSpan.FromMinutes(5);
        private int _maxAutoSavePoints = 10;
        private DateTime _lastAutoSave = DateTime.MinValue;

        public RewindService(IStateDeltaService? deltaService = null)
        {
            _deltaService = deltaService ?? new StateDeltaService();
        }

        public void CreateRewindPoint(string name, string? description = null, bool isAutoSave = false)
        {
            var snapshot = _deltaService.CreateSnapshot();
            
            var point = new RewindPoint
            {
                Sequence = snapshot.DeltaSequence,
                Name = name,
                Description = description,
                Timestamp = DateTime.UtcNow,
                IsAutoSave = isAutoSave,
                Snapshot = snapshot
            };

            _rewindPoints.Add(point);

            // Prune old autosaves
            if (isAutoSave)
            {
                var autoSaves = _rewindPoints.Where(p => p.IsAutoSave).OrderByDescending(p => p.Timestamp).ToList();
                while (autoSaves.Count > _maxAutoSavePoints)
                {
                    var oldest = autoSaves.Last();
                    _rewindPoints.Remove(oldest);
                    autoSaves.Remove(oldest);
                }
            }
        }

        public IEnumerable<RewindPoint> GetRewindPoints()
        {
            return _rewindPoints.OrderByDescending(p => p.Timestamp);
        }

        public RewindPoint? GetRewindPoint(string pointId)
        {
            return _rewindPoints.FirstOrDefault(p => p.Id == pointId);
        }

        public RewindPreview PreviewRewind(string pointId)
        {
            var point = GetRewindPoint(pointId);
            if (point?.Snapshot == null)
            {
                return new RewindPreview
                {
                    Target = point ?? new RewindPoint(),
                    Summary = "Rewind point not found"
                };
            }

            var currentSnapshot = _deltaService.CreateSnapshot();
            var targetSnapshot = point.Snapshot;

            var changes = new List<StateDiff>();
            var lostProgress = new List<string>();

            // Compare flags
            var allFlags = currentSnapshot.Flags.Keys.Union(targetSnapshot.Flags.Keys);
            foreach (var key in allFlags)
            {
                var current = currentSnapshot.Flags.TryGetValue(key, out var c) ? c : (bool?)null;
                var target = targetSnapshot.Flags.TryGetValue(key, out var t) ? t : (bool?)null;
                
                if (current != target)
                {
                    changes.Add(new StateDiff
                    {
                        Key = key,
                        Category = "flag",
                        CurrentValue = current,
                        TargetValue = target
                    });

                    // Track lost progress (things becoming false/null)
                    if (current == true && (target != true))
                    {
                        lostProgress.Add($"Progress lost: {key}");
                    }
                }
            }

            // Compare counters
            var allCounters = currentSnapshot.Counters.Keys.Union(targetSnapshot.Counters.Keys);
            foreach (var key in allCounters)
            {
                var current = currentSnapshot.Counters.TryGetValue(key, out var c) ? c : 0;
                var target = targetSnapshot.Counters.TryGetValue(key, out var t) ? t : 0;
                
                if (current != target)
                {
                    changes.Add(new StateDiff
                    {
                        Key = key,
                        Category = "counter",
                        CurrentValue = current,
                        TargetValue = target
                    });

                    if (current > target)
                    {
                        lostProgress.Add($"{key} will decrease: {current} → {target}");
                    }
                }
            }

            var stepsBack = currentSnapshot.DeltaSequence - targetSnapshot.DeltaSequence;

            return new RewindPreview
            {
                Target = point,
                Changes = changes,
                StepsBack = stepsBack,
                LostProgress = lostProgress,
                Summary = $"Rewinding {stepsBack} steps will change {changes.Count} values"
            };
        }

        public StateSnapshot Rewind(string pointId)
        {
            var point = GetRewindPoint(pointId);
            if (point == null)
            {
                return _deltaService.CreateSnapshot();
            }

            return _deltaService.RevertToSequence(point.Sequence);
        }

        public StateSnapshot RewindToSequence(int sequence)
        {
            return _deltaService.RevertToSequence(sequence);
        }

        public void ConfigureAutoSave(TimeSpan interval, int maxPoints)
        {
            _autoSaveInterval = interval;
            _maxAutoSavePoints = maxPoints;
        }

        public void CheckAutoSave()
        {
            if (DateTime.UtcNow - _lastAutoSave >= _autoSaveInterval)
            {
                CreateRewindPoint($"Auto-Save {DateTime.UtcNow:HH:mm}", isAutoSave: true);
                _lastAutoSave = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Generate a visual diff for display
        /// </summary>
        public string GenerateDiffVisualization(RewindPreview preview)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Rewind Preview: {preview.Target.Name} ===");
            sb.AppendLine($"Steps back: {preview.StepsBack}");
            sb.AppendLine();

            if (preview.LostProgress.Count > 0)
            {
                sb.AppendLine("⚠️ LOST PROGRESS:");
                foreach (var lost in preview.LostProgress)
                {
                    sb.AppendLine($"  • {lost}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("CHANGES:");
            foreach (var change in preview.Changes.Take(20))
            {
                var arrow = change.WillChange ? "→" : "=";
                sb.AppendLine($"  [{change.Category}] {change.Key}: {change.CurrentValue} {arrow} {change.TargetValue}");
            }

            if (preview.Changes.Count > 20)
            {
                sb.AppendLine($"  ... and {preview.Changes.Count - 20} more changes");
            }

            return sb.ToString();
        }
    }
}
