using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Services.BioFeedbackCombat.Engines;

namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Bio-feedback combat service providing physiological data integration,
/// heart rate weapons, breathing patterns, and adaptive combat mechanics.
/// Refactored to use extracted engines and models.
/// </summary>
public class BioFeedbackCombatService : IBioFeedbackCombatService
{
    private readonly ILogger<BioFeedbackCombatService> _logger;
    private readonly ICacheService _cache;
    private readonly Dictionary<string, BioProfile> _bioProfiles = new();
    private readonly Dictionary<string, BioFeedbackCombatSession> _combatSessions = new();
    private readonly HeartRateEngine _heartRateEngine;
    private readonly BreathingEngine _breathingEngine;
    private readonly MuscleTensionEngine _muscleEngine;
    private readonly AdrenalineEngine _adrenalineEngine;
    private readonly MeditationEngine _meditationEngine;

    public BioFeedbackCombatService(
        ILogger<BioFeedbackCombatService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache)
    {
        _logger = logger;
        _cache = cache;
        _heartRateEngine = new HeartRateEngine(loggerFactory.CreateLogger<HeartRateEngine>());
        _breathingEngine = new BreathingEngine(loggerFactory.CreateLogger<BreathingEngine>());
        _muscleEngine = new MuscleTensionEngine(loggerFactory.CreateLogger<MuscleTensionEngine>());
        _adrenalineEngine = new AdrenalineEngine(loggerFactory.CreateLogger<AdrenalineEngine>());
        _meditationEngine = new MeditationEngine(loggerFactory.CreateLogger<MeditationEngine>());

        InitializeBioFeedback();
    }

    public async Task<Result<BioProfile>> CreateBioProfileAsync(BioProfileRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating bio profile for player {PlayerId}", request.PlayerId);

            var profile = new BioProfile
            {
                ProfileId = Guid.NewGuid().ToString(),
                PlayerId = request.PlayerId,
                BaselineMetrics = await EstablishBaselinesAsync(request.PlayerId, ct),
                CalibrationData = new BioCalibration
                {
                    RestingHeartRate = 70,
                    NormalBreathingRate = 14,
                    BaselineMuscleTension = 0.3f,
                    CalibratedAt = DateTime.UtcNow
                },
                BioSettings = new BioSettings
                {
                    HeartRateSensitivity = request.HeartRateSensitivity,
                    BreathingSensitivity = request.BreathingSensitivity,
                    MuscleSensitivity = request.MuscleSensitivity,
                    AdrenalineThreshold = request.AdrenalineThreshold,
                    MeditationEnabled = request.MeditationEnabled
                },
                CombatModifiers = new BioCombatModifiers
                {
                    HeartRateDamageBonus = 0.1f,
                    BreathingComboBonus = 0.15f,
                    MuscleTensionDefenseBonus = 0.2f,
                    AdrenalineBurstEnabled = true
                },
                CreatedAt = DateTime.UtcNow,
                LastCalibration = DateTime.UtcNow,
                Status = BioProfileStatus.Active
            };

            _bioProfiles[profile.ProfileId] = profile;

            _logger.LogInformation("Bio profile created: {ProfileId}", profile.ProfileId);
            return Result.Success<BioProfile>(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bio profile for player {PlayerId}", request.PlayerId);
            return Result.Failure<BioProfile>($"Bio profile creation failed: {ex.Message}");
        }
    }

    public async Task<Result<BioFeedbackCombatSession>> StartCombatSessionAsync(string profileId, CombatSessionRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_bioProfiles.TryGetValue(profileId, out var profile))
            {
                return Result.Failure<BioFeedbackCombatSession>("Bio profile not found");
            }

            _logger.LogInformation("Starting combat session for profile {ProfileId}", profileId);

            var session = new BioFeedbackCombatSession
            {
                SessionId = Guid.NewGuid().ToString(),
                ProfileId = profileId,
                PlayerId = profile.PlayerId,
                BioDataStream = new BioDataStream
                {
                    HeartRateData = new List<BioDataPoint>(),
                    BreathingData = new List<BioDataPoint>(),
                    MuscleTensionData = new List<BioDataPoint>(),
                    SkinConductanceData = new List<BioDataPoint>(),
                    TemperatureData = new List<BioDataPoint>()
                },
                CombatMetrics = new CombatBioMetrics
                {
                    TotalCombos = 0,
                    HeartRatePoweredMoves = 0,
                    BreathingEnhancedCombos = 0,
                    MusclePoweredBlocks = 0,
                    AdrenalineBursts = 0,
                    MeditationPeriods = 0
                },
                PhysiologicalState = new PhysiologicalState
                {
                    CurrentHeartRate = profile.BaselineMetrics.RestingHeartRate,
                    CurrentBreathingRate = profile.BaselineMetrics.NormalBreathingRate,
                    CurrentMuscleTension = profile.BaselineMetrics.BaselineMuscleTension,
                    StressLevel = 0.2f,
                    FocusLevel = 0.8f,
                    FatigueLevel = 0.1f
                },
                StartedAt = DateTime.UtcNow,
                Status = CombatStatus.Active
            };

            _combatSessions[session.SessionId] = session;

            _logger.LogInformation("Combat session started: {SessionId}", session.SessionId);
            return Result.Success<BioFeedbackCombatSession>(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting combat session for profile {ProfileId}", profileId);
            return Result.Failure<BioFeedbackCombatSession>($"Combat session start failed: {ex.Message}");
        }
    }

    public async Task<Result<BioFeedback>> ProcessBioDataAsync(string sessionId, BioDataInput input, CancellationToken ct = default)
    {
        try
        {
            if (!_combatSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<BioFeedback>("Combat session not found");
            }

            if (!_bioProfiles.TryGetValue(session.ProfileId, out var profile))
            {
                return Result.Failure<BioFeedback>("Bio profile not found");
            }

            _logger.LogInformation("Processing bio data for session {SessionId}", sessionId);

            // Process each bio data type
            var heartRateFeedback = await _heartRateEngine.ProcessHeartRateAsync(session, input.HeartRate, profile, ct);
            var breathingFeedback = await _breathingEngine.ProcessBreathingAsync(session, input.BreathingRate, profile, ct);
            var muscleFeedback = await _muscleEngine.ProcessMuscleTensionAsync(session, input.MuscleTension, profile, ct);

            // Combine all feedback
            var combinedFeedback = CombineBioFeedback(heartRateFeedback, breathingFeedback, muscleFeedback);

            // Update session data
            UpdateSessionData(session, input);

            // Check for adrenaline burst
            var adrenalineBurst = await CheckAdrenalineBurstAsync(session, profile, ct);

            _logger.LogInformation("Bio feedback processed: damage bonus {DamageBonus:F2}, speed bonus {SpeedBonus:F2}",
                combinedFeedback.DamageBonus, combinedFeedback.SpeedBonus);

            return Result.Success<BioFeedback>(combinedFeedback);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bio data for session {SessionId}", sessionId);
            return Result.Failure<BioFeedback>($"Bio data processing failed: {ex.Message}");
        }
    }

    public async Task<Result<HeartRateWeapon>> ChargeHeartRateWeaponAsync(string sessionId, WeaponChargeRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_combatSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<HeartRateWeapon>("Combat session not found");
            }

            _logger.LogInformation("Charging heart rate weapon for session {SessionId}", sessionId);

            var weapon = await _heartRateEngine.ChargeWeaponAsync(session, request, ct);

            // Update combat metrics
            session.CombatMetrics.HeartRatePoweredMoves++;

            _logger.LogInformation("Heart rate weapon charged: {Power} power", weapon.Power);
            return Result.Success<HeartRateWeapon>(weapon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error charging heart rate weapon for session {SessionId}", sessionId);
            return Result.Failure<HeartRateWeapon>($"Heart rate weapon charging failed: {ex.Message}");
        }
    }

    public async Task<Result<BreathingCombo>> EnhanceComboWithBreathingAsync(string sessionId, ComboEnhancementRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_combatSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<BreathingCombo>("Combat session not found");
            }

            _logger.LogInformation("Enhancing combo with breathing for session {SessionId}", sessionId);

            var enhancedCombo = await _breathingEngine.EnhanceComboAsync(session, request, ct);

            // Update combat metrics
            session.CombatMetrics.BreathingEnhancedCombos++;

            _logger.LogInformation("Combo enhanced with breathing: {HitCount} hits, {TotalDamage} damage",
                enhancedCombo.HitCount, enhancedCombo.TotalDamage);

            return Result.Success<BreathingCombo>(enhancedCombo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing combo with breathing for session {SessionId}", sessionId);
            return Result.Failure<BreathingCombo>($"Combo enhancement failed: {ex.Message}");
        }
    }

    public async Task<Result<MusclePoweredDefense>> PowerDefenseWithMusclesAsync(string sessionId, DefenseRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_combatSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<MusclePoweredDefense>("Combat session not found");
            }

            _logger.LogInformation("Powering defense with muscles for session {SessionId}", sessionId);

            var poweredDefense = await _muscleEngine.PowerDefenseAsync(session, request, ct);

            // Update combat metrics
            session.CombatMetrics.MusclePoweredBlocks++;

            _logger.LogInformation("Defense powered with muscles: {BlockStrength:F2} strength", poweredDefense.BlockStrength);
            return Result.Success<MusclePoweredDefense>(poweredDefense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error powering defense with muscles for session {SessionId}", sessionId);
            return Result.Failure<MusclePoweredDefense>($"Defense powering failed: {ex.Message}");
        }
    }

    public async Task<Result<AdrenalineBurst>> TriggerAdrenalineBurstAsync(string sessionId, BurstTrigger trigger, CancellationToken ct = default)
    {
        try
        {
            if (!_combatSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<AdrenalineBurst>("Combat session not found");
            }

            if (!_bioProfiles.TryGetValue(session.ProfileId, out var profile))
            {
                return Result.Failure<AdrenalineBurst>("Bio profile not found");
            }

            _logger.LogInformation("Triggering adrenaline burst for session {SessionId}", sessionId);

            var burst = await _adrenalineEngine.TriggerBurstAsync(session, profile, trigger, ct);

            // Update combat metrics
            session.CombatMetrics.AdrenalineBursts++;

            // Apply burst effects to physiological state
            session.PhysiologicalState.StressLevel *= 0.5f;
            session.PhysiologicalState.FocusLevel = Math.Min(session.PhysiologicalState.FocusLevel + 0.3f, 1.0f);

            _logger.LogInformation("Adrenaline burst triggered: {Duration} duration, {PowerMultiplier:F2} power multiplier",
                burst.Duration, burst.PowerMultiplier);

            return Result.Success<AdrenalineBurst>(burst);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering adrenaline burst for session {SessionId}", sessionId);
            return Result.Failure<AdrenalineBurst>($"Adrenaline burst failed: {ex.Message}");
        }
    }

    public async Task<Result<MeditationMode>> EnterMeditationModeAsync(string sessionId, MeditationRequest request, CancellationToken ct = default)
    {
        try
        {
            if (!_combatSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<MeditationMode>("Combat session not found");
            }

            _logger.LogInformation("Entering meditation mode for session {SessionId}", sessionId);

            var meditation = await _meditationEngine.StartMeditationAsync(session, request, ct);

            // Update combat metrics
            session.CombatMetrics.MeditationPeriods++;

            // Apply meditation effects
            session.PhysiologicalState.StressLevel *= 0.3f;
            session.PhysiologicalState.FatigueLevel *= 0.7f;
            session.PhysiologicalState.FocusLevel = Math.Min(session.PhysiologicalState.FocusLevel + 0.2f, 1.0f);

            _logger.LogInformation("Meditation mode entered: {Technique} technique", meditation.Technique);
            return Result.Success<MeditationMode>(meditation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error entering meditation mode for session {SessionId}", sessionId);
            return Result.Failure<MeditationMode>($"Meditation mode failed: {ex.Message}");
        }
    }

    public async Task<Result<BioCombatReport>> GenerateCombatReportAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            if (!_combatSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure<BioCombatReport>("Combat session not found");
            }

            _logger.LogInformation("Generating bio combat report for session {SessionId}", sessionId);

            var report = new BioCombatReport
            {
                SessionId = sessionId,
                Duration = DateTime.UtcNow - session.StartedAt,
                CombatMetrics = session.CombatMetrics,
                PhysiologicalTrends = AnalyzePhysiologicalTrends(session),
                BioEffectiveness = CalculateBioEffectiveness(session),
                PeakPerformanceMoments = IdentifyPeakMoments(session),
                FatigueAccumulation = CalculateFatigueAccumulation(session),
                StressManagement = AnalyzeStressManagement(session),
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Bio combat report generated successfully");
            return Result.Success<BioCombatReport>(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating combat report for session {SessionId}", sessionId);
            return Result.Failure<BioCombatReport>($"Report generation failed: {ex.Message}");
        }
    }

    public async Task<Result> EndCombatSessionAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            if (!_combatSessions.TryGetValue(sessionId, out var session))
            {
                return Result.Failure("Combat session not found");
            }

            _logger.LogInformation("Ending combat session {SessionId}", sessionId);

            // Generate final report
            var report = await GenerateCombatReportAsync(sessionId, ct);

            // Update bio profile with session data
            if (_bioProfiles.TryGetValue(session.ProfileId, out var profile))
            {
                await UpdateBioProfileFromSessionAsync(profile, session, ct);
            }

            // Clean up session
            session.Status = CombatStatus.Completed;
            session.EndedAt = DateTime.UtcNow;

            _logger.LogInformation("Combat session ended successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending combat session {SessionId}", sessionId);
            return Result.Failure($"Session end failed: {ex.Message}");
        }
    }

    #region Private Methods

    private void InitializeBioFeedback()
    {
        // Initialize bio feedback constants and baseline values
        _logger.LogInformation("Bio feedback combat system initialized");
    }

    private async Task<BaselineMetrics> EstablishBaselinesAsync(string playerId, CancellationToken ct)
    {
        // Establish physiological baselines for player
        return new BaselineMetrics
        {
            RestingHeartRate = 70,
            NormalBreathingRate = 14,
            BaselineMuscleTension = 0.3f,
            NormalSkinConductance = 2.0f,
            BaselineTemperature = 36.6f,
            EstablishedAt = DateTime.UtcNow
        };
    }

    private BioFeedback CombineBioFeedback(HeartRateFeedback heartRate, BreathingFeedback breathing, MuscleFeedback muscle)
    {
        // Combine all bio feedback into unified response
        return new BioFeedback
        {
            HeartRateComponent = heartRate,
            BreathingComponent = breathing,
            MuscleComponent = muscle,
            OverallIntensity = (heartRate.Intensity + breathing.Intensity + muscle.Intensity) / 3,
            DamageBonus = heartRate.DamageBonus + breathing.DamageBonus + muscle.DamageBonus,
            SpeedBonus = heartRate.SpeedBonus + breathing.SpeedBonus + muscle.SpeedBonus,
            DefenseBonus = heartRate.DefenseBonus + breathing.DefenseBonus + muscle.DefenseBonus,
            Timestamp = DateTime.UtcNow
        };
    }

    private async Task<AdrenalineBurst?> CheckAdrenalineBurstAsync(BioFeedbackCombatSession session, BioProfile profile, CancellationToken ct)
    {
        // Check if conditions are met for adrenaline burst
        var heartRateThreshold = profile.BioSettings.AdrenalineThreshold;
        var currentHeartRate = session.PhysiologicalState.CurrentHeartRate;

        if (currentHeartRate >= heartRateThreshold && session.PhysiologicalState.StressLevel > 0.7f)
        {
            var burstResult = await TriggerAdrenalineBurstAsync(session.SessionId, new BurstTrigger
            {
                TriggerType = BurstTriggerType.Physiological,
                Intensity = (currentHeartRate - profile.BaselineMetrics.RestingHeartRate) / 50.0f
            }, ct);
            return burstResult.Value;
        }

        return null;
    }

    private void UpdateSessionData(BioFeedbackCombatSession session, BioDataInput input)
    {
        // Update session with new bio data
        var timestamp = DateTime.UtcNow;

        var heartRateData = session.BioDataStream.HeartRateData?.ToList() ?? new List<BioDataPoint>();
        heartRateData.Add(new BioDataPoint { Value = input.HeartRate, Timestamp = timestamp });
        session.BioDataStream.HeartRateData = heartRateData;

        var breathingData = session.BioDataStream.BreathingData?.ToList() ?? new List<BioDataPoint>();
        breathingData.Add(new BioDataPoint { Value = input.BreathingRate, Timestamp = timestamp });
        session.BioDataStream.BreathingData = breathingData;

        var muscleTensionData = session.BioDataStream.MuscleTensionData?.ToList() ?? new List<BioDataPoint>();
        muscleTensionData.Add(new BioDataPoint { Value = input.MuscleTension, Timestamp = timestamp });
        session.BioDataStream.MuscleTensionData = muscleTensionData;

        // Update physiological state
        session.PhysiologicalState.CurrentHeartRate = input.HeartRate;
        session.PhysiologicalState.CurrentBreathingRate = input.BreathingRate;
        session.PhysiologicalState.CurrentMuscleTension = input.MuscleTension;
    }

    private PhysiologicalTrends AnalyzePhysiologicalTrends(BioFeedbackCombatSession session)
    {
        // Analyze physiological data trends
        return new PhysiologicalTrends
        {
            HeartRateTrend = CalculateTrend(session.BioDataStream.HeartRateData),
            BreathingTrend = CalculateTrend(session.BioDataStream.BreathingData),
            MuscleTensionTrend = CalculateTrend(session.BioDataStream.MuscleTensionData),
            StressAccumulation = session.PhysiologicalState.StressLevel,
            FatigueAccumulation = session.PhysiologicalState.FatigueLevel
        };
    }

    private BioEffectiveness CalculateBioEffectiveness(BioFeedbackCombatSession session)
    {
        // Calculate how effective bio feedback was in combat
        return new BioEffectiveness
        {
            HeartRateUtilization = session.CombatMetrics.HeartRatePoweredMoves / Math.Max(session.CombatMetrics.TotalCombos, 1),
            BreathingSynchronization = session.CombatMetrics.BreathingEnhancedCombos / Math.Max(session.CombatMetrics.TotalCombos, 1),
            MuscleTensionEfficiency = session.CombatMetrics.MusclePoweredBlocks / Math.Max(1, 1), // Placeholder for blocks
            OverallBioIntegration = (session.CombatMetrics.HeartRatePoweredMoves +
                                   session.CombatMetrics.BreathingEnhancedCombos +
                                   session.CombatMetrics.MusclePoweredBlocks) / Math.Max(session.CombatMetrics.TotalCombos * 3, 1)
        };
    }

    private List<PeakMoment> IdentifyPeakMoments(BioFeedbackCombatSession session)
    {
        // Identify peak performance moments
        return new List<PeakMoment>
        {
            new PeakMoment
            {
                Timestamp = session.StartedAt.AddMinutes(5),
                Type = PeakType.AdrenalineBurst,
                Intensity = 0.9f,
                Trigger = "High heart rate + stress"
            }
        };
    }

    private FatigueAnalysis CalculateFatigueAccumulation(BioFeedbackCombatSession session)
    {
        // Calculate fatigue accumulation over session
        return new FatigueAnalysis
        {
            TotalFatigue = session.PhysiologicalState.FatigueLevel,
            FatigueRate = (float)(session.PhysiologicalState.FatigueLevel / (DateTime.UtcNow - session.StartedAt).TotalHours),
            RecoveryTime = TimeSpan.FromHours(session.PhysiologicalState.FatigueLevel * 2)
        };
    }

    private StressAnalysis AnalyzeStressManagement(BioFeedbackCombatSession session)
    {
        // Analyze how well stress was managed
        return new StressAnalysis
        {
            PeakStressLevel = 0.8f,
            AverageStressLevel = session.PhysiologicalState.StressLevel,
            StressReductionTechniques = session.CombatMetrics.MeditationPeriods > 0 ? "Meditation" : "Combat Flow",
            StressManagementEffectiveness = 1.0f - session.PhysiologicalState.StressLevel
        };
    }

    private TrendDirection CalculateTrend(IReadOnlyList<BioDataPoint> data)
    {
        // Calculate trend direction from data points
        var count = data.Count;
        if (count < 2) return TrendDirection.Stable;

        var firstHalf = data.Take(count / 2).Average(d => (double)d.Value);
        var secondHalf = data.Skip(count / 2).Average(d => (double)d.Value);

        var difference = secondHalf - firstHalf;
        if (difference > 1) return TrendDirection.Increasing;
        if (difference < -1) return TrendDirection.Decreasing;
        return TrendDirection.Stable;
    }

    private async Task UpdateBioProfileFromSessionAsync(BioProfile profile, BioFeedbackCombatSession session, CancellationToken ct)
    {
        // Update bio profile with session insights
        profile.LastCalibration = DateTime.UtcNow;

        // Adjust sensitivity based on effectiveness
        var effectiveness = CalculateBioEffectiveness(session);
        profile.BioSettings.HeartRateSensitivity *= (0.8f + effectiveness.HeartRateUtilization * 0.4f);
        profile.BioSettings.BreathingSensitivity *= (0.8f + effectiveness.BreathingSynchronization * 0.4f);
        profile.BioSettings.MuscleSensitivity *= (0.8f + effectiveness.MuscleTensionEfficiency * 0.4f);
    }

    #endregion
}
