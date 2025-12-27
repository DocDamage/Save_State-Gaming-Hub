using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Core.Services.GameState
{
    /// <summary>
    /// Central source of truth for world state.
    /// Tracks flags, counters, relations, and timelines.
    /// </summary>
    public class ActiveTimeline
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> State { get; set; } = new();
    }

    public class WorldState
    {
        public Dictionary<string, bool> Flags { get; set; } = new();      // NPC_ALIVE, QUEST_COMPLETE
        public Dictionary<string, int> Counters { get; set; } = new();    // CORRUPTION_LEVEL, GOLD
        public Dictionary<string, string> Relations { get; set; } = new(); // PLAYER_REP_FACTION_A
        public List<ActiveTimeline> Timelines { get; set; } = new();
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
        public string? CurrentLocation { get; set; }
        public string? CurrentQuest { get; set; }
    }

    public class StateChangeEvent
    {
        public string Key { get; set; } = string.Empty;
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Source { get; set; }
    }

    public interface IWorldStateService
    {
        WorldState CurrentState { get; }
        bool GetFlag(string key, bool defaultValue = false);
        void SetFlag(string key, bool value, string? source = null);
        int GetCounter(string key, int defaultValue = 0);
        void SetCounter(string key, int value, string? source = null);
        void IncrementCounter(string key, int amount = 1, string? source = null);
        string? GetRelation(string key);
        void SetRelation(string key, string value, string? source = null);
        IEnumerable<StateChangeEvent> GetRecentChanges(int count = 10);
        Task SaveAsync();
        Task LoadAsync();
        event EventHandler<StateChangeEvent>? StateChanged;
    }

    public class WorldStateService : IWorldStateService
    {
        private WorldState _state = new();
        private readonly List<StateChangeEvent> _changeHistory = new();
        private readonly string _storagePath;

        public WorldState CurrentState => _state;
        public event EventHandler<StateChangeEvent>? StateChanged;

        public WorldStateService(string? storagePath = null)
        {
            _storagePath = storagePath ?? System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SaveState", "WorldState", "state.json");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_storagePath)!);
        }

        public bool GetFlag(string key, bool defaultValue = false) =>
            _state.Flags.TryGetValue(key, out var value) ? value : defaultValue;

        public void SetFlag(string key, bool value, string? source = null)
        {
            var oldValue = _state.Flags.TryGetValue(key, out var old) ? old : (bool?)null;
            _state.Flags[key] = value;
            _state.LastModified = DateTime.UtcNow;
            RecordChange(key, oldValue, value, "flag", source);
        }

        public int GetCounter(string key, int defaultValue = 0) =>
            _state.Counters.TryGetValue(key, out var value) ? value : defaultValue;

        public void SetCounter(string key, int value, string? source = null)
        {
            var oldValue = _state.Counters.TryGetValue(key, out var old) ? old : (int?)null;
            _state.Counters[key] = value;
            _state.LastModified = DateTime.UtcNow;
            RecordChange(key, oldValue, value, "counter", source);
        }

        public void IncrementCounter(string key, int amount = 1, string? source = null)
        {
            var current = GetCounter(key);
            SetCounter(key, current + amount, source);
        }

        public string? GetRelation(string key) =>
            _state.Relations.TryGetValue(key, out var value) ? value : null;

        public void SetRelation(string key, string value, string? source = null)
        {
            var oldValue = _state.Relations.TryGetValue(key, out var old) ? old : null;
            _state.Relations[key] = value;
            _state.LastModified = DateTime.UtcNow;
            RecordChange(key, oldValue, value, "relation", source);
        }

        public IEnumerable<StateChangeEvent> GetRecentChanges(int count = 10) =>
            _changeHistory.OrderByDescending(c => c.Timestamp).Take(count);

        private void RecordChange(string key, object? oldValue, object? newValue, string type, string? source)
        {
            var change = new StateChangeEvent
            {
                Key = key,
                OldValue = oldValue,
                NewValue = newValue,
                ChangeType = type,
                Source = source
            };
            _changeHistory.Add(change);
            StateChanged?.Invoke(this, change);
        }

        public async Task SaveAsync()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_state);
            await System.IO.File.WriteAllTextAsync(_storagePath, json);
        }

        public async Task LoadAsync()
        {
            if (System.IO.File.Exists(_storagePath))
            {
                var json = await System.IO.File.ReadAllTextAsync(_storagePath);
                _state = System.Text.Json.JsonSerializer.Deserialize<WorldState>(json) ?? new WorldState();
            }
        }
    }
}
