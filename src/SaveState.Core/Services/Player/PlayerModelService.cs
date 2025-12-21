using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;
using SaveState.Core.Services.Ai.Prompts;

namespace SaveState.Core.Services.Player
{
    /// <summary>
    /// Tracks player behavioral patterns.
    /// </summary>
    public class PlayerModel
    {
        public string PlayerId { get; set; } = string.Empty;
        public float AggressionScore { get; set; } = 0.5f;       // 0-1
        public float ExplorationTendency { get; set; } = 0.5f;   // 0-1
        public float HumorTolerance { get; set; } = 0.5f;        // 0-1
        public float MoralAlignment { get; set; } = 0f;          // -1 to 1
        public float ComplexityPreference { get; set; } = 0.5f;  // 0-1
        public float PacingPreference { get; set; } = 0.5f;      // 0=methodical, 1=rush
        public float SocialEngagement { get; set; } = 0.5f;      // 0-1
        public float RiskTaking { get; set; } = 0.5f;            // 0-1
        public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public int TotalActions { get; set; } = 0;
        public Dictionary<string, int> ActionCounts { get; set; } = new();
        public Dictionary<string, float> CustomTraits { get; set; } = new();
    }

    public interface IPlayerModelService
    {
        Task<PlayerModel> GetModel(string playerId);
        Task UpdateFromAction(string playerId, PlayerAction action);
        Task<PlayerProfile> GetProfile(string playerId);
        Task SaveAsync();
        Task LoadAsync();
    }

    public class PlayerModelService : IPlayerModelService
    {
        private readonly Dictionary<string, PlayerModel> _models = new();
        private readonly string _storagePath;
        private readonly float _learningRate = 0.1f;

        public PlayerModelService(string? storagePath = null)
        {
            _storagePath = storagePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SaveState", "Player", "models.json");
            Directory.CreateDirectory(Path.GetDirectoryName(_storagePath)!);
        }

        public async Task<PlayerModel> GetModel(string playerId)
        {
            if (!_models.ContainsKey(playerId))
            {
                _models[playerId] = new PlayerModel { PlayerId = playerId };
            }
            return await Task.FromResult(_models[playerId]);
        }

        public async Task UpdateFromAction(string playerId, PlayerAction action)
        {
            var model = await GetModel(playerId);
            
            // Update action counts
            var actionType = action.ActionType.ToLowerInvariant();
            model.ActionCounts.TryGetValue(actionType, out var count);
            model.ActionCounts[actionType] = count + 1;
            model.TotalActions++;

            // Update traits based on action
            switch (action.Category)
            {
                case ActionCategory.Combat:
                    model.AggressionScore = Lerp(model.AggressionScore, 1.0f, _learningRate);
                    if (action.Metadata.TryGetValue("was_first_strike", out var fs) && (bool)fs)
                        model.RiskTaking = Lerp(model.RiskTaking, 0.8f, _learningRate);
                    break;

                case ActionCategory.Dialogue:
                    model.SocialEngagement = Lerp(model.SocialEngagement, 1.0f, _learningRate);
                    if (action.Metadata.TryGetValue("chose_peaceful", out var cp) && (bool)cp)
                        model.AggressionScore = Lerp(model.AggressionScore, 0.0f, _learningRate);
                    break;

                case ActionCategory.Exploration:
                    model.ExplorationTendency = Lerp(model.ExplorationTendency, 1.0f, _learningRate);
                    if (action.Metadata.TryGetValue("found_secret", out var _))
                        model.ComplexityPreference = Lerp(model.ComplexityPreference, 0.8f, _learningRate);
                    break;

                case ActionCategory.Quest:
                    // Speed of completion affects pacing preference
                    if (action.Metadata.TryGetValue("completion_time_ratio", out var ratio))
                    {
                        var r = Convert.ToSingle(ratio);
                        model.PacingPreference = Lerp(model.PacingPreference, r > 1.0f ? 0.3f : 0.7f, _learningRate);
                    }
                    break;

                case ActionCategory.MoralChoice:
                    if (action.Metadata.TryGetValue("choice_alignment", out var alignment))
                    {
                        var alignVal = Convert.ToSingle(alignment);
                        model.MoralAlignment = Lerp(model.MoralAlignment, alignVal, _learningRate);
                    }
                    break;
            }

            // Humor detection from dialogue choices
            if (action.Metadata.TryGetValue("chose_humor", out var humor) && (bool)humor)
            {
                model.HumorTolerance = Lerp(model.HumorTolerance, 1.0f, _learningRate);
            }

            model.LastUpdated = DateTime.UtcNow;
        }

        public async Task<PlayerProfile> GetProfile(string playerId)
        {
            var model = await GetModel(playerId);
            return new PlayerProfile
            {
                PlayerId = model.PlayerId,
                AggressionScore = model.AggressionScore,
                ExplorationTendency = model.ExplorationTendency,
                HumorTolerance = model.HumorTolerance,
                MoralAlignment = model.MoralAlignment,
                ComplexityPreference = model.ComplexityPreference,
                PacingPreference = model.PacingPreference,
                Preferences = new Dictionary<string, float>(model.CustomTraits)
            };
        }

        private float Lerp(float current, float target, float rate)
        {
            return current + (target - current) * rate;
        }

        public async Task SaveAsync()
        {
            var json = JsonSerializer.Serialize(_models, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_storagePath, json);
        }

        public async Task LoadAsync()
        {
            if (!File.Exists(_storagePath)) return;

            var json = await File.ReadAllTextAsync(_storagePath);
            var models = JsonSerializer.Deserialize<Dictionary<string, PlayerModel>>(json);
            if (models != null)
            {
                _models.Clear();
                foreach (var (key, value) in models)
                {
                    _models[key] = value;
                }
            }
        }
    }
}
