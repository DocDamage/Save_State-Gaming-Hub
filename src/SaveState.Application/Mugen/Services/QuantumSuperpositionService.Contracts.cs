using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Quantum Superposition Service interface.
/// </summary>
public interface QuantumSuperpositionServiceIQuantumSuperpositionService
{
    Task<Result<QuantumSuperpositionServiceQuantumMove>> InitializeQuantumMoveAsync(QuantumSuperpositionServiceQuantumMoveRequest request, CancellationToken ct = default);
    Task<Result<QuantumSuperpositionServiceWaveFunctionCollapse>> CollapseWaveFunctionAsync(string stateId, QuantumSuperpositionServiceCollapseTrigger trigger, CancellationToken ct = default);
    Task<Result<QuantumSuperpositionServiceQuantumEntanglement>> CreateEntanglementAsync(QuantumSuperpositionServiceEntanglementRequest request, CancellationToken ct = default);
    Task<Result<QuantumSuperpositionServiceUncertaintyMeasurement>> MeasureUncertaintyAsync(string stateId, QuantumSuperpositionServiceMeasurementType measurementType, CancellationToken ct = default);
    Task<Result<QuantumSuperpositionServiceSuperpositionTraining>> StartSuperpositionTrainingAsync(QuantumSuperpositionServiceTrainingRequest request, CancellationToken ct = default);
    Task<Result<QuantumSuperpositionServiceQuantumProbability>> CalculateProbabilitiesAsync(string stateId, CancellationToken ct = default);
    Task<Result<QuantumSuperpositionServiceQuantumInterference>> ApplyQuantumInterferenceAsync(string stateId, QuantumSuperpositionServiceInterferencePattern pattern, CancellationToken ct = default);
    Task<Result<QuantumSuperpositionServiceQuantumAnalytics>> GetQuantumAnalyticsAsync(TimeSpan period, CancellationToken ct = default);
}
