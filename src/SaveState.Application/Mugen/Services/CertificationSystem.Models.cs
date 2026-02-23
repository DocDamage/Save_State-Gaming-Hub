using SaveState.Core.Mugen.ValueObjects;
using SkillLevel = SaveState.Application.Mugen.Models.Educational.SkillLevel;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Skill assessment data.
/// </summary>
public class CertificationSystemSkillAssessment
{
    public string AssessmentId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public CertificationSystemAssessmentType CertificationSystemAssessmentType { get; set; } = default!;
    public IReadOnlyDictionary<string, double> SkillScores { get; set; } = default!;
    public double OverallScore { get; set; } = default!;
    public DateTime AssessedAt { get; set; } = default!;
    public TimeSpan ValidityPeriod { get; set; } = default!;
}

/// <summary>
/// CertificationSystemCertification data.
/// </summary>
public class CertificationSystemCertification
{
    public string CertificationId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DifficultyLevel Difficulty { get; set; } = default!;
    public CertificationSystemCertificationCategory Category { get; set; } = default!;
    public IReadOnlyList<string> Prerequisites { get; set; } = default!;
    public IReadOnlyList<CertificationSystemAssessmentType> RequiredAssessments { get; set; } = default!;
    public double MinimumScore { get; set; } = default!;
    public TimeSpan EstimatedDuration { get; set; } = default!;
    public IReadOnlyList<string> SkillsCovered { get; set; } = default!;
    public string? BadgeReward { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public int TotalAwarded { get; set; } = default!;
}

/// <summary>
/// CertificationSystemCertification application data.
/// </summary>
public class CertificationSystemCertificationApplication
{
    public string ApplicationId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string CertificationId { get; set; } = default!;
    public DateTime AppliedAt { get; set; } = default!;
    public CertificationSystemApplicationStatus Status { get; set; } = default!;
    public bool PrerequisitesMet { get; set; } = default!;
    public IReadOnlyList<CertificationSystemAssessmentType> RequiredAssessments { get; set; } = default!;
    public Dictionary<string, CertificationSystemAssessmentResult> AssessmentResults { get; set; } = default!;
    public string? ReviewNotes { get; set; } = default!;
    public DateTime? ReviewedAt { get; set; } = default!;
    public DateTime ExpiresAt { get; set; } = default!;
}

/// <summary>
/// CertificationSystemBadge data.
/// </summary>
public class CertificationSystemBadge
{
    public string BadgeId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string IconUrl { get; set; } = default!;
    public CertificationSystemBadgeRarity Rarity { get; set; } = default!;
    public CertificationSystemBadgeCategory Category { get; set; } = default!;
    public CertificationSystemBadgeCriteria Criteria { get; set; } = default!;
    public int PointsValue { get; set; } = default!;
    public int UnlockedBy { get; set; } = default!;
}

/// <summary>
/// CertificationSystemBadge criteria data.
/// </summary>
public class CertificationSystemBadgeCriteria
{
    public CertificationSystemCriteriaType Type { get; set; } = default!;
    public double TargetValue { get; set; } = default!;
    public CertificationSystemTimeFrame CertificationSystemTimeFrame { get; set; } = default!;
}

/// <summary>
/// CertificationSystemBadge award data.
/// </summary>
public class CertificationSystemBadgeAward
{
    public string BadgeId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTime AwardedAt { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public int PointsEarned { get; set; } = default!;
}

/// <summary>
/// CertificationSystemBadge eligibility data.
/// </summary>
public class CertificationSystemBadgeEligibility
{
    public bool IsEligible { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public double Progress { get; set; } = default!;
}

/// <summary>
/// User certifications data.
/// </summary>
public class CertificationSystemUserCertifications
{
    public string UserId { get; set; } = default!;
    public IReadOnlyList<CertificationSystemEarnedCertification> EarnedCertifications { get; set; } = default!;
    public IReadOnlyList<CertificationSystemEarnedBadge> EarnedBadges { get; set; } = default!;
    public int CertificationPoints { get; set; } = default!;
    public int BadgePoints { get; set; } = default!;
    public SkillLevel SkillLevel { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
}

/// <summary>
/// Earned certification data.
/// </summary>
public class CertificationSystemEarnedCertification
{
    public string CertificationId { get; set; } = default!;
    public DateTime EarnedAt { get; set; } = default!;
    public IReadOnlyDictionary<string, double> AssessmentScores { get; set; } = default!;
    public string CertificateUrl { get; set; } = default!;
}

/// <summary>
/// Earned badge data.
/// </summary>
public class CertificationSystemEarnedBadge
{
    public string BadgeId { get; set; } = default!;
    public DateTime EarnedAt { get; set; } = default!;
    public string EarnedReason { get; set; } = default!;
}

/// <summary>
/// CertificationSystemCertification progress data.
/// </summary>
public class CertificationSystemCertificationProgress
{
    public string CertificationId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public int PrerequisitesCompleted { get; set; } = default!;
    public int PrerequisitesTotal { get; set; } = default!;
    public int AssessmentsCompleted { get; set; } = default!;
    public int AssessmentsTotal { get; set; } = default!;
    public double OverallProgress { get; set; } = default!;
    public TimeSpan EstimatedCompletion { get; set; } = default!;
    public IReadOnlyList<string> NextSteps { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
}

/// <summary>
/// Assessment result data.
/// </summary>
public class CertificationSystemAssessmentResult
{
    public CertificationSystemAssessmentType CertificationSystemAssessmentType { get; set; } = default!;
    public double Score { get; set; } = default!;
    public bool Passed { get; set; } = default!;
    public DateTime CompletedAt { get; set; } = default!;
    public IReadOnlyDictionary<string, double> DetailedScores { get; set; } = default!;
    public string Feedback { get; set; } = default!;
}

/// <summary>
/// Leaderboard entry data.
/// </summary>
public class CertificationSystemLeaderboardEntry
{
    public int Rank { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public int Score { get; set; } = default!;
    public int Change { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Metadata { get; set; } = default!;
}

/// <summary>
/// Leaderboard data.
/// </summary>
public class CertificationSystemLeaderboardData
{
    public CertificationSystemLeaderboardType CertificationSystemLeaderboardType { get; set; } = default!;
    public CertificationSystemTimeFrame CertificationSystemTimeFrame { get; set; } = default!;
    public IReadOnlyList<CertificationSystemLeaderboardEntry> Entries { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
    public int TotalEntries { get; set; } = default!;
}

/// <summary>
/// CertificationSystemCertification leaderboard query.
/// </summary>
public class CertificationSystemCertificationLeaderboardQuery
{
    public string CertificationId { get; set; } = default!;
    public CertificationSystemTimeFrame CertificationSystemTimeFrame { get; set; } = default!;
    public int Limit { get; set; } = default!;
}

/// <summary>
/// CertificationSystemCertification analytics data.
/// </summary>
public class CertificationSystemCertificationAnalytics
{
    public string CertificationId { get; set; } = default!;
    public TimeSpan Period { get; set; } = default!;
    public int TotalApplications { get; set; } = default!;
    public int ApprovedApplications { get; set; } = default!;
    public double AverageAssessmentScore { get; set; } = default!;
    public double CompletionRate { get; set; } = default!;
    public TimeSpan AverageCompletionTime { get; set; } = default!;
    public IReadOnlyList<CertificationSystemAssessmentType> PopularAssessmentTypes { get; set; } = default!;
    public double SkillImprovement { get; set; } = default!;
    public IReadOnlyDictionary<string, double> DropOutPoints { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Prerequisite check data.
/// </summary>
public class CertificationSystemPrerequisiteCheck
{
    public bool IsMet { get; set; } = default!;
    public string Reason { get; set; } = default!;
}

/// <summary>
/// Various enumeration types.
/// </summary>
public enum CertificationSystemAssessmentType { Fundamentals, Controls, Combos, Spacing, Defense, Advanced, Expert }
public enum CertificationSystemCertificationCategory { Fundamentals, Techniques, Strategy, Competitive, Educational }
public enum CertificationSystemApplicationStatus { Pending, UnderReview, Approved, Rejected, Expired }
public enum CertificationSystemBadgeRarity { Common, Uncommon, Rare, Epic, Legendary }
public enum CertificationSystemBadgeCategory { Achievement, Skill, Social, Event, Special }
public enum CertificationSystemCriteriaType { MatchResult, TrainingCompletion, CertificationSystemSkillAssessment, SocialInteraction, TimePlayed }
public enum CertificationSystemLeaderboardType { CertificationSystemCertification, BadgePoints, SkillLevel, AssessmentScores }
public enum CertificationSystemTimeFrame { Daily, Weekly, Monthly, AllTime }
