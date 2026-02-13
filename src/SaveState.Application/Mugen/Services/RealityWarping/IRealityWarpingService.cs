using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.RealityWarping;

/// <summary>
/// Interface for reality warping operations.
/// </summary>
public interface IRealityWarpingService
{
    Task<Result<Models.RealityWarping.GravityWell>> CreateGravityWellAsync(
        Models.RealityWarping.GravityWellRequest request, 
        CancellationToken ct = default);
    
    Task<Result<Models.RealityWarping.TimeDilationZone>> CreateTimeDilationZoneAsync(
        Models.RealityWarping.TimeDilationRequest request, 
        CancellationToken ct = default);
    
    Task<Result<Models.RealityWarping.DimensionalRift>> CreateDimensionalRiftAsync(
        Models.RealityWarping.DimensionalRiftRequest request, 
        CancellationToken ct = default);
    
    Task<Result<Models.RealityWarping.PhasingEffect>> ApplyMatterPhasingAsync(
        string entityId, 
        Models.RealityWarping.PhasingRequest request, 
        CancellationToken ct = default);
    
    Task<Result<Models.RealityWarping.RealityWarp>> InitiateRealityWarpAsync(
        Models.RealityWarping.RealityWarpRequest request, 
        CancellationToken ct = default);
    
    Task<Result<Models.RealityWarping.CausalityParadox>> TriggerCausalityParadoxAsync(
        Models.RealityWarping.CausalityParadoxRequest request, 
        CancellationToken ct = default);
    
    Task<Result<Models.RealityWarping.RealityState>> GetRealityStateAsync(
        string areaId, 
        CancellationToken ct = default);
    
    Task<Result> CollapseRealityWarpAsync(
        string warpId, 
        CancellationToken ct = default);
    
    Task<Result<Models.RealityWarping.RealityWarpingAnalytics>> GetRealityWarpingAnalyticsAsync(
        TimeSpan period, 
        CancellationToken ct = default);
}
