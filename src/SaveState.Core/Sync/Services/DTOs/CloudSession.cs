namespace SaveState.Core.Sync.Services.DTOs;

/// <summary>
/// Represents an active cloud gaming session.
/// </summary>
public sealed record CloudSession(
    Guid Id,
    Guid GameId,
    CloudGamingProvider Provider,
    DateTime StartedAt,
    NetworkQuality InitialQuality,
    CloudSessionStatus Status = CloudSessionStatus.Active);

/// <summary>
/// Status of a cloud gaming session.
/// </summary>
public enum CloudSessionStatus
{
    Active,
    Ended,
    Interrupted,
    QualityDegraded
}