using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.Performance.Commands;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the AI-powered memory auto-discovery overlay.
/// Provides guided discovery with real-time results and confidence scoring.
/// </summary>
public partial class AutoDiscoveryOverlayViewModel : ObservableObject, IAsyncDisposable
{
    private readonly IAutoDiscoveryEngine? _discoveryEngine;
    private readonly IMediator? _mediator;
    private readonly ILogger<AutoDiscoveryOverlayViewModel>? _logger;
    private readonly ITimeProvider? _timeProvider;
    private DiscoverySession? _currentSession;
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;

    #region Observable Properties

    [ObservableProperty]
    private Guid _gameId;

    [ObservableProperty]
    private int _processId;

    [ObservableProperty]
    private string _gameTitle = "Unknown Game";

    [ObservableProperty]
    private string _statusMessage = "Ready to start discovery";

    [ObservableProperty]
    private bool _isDiscovering;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private bool _canReportAction;

    [ObservableProperty]
    private int _candidateCount;

    [ObservableProperty]
    private int _sessionPass;

    [ObservableProperty]
    private double _overallProgress;

    /// <summary>
    /// Collection of discovered values ranked by confidence.
    /// </summary>
    public ObservableCollection<DiscoveredValueViewModel> DiscoveredValues { get; } = new();

    /// <summary>
    /// Collection of player action buttons.
    /// </summary>
    public ObservableCollection<PlayerActionButtonViewModel> ActionButtons { get; } = new();

    #endregion

    #region Constructors

    /// <summary>
    /// Design-time constructor with sample data.
    /// </summary>
    public AutoDiscoveryOverlayViewModel()
    {
        // Sample data for XAML designer
        GameTitle = "Sample Game (Design Time)";
        StatusMessage = "Design time mode";
        IsDiscovering = true;
        CandidateCount = 42;
        SessionPass = 3;
        OverallProgress = 0.65;

        // Sample discovered values
        DiscoveredValues.Add(new DiscoveredValueViewModel(
            new DiscoveredValue
            {
                Address = new IntPtr(0x00FF3420),
                ValueType = "Int32",
                CurrentValue = 85,
                ConfidenceScore = 0.92,
                SuggestedName = "Health",
                Category = "Health"
            }, _mediator, Guid.Empty, 0, null, Guid.Empty));

        DiscoveredValues.Add(new DiscoveredValueViewModel(
            new DiscoveredValue
            {
                Address = new IntPtr(0x00FF5620),
                ValueType = "Int32",
                CurrentValue = 1500,
                ConfidenceScore = 0.78,
                SuggestedName = "Gold/Credits",
                Category = "Currency"
            }, _mediator, Guid.Empty, 0, null, Guid.Empty));

        DiscoveredValues.Add(new DiscoveredValueViewModel(
            new DiscoveredValue
            {
                Address = new IntPtr(0x00FF8920),
                ValueType = "Int32",
                CurrentValue = 30,
                ConfidenceScore = 0.65,
                SuggestedName = "Ammo Count",
                Category = "Ammo"
            }, _mediator, Guid.Empty, 0, null, Guid.Empty));

        InitializeActionButtons();
    }

    /// <summary>
    /// Runtime constructor.
    /// </summary>
    public AutoDiscoveryOverlayViewModel(
        IAutoDiscoveryEngine discoveryEngine,
        IMediator mediator,
        ILogger<AutoDiscoveryOverlayViewModel> logger,
        ITimeProvider timeProvider)
    {
        _discoveryEngine = discoveryEngine ?? throw new ArgumentNullException(nameof(discoveryEngine));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        InitializeActionButtons();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the view model for a specific game process.
    /// </summary>
    public async Task InitializeAsync(Guid gameId, int processId, string gameTitle)
    {
        GameId = gameId;
        ProcessId = processId;
        GameTitle = $"Auto-Discovery - {gameTitle}";

        _logger?.LogInformation("Initializing auto-discovery for game {GameId}, process {ProcessId}", gameId, processId);

        // Start discovery session
        await StartDiscoverySessionAsync();
    }

    private void InitializeActionButtons()
    {
        ActionButtons.Clear();

        ActionButtons.Add(new PlayerActionButtonViewModel("😱 Take Damage", "Report that your character took damage", PlayerAction.TookDamage, this));
        ActionButtons.Add(new PlayerActionButtonViewModel("💚 Heal", "Report that your character was healed", PlayerAction.Healed, this));
        ActionButtons.Add(new PlayerActionButtonViewModel("💸 Spend Money", "Report that you spent money/currency", PlayerAction.SpentMoney, this));
        ActionButtons.Add(new PlayerActionButtonViewModel("💰 Earn Money", "Report that you earned money/currency", PlayerAction.EarnedMoney, this));
        ActionButtons.Add(new PlayerActionButtonViewModel("🔫 Use Ammo", "Report that you fired a weapon/used ammo", PlayerAction.UsedAmmo, this));
        ActionButtons.Add(new PlayerActionButtonViewModel("🔄 Reload", "Report that you reloaded your weapon", PlayerAction.Reloaded, this));
        ActionButtons.Add(new PlayerActionButtonViewModel("⭐ Gain XP", "Report that you gained experience points", PlayerAction.GainedXp, this));
        ActionButtons.Add(new PlayerActionButtonViewModel("🏆 Level Up", "Report that you leveled up", PlayerAction.LeveledUp, this));
        ActionButtons.Add(new PlayerActionButtonViewModel("🎯 Score +", "Report that your score increased", PlayerAction.ScoreIncreased, this));
        ActionButtons.Add(new PlayerActionButtonViewModel("📍 Moved", "Report that your position changed", PlayerAction.PositionChanged, this));
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task StartDiscoverySessionAsync()
    {
        if (_discoveryEngine == null || IsDiscovering)
            return;

        try
        {
            IsDiscovering = true;
            StatusMessage = "Starting discovery session...";

            var options = new DiscoveryOptions
            {
                MaxCandidates = 5000,
                MinConfidenceThreshold = 0.3,
                ScanIntegers = true,
                ScanFloats = true,
                MaxResults = 20
            };

            var result = await _discoveryEngine.StartDiscoverySessionAsync(ProcessId, options);

            if (result.IsSuccess)
            {
                _currentSession = result.Value;
                StatusMessage = "Discovery session started. Perform in-game actions and report them to narrow down values.";
                CanReportAction = true;

                // Start monitoring for real-time updates
                StartMonitoring();
            }
            else
            {
                StatusMessage = $"Failed to start discovery: {result.Error}";
                IsDiscovering = false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting discovery session");
            StatusMessage = $"Error: {ex.Message}";
            IsDiscovering = false;
        }
    }

    [RelayCommand]
    public async Task ReportActionAsync(PlayerAction action)
    {
        if (_discoveryEngine == null || _currentSession == null || !CanReportAction)
            return;

        try
        {
            CanReportAction = false;
            StatusMessage = $"Analyzing {GetActionDisplayName(action)}...";

            var result = await _discoveryEngine.AnalyzeChangeAsync(_currentSession, action);

            if (result.IsSuccess)
            {
                var discoveryResult = result.Value;
                CandidateCount = discoveryResult.RemainingCandidates;
                SessionPass = _currentSession.CurrentPass;
                OverallProgress = Math.Min(1.0, _currentSession.CurrentPass / 10.0);

                StatusMessage = $"Analyzed {GetActionDisplayName(action)}. " +
                    $"Remaining candidates: {discoveryResult.RemainingCandidates}, " +
                    $"Eliminated: {discoveryResult.EliminatedCandidates}";

                // Update discovered values
                await RefreshDiscoveredValuesAsync();
            }
            else
            {
                StatusMessage = $"Analysis failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error reporting action {Action}", action);
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            CanReportAction = true;
        }
    }

    [RelayCommand]
    private async Task StopDiscoveryAsync()
    {
        if (_discoveryEngine == null || _currentSession == null)
            return;

        try
        {
            StopMonitoring();
            await _discoveryEngine.StopDiscoverySessionAsync(_currentSession);
            IsDiscovering = false;
            CanReportAction = false;
            StatusMessage = "Discovery session stopped.";
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping discovery session");
        }
    }

    [RelayCommand]
    private void Close()
    {
        _ = StopDiscoveryAsync();
        // Trigger close event - handled by view
    }

    #endregion

    #region Monitoring

    private void StartMonitoring()
    {
        if (IsMonitoring || _discoveryEngine == null || _currentSession == null)
            return;

        IsMonitoring = true;
        _monitoringCts = new CancellationTokenSource();

        _monitoringTask = Task.Run(async () =>
        {
            while (!_monitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    if (_currentSession?.IsActive == true)
                    {
                        await RefreshDiscoveredValuesAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in monitoring loop");
                }

                await Task.Delay(1000, _monitoringCts.Token);
            }
        }, _monitoringCts.Token);
    }

    private void StopMonitoring()
    {
        if (!IsMonitoring)
            return;

        IsMonitoring = false;
        _monitoringCts?.Cancel();
    }

    private async Task RefreshDiscoveredValuesAsync()
    {
        if (_discoveryEngine == null || _currentSession == null)
            return;

        var result = await _discoveryEngine.GetRankedResultsAsync(_currentSession);

        if (!result.IsSuccess || result.Value == null)
            return;

        var newValues = result.Value;

        // Update existing and add new
        foreach (var value in newValues)
        {
            var existing = DiscoveredValues.FirstOrDefault(v => v.Address == value.Address);
            if (existing != null)
            {
                existing.UpdateFrom(value);
            }
            else
            {
                DiscoveredValues.Add(new DiscoveredValueViewModel(
                    value, _mediator, GameId, ProcessId, _discoveryEngine, _currentSession?.SessionId ?? Guid.Empty));
            }
        }

        // Remove values that are no longer candidates
        var addresses = newValues.Select(v => v.Address).ToHashSet();
        var toRemove = DiscoveredValues.Where(v => !addresses.Contains(v.Address)).ToList();
        foreach (var vm in toRemove)
        {
            DiscoveredValues.Remove(vm);
        }

        // Sort by confidence
        var sorted = DiscoveredValues.OrderByDescending(v => v.ConfidenceScore).ToList();
        DiscoveredValues.Clear();
        foreach (var vm in sorted)
        {
            DiscoveredValues.Add(vm);
        }
    }

    #endregion

    #region Helper Methods

    private static string GetActionDisplayName(PlayerAction action)
    {
        return action switch
        {
            PlayerAction.TookDamage => "taking damage",
            PlayerAction.Healed => "healing",
            PlayerAction.SpentMoney => "spending money",
            PlayerAction.EarnedMoney => "earning money",
            PlayerAction.UsedAmmo => "using ammo",
            PlayerAction.Reloaded => "reloading",
            PlayerAction.GainedXp => "gaining XP",
            PlayerAction.LeveledUp => "leveling up",
            PlayerAction.PositionChanged => "moving",
            PlayerAction.ScoreIncreased => "scoring",
            _ => action.ToString()
        };
    }

    #endregion

    #region IAsyncDisposable

    public async ValueTask DisposeAsync()
    {
        StopMonitoring();

        if (_monitoringTask != null)
        {
            try
            {
                await _monitoringTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        _monitoringCts?.Dispose();

        if (_discoveryEngine != null && _currentSession != null)
        {
            await _discoveryEngine.StopDiscoverySessionAsync(_currentSession);
        }
    }

    #endregion
}

/// <summary>
/// ViewModel for a player action button.
/// </summary>
public partial class PlayerActionButtonViewModel : ObservableObject
{
    private readonly AutoDiscoveryOverlayViewModel _parent;

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private string _tooltip;

    [ObservableProperty]
    private PlayerAction _action;

    [ObservableProperty]
    private bool _isEnabled = true;

    public PlayerActionButtonViewModel(string displayName, string tooltip, PlayerAction action, AutoDiscoveryOverlayViewModel parent)
    {
        _displayName = displayName;
        _tooltip = tooltip;
        _action = action;
        _parent = parent;
    }

    [RelayCommand]
    private async Task ExecuteAsync()
    {
        if (_parent != null)
        {
            await _parent.ReportActionAsync(Action);
        }
    }
}

/// <summary>
/// ViewModel for a discovered memory value.
/// </summary>
public partial class DiscoveredValueViewModel : ObservableObject
{
    private readonly IMediator? _mediator;
    private readonly Guid _gameId;
    private readonly int _processId;
    private readonly IAutoDiscoveryEngine? _discoveryEngine;
    private readonly Guid _sessionId;

    [ObservableProperty]
    private IntPtr _address;

    [ObservableProperty]
    private string _addressHex = string.Empty;

    [ObservableProperty]
    private string _valueType = "Unknown";

    [ObservableProperty]
    private string _currentValue = "---";

    [ObservableProperty]
    private double _confidenceScore;

    [ObservableProperty]
    private string _suggestedName = "Unknown";

    [ObservableProperty]
    private string _category = "Unknown";

    [ObservableProperty]
    private int _observationCount;

    [ObservableProperty]
    private bool _isConfirmed;

    [ObservableProperty]
    private bool _isAddedToWatch;

    [ObservableProperty]
    private string _confidenceColor = "#FFFFD700"; // Gold default

    public DiscoveredValueViewModel(
        DiscoveredValue value,
        IMediator? mediator,
        Guid gameId,
        int processId,
        IAutoDiscoveryEngine? discoveryEngine,
        Guid sessionId)
    {
        _mediator = mediator;
        _gameId = gameId;
        _processId = processId;
        _discoveryEngine = discoveryEngine;
        _sessionId = sessionId;

        UpdateFrom(value);
    }

    public void UpdateFrom(DiscoveredValue value)
    {
        Address = value.Address;
        AddressHex = $"0x{value.Address:X8}";
        ValueType = value.ValueType;
        CurrentValue = value.CurrentValue?.ToString() ?? "---";
        ConfidenceScore = value.ConfidenceScore;
        SuggestedName = value.SuggestedName;
        Category = value.Category;
        ObservationCount = value.ObservationCount;
        IsConfirmed = value.IsConfirmed;

        UpdateConfidenceColor();
    }

    partial void OnConfidenceScoreChanged(double value)
    {
        UpdateConfidenceColor();
    }

    private void UpdateConfidenceColor()
    {
        ConfidenceColor = ConfidenceScore switch
        {
            >= 0.8 => "#FF00FF00",   // Green - High confidence
            >= 0.6 => "#FF90EE90",   // Light green
            >= 0.4 => "#FFFFD700",   // Gold - Medium
            >= 0.2 => "#FFFFA500",   // Orange
            _ => "#FFFF0000"         // Red - Low confidence
        };
    }

    [RelayCommand]
    private async Task AddToWatchAsync()
    {
        if (_mediator == null)
            return;

        try
        {
            if (!Enum.TryParse<MemoryDataType>(ValueType, true, out var dataType))
            {
                dataType = MemoryDataType.Int32;
            }

            var command = new AddMemoryWatchCommand(
                _gameId,
                SuggestedName,
                Address.ToInt64(),
                dataType);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                IsAddedToWatch = true;
            }
        }
        catch (Exception ex)
        {
            // Log error
        }
    }

    [RelayCommand]
    private async Task ConfirmCorrectAsync()
    {
        if (_discoveryEngine == null || _sessionId == Guid.Empty)
            return;

        var feedback = new DiscoveryFeedback
        {
            SessionId = _sessionId,
            Address = Address,
            WasCorrect = true,
            CorrectName = SuggestedName,
            CorrectCategory = Category
        };

        await _discoveryEngine.SubmitFeedbackAsync(feedback);
        IsConfirmed = true;
    }

    [RelayCommand]
    private async Task ReportIncorrectAsync()
    {
        if (_discoveryEngine == null || _sessionId == Guid.Empty)
            return;

        var feedback = new DiscoveryFeedback
        {
            SessionId = _sessionId,
            Address = Address,
            WasCorrect = false
        };

        await _discoveryEngine.SubmitFeedbackAsync(feedback);
    }
}
