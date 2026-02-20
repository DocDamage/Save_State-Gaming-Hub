using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Core.BiometricGaming.Models;

/// <summary>
/// Represents a biometric sensor device type.
/// </summary>
public enum BiometricSensorType
{
    EEG,
    GSR,
    HeartRate,
    BloodOxygen,
    Respiration,
    EMG,
    Temperature,
    EyeTracking
}

/// <summary>
/// Represents a biometric sensor device.
/// </summary>
public record BiometricSensor
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string DeviceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public BiometricSensorType Type { get; init; }
    public string Manufacturer { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public bool IsConnected { get; init; }
    public DateTime? LastReading { get; init; }
    public IReadOnlyDictionary<string, object> Capabilities { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents biometric data from sensors.
/// </summary>
public record BiometricData
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public IReadOnlyList<SensorReading> Readings { get; init; } = Array.Empty<SensorReading>();
    public CognitiveState? CognitiveState { get; init; }
    public EmotionalState? EmotionalState { get; init; }
    public PhysiologicalState? PhysiologicalState { get; init; }
}

/// <summary>
/// Represents a reading from a specific sensor.
/// </summary>
public record SensorReading
{
    public string SensorId { get; init; } = string.Empty;
    public BiometricSensorType SensorType { get; init; }
    public double Value { get; init; }
    public string Unit { get; init; } = string.Empty;
    public double? RawValue { get; init; }
    public DateTime Timestamp { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public ReadingQuality Quality { get; init; } = ReadingQuality.Good;
}

/// <summary>
/// Quality of sensor reading.
/// </summary>
public enum ReadingQuality
{
    Excellent,
    Good,
    Fair,
    Poor,
    Unreliable
}

/// <summary>
/// Represents cognitive state derived from biometric data.
/// </summary>
public record CognitiveState
{
    public float FocusLevel { get; init; }
    public float MentalFatigue { get; init; }
    public float StressLevel { get; init; }
    public float EngagementLevel { get; init; }
    public float CognitiveLoad { get; init; }
    public AttentionState AttentionState { get; init; }
    public DateTime CalculatedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Attention states.
/// </summary>
public enum AttentionState
{
    HighlyFocused,
    Focused,
    Neutral,
    Distracted,
    Unfocused
}

/// <summary>
/// Represents emotional state derived from biometric data.
/// </summary>
public record EmotionalState
{
    public float Arousal { get; init; }
    public float Valence { get; init; }
    public float Excitement { get; init; }
    public float Frustration { get; init; }
    public float Relaxation { get; init; }
    public DominantEmotion DominantEmotion { get; init; }
    public DateTime CalculatedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Dominant emotion types.
/// </summary>
public enum DominantEmotion
{
    Neutral,
    Happy,
    Sad,
    Angry,
    Fearful,
    Surprised,
    Disgusted,
    Excited,
    Relaxed,
    Frustrated
}

/// <summary>
/// Represents physiological state derived from biometric data.
/// </summary>
public record PhysiologicalState
{
    public int HeartRate { get; init; }
    public float Hrv { get; init; }
    public float GsrConductance { get; init; }
    public float SkinTemperature { get; init; }
    public float BloodOxygen { get; init; }
    public float RespirationRate { get; init; }
    public PhysicalEffortLevel PhysicalEffort { get; init; }
    public DateTime CalculatedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Physical effort levels.
/// </summary>
public enum PhysicalEffortLevel
{
    Rest,
    Light,
    Moderate,
    Vigorous,
    Maximum
}

/// <summary>
/// Represents adaptive difficulty adjustment based on biometric data.
/// </summary>
public record AdaptiveDifficultyAdjustment
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string GameId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public float RecommendedDifficulty { get; init; }
    public float CurrentDifficulty { get; init; }
    public AdjustmentReason Reason { get; init; }
    public string Explanation { get; init; } = string.Empty;
    public IReadOnlyList<BiometricFactor> ContributingFactors { get; init; } = Array.Empty<BiometricFactor>();
    public DateTime CalculatedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
}

/// <summary>
/// Reasons for difficulty adjustment.
/// </summary>
public enum AdjustmentReason
{
    HighStress,
    LowEngagement,
    MentalFatigue,
    HighFrustration,
    OptimalChallenge,
    IncreasingChallenge,
    DecreasingChallenge,
    PlayerPreference
}

/// <summary>
/// Biometric factor contributing to adjustment.
/// </summary>
public record BiometricFactor
{
    public string MetricName { get; init; } = string.Empty;
    public float Value { get; init; }
    public float Weight { get; init; }
    public string Impact { get; init; } = string.Empty;
}

/// <summary>
/// Configuration for biometric gaming hub.
/// </summary>
public record BiometricGamingConfiguration
{
    public bool AdaptiveDifficultyEnabled { get; init; } = true;
    public bool RealTimeFeedbackEnabled { get; init; } = true;
    public bool DataCollectionEnabled { get; init; } = true;
    public int DataCollectionIntervalMs { get; init; } = 1000;
    public float DifficultyAdjustmentThreshold { get; init; } = 0.2f;
    public TimeSpan AdjustmentCooldown { get; init; } = TimeSpan.FromMinutes(5);
    public IReadOnlyList<BiometricSensorType> ActiveSensors { get; init; } = Array.Empty<BiometricSensorType>();
    public CognitiveStateProfile TargetState { get; init; } = new();
}

/// <summary>
/// Target cognitive state profile.
/// </summary>
public record CognitiveStateProfile
{
    public float TargetFocusMin { get; init; } = 0.6f;
    public float TargetFocusMax { get; init; } = 0.9f;
    public float MaxStressLevel { get; init; } = 0.7f;
    public float MaxFatigueLevel { get; init; } = 0.8f;
    public float MinEngagementLevel { get; init; } = 0.5f;
}

/// <summary>
/// Represents a biometric gaming session.
/// </summary>
public record BiometricGamingSession
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = string.Empty;
    public string GameId { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; } = SystemTimeProvider.Instance.UtcNow;
    public DateTime? EndedAt { get; init; }
    public IReadOnlyList<BiometricData> DataPoints { get; init; } = Array.Empty<BiometricData>();
    public IReadOnlyList<AdaptiveDifficultyAdjustment> DifficultyAdjustments { get; init; } = Array.Empty<AdaptiveDifficultyAdjustment>();
    public BiometricSessionSummary? Summary { get; init; }
}

/// <summary>
/// Summary of a biometric gaming session.
/// </summary>
public record BiometricSessionSummary
{
    public TimeSpan Duration { get; init; }
    public float AverageFocusLevel { get; init; }
    public float AverageStressLevel { get; init; }
    public float PeakStressLevel { get; init; }
    public int DifficultyAdjustmentsCount { get; init; }
    public DominantEmotion DominantEmotion { get; init; }
    public float SessionEnjoymentEstimate { get; init; }
}
