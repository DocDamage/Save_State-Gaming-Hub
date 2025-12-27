using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using SaveState.Core.Services.Ai;

namespace SaveState.Core.Services.EmulatorEnhancements
{
    public class TimeCapsule
    {
        public string Id { get; set; } = string.Empty;
        public string GameId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatorName { get; set; } = string.Empty;
        public byte[] SaveStateData { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAt { get; set; }
        public DateTime UnlockAt { get; set; }
        public bool IsUnlocked { get; set; }
        public string? ChallengeType { get; set; } // "speedrun", "no-damage", "collectibles", null
        public int? ChallengeTarget { get; set; }
        public List<CapsuleReaction> Reactions { get; set; } = new();
        public List<CapsuleComment> Comments { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string? LlmTeaser { get; set; }  // AI-generated teaser text
    }

    public class CapsuleReaction
    {
        public string UserId { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class CapsuleComment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AuthorName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? ParentId { get; set; }  // For nested replies
        public byte[]? AttachedSaveState { get; set; }  // Reply with save state
    }

    public class TimeCapsuleService
    {
        private readonly List<TimeCapsule> _capsules = new();
        private readonly string _capsulesPath;
        private readonly ILlmService? _llmService;

        public TimeCapsuleService(ILlmService? llmService = null)
        {
            _llmService = llmService ?? new LlmService();
            _capsulesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "SaveState2", "data", "time_capsules");
            if (!Directory.Exists(_capsulesPath)) Directory.CreateDirectory(_capsulesPath);
            LoadCapsules();
        }

        public async Task<TimeCapsule> CreateCapsuleAsync(string gameId, string title, string description, 
            string creatorName, byte[] saveStateData, TimeSpan unlockDelay,
            string? challengeType = null, int? challengeTarget = null, List<string>? tags = null)
        {
            var capsule = new TimeCapsule
            {
                Id = Guid.NewGuid().ToString(),
                GameId = gameId,
                Title = title,
                Description = description,
                CreatorName = creatorName,
                SaveStateData = saveStateData,
                CreatedAt = DateTime.Now,
                UnlockAt = DateTime.Now + unlockDelay,
                IsUnlocked = false,
                ChallengeType = challengeType,
                ChallengeTarget = challengeTarget,
                Tags = tags ?? new()
            };

            // Generate LLM teaser
            if (_llmService?.IsAvailable == true)
            {
                capsule.LlmTeaser = await GenerateCapsuleTeaserAsync(title, description, challengeType, unlockDelay);
            }

            _capsules.Add(capsule);
            SaveCapsule(capsule);
            return capsule;
        }

        // Synchronous wrapper
        public TimeCapsule CreateCapsule(string gameId, string title, string description, 
            string creatorName, byte[] saveStateData, TimeSpan unlockDelay,
            string? challengeType = null, int? challengeTarget = null)
        {
            return CreateCapsuleAsync(gameId, title, description, creatorName, saveStateData, 
                unlockDelay, challengeType, challengeTarget).GetAwaiter().GetResult();
        }

        private async Task<string?> GenerateCapsuleTeaserAsync(string title, string description, 
            string? challengeType, TimeSpan unlockDelay)
        {
            if (_llmService == null) return null;

            var challengeText = challengeType != null ? $" with a {challengeType} challenge" : "";
            var timeText = unlockDelay.TotalHours < 24 
                ? $"{unlockDelay.TotalHours:F0} hours" 
                : $"{unlockDelay.TotalDays:F0} days";

            var prompt = $"Write a mysterious 15-word teaser for a locked gaming time capsule titled '{title}'{challengeText} that unlocks in {timeText}. Make it intriguing!";
            
            return await _llmService.CompleteAsync(prompt, 
                "You are a mysterious narrator. Write cryptic, enticing teasers. No quotes.");
        }

        public async Task<string?> GenerateUnlockMessageAsync(TimeCapsule capsule)
        {
            if (_llmService == null || !_llmService.IsAvailable) return null;

            var ageText = (DateTime.Now - capsule.CreatedAt).TotalDays;
            var prompt = $"Write an exciting 20-word message for someone who just unlocked a time capsule from {ageText:F0} days ago titled '{capsule.Title}'. Be celebratory!";
            
            return await _llmService.CompleteAsync(prompt,
                "You are an enthusiastic announcer celebrating a special moment.");
        }

        public List<TimeCapsule> GetAllCapsules() => _capsules;

        public List<TimeCapsule> GetUnlockedCapsules() => 
            _capsules.Where(c => c.IsUnlocked || DateTime.Now >= c.UnlockAt).ToList();

        public List<TimeCapsule> GetLockedCapsules() =>
            _capsules.Where(c => !c.IsUnlocked && DateTime.Now < c.UnlockAt).ToList();

        public List<TimeCapsule> SearchByTags(params string[] tags) =>
            _capsules.Where(c => tags.Any(t => c.Tags.Contains(t, StringComparer.OrdinalIgnoreCase))).ToList();

        public TimeCapsule? GetCapsule(string id)
        {
            var capsule = _capsules.FirstOrDefault(c => c.Id == id);
            if (capsule != null && DateTime.Now >= capsule.UnlockAt && !capsule.IsUnlocked)
            {
                capsule.IsUnlocked = true;
                SaveCapsule(capsule);
            }
            return capsule;
        }

        public bool TryUnlock(string id)
        {
            var capsule = _capsules.FirstOrDefault(c => c.Id == id);
            if (capsule == null) return false;

            if (DateTime.Now >= capsule.UnlockAt)
            {
                capsule.IsUnlocked = true;
                SaveCapsule(capsule);
                return true;
            }
            return false;
        }

        public void AddReaction(string capsuleId, string userId, string emoji)
        {
            var capsule = _capsules.FirstOrDefault(c => c.Id == capsuleId);
            if (capsule == null) return;

            // Remove existing reaction from user
            capsule.Reactions.RemoveAll(r => r.UserId == userId);
            
            capsule.Reactions.Add(new CapsuleReaction
            {
                UserId = userId,
                Emoji = emoji,
                Timestamp = DateTime.Now
            });
            SaveCapsule(capsule);
        }

        public void AddComment(string capsuleId, string authorName, string text, 
            string? parentId = null, byte[]? attachedSaveState = null)
        {
            var capsule = _capsules.FirstOrDefault(c => c.Id == capsuleId);
            if (capsule == null) return;

            capsule.Comments.Add(new CapsuleComment
            {
                AuthorName = authorName,
                Text = text,
                Timestamp = DateTime.Now,
                ParentId = parentId,
                AttachedSaveState = attachedSaveState
            });
            SaveCapsule(capsule);
        }

        // Simplified overload for compatibility
        public void AddComment(string capsuleId, string comment)
        {
            AddComment(capsuleId, "Anonymous", comment);
        }

        public List<CapsuleComment> GetCommentThread(string capsuleId)
        {
            var capsule = _capsules.FirstOrDefault(c => c.Id == capsuleId);
            if (capsule == null) return new();

            // Return comments with nested structure
            return capsule.Comments.OrderBy(c => c.Timestamp).ToList();
        }

        public TimeSpan? GetTimeUntilUnlock(string id)
        {
            var capsule = _capsules.FirstOrDefault(c => c.Id == id);
            if (capsule == null || capsule.IsUnlocked) return null;
            
            var remaining = capsule.UnlockAt - DateTime.Now;
            return remaining > TimeSpan.Zero ? remaining : null;
        }

        public string ExportCapsuleLink(string id)
        {
            // Generate a shareable link/code for the capsule
            var capsule = _capsules.FirstOrDefault(c => c.Id == id);
            if (capsule == null) return string.Empty;

            // Create a simple encoded link
            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(id));
            return $"savestate://capsule/{encoded}";
        }

        private void SaveCapsule(TimeCapsule capsule)
        {
            var path = Path.Combine(_capsulesPath, $"{capsule.Id}.json");
            // Don't serialize SaveStateData to JSON (too large), store separately
            var dataPath = Path.Combine(_capsulesPath, $"{capsule.Id}.sav");
            
            if (capsule.SaveStateData.Length > 0)
            {
                File.WriteAllBytes(dataPath, capsule.SaveStateData);
            }

            // Temporarily clear data for JSON serialization
            var tempData = capsule.SaveStateData;
            capsule.SaveStateData = Array.Empty<byte>();
            
            var json = JsonSerializer.Serialize(capsule, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            
            capsule.SaveStateData = tempData;
        }

        private void LoadCapsules()
        {
            if (!Directory.Exists(_capsulesPath)) return;

            foreach (var file in Directory.GetFiles(_capsulesPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var capsule = JsonSerializer.Deserialize<TimeCapsule>(json);
                    if (capsule != null)
                    {
                        // Load save state data if exists
                        var dataPath = Path.Combine(_capsulesPath, $"{capsule.Id}.sav");
                        if (File.Exists(dataPath))
                        {
                            capsule.SaveStateData = File.ReadAllBytes(dataPath);
                        }
                        _capsules.Add(capsule);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Operation failed: {ex.Message}"); }
            }
        }

        public void DeleteCapsule(string id)
        {
            var capsule = _capsules.FirstOrDefault(c => c.Id == id);
            if (capsule != null)
            {
                _capsules.Remove(capsule);
                var path = Path.Combine(_capsulesPath, $"{capsule.Id}.json");
                var dataPath = Path.Combine(_capsulesPath, $"{capsule.Id}.sav");
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(dataPath)) File.Delete(dataPath);
            }
        }
    }
}
