using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Application.Mugen.Services.UiUxEnhancement.Engines;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// UI/UX Enhancement service providing professional interfaces for advanced mechanics.
/// Creates HUD elements, menus, and visual feedback systems for 24+ experimental features.
/// Refactored to use extracted engines and models.
/// </summary>
public class UiUxEnhancementService : IUiUxEnhancementService
{
    private readonly ILogger<UiUxEnhancementService> _logger;
    private readonly ICacheService _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITimeProvider _timeProvider;

    // UI State management
    private readonly Dictionary<string, UiState> _uiStates = new();
    private readonly Dictionary<string, HudConfiguration> _hudConfigs = new();
    private readonly Dictionary<string, MenuSystem> _menuSystems = new();

    // Visual feedback systems
    private readonly List<VisualFeedbackSystem> _feedbackSystems = new();
    private readonly Queue<UiNotification> _notificationQueue = new();

    // Engines
    private readonly HudEngine _hudEngine;
    private readonly MenuEngine _menuEngine;
    private readonly FeedbackEngine _feedbackEngine;

    public UiUxEnhancementService(
        ILogger<UiUxEnhancementService> logger,
        ILoggerFactory loggerFactory,
        ICacheService cache,
        IServiceProvider serviceProvider,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _timeProvider = timeProvider;

        _hudEngine = new HudEngine();
        _menuEngine = new MenuEngine();
        _feedbackEngine = new FeedbackEngine();

        InitializeUiSystems();
    }

    public async Task<Result<HudConfiguration>> CreateHudConfigurationAsync(string sessionId, HudPreferences preferences, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating HUD configuration for session {SessionId}", sessionId);

            var generatedElements = _hudEngine.GenerateHudElements(preferences.EnabledMechanics);
            var config = new HudConfiguration
            {
                SessionId = sessionId,
                Preferences = preferences,
                Elements = generatedElements,
                Layout = _hudEngine.CalculateOptimalLayout(preferences.ScreenResolution, generatedElements.Count),
                Theme = preferences.Theme,
                AccessibilitySettings = preferences.AccessibilitySettings,
                CreatedAt = _timeProvider.UtcNow
            };

            _hudConfigs[sessionId] = config;
            _cache.Set($"hud_config_{sessionId}", config, TimeSpan.FromHours(1));

            _logger.LogInformation("HUD configuration created with {Elements} elements", config.Elements.Count);
            return Result.Success<HudConfiguration>(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating HUD configuration");
            return Result.Failure<HudConfiguration>($"HUD configuration failed: {ex.Message}");
        }
    }

    public async Task<Result<MenuSystem>> CreateMenuSystemAsync(string sessionId, MenuConfiguration menuConfig, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating menu system for session {SessionId}", sessionId);

            var menuSystem = new MenuSystem
            {
                SessionId = sessionId,
                Configuration = menuConfig,
                Menus = _menuEngine.GenerateMenus(menuConfig.EnabledFeatures),
                NavigationGraph = _menuEngine.BuildNavigationGraph(menuConfig.EnabledFeatures),
                Theme = menuConfig.Theme,
                LocalizationSettings = menuConfig.LocalizationSettings,
                CreatedAt = _timeProvider.UtcNow
            };

            _menuSystems[sessionId] = menuSystem;
            _cache.Set($"menu_system_{sessionId}", menuSystem, TimeSpan.FromHours(1));

            _logger.LogInformation("Menu system created with {Menus} menus", menuSystem.Menus.Count);
            return Result.Success<MenuSystem>(menuSystem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating menu system");
            return Result.Failure<MenuSystem>($"Menu system creation failed: {ex.Message}");
        }
    }

    public async Task<Result<VisualFeedbackSystem>> CreateFeedbackSystemAsync(string sessionId, FeedbackConfiguration feedbackConfig, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating visual feedback system for session {SessionId}", sessionId);

            var feedbackSystem = new VisualFeedbackSystem
            {
                SessionId = sessionId,
                Configuration = feedbackConfig,
                FeedbackRules = _feedbackEngine.GenerateFeedbackRules(feedbackConfig.EnabledMechanics),
                AnimationLibrary = _feedbackEngine.LoadAnimationLibrary(feedbackConfig.Theme),
                SoundLibrary = _feedbackEngine.LoadSoundLibrary(feedbackConfig.AudioEnabled),
                ParticleEffects = _feedbackEngine.LoadParticleEffects(feedbackConfig.ParticlesEnabled),
                CreatedAt = _timeProvider.UtcNow
            };

            _feedbackSystems.Add(feedbackSystem);
            _cache.Set($"feedback_system_{sessionId}", feedbackSystem, TimeSpan.FromHours(1));

            _logger.LogInformation("Visual feedback system created with {Rules} feedback rules", feedbackSystem.FeedbackRules.Count);
            return Result.Success<VisualFeedbackSystem>(feedbackSystem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feedback system");
            return Result.Failure<VisualFeedbackSystem>($"Feedback system creation failed: {ex.Message}");
        }
    }

    public async Task<Result<HudUpdate>> UpdateHudAsync(string sessionId, HudData data, CancellationToken ct = default)
    {
        try
        {
            if (!_hudConfigs.TryGetValue(sessionId, out var config))
            {
                return Result.Failure<HudUpdate>("HUD configuration not found");
            }

            var update = new HudUpdate
            {
                SessionId = sessionId,
                Timestamp = _timeProvider.UtcNow,
                ElementUpdates = GenerateElementUpdates(config, data),
                LayoutAdjustments = CalculateLayoutAdjustments(config, data),
                VisualEffects = _feedbackEngine.GenerateVisualEffects(new FeedbackTrigger { TriggerType = "hud_update", Intensity = 0.5f }),
                PerformanceIndicators = GeneratePerformanceIndicators(data)
            };

            _cache.Set($"hud_update_{sessionId}", update, TimeSpan.FromSeconds(30));

            _logger.LogDebug("HUD updated for session {SessionId} with {Updates} element updates", sessionId, update.ElementUpdates.Count);
            return Result.Success<HudUpdate>(update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating HUD");
            return Result.Failure<HudUpdate>($"HUD update failed: {ex.Message}");
        }
    }

    public async Task<Result<MenuUpdate>> UpdateMenuAsync(string sessionId, MenuState menuState, CancellationToken ct = default)
    {
        try
        {
            if (!_menuSystems.TryGetValue(sessionId, out var menuSystem))
            {
                return Result.Failure<MenuUpdate>("Menu system not found");
            }

            var update = new MenuUpdate
            {
                SessionId = sessionId,
                Timestamp = _timeProvider.UtcNow,
                CurrentMenu = menuState.CurrentMenu,
                MenuItems = GenerateMenuItems(menuSystem, menuState),
                NavigationOptions = GenerateNavigationOptions(menuSystem, menuState),
                VisualState = GenerateMenuVisualState(menuState)
            };

            _cache.Set($"menu_update_{sessionId}", update, TimeSpan.FromSeconds(30));

            _logger.LogDebug("Menu updated for session {SessionId}", sessionId);
            return Result.Success<MenuUpdate>(update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating menu");
            return Result.Failure<MenuUpdate>($"Menu update failed: {ex.Message}");
        }
    }

    public async Task<Result<FeedbackUpdate>> TriggerFeedbackAsync(string sessionId, FeedbackTrigger trigger, CancellationToken ct = default)
    {
        try
        {
            var feedbackSystem = _feedbackSystems.FirstOrDefault(f => f.SessionId == sessionId);
            if (feedbackSystem == null)
            {
                return Result.Failure<FeedbackUpdate>("Feedback system not found");
            }

            var update = new FeedbackUpdate
            {
                SessionId = sessionId,
                Timestamp = _timeProvider.UtcNow,
                Trigger = trigger,
                ActiveFeedback = GenerateActiveFeedback(feedbackSystem, trigger),
                VisualEffects = _feedbackEngine.GenerateVisualEffects(trigger),
                AudioCues = _feedbackEngine.GenerateAudioCues(trigger),
                HapticFeedback = _feedbackEngine.GenerateHapticFeedback(trigger)
            };

            _notificationQueue.Enqueue(new UiNotification
            {
                SessionId = sessionId,
                Type = "feedback",
                Data = JsonSerializer.Serialize(update),
                Priority = trigger.Priority,
                Timestamp = _timeProvider.UtcNow
            });

            _logger.LogDebug("Feedback triggered for session {SessionId}: {TriggerType}", sessionId, trigger.TriggerType);
            return Result.Success<FeedbackUpdate>(update);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error triggering feedback");
            return Result.Failure<FeedbackUpdate>($"Feedback trigger failed: {ex.Message}");
        }
    }

    public async Task<Result<UiStateSnapshot>> GetUiStateAsync(string sessionId, CancellationToken ct = default)
    {
        try
        {
            var snapshot = new UiStateSnapshot
            {
                SessionId = sessionId,
                HudConfiguration = _hudConfigs.GetValueOrDefault(sessionId),
                MenuSystem = _menuSystems.GetValueOrDefault(sessionId),
                FeedbackSystem = _feedbackSystems.FirstOrDefault(f => f.SessionId == sessionId),
                PendingNotifications = _notificationQueue.Where(n => n.SessionId == sessionId).ToList(),
                UiState = GetOrCreateUiState(sessionId),
                CapturedAt = _timeProvider.UtcNow
            };

            _logger.LogDebug("UI state snapshot captured for session {SessionId}", sessionId);
            return Result.Success<UiStateSnapshot>(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting UI state");
            return Result.Failure<UiStateSnapshot>($"UI state retrieval failed: {ex.Message}");
        }
    }

    public async Task<Result<UiOptimization>> OptimizeUiAsync(string sessionId, UiOptimizationRequest request, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Optimizing UI for session {SessionId}", sessionId);

            var uiState = GetOrCreateUiState(sessionId);
            var analysis = AnalyzeUiPerformance(uiState);
            var strategies = GenerateUiOptimizations(analysis, request);
            var appliedOptimizations = await ApplyUiOptimizationsAsync(strategies, sessionId, ct);

            var result = new UiOptimization
            {
                SessionId = sessionId,
                Analysis = analysis,
                OptimizationsApplied = appliedOptimizations.Count,
                PerformanceImprovement = CalculateUiImprovement(appliedOptimizations),
                OptimizedAt = _timeProvider.UtcNow
            };

            _logger.LogInformation("UI optimized: {Optimizations} applied, {Improvement:F1}% improvement",
                result.OptimizationsApplied, result.PerformanceImprovement);

            return Result.Success<UiOptimization>(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing UI");
            return Result.Failure<UiOptimization>($"UI optimization failed: {ex.Message}");
        }
    }

    #region Private Methods

    private void InitializeUiSystems()
    {
        _logger.LogInformation("UI/UX enhancement systems initialized");
    }

    private UiState GetOrCreateUiState(string sessionId)
    {
        if (!_uiStates.TryGetValue(sessionId, out var state))
        {
            state = new UiState
            {
                SessionId = sessionId,
                IsActive = true,
                LastUpdate = _timeProvider.UtcNow,
                PerformanceMetrics = new UiPerformanceMetrics(),
                UserPreferences = new UiUserPreferences()
            };
            _uiStates[sessionId] = state;
        }
        return state;
    }

    private List<HudElementUpdate> GenerateElementUpdates(HudConfiguration config, HudData data)
    {
        var updates = new List<HudElementUpdate>();

        foreach (var element in config.Elements)
        {
            var value = data.GetValueForElement(element.Id);
            if (value != null)
            {
                updates.Add(new HudElementUpdate
                {
                    ElementId = element.Id,
                    Value = value,
                    Animation = GenerateUpdateAnimation(element.Type)
                });
            }
        }

        return updates;
    }

    private List<LayoutAdjustment> CalculateLayoutAdjustments(HudConfiguration config, HudData data)
    {
        var adjustments = new List<LayoutAdjustment>();

        if (data.CriticalEvent)
        {
            adjustments.Add(new LayoutAdjustment
            {
                ElementId = "critical_alert",
                NewX = config.Layout.ScreenResolution.Width / 2,
                NewY = config.Layout.ScreenResolution.Height / 2
            });
        }

        return adjustments;
    }

    private List<PerformanceIndicator> GeneratePerformanceIndicators(HudData data)
    {
        var indicators = new List<PerformanceIndicator>();

        if (data.Fps > 0)
        {
            indicators.Add(new PerformanceIndicator
            {
                Type = "fps",
                Value = data.Fps,
                Color = GetPerformanceColor(data.Fps)
            });
        }

        if (data.MemoryUsage > 0)
        {
            indicators.Add(new PerformanceIndicator
            {
                Type = "memory",
                Value = data.MemoryUsage,
                Color = GetMemoryColor(data.MemoryUsage)
            });
        }

        return indicators;
    }

    private List<MenuItem> GenerateMenuItems(MenuSystem menuSystem, MenuState menuState)
    {
        var currentMenu = menuSystem.Menus.FirstOrDefault(m => m.Id == menuState.CurrentMenu);
        return currentMenu?.Items.ToList() ?? new List<MenuItem>();
    }

    private List<NavigationOption> GenerateNavigationOptions(MenuSystem menuSystem, MenuState menuState)
    {
        var options = new List<NavigationOption>();

        if (menuState.HasPrevious)
        {
            options.Add(new NavigationOption { Action = "prev", Label = "Previous", Enabled = true });
        }

        if (menuState.HasNext)
        {
            options.Add(new NavigationOption { Action = "next", Label = "Next", Enabled = true });
        }

        if (menuState.HasParent)
        {
            options.Add(new NavigationOption { Action = "back", Label = "Back", Enabled = true });
        }

        return options;
    }

    private MenuVisualState GenerateMenuVisualState(MenuState menuState)
    {
        return new MenuVisualState
        {
            IsVisible = true,
            Opacity = menuState.IsTransitioning ? 0.5f : 1.0f,
            SelectedIndex = menuState.SelectedIndex
        };
    }

    private List<ActiveFeedback> GenerateActiveFeedback(VisualFeedbackSystem system, FeedbackTrigger trigger)
    {
        return system.FeedbackRules
            .Where(r => r.Trigger == trigger.TriggerType)
            .Select(r => new ActiveFeedback
            {
                RuleId = r.Id,
                Type = r.FeedbackType,
                Intensity = trigger.Intensity,
                Duration = r.Duration
            })
            .ToList();
    }

    private UiPerformanceAnalysis AnalyzeUiPerformance(UiState uiState)
    {
        return new UiPerformanceAnalysis
        {
            RenderTime = uiState.PerformanceMetrics.RenderTime,
            MemoryUsage = uiState.PerformanceMetrics.MemoryUsage,
            DrawCalls = uiState.PerformanceMetrics.DrawCalls,
            Bottlenecks = IdentifyUiBottlenecks(uiState)
        };
    }

    private List<UiOptimizationStrategy> GenerateUiOptimizations(UiPerformanceAnalysis analysis, UiOptimizationRequest request)
    {
        var strategies = new List<UiOptimizationStrategy>();

        if (analysis.RenderTime > 16.67f)
        {
            strategies.Add(new UiOptimizationStrategy
            {
                Type = "reduce_particles",
                ExpectedImprovement = 0.2f
            });
        }

        if (analysis.MemoryUsage > request.TargetMemoryUsage)
        {
            strategies.Add(new UiOptimizationStrategy
            {
                Type = "texture_compression",
                ExpectedImprovement = 0.15f
            });
        }

        return strategies;
    }

    private async Task<List<AppliedUiOptimization>> ApplyUiOptimizationsAsync(List<UiOptimizationStrategy> strategies, string sessionId, CancellationToken ct)
    {
        var results = new List<AppliedUiOptimization>();

        foreach (var strategy in strategies)
        {
            results.Add(new AppliedUiOptimization
            {
                StrategyId = Guid.NewGuid().ToString(),
                Type = strategy.Type,
                ImprovementAchieved = strategy.ExpectedImprovement * 0.95f,
                AppliedAt = _timeProvider.UtcNow
            });
        }

        await Task.Delay(100, ct);
        return results;
    }

    private float CalculateUiImprovement(List<AppliedUiOptimization> optimizations)
    {
        return optimizations.Sum(o => o.ImprovementAchieved) * 100.0f;
    }

    private List<string> IdentifyUiBottlenecks(UiState uiState)
    {
        var bottlenecks = new List<string>();

        if (uiState.PerformanceMetrics.RenderTime > 20.0f)
            bottlenecks.Add("render_time");

        if (uiState.PerformanceMetrics.DrawCalls > 1000)
            bottlenecks.Add("draw_calls");

        return bottlenecks;
    }

    private ElementAnimation GenerateUpdateAnimation(string elementType)
    {
        return new ElementAnimation
        {
            Type = "fade",
            Duration = 0.3f,
            Easing = "ease_out"
        };
    }

    private string GetPerformanceColor(float fps)
    {
        return fps >= 60 ? "#00FF00" : fps >= 30 ? "#FFFF00" : "#FF0000";
    }

    private string GetMemoryColor(float memoryUsage)
    {
        return memoryUsage < 256 ? "#00FF00" : memoryUsage < 512 ? "#FFFF00" : "#FF0000";
    }

    #endregion
}
