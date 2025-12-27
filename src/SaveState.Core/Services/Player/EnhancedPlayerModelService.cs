using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace SaveState.Core.Services.Player
{
    /// <summary>
    /// Enhanced Player Modeling with:
    /// - Skill progression tracking
    /// - Play session pattern analysis
    /// - Preference learning from choices
    /// - Anomaly detection (bot/cheating detection)
    /// - Engagement prediction
    /// - Churn risk assessment
    /// - Personalization recommendations
    /// - Dynamic difficulty adjustment data
    /// </summary>
    public class EnhancedPlayerModel
    {
        public string PlayerId { get; set; } = string.Empty;
        
        // Core behavioral traits (0-1 scale)
        public float AggressionScore { get; set; } = 0.5f;
        public float ExplorationTendency { get; set; } = 0.5f;
        public float HumorTolerance { get; set; } = 0.5f;
        public float MoralAlignment { get; set; } = 0f; // -1 to 1
        public float ComplexityPreference { get; set; } = 0.5f;
        public float PacingPreference { get; set; } = 0.5f;
        public float SocialEngagement { get; set; } = 0.5f;
        public float RiskTaking { get; set; } = 0.5f;
        
        // Skill progression
        public float OverallSkillLevel { get; set; } = 0.5f;
        public float SkillGrowthRate { get; set; } = 0f;
        public Dictionary<string, float> SkillByCategory { get; set; } = new();
        public List<SkillMilestone> Milestones { get; set; } = new();
        
        // Session patterns
        public TimeSpan AverageSessionLength { get; set; }
        public TimeSpan TotalPlayTime { get; set; }
        public int TotalSessions { get; set; }
        public float SessionConsistency { get; set; } // 0-1, high = plays regularly
        public List<int> PreferredPlayHours { get; set; } = new(); // 0-23
        public DayOfWeek[] PreferredPlayDays { get; set; } = Array.Empty<DayOfWeek>();
        
        // Engagement metrics
        public float EngagementScore { get; set; } = 0.5f;
        public float ChurnRisk { get; set; } = 0.3f;
        public int DaysSinceLastPlay { get; set; }
        public float RetentionProbability { get; set; } = 0.7f;
        
        // Preferences learned
        public Dictionary<string, float> ContentPreferences { get; set; } = new();
        public Dictionary<string, float> FeatureUsage { get; set; } = new();
        public List<string> DislikedContent { get; set; } = new();
        public List<string> FavoriteContent { get; set; } = new();
        
        // Difficulty adaptation
        public float CurrentDifficultyLevel { get; set; } = 0.5f;
        public float OptimalChallenge { get; set; } = 0.5f; // Sweet spot for flow state
        public float FrustrationThreshold { get; set; } = 0.7f;
        public float BoredomThreshold { get; set; } = 0.3f;
        public int ConsecutiveFailures { get; set; }
        public int ConsecutiveSuccesses { get; set; }
        
        // Anomaly detection
        public float AnomalyScore { get; set; } = 0f; // 0 = normal, 1 = definitely anomalous
        public List<AnomalyFlag> AnomalyFlags { get; set; } = new();
        
        // Timestamps
        public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime LastSession { get; set; } = DateTime.UtcNow;
    }

    public class SkillMilestone
    {
        public string Name { get; set; } = string.Empty;
        public DateTime AchievedAt { get; set; }
        public float SkillLevelAtTime { get; set; }
        public string? AchievementContext { get; set; }
    }

    public class AnomalyFlag
    {
        public string Type { get; set; } = string.Empty; // "speed", "accuracy", "pattern", "timing"
        public float Severity { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class PlaySession
    {
        public string SessionId { get; set; } = Guid.NewGuid().ToString();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; }
        public TimeSpan Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.UtcNow - StartTime;
        public List<EnhancedPlayerAction> Actions { get; set; } = new();
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public float AverageResponseTimeMs { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class EnhancedPlayerAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ActionType { get; set; } = string.Empty;
        public ActionCategory Category { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public float ResponseTimeMs { get; set; }
        public bool WasSuccessful { get; set; }
        public string? Context { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public float? Difficulty { get; set; }
    }

    public class PlayerRecommendation
    {
        public string Type { get; set; } = string.Empty; // "content", "difficulty", "feature", "break"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
    }

    public class DifficultyAdjustment
    {
        public float CurrentLevel { get; set; }
        public float RecommendedLevel { get; set; }
        public float AdjustmentMagnitude { get; set; }
        public string Reason { get; set; } = string.Empty;
        public bool ShouldApply { get; set; }
    }

    public interface IEnhancedPlayerModelService
    {
        Task<EnhancedPlayerModel> GetModelAsync(string playerId);
        Task UpdateFromActionAsync(string playerId, EnhancedPlayerAction action);
        Task StartSessionAsync(string playerId);
        Task EndSessionAsync(string playerId);
        Task<List<PlayerRecommendation>> GetRecommendationsAsync(string playerId);
        Task<DifficultyAdjustment> CalculateDifficultyAdjustmentAsync(string playerId);
        Task<float> PredictChurnRiskAsync(string playerId);
        Task<bool> DetectAnomalyAsync(string playerId, EnhancedPlayerAction action);
        PlayerModelStatistics GetStatistics();
        Task SaveAsync();
        Task LoadAsync();
    }

    public class PlayerModelStatistics
    {
        public int TotalPlayers { get; set; }
        public int ActivePlayers { get; set; }
        public int AtRiskPlayers { get; set; }
        public float AverageSkillLevel { get; set; }
        public float AverageEngagement { get; set; }
        public Dictionary<string, int> PlayersBySkillBracket { get; set; } = new();
    }

    public class EnhancedPlayerModelService : IEnhancedPlayerModelService
    {
        private readonly ILogger _logger = Log.ForContext<EnhancedPlayerModelService>();
        private readonly ConcurrentDictionary<string, EnhancedPlayerModel> _models = new();
        private readonly ConcurrentDictionary<string, PlaySession> _activeSessions = new();
        private readonly ConcurrentDictionary<string, List<EnhancedPlayerAction>> _recentActions = new();
        private readonly EnhancedPlayerModelConfig _config;
        private readonly string _storagePath;

        public EnhancedPlayerModelService(EnhancedPlayerModelConfig? config = null)
        {
            _config = config ?? new EnhancedPlayerModelConfig();
            _storagePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SaveState", "Player", "enhanced_models.json");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_storagePath)!);
        }

        public Task<EnhancedPlayerModel> GetModelAsync(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new ArgumentException("Player ID cannot be empty", nameof(playerId));
            }

            return Task.FromResult(_models.GetOrAdd(playerId, id => new EnhancedPlayerModel { PlayerId = id }));
        }

        public async Task UpdateFromActionAsync(string playerId, EnhancedPlayerAction action)
        {
            var model = await GetModelAsync(playerId);
            
            // Add to recent actions for pattern analysis
            var recentActions = _recentActions.GetOrAdd(playerId, _ => new List<EnhancedPlayerAction>());
            lock (recentActions)
            {
                recentActions.Add(action);
                while (recentActions.Count > _config.MaxRecentActions)
                {
                    recentActions.RemoveAt(0);
                }
            }

            // Update behavioral traits based on action
            UpdateBehavioralTraits(model, action);
            
            // Update skill tracking
            UpdateSkillTracking(model, action);
            
            // Update engagement metrics
            UpdateEngagementMetrics(model, action);
            
            // Update difficulty adaptation data
            UpdateDifficultyData(model, action);
            
            // Check for anomalies
            await DetectAnomalyAsync(playerId, action);
            
            model.LastUpdated = DateTime.UtcNow;
        }

        public async Task StartSessionAsync(string playerId)
        {
            var model = await GetModelAsync(playerId);
            
            var session = new PlaySession();
            _activeSessions[playerId] = session;
            
            model.TotalSessions++;
            model.DaysSinceLastPlay = (int)(DateTime.UtcNow - model.LastSession).TotalDays;
            model.LastSession = DateTime.UtcNow;
            
            // Track preferred play times
            var hour = DateTime.Now.Hour;
            if (!model.PreferredPlayHours.Contains(hour))
            {
                model.PreferredPlayHours.Add(hour);
                if (model.PreferredPlayHours.Count > 5)
                {
                    // Keep only most frequent hours
                    model.PreferredPlayHours = model.PreferredPlayHours
                        .GroupBy(h => h)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .Select(g => g.Key)
                        .ToList();
                }
            }
        }

        public async Task EndSessionAsync(string playerId)
        {
            if (_activeSessions.TryRemove(playerId, out var session))
            {
                session.EndTime = DateTime.UtcNow;
                
                var model = await GetModelAsync(playerId);
                
                // Update average session length
                var totalMinutes = model.AverageSessionLength.TotalMinutes * (model.TotalSessions - 1);
                model.AverageSessionLength = TimeSpan.FromMinutes(
                    (totalMinutes + session.Duration.TotalMinutes) / model.TotalSessions);
                
                model.TotalPlayTime += session.Duration;
                
                // Update session consistency
                CalculateSessionConsistency(model);
                
                // Update engagement after session
                CalculateEngagement(model, session);
            }
        }

        public async Task<List<PlayerRecommendation>> GetRecommendationsAsync(string playerId)
        {
            var model = await GetModelAsync(playerId);
            var recommendations = new List<PlayerRecommendation>();

            // Check for break recommendation
            if (_activeSessions.TryGetValue(playerId, out var session))
            {
                if (session.Duration.TotalMinutes > 120)
                {
                    recommendations.Add(new PlayerRecommendation
                    {
                        Type = "break",
                        Title = "Take a Break",
                        Description = "You've been playing for a while. Consider taking a short break!",
                        Confidence = 0.8f
                    });
                }
            }

            // Check for difficulty adjustment recommendation
            var diffAdj = await CalculateDifficultyAdjustmentAsync(playerId);
            if (diffAdj.ShouldApply)
            {
                recommendations.Add(new PlayerRecommendation
                {
                    Type = "difficulty",
                    Title = diffAdj.RecommendedLevel > diffAdj.CurrentLevel ? "Increase Challenge" : "Reduce Difficulty",
                    Description = diffAdj.Reason,
                    Confidence = 0.7f,
                    Data = new Dictionary<string, object>
                    {
                        ["current"] = diffAdj.CurrentLevel,
                        ["recommended"] = diffAdj.RecommendedLevel
                    }
                });
            }

            // Content recommendations based on preferences
            if (model.FavoriteContent.Any())
            {
                recommendations.Add(new PlayerRecommendation
                {
                    Type = "content",
                    Title = "Similar Content Available",
                    Description = $"Based on your enjoyment of {model.FavoriteContent.First()}, you might like similar content.",
                    Confidence = 0.65f,
                    Data = new Dictionary<string, object>
                    {
                        ["basedOn"] = model.FavoriteContent.Take(3).ToList()
                    }
                });
            }

            // Feature recommendation for underused features
            var unusedFeatures = _config.TrackableFeatures
                .Where(f => model.FeatureUsage.GetValueOrDefault(f, 0) < 0.1f)
                .ToList();
            
            if (unusedFeatures.Any())
            {
                recommendations.Add(new PlayerRecommendation
                {
                    Type = "feature",
                    Title = "Discover New Features",
                    Description = $"Have you tried the {unusedFeatures.First()} feature?",
                    Confidence = 0.5f
                });
            }

            return recommendations.OrderByDescending(r => r.Confidence).ToList();
        }

        public async Task<DifficultyAdjustment> CalculateDifficultyAdjustmentAsync(string playerId)
        {
            var model = await GetModelAsync(playerId);
            
            var adjustment = new DifficultyAdjustment
            {
                CurrentLevel = model.CurrentDifficultyLevel,
                RecommendedLevel = model.CurrentDifficultyLevel,
                ShouldApply = false
            };

            // Check for frustration (too many consecutive failures)
            if (model.ConsecutiveFailures >= _config.FrustrationThreshold)
            {
                var decrease = Math.Min(0.2f, model.ConsecutiveFailures * 0.03f);
                adjustment.RecommendedLevel = Math.Max(0.1f, model.CurrentDifficultyLevel - decrease);
                adjustment.AdjustmentMagnitude = -decrease;
                adjustment.Reason = $"Player has failed {model.ConsecutiveFailures} times in a row. Reducing difficulty to prevent frustration.";
                adjustment.ShouldApply = true;
            }
            // Check for boredom (too many consecutive successes with fast times)
            else if (model.ConsecutiveSuccesses >= _config.BoredomThreshold)
            {
                var increase = Math.Min(0.15f, model.ConsecutiveSuccesses * 0.02f);
                adjustment.RecommendedLevel = Math.Min(1.0f, model.CurrentDifficultyLevel + increase);
                adjustment.AdjustmentMagnitude = increase;
                adjustment.Reason = $"Player has succeeded {model.ConsecutiveSuccesses} times easily. Increasing challenge.";
                adjustment.ShouldApply = true;
            }
            // Gradual adjustment toward optimal challenge
            else
            {
                var diff = model.OptimalChallenge - model.CurrentDifficultyLevel;
                if (Math.Abs(diff) > 0.1f)
                {
                    adjustment.RecommendedLevel = model.CurrentDifficultyLevel + (diff * 0.1f);
                    adjustment.AdjustmentMagnitude = diff * 0.1f;
                    adjustment.Reason = "Gradual adjustment toward optimal challenge level.";
                    adjustment.ShouldApply = true;
                }
            }

            return adjustment;
        }

        public async Task<float> PredictChurnRiskAsync(string playerId)
        {
            var model = await GetModelAsync(playerId);
            
            float risk = 0;

            // Days since last play is strongest indicator
            if (model.DaysSinceLastPlay > 7)
                risk += 0.3f;
            else if (model.DaysSinceLastPlay > 3)
                risk += 0.15f;

            // Declining session lengths
            // (Would need historical data to calculate properly)

            // Low engagement score
            if (model.EngagementScore < 0.3f)
                risk += 0.2f;
            else if (model.EngagementScore < 0.5f)
                risk += 0.1f;

            // Frustration indicators
            if (model.ConsecutiveFailures > 5)
                risk += 0.15f;

            // Low session count (never really engaged)
            if (model.TotalSessions < 3)
                risk += 0.1f;

            // Session consistency declining
            if (model.SessionConsistency < 0.3f)
                risk += 0.1f;

            model.ChurnRisk = Math.Min(1.0f, risk);
            return model.ChurnRisk;
        }

        public async Task<bool> DetectAnomalyAsync(string playerId, EnhancedPlayerAction action)
        {
            var model = await GetModelAsync(playerId);
            var recentActions = _recentActions.GetOrAdd(playerId, _ => new List<EnhancedPlayerAction>());
            
            var anomalies = new List<AnomalyFlag>();

            // Check for superhuman reaction times
            if (action.ResponseTimeMs > 0 && action.ResponseTimeMs < _config.MinHumanReactionTimeMs)
            {
                anomalies.Add(new AnomalyFlag
                {
                    Type = "speed",
                    Severity = 0.8f,
                    DetectedAt = DateTime.UtcNow,
                    Description = $"Response time of {action.ResponseTimeMs}ms is below human capability"
                });
            }

            // Check for impossible accuracy
            if (recentActions.Count >= 20)
            {
                var recentSuccessRate = recentActions.TakeLast(20).Count(a => a.WasSuccessful) / 20f;
                if (recentSuccessRate > _config.SuspiciousSuccessRate && 
                    recentActions.TakeLast(20).All(a => a.Difficulty >= 0.7f))
                {
                    anomalies.Add(new AnomalyFlag
                    {
                        Type = "accuracy",
                        Severity = 0.7f,
                        DetectedAt = DateTime.UtcNow,
                        Description = $"Success rate of {recentSuccessRate:P0} on hard content is suspicious"
                    });
                }
            }

            // Check for robotic patterns (too consistent timing)
            if (recentActions.Count >= 10)
            {
                var times = recentActions.TakeLast(10).Select(a => a.ResponseTimeMs).ToList();
                var avgTime = times.Average();
                var variance = times.Select(t => Math.Pow(t - avgTime, 2)).Average();
                var stdDev = Math.Sqrt(variance);
                
                // Humans have natural variance; very low variance is suspicious
                if (stdDev < _config.MinNaturalVarianceMs && avgTime < 500)
                {
                    anomalies.Add(new AnomalyFlag
                    {
                        Type = "pattern",
                        Severity = 0.6f,
                        DetectedAt = DateTime.UtcNow,
                        Description = $"Response time variance ({stdDev:F1}ms) is unnaturally low"
                    });
                }
            }

            // Update model anomaly score
            if (anomalies.Any())
            {
                model.AnomalyFlags.AddRange(anomalies);
                model.AnomalyScore = Math.Min(1.0f, model.AnomalyScore + anomalies.Sum(a => a.Severity * 0.1f));
                
                // Decay old flags
                model.AnomalyFlags = model.AnomalyFlags
                    .Where(f => (DateTime.UtcNow - f.DetectedAt).TotalHours < 24)
                    .ToList();
                
                return true;
            }

            // Natural decay of anomaly score
            model.AnomalyScore = Math.Max(0, model.AnomalyScore - 0.01f);
            return false;
        }

        public PlayerModelStatistics GetStatistics()
        {
            var models = _models.Values.ToList();
            
            return new PlayerModelStatistics
            {
                TotalPlayers = models.Count,
                ActivePlayers = models.Count(m => m.DaysSinceLastPlay <= 7),
                AtRiskPlayers = models.Count(m => m.ChurnRisk > 0.6f),
                AverageSkillLevel = models.Any() ? models.Average(m => m.OverallSkillLevel) : 0,
                AverageEngagement = models.Any() ? models.Average(m => m.EngagementScore) : 0,
                PlayersBySkillBracket = new Dictionary<string, int>
                {
                    ["Beginner (0-0.3)"] = models.Count(m => m.OverallSkillLevel < 0.3f),
                    ["Intermediate (0.3-0.6)"] = models.Count(m => m.OverallSkillLevel >= 0.3f && m.OverallSkillLevel < 0.6f),
                    ["Advanced (0.6-0.8)"] = models.Count(m => m.OverallSkillLevel >= 0.6f && m.OverallSkillLevel < 0.8f),
                    ["Expert (0.8+)"] = models.Count(m => m.OverallSkillLevel >= 0.8f)
                }
            };
        }

        public async Task SaveAsync()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_models, 
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            await System.IO.File.WriteAllTextAsync(_storagePath, json);
        }

        public async Task LoadAsync()
        {
            if (!System.IO.File.Exists(_storagePath)) return;

            try
            {
                var json = await System.IO.File.ReadAllTextAsync(_storagePath);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<ConcurrentDictionary<string, EnhancedPlayerModel>>(json);
                
                if (loaded != null)
                {
                    foreach (var (key, value) in loaded)
                    {
                        _models[key] = value;
                    }
                }
            }
            catch (Exception ex) { _logger.Warning(ex, "Failed to load player models"); }
        }

        // ============ Private Helper Methods ============

        private void UpdateBehavioralTraits(EnhancedPlayerModel model, EnhancedPlayerAction action)
        {
            var learningRate = _config.TraitLearningRate;

            switch (action.Category)
            {
                case ActionCategory.Combat:
                    model.AggressionScore = Lerp(model.AggressionScore, 0.8f, learningRate);
                    if (action.Metadata.TryGetValue("was_first_strike", out var fs) && (bool)fs)
                        model.RiskTaking = Lerp(model.RiskTaking, 0.8f, learningRate);
                    break;

                case ActionCategory.Dialogue:
                    model.SocialEngagement = Lerp(model.SocialEngagement, 0.8f, learningRate);
                    if (action.Metadata.TryGetValue("chose_peaceful", out var cp) && (bool)cp)
                        model.AggressionScore = Lerp(model.AggressionScore, 0.2f, learningRate);
                    break;

                case ActionCategory.Exploration:
                    model.ExplorationTendency = Lerp(model.ExplorationTendency, 0.8f, learningRate);
                    break;

                case ActionCategory.MoralChoice:
                    if (action.Metadata.TryGetValue("choice_alignment", out var alignment))
                    {
                        var alignVal = Convert.ToSingle(alignment);
                        model.MoralAlignment = Lerp(model.MoralAlignment, alignVal, learningRate);
                    }
                    break;
            }
        }

        private void UpdateSkillTracking(EnhancedPlayerModel model, EnhancedPlayerAction action)
        {
            if (!action.Difficulty.HasValue) return;

            var categoryKey = action.Category.ToString();
            var currentSkill = model.SkillByCategory.GetValueOrDefault(categoryKey, 0.5f);

            if (action.WasSuccessful)
            {
                // Skill increases more for harder challenges
                var increase = action.Difficulty.Value * 0.02f;
                currentSkill = Math.Min(1.0f, currentSkill + increase);
            }
            else
            {
                // Slight decrease for failures (learning through failure)
                var decrease = 0.005f;
                currentSkill = Math.Max(0.1f, currentSkill - decrease);
            }

            model.SkillByCategory[categoryKey] = currentSkill;
            
            // Update overall skill as weighted average
            if (model.SkillByCategory.Any())
            {
                model.OverallSkillLevel = model.SkillByCategory.Values.Average();
            }
        }

        private void UpdateEngagementMetrics(EnhancedPlayerModel model, EnhancedPlayerAction action)
        {
            // Track content preferences
            if (action.Context != null)
            {
                var currentPref = model.ContentPreferences.GetValueOrDefault(action.Context, 0.5f);
                var adjustment = action.WasSuccessful ? 0.05f : -0.02f;
                model.ContentPreferences[action.Context] = Math.Clamp(currentPref + adjustment, 0, 1);
            }
        }

        private void UpdateDifficultyData(EnhancedPlayerModel model, EnhancedPlayerAction action)
        {
            if (action.WasSuccessful)
            {
                model.ConsecutiveSuccesses++;
                model.ConsecutiveFailures = 0;
                
                // If succeeding at higher difficulty, adjust optimal challenge up
                if (action.Difficulty.HasValue && action.Difficulty.Value > model.OptimalChallenge)
                {
                    model.OptimalChallenge = Lerp(model.OptimalChallenge, action.Difficulty.Value, 0.1f);
                }
            }
            else
            {
                model.ConsecutiveFailures++;
                model.ConsecutiveSuccesses = 0;
                
                // If failing at lower difficulty, adjust optimal challenge down
                if (action.Difficulty.HasValue && action.Difficulty.Value < model.OptimalChallenge)
                {
                    model.OptimalChallenge = Lerp(model.OptimalChallenge, action.Difficulty.Value, 0.1f);
                }
            }
        }

        private void CalculateSessionConsistency(EnhancedPlayerModel model)
        {
            // This would ideally use historical session start times
            // For now, use a simplified heuristic
            if (model.TotalSessions > 1)
            {
                var avgDaysBetween = (DateTime.UtcNow - model.FirstSeen).TotalDays / model.TotalSessions;
                model.SessionConsistency = avgDaysBetween <= 2 ? 0.9f :
                                          avgDaysBetween <= 5 ? 0.7f :
                                          avgDaysBetween <= 10 ? 0.5f :
                                          avgDaysBetween <= 20 ? 0.3f : 0.1f;
            }
        }

        private void CalculateEngagement(EnhancedPlayerModel model, PlaySession session)
        {
            float engagement = 0.5f;

            // Longer sessions = more engaged
            if (session.Duration.TotalMinutes > 30) engagement += 0.1f;
            if (session.Duration.TotalMinutes > 60) engagement += 0.1f;

            // Activity rate (actions per minute)
            var actionsPerMin = session.Actions.Count / Math.Max(1, session.Duration.TotalMinutes);
            if (actionsPerMin > 2) engagement += 0.1f;

            // Success rate in session
            var successRate = session.Actions.Count > 0 
                ? session.Actions.Count(a => a.WasSuccessful) / (float)session.Actions.Count 
                : 0.5f;
            engagement += (successRate - 0.5f) * 0.2f; // Boost for wins, penalty for too many losses

            model.EngagementScore = Lerp(model.EngagementScore, Math.Clamp(engagement, 0, 1), 0.3f);
        }

        private float Lerp(float current, float target, float rate)
        {
            return current + (target - current) * rate;
        }
    }

    public class EnhancedPlayerModelConfig
    {
        public float TraitLearningRate { get; set; } = 0.1f;
        public int MaxRecentActions { get; set; } = 100;
        public int FrustrationThreshold { get; set; } = 5; // Consecutive failures
        public int BoredomThreshold { get; set; } = 8; // Consecutive easy successes
        public float MinHumanReactionTimeMs { get; set; } = 100f;
        public float SuspiciousSuccessRate { get; set; } = 0.98f;
        public float MinNaturalVarianceMs { get; set; } = 20f;
        public string[] TrackableFeatures { get; set; } = new[]
        {
            "crafting", "trading", "pvp", "guilds", "achievements", "leaderboards"
        };
    }
}
