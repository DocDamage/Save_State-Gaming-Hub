using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.GameLibrary.Heuristics;

namespace SaveState.Infrastructure.GameLibrary.Services;

/// <summary>
/// Manages heuristic scoring and ranking of discovered values.
/// </summary>
public sealed class HeuristicAnalysisManager
{
    private readonly ILogger<HeuristicAnalysisManager> _logger;
    private readonly List<IValueHeuristic> _heuristics;

    public HeuristicAnalysisManager(ILogger<HeuristicAnalysisManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Initialize all heuristics (91 existing + 32 new = 123 total)
        _heuristics = new List<IValueHeuristic>
        {
            // Original 7 heuristics
            new HealthHeuristic(),
            new CurrencyHeuristic(),
            new PositionHeuristic(),
            new AmmoHeuristic(),
            new ExperienceHeuristic(),
            new ScoreHeuristic(),
            new TimerHeuristic(),

            // Movement & Physics (8)
            new SpeedHeuristic(),
            new VelocityHeuristic(),
            new JumpHeightHeuristic(),
            new GravityHeuristic(),
            new AltitudeHeuristic(),
            new RotationHeuristic(),
            new AccelerationHeuristic(),
            new DistanceTraveledHeuristic(),

            // Combat Mechanics (18)
            new CooldownHeuristic(),
            new DamageHeuristic(),
            new CriticalChanceHeuristic(),
            new ArmorRatingHeuristic(),
            new ShieldHeuristic(),
            new EnergyHeuristic(),
            new StaminaHeuristic(),
            new ComboCountHeuristic(),
            new KillCountHeuristic(),
            new HeadshotCountHeuristic(),
            new MultiKillHeuristic(),
            new AccuracyHeuristic(),
            new CritDamageHeuristic(),
            new LifeStealHeuristic(),
            new DodgeChanceHeuristic(),
            new FireRateHeuristic(),
            new ReloadSpeedHeuristic(),
            new BlockChanceHeuristic(),

            // RPG Progression (22)
            new SkillPointsHeuristic(),
            new ReputationHeuristic(),
            new CarryWeightHeuristic(),
            new LevelProgressHeuristic(),
            new GoldHeuristic(),
            new QuestProgressHeuristic(),
            new FameHeuristic(),
            new BlessingHeuristic(),
            new AttributeStrengthHeuristic(),
            new AttributeAgilityHeuristic(),
            new AttributeIntelligenceHeuristic(),
            new AttributeVitalityHeuristic(),
            new CharismaHeuristic(),
            new LuckHeuristic(),
            new PerceptionHeuristic(),
            new TalentPointsHeuristic(),
            new ResistFireHeuristic(),
            new ResistIceHeuristic(),
            new ResistLightningHeuristic(),
            new ResistPoisonHeuristic(),

            // Resource Management (13)
            new ManaHeuristic(),
            new DurabilityHeuristic(),
            new ResourceCountHeuristic(),
            new KeyCountHeuristic(),
            new GemCountHeuristic(),
            new EnergyCellHeuristic(),
            new ScrapHeuristic(),
            new WoodHeuristic(),
            new StoneHeuristic(),
            new FiberHeuristic(),
            new HideHeuristic(),
            new FoodHeuristic(),
            new AmmoSpecialHeuristic(),

            // Game State (15)
            new DifficultyHeuristic(),
            new GameTimeHeuristic(),
            new CompletionHeuristic(),
            new LivesHeuristic(),
            new WaveNumberHeuristic(),
            new DayNumberHeuristic(),
            new DeathCountHeuristic(),
            new PlayTimeHeuristic(),
            new MissionTimerHeuristic(),
            new CheckpointHeuristic(),
            new SecretCountHeuristic(),
            new RankHeuristic(),
            new LoadingProgressHeuristic(),
            new MatchTimeHeuristic(),
            new TutorialProgressHeuristic(),

            // Survival (10)
            new HungerHeuristic(),
            new ThirstHeuristic(),
            new TemperatureHeuristic(),
            new OxygenHeuristic(),
            new RadiationHeuristic(),
            new FatigueHeuristic(),
            new SanityHeuristic(),
            new PoisonHeuristic(),
            new WetnessHeuristic(),
            new BleedingHeuristic(),

            // Vehicle (9)
            new FuelHeuristic(),
            new VehicleSpeedHeuristic(),
            new NitroHeuristic(),
            new GearHeuristic(),
            new BrakeTemperatureHeuristic(),
            new TireWearHeuristic(),
            new SuspensionHeightHeuristic(),
            new OilTemperatureHeuristic(),
            new DownforceHeuristic(),

            // Map (4)
            new MapZoomHeuristic(),
            new MapRotationHeuristic(),
            new MapMarkerCountHeuristic(),
            new FogOfWarHeuristic(),

            // Multiplayer (3)
            new PingHeuristic(),
            new PlayerCountHeuristic(),
            new TeamScoreHeuristic()
        };

        _logger.LogInformation("HeuristicAnalysisManager initialized with {Count} heuristics", _heuristics.Count);
    }

    /// <summary>
    /// Applies heuristics to all candidates and returns them ranked by confidence.
    /// </summary>
    public List<DiscoveredValue> ApplyHeuristicsAndRank(DiscoverySession session)
    {
        foreach (var candidate in session.Candidates)
        {
            // Run all applicable heuristics
            var bestHeuristic = _heuristics
                .Where(h => h.SupportsValueType(candidate.ValueType))
                .Select(h => new
                {
                    Heuristic = h,
                    Confidence = h.CalculateConfidence(candidate, candidate.ObservationHistory)
                })
                .OrderByDescending(h => h.Confidence)
                .FirstOrDefault();

            if (bestHeuristic != null)
            {
                candidate.ConfidenceScore = bestHeuristic.Confidence;
                candidate.Category = bestHeuristic.Heuristic.Category;
                candidate.SuggestedName = SuggestName(candidate);
            }
        }

        var ranked = session.Candidates
            .OrderByDescending(c => c.ConfidenceScore)
            .ToList();
            
        _logger.LogDebug(
            "Applied heuristics to {Count} candidates for session {SessionId}. Top confidence: {TopConfidence:P}",
            ranked.Count,
            session.SessionId,
            ranked.FirstOrDefault()?.ConfidenceScore ?? 0);

        return ranked;
    }

    /// <summary>
    /// Applies initial heuristic scoring to a new candidate.
    /// </summary>
    public void ApplyInitialHeuristicScoring(DiscoveredValue candidate)
    {
        var bestHeuristic = _heuristics
            .Where(h => h.SupportsValueType(candidate.ValueType))
            .Select(h => new
            {
                Heuristic = h,
                Confidence = h.CalculateConfidence(candidate, candidate.ObservationHistory)
            })
            .OrderByDescending(h => h.Confidence)
            .FirstOrDefault();

        if (bestHeuristic != null)
        {
            candidate.ConfidenceScore = bestHeuristic.Confidence * 0.5; // Initial lower confidence
            candidate.Category = bestHeuristic.Heuristic.Category;
            candidate.SuggestedName = SuggestName(candidate);
        }
    }

    /// <summary>
    /// Suggests a name for a discovered value based on its category and type.
    /// </summary>
    private static string SuggestName(DiscoveredValue value)
    {
        var baseName = value.Category switch
        {
            "Health" => value.ValueType.ToLowerInvariant() == "float" ? "Health (Float)" : "Health",
            "Currency" => "Gold/Credits",
            "Ammo" => "Ammo Count",
            "Position" => "Player Position",
            "Experience" => "Experience Points",
            "Score" => "Score",
            "Timer" => "Timer",
            _ => $"Unknown ({value.ValueType})"
        };

        // Add address for uniqueness
        return $"{baseName} @ 0x{value.Address:X8}";
    }

    /// <summary>
    /// Gets ranked results filtered by confidence threshold.
    /// </summary>
    public List<DiscoveredValue> GetRankedResults(DiscoverySession session)
    {
        return session.Candidates
            .Where(c => c.ConfidenceScore >= session.Options.MinConfidenceThreshold)
            .OrderByDescending(c => c.ConfidenceScore)
            .Take(session.Options.MaxResults)
            .ToList();
    }
}
