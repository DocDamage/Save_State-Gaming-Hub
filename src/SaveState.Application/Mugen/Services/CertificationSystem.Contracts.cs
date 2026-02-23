using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// CertificationSystemCertification System interface.
/// </summary>
public interface CertificationSystemICertificationSystem
{
    Task<Result<CertificationSystemSkillAssessment>> AssessPlayerSkillAsync(string userId, CertificationSystemAssessmentType assessmentType, CancellationToken ct = default);
    Task<Result<CertificationSystemCertification>> GetCertificationAsync(string certificationId, CancellationToken ct = default);
    Task<Result<CertificationSystemCertificationApplication>> ApplyForCertificationAsync(string userId, string certificationId, CancellationToken ct = default);
    Task<Result<CertificationSystemBadge>> GetBadgeAsync(string badgeId, CancellationToken ct = default);
    Task<Result<CertificationSystemBadgeAward>> CheckBadgeEligibilityAsync(string userId, string badgeId, CancellationToken ct = default);
    Task<Result<CertificationSystemUserCertifications>> GetUserCertificationsAsync(string userId, CancellationToken ct = default);
    Task<Result<CertificationSystemCertificationProgress>> GetCertificationProgressAsync(string userId, string certificationId, CancellationToken ct = default);
    Task<Result<CertificationSystemAssessmentResult>> GetAssessmentResultAsync(string userId, CertificationSystemAssessmentType assessmentType, CancellationToken ct = default);
    Task<Result<CertificationSystemLeaderboardData>> GetCertificationLeaderboardAsync(CertificationSystemCertificationLeaderboardQuery query, CancellationToken ct = default);
    Task<Result<CertificationSystemCertificationAnalytics>> GetCertificationAnalyticsAsync(string certificationId, TimeSpan period, CancellationToken ct = default);
}
