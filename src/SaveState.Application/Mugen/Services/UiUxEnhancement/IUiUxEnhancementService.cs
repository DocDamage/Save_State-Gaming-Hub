using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// UI/UX enhancement service interface.
/// </summary>
public interface IUiUxEnhancementService
{
    Task<Result<HudConfiguration>> CreateHudConfigurationAsync(string sessionId, HudPreferences preferences, CancellationToken ct = default);
    Task<Result<MenuSystem>> CreateMenuSystemAsync(string sessionId, MenuConfiguration menuConfig, CancellationToken ct = default);
    Task<Result<VisualFeedbackSystem>> CreateFeedbackSystemAsync(string sessionId, FeedbackConfiguration feedbackConfig, CancellationToken ct = default);
    Task<Result<HudUpdate>> UpdateHudAsync(string sessionId, HudData data, CancellationToken ct = default);
    Task<Result<MenuUpdate>> UpdateMenuAsync(string sessionId, MenuState menuState, CancellationToken ct = default);
    Task<Result<FeedbackUpdate>> TriggerFeedbackAsync(string sessionId, FeedbackTrigger trigger, CancellationToken ct = default);
    Task<Result<UiStateSnapshot>> GetUiStateAsync(string sessionId, CancellationToken ct = default);
    Task<Result<UiOptimization>> OptimizeUiAsync(string sessionId, UiOptimizationRequest request, CancellationToken ct = default);
}
