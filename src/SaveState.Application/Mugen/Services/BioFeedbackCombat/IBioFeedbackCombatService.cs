using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.BioFeedbackCombat;

/// <summary>
/// Bio Feedback Combat Service interface.
/// </summary>
public interface IBioFeedbackCombatService
{
    Task<Result<BioProfile>> CreateBioProfileAsync(BioProfileRequest request, CancellationToken ct = default);
    Task<Result<BioFeedbackCombatSession>> StartCombatSessionAsync(string profileId, CombatSessionRequest request, CancellationToken ct = default);
    Task<Result<BioFeedback>> ProcessBioDataAsync(string sessionId, BioDataInput input, CancellationToken ct = default);
    Task<Result<HeartRateWeapon>> ChargeHeartRateWeaponAsync(string sessionId, WeaponChargeRequest request, CancellationToken ct = default);
    Task<Result<BreathingCombo>> EnhanceComboWithBreathingAsync(string sessionId, ComboEnhancementRequest request, CancellationToken ct = default);
    Task<Result<MusclePoweredDefense>> PowerDefenseWithMusclesAsync(string sessionId, DefenseRequest request, CancellationToken ct = default);
    Task<Result<AdrenalineBurst>> TriggerAdrenalineBurstAsync(string sessionId, BurstTrigger trigger, CancellationToken ct = default);
    Task<Result<MeditationMode>> EnterMeditationModeAsync(string sessionId, MeditationRequest request, CancellationToken ct = default);
    Task<Result<BioCombatReport>> GenerateCombatReportAsync(string sessionId, CancellationToken ct = default);
    Task<Result> EndCombatSessionAsync(string sessionId, CancellationToken ct = default);
}
