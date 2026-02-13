using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// VR/AR integration service interface.
/// </summary>
public interface IVrArIntegrationService
{
    Task<Result<VrSession>> InitializeVrSessionAsync(string userId, VrConfiguration config, CancellationToken ct = default);
    Task<Result<ArSession>> InitializeArSessionAsync(string userId, ArConfiguration config, CancellationToken ct = default);
    Task<Result<VrInputResponse>> ProcessVrInputAsync(string sessionId, VrInput input, CancellationToken ct = default);
    Task<Result<ArInputResponse>> ProcessArInputAsync(string sessionId, ArInput input, CancellationToken ct = default);
    Task<Result> TerminateVrSessionAsync(string sessionId, CancellationToken ct = default);
    Task<Result> TerminateArSessionAsync(string sessionId, CancellationToken ct = default);
    Task<Result<VrCalibrationResult>> CalibrateVrSystemAsync(string sessionId, CancellationToken ct = default);
    Task<Result<ArCalibrationResult>> CalibrateArSystemAsync(string sessionId, CancellationToken ct = default);
}
