using System;
using System.Collections.Generic;
using System.Linq;

namespace SaveState.Core.Services.Ai.Models
{
    /// <summary>
    /// Model catalog with capabilities.
    /// </summary>
    public class ModelProfile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string[] Specialties { get; set; } = Array.Empty<string>(); // "narrative", "code", "analysis"
        public ModelConfig Config { get; set; } = new();
        public ModelCapabilities Capabilities { get; set; } = new();
        public bool IsAvailable { get; set; } = true;
        public int Priority { get; set; } = 0;
    }

    public class ModelConfig
    {
        public float Temperature { get; set; } = 0.7f;
        public int MaxTokens { get; set; } = 500;
        public float TopP { get; set; } = 0.9f;
        public float FrequencyPenalty { get; set; } = 0f;
        public float PresencePenalty { get; set; } = 0f;
        public string[]? StopSequences { get; set; }
    }

    public class ModelCapabilities
    {
        public bool SupportsStreaming { get; set; } = true;
        public bool SupportsVision { get; set; } = false;
        public bool SupportsTools { get; set; } = false;
        public int ContextWindow { get; set; } = 4096;
        public float CostPer1KTokens { get; set; } = 0f;
        public string SpeedTier { get; set; } = "standard"; // fast, standard, slow
    }

    public interface IModelRegistry
    {
        void RegisterModel(ModelProfile profile);
        void UnregisterModel(string modelId);
        ModelProfile? GetModel(string modelId);
        ModelProfile? GetBestModelForTask(string taskType);
        IEnumerable<ModelProfile> GetModelsBySpecialty(string specialty);
        IEnumerable<ModelProfile> GetAllModels();
        void UpdateAvailability(string modelId, bool isAvailable);
    }

    public class ModelRegistry : IModelRegistry
    {
        private readonly Dictionary<string, ModelProfile> _models = new();

        public ModelRegistry()
        {
            RegisterDefaultModels();
        }

        private void RegisterDefaultModels()
        {
            // Narrative-focused model
            RegisterModel(new ModelProfile
            {
                Id = "narrative_creative",
                Name = "Creative Narrative",
                Provider = "local",
                Specialties = new[] { "narrative", "storytelling", "dialogue", "creative" },
                Config = new ModelConfig
                {
                    Temperature = 0.85f,
                    MaxTokens = 800,
                    TopP = 0.95f
                },
                Capabilities = new ModelCapabilities
                {
                    ContextWindow = 8192,
                    SpeedTier = "standard"
                },
                Priority = 10
            });

            // Fast response model
            RegisterModel(new ModelProfile
            {
                Id = "fast_response",
                Name = "Fast Response",
                Provider = "local",
                Specialties = new[] { "combat", "quick", "action" },
                Config = new ModelConfig
                {
                    Temperature = 0.5f,
                    MaxTokens = 300,
                    TopP = 0.8f
                },
                Capabilities = new ModelCapabilities
                {
                    ContextWindow = 4096,
                    SpeedTier = "fast"
                },
                Priority = 8
            });

            // Precise knowledge model
            RegisterModel(new ModelProfile
            {
                Id = "knowledge_precise",
                Name = "Precise Knowledge",
                Provider = "local",
                Specialties = new[] { "lore", "analysis", "system", "facts" },
                Config = new ModelConfig
                {
                    Temperature = 0.3f,
                    MaxTokens = 600,
                    TopP = 0.7f
                },
                Capabilities = new ModelCapabilities
                {
                    ContextWindow = 8192,
                    SpeedTier = "standard"
                },
                Priority = 9
            });

            // Code generation model
            RegisterModel(new ModelProfile
            {
                Id = "code_gen",
                Name = "Code Generator",
                Provider = "local",
                Specialties = new[] { "code", "technical", "structured" },
                Config = new ModelConfig
                {
                    Temperature = 0.2f,
                    MaxTokens = 1000,
                    TopP = 0.8f
                },
                Capabilities = new ModelCapabilities
                {
                    ContextWindow = 16384,
                    SpeedTier = "slow",
                    SupportsTools = true
                },
                Priority = 7
            });

            // Balanced general model
            RegisterModel(new ModelProfile
            {
                Id = "general_balanced",
                Name = "General Balanced",
                Provider = "local",
                Specialties = new[] { "general", "balanced", "default" },
                Config = new ModelConfig
                {
                    Temperature = 0.7f,
                    MaxTokens = 500
                },
                Capabilities = new ModelCapabilities
                {
                    ContextWindow = 4096,
                    SpeedTier = "standard"
                },
                Priority = 5
            });
        }

        public void RegisterModel(ModelProfile profile)
        {
            _models[profile.Id] = profile;
        }

        public void UnregisterModel(string modelId)
        {
            _models.Remove(modelId);
        }

        public ModelProfile? GetModel(string modelId)
        {
            return _models.TryGetValue(modelId, out var model) ? model : null;
        }

        public ModelProfile? GetBestModelForTask(string taskType)
        {
            var taskLower = taskType.ToLowerInvariant();
            
            // Map task types to specialties
            var specialty = taskLower switch
            {
                "narrative" or "story" or "dialogue" => "narrative",
                "combat" or "action" or "battle" => "combat",
                "lore" or "history" or "knowledge" => "lore",
                "code" or "technical" or "script" => "code",
                "quick" or "fast" or "rapid" => "quick",
                _ => "general"
            };

            var candidates = _models.Values
                .Where(m => m.IsAvailable)
                .Where(m => m.Specialties.Any(s => s.Contains(specialty) || specialty.Contains(s)))
                .OrderByDescending(m => m.Priority)
                .ToList();

            return candidates.FirstOrDefault() ?? _models.Values
                .FirstOrDefault(m => m.IsAvailable && m.Specialties.Contains("general"));
        }

        public IEnumerable<ModelProfile> GetModelsBySpecialty(string specialty)
        {
            var specialtyLower = specialty.ToLowerInvariant();
            return _models.Values
                .Where(m => m.Specialties.Any(s => s.Contains(specialtyLower)))
                .OrderByDescending(m => m.Priority);
        }

        public IEnumerable<ModelProfile> GetAllModels()
        {
            return _models.Values.OrderByDescending(m => m.Priority);
        }

        public void UpdateAvailability(string modelId, bool isAvailable)
        {
            if (_models.TryGetValue(modelId, out var model))
            {
                model.IsAvailable = isAvailable;
            }
        }
    }
}
