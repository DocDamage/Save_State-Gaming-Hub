using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SaveState.Core.Services.Timeline
{
    /// <summary>
    /// Tracks incremental state changes.
    /// - Delta compression
    /// - Branch point marking
    /// - Timeline divergence tracking
    /// </summary>
    public class StateDelta
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TimelineId { get; set; } = string.Empty;
        public int SequenceNumber { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, DeltaChange> Changes { get; set; } = new();
        public string? TriggerEvent { get; set; }
        public bool IsBranchPoint { get; set; }
        public string? Description { get; set; }
    }

    public class DeltaChange
    {
        public string Key { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // flag, counter, relation
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
    }

    public class StateSnapshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string TimelineId { get; set; } = string.Empty;
        public int DeltaSequence { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, bool> Flags { get; set; } = new();
        public Dictionary<string, int> Counters { get; set; } = new();
        public Dictionary<string, string> Relations { get; set; } = new();
    }

    public interface IStateDeltaService
    {
        StateDelta RecordDelta(Dictionary<string, DeltaChange> changes, string? triggerEvent = null, bool isBranchPoint = false);
        IEnumerable<StateDelta> GetDeltas(int fromSequence = 0, int? toSequence = null);
        StateDelta? GetDelta(string deltaId);
        StateSnapshot CreateSnapshot();
        StateSnapshot ApplyDeltas(StateSnapshot baseSnapshot, IEnumerable<StateDelta> deltas);
        StateSnapshot RevertToSequence(int sequenceNumber);
        IEnumerable<StateDelta> GetBranchPoints();
        void MarkBranchPoint(string description);
    }

    public class StateDeltaService : IStateDeltaService
    {
        private readonly List<StateDelta> _deltas = new();
        private readonly Dictionary<string, StateDelta> _deltaIndex = new();
        private StateSnapshot _currentSnapshot = new();
        private string _currentTimelineId = "main";
        private int _sequenceCounter = 0;
        private readonly string _storagePath;

        public StateDeltaService(string? storagePath = null)
        {
            _storagePath = storagePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SaveState", "Timeline", "deltas.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
        }

        public StateDelta RecordDelta(Dictionary<string, DeltaChange> changes, string? triggerEvent = null, bool isBranchPoint = false)
        {
            var delta = new StateDelta
            {
                TimelineId = _currentTimelineId,
                SequenceNumber = ++_sequenceCounter,
                Changes = changes,
                TriggerEvent = triggerEvent,
                IsBranchPoint = isBranchPoint
            };

            _deltas.Add(delta);
            _deltaIndex[delta.Id] = delta;

            // Apply delta to current snapshot
            ApplyDeltaToSnapshot(_currentSnapshot, delta);

            return delta;
        }

        public IEnumerable<StateDelta> GetDeltas(int fromSequence = 0, int? toSequence = null)
        {
            return _deltas
                .Where(d => d.SequenceNumber >= fromSequence)
                .Where(d => !toSequence.HasValue || d.SequenceNumber <= toSequence.Value)
                .OrderBy(d => d.SequenceNumber);
        }

        public StateDelta? GetDelta(string deltaId)
        {
            return _deltaIndex.TryGetValue(deltaId, out var delta) ? delta : null;
        }

        public StateSnapshot CreateSnapshot()
        {
            return new StateSnapshot
            {
                TimelineId = _currentTimelineId,
                DeltaSequence = _sequenceCounter,
                Flags = new Dictionary<string, bool>(_currentSnapshot.Flags),
                Counters = new Dictionary<string, int>(_currentSnapshot.Counters),
                Relations = new Dictionary<string, string>(_currentSnapshot.Relations)
            };
        }

        public StateSnapshot ApplyDeltas(StateSnapshot baseSnapshot, IEnumerable<StateDelta> deltas)
        {
            var result = new StateSnapshot
            {
                TimelineId = baseSnapshot.TimelineId,
                DeltaSequence = baseSnapshot.DeltaSequence,
                Flags = new Dictionary<string, bool>(baseSnapshot.Flags),
                Counters = new Dictionary<string, int>(baseSnapshot.Counters),
                Relations = new Dictionary<string, string>(baseSnapshot.Relations)
            };

            foreach (var delta in deltas.OrderBy(d => d.SequenceNumber))
            {
                ApplyDeltaToSnapshot(result, delta);
            }

            return result;
        }

        public StateSnapshot RevertToSequence(int sequenceNumber)
        {
            // Start from empty and apply deltas up to sequence
            _currentSnapshot = new StateSnapshot { TimelineId = _currentTimelineId };
            
            foreach (var delta in GetDeltas(0, sequenceNumber))
            {
                ApplyDeltaToSnapshot(_currentSnapshot, delta);
            }

            _currentSnapshot.DeltaSequence = sequenceNumber;
            return _currentSnapshot;
        }

        public IEnumerable<StateDelta> GetBranchPoints()
        {
            return _deltas.Where(d => d.IsBranchPoint).OrderBy(d => d.SequenceNumber);
        }

        public void MarkBranchPoint(string description)
        {
            if (_deltas.Count > 0)
            {
                var last = _deltas.Last();
                last.IsBranchPoint = true;
                last.Description = description;
            }
        }

        private void ApplyDeltaToSnapshot(StateSnapshot snapshot, StateDelta delta)
        {
            foreach (var change in delta.Changes.Values)
            {
                switch (change.Type.ToLowerInvariant())
                {
                    case "flag":
                        if (change.NewValue is bool boolVal)
                            snapshot.Flags[change.Key] = boolVal;
                        else if (change.NewValue != null)
                            snapshot.Flags[change.Key] = Convert.ToBoolean(change.NewValue);
                        break;
                    case "counter":
                        if (change.NewValue is int intVal)
                            snapshot.Counters[change.Key] = intVal;
                        else if (change.NewValue != null)
                            snapshot.Counters[change.Key] = Convert.ToInt32(change.NewValue);
                        break;
                    case "relation":
                        snapshot.Relations[change.Key] = change.NewValue?.ToString() ?? "";
                        break;
                }
            }
            snapshot.DeltaSequence = delta.SequenceNumber;
        }

        public async Task SaveAsync()
        {
            var data = new { Deltas = _deltas, CurrentTimeline = _currentTimelineId, Sequence = _sequenceCounter };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_storagePath, json);
        }

        public async Task LoadAsync()
        {
            if (!File.Exists(_storagePath)) return;

            var json = await File.ReadAllTextAsync(_storagePath);
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("Deltas", out var deltasEl))
            {
                var deltas = JsonSerializer.Deserialize<List<StateDelta>>(deltasEl.GetRawText());
                if (deltas != null)
                {
                    _deltas.Clear();
                    _deltaIndex.Clear();
                    foreach (var delta in deltas)
                    {
                        _deltas.Add(delta);
                        _deltaIndex[delta.Id] = delta;
                    }
                }
            }

            if (doc.RootElement.TryGetProperty("CurrentTimeline", out var timeline))
                _currentTimelineId = timeline.GetString() ?? "main";

            if (doc.RootElement.TryGetProperty("Sequence", out var seq))
                _sequenceCounter = seq.GetInt32();

            // Rebuild current snapshot
            _currentSnapshot = new StateSnapshot { TimelineId = _currentTimelineId };
            foreach (var delta in _deltas)
            {
                ApplyDeltaToSnapshot(_currentSnapshot, delta);
            }
        }
    }
}
