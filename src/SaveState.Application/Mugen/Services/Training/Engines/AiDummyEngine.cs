namespace SaveState.Application.Mugen.Services.Training.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Manages AI dummy behavior and state for training.
/// </summary>
public class AiDummyEngine
{
    private readonly ILogger<AiDummyEngine> _logger;
    private readonly Dictionary<string, DummyState> _dummyStates = new();
    private readonly Dictionary<string, DummySettings> _dummySettings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AiDummyEngine"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AiDummyEngine(ILogger<AiDummyEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a new AI dummy with the specified settings.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    /// <param name="settings">The dummy settings.</param>
    /// <returns>The created dummy state.</returns>
    public DummyState CreateDummy(string dummyId, DummySettings? settings = null)
    {
        settings ??= new DummySettings();

        var state = new DummyState
        {
            DummyId = dummyId,
            CurrentBehavior = settings.Behavior,
            CurrentHealth = 1000,
            MaxHealth = 1000,
            CurrentGuardBar = settings.GuardBar,
            MaxGuardBar = 1000,
            IsBlocking = settings.Behavior == DummyBehavior.Block || settings.Behavior == DummyBehavior.BlockAll,
            IsHit = false,
            IsKnockedDown = false,
            StateTime = TimeSpan.Zero,
            CurrentActionIndex = 0,
            TimesHit = 0,
            ComboHitsTaken = 0,
            TotalDamageTaken = 0
        };

        _dummyStates[dummyId] = state;
        _dummySettings[dummyId] = settings;

        _logger.LogInformation("Created AI dummy {DummyId} with behavior {Behavior}", dummyId, settings.Behavior);

        return state;
    }

    /// <summary>
    /// Gets a dummy's current state.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    /// <returns>The dummy state if found.</returns>
    public DummyState? GetDummyState(string dummyId)
    {
        return _dummyStates.TryGetValue(dummyId, out var state) ? state : null;
    }

    /// <summary>
    /// Gets a dummy's settings.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    /// <returns>The dummy settings if found.</returns>
    public DummySettings? GetDummySettings(string dummyId)
    {
        return _dummySettings.TryGetValue(dummyId, out var settings) ? settings : null;
    }

    /// <summary>
    /// Updates the dummy's behavior.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    /// <param name="behavior">The new behavior.</param>
    /// <returns>True if updated.</returns>
    public bool SetBehavior(string dummyId, DummyBehavior behavior)
    {
        if (_dummySettings.TryGetValue(dummyId, out var settings) &&
            _dummyStates.TryGetValue(dummyId, out var state))
        {
            settings.Behavior = behavior;
            state.CurrentBehavior = behavior;
            state.IsBlocking = behavior == DummyBehavior.Block || behavior == DummyBehavior.BlockAll;

            _logger.LogDebug("Set dummy {DummyId} behavior to {Behavior}", dummyId, behavior);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Processes when the dummy takes a hit.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    /// <param name="damage">The damage amount.</param>
    /// <param name="isPartOfCombo">Whether this is part of a combo.</param>
    public void TakeHit(string dummyId, int damage, bool isPartOfCombo = false)
    {
        if (!_dummyStates.TryGetValue(dummyId, out var state) ||
            !_dummySettings.TryGetValue(dummyId, out var settings))
        {
            return;
        }

        state.IsHit = true;
        state.TimesHit++;

        if (isPartOfCombo)
        {
            state.ComboHitsTaken++;
        }
        else
        {
            state.ComboHitsTaken = 1;
        }

        if (state.IsBlocking && (settings.Behavior == DummyBehavior.BlockAll || Random.Shared.Next(100) < settings.GuardLevel))
        {
            var chipDamage = damage / 10;
            state.CurrentHealth = Math.Max(0, state.CurrentHealth - chipDamage);
            state.CurrentGuardBar = Math.Max(0, state.CurrentGuardBar - damage);
            state.TotalDamageTaken += chipDamage;
        }
        else
        {
            state.CurrentHealth = Math.Max(0, state.CurrentHealth - damage);
            state.TotalDamageTaken += damage;
        }

        if (state.CurrentGuardBar <= 0)
        {
            state.IsBlocking = false;
        }

        _logger.LogDebug("Dummy {DummyId} took {Damage} damage, health now {Health}",
            dummyId, damage, state.CurrentHealth);
    }

    /// <summary>
    /// Processes when the dummy is knocked down.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    public void KnockDown(string dummyId)
    {
        if (_dummyStates.TryGetValue(dummyId, out var state))
        {
            state.IsKnockedDown = true;
            state.IsBlocking = false;
            _logger.LogDebug("Dummy {DummyId} knocked down", dummyId);
        }
    }

    /// <summary>
    /// Recovers the dummy from knockdown.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    public void Recover(string dummyId)
    {
        if (!_dummyStates.TryGetValue(dummyId, out var state) ||
            !_dummySettings.TryGetValue(dummyId, out var settings))
        {
            return;
        }

        state.IsKnockedDown = false;
        state.IsHit = false;
        state.ComboHitsTaken = 0;

        if (settings.RandomReversal && Random.Shared.NextDouble() < settings.ReversalChance)
        {
            state.CurrentBehavior = DummyBehavior.Reversal;
            _logger.LogDebug("Dummy {DummyId} performed reversal", dummyId);
        }
        else if (settings.Behavior == DummyBehavior.Recovery)
        {
            state.CurrentBehavior = DummyBehavior.Stand;
        }
        else
        {
            state.CurrentBehavior = settings.Behavior;
        }

        if (settings.Behavior == DummyBehavior.Block || settings.Behavior == DummyBehavior.BlockAll)
        {
            state.IsBlocking = true;
        }

        _logger.LogDebug("Dummy {DummyId} recovered", dummyId);
    }

    /// <summary>
    /// Updates the dummy state (called each frame).
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    /// <param name="deltaTime">Time since last update.</param>
    public void Update(string dummyId, TimeSpan deltaTime)
    {
        if (_dummyStates.TryGetValue(dummyId, out var state))
        {
            state.StateTime += deltaTime;

            if (state.IsHit && state.StateTime > TimeSpan.FromMilliseconds(100))
            {
                state.IsHit = false;
            }

            ProcessActionSequence(dummyId, deltaTime);
        }
    }

    /// <summary>
    /// Resets the dummy to initial state.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    public void Reset(string dummyId)
    {
        if (!_dummyStates.TryGetValue(dummyId, out var state) ||
            !_dummySettings.TryGetValue(dummyId, out var settings))
        {
            return;
        }

        state.CurrentHealth = state.MaxHealth;
        state.CurrentGuardBar = settings.GuardBar;
        state.CurrentBehavior = settings.Behavior;
        state.IsBlocking = settings.Behavior == DummyBehavior.Block || settings.Behavior == DummyBehavior.BlockAll;
        state.IsHit = false;
        state.IsKnockedDown = false;
        state.StateTime = TimeSpan.Zero;
        state.CurrentActionIndex = 0;
        state.TimesHit = 0;
        state.ComboHitsTaken = 0;
        state.TotalDamageTaken = 0;

        _logger.LogDebug("Reset dummy {DummyId}", dummyId);
    }

    /// <summary>
    /// Fully heals the dummy.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    public void FullHeal(string dummyId)
    {
        if (_dummyStates.TryGetValue(dummyId, out var state))
        {
            state.CurrentHealth = state.MaxHealth;
            _logger.LogDebug("Full heal for dummy {DummyId}", dummyId);
        }
    }

    /// <summary>
    /// Fully restores the guard bar.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    public void RestoreGuard(string dummyId)
    {
        if (!_dummyStates.TryGetValue(dummyId, out var state) ||
            !_dummySettings.TryGetValue(dummyId, out var settings))
        {
            return;
        }

        state.CurrentGuardBar = settings.GuardBar;
        if (settings.Behavior == DummyBehavior.Block || settings.Behavior == DummyBehavior.BlockAll)
        {
            state.IsBlocking = true;
        }

        _logger.LogDebug("Guard restored for dummy {DummyId}", dummyId);
    }

    /// <summary>
    /// Determines if the dummy should perform a counter action.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    /// <returns>True if counter should be performed.</returns>
    public bool ShouldCounter(string dummyId)
    {
        if (!_dummyStates.TryGetValue(dummyId, out var state) ||
            !_dummySettings.TryGetValue(dummyId, out var settings))
        {
            return false;
        }

        if (state.CurrentBehavior == DummyBehavior.Counter ||
            state.CurrentBehavior == DummyBehavior.Reversal)
        {
            return true;
        }

        if (settings.Behavior == DummyBehavior.Counter && state.ComboHitsTaken >= 2)
        {
            return Random.Shared.Next(100) < settings.GuardLevel;
        }

        return false;
    }

    /// <summary>
    /// Determines if the dummy should guard cancel.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    /// <returns>True if guard cancel should be performed.</returns>
    public bool ShouldGuardCancel(string dummyId)
    {
        if (!_dummyStates.TryGetValue(dummyId, out var state))
        {
            return false;
        }

        return state.CurrentBehavior == DummyBehavior.GuardCancel && state.IsBlocking;
    }

    /// <summary>
    /// Deletes a dummy.
    /// </summary>
    /// <param name="dummyId">The dummy ID.</param>
    /// <returns>True if deleted.</returns>
    public bool DeleteDummy(string dummyId)
    {
        _dummySettings.Remove(dummyId);
        return _dummyStates.Remove(dummyId);
    }

    private void ProcessActionSequence(string dummyId, TimeSpan deltaTime)
    {
        if (!_dummySettings.TryGetValue(dummyId, out var settings) ||
            settings.ActionSequence.Count == 0)
        {
            return;
        }

        if (!_dummyStates.TryGetValue(dummyId, out var state))
        {
            return;
        }

        if (state.CurrentActionIndex >= settings.ActionSequence.Count)
        {
            if (settings.LoopActions)
            {
                state.CurrentActionIndex = 0;
                state.StateTime = TimeSpan.Zero;
            }
            return;
        }

        var currentAction = settings.ActionSequence[state.CurrentActionIndex];

        if (state.StateTime >= currentAction.Duration)
        {
            state.CurrentActionIndex++;
            state.StateTime = TimeSpan.Zero;

            if (state.CurrentActionIndex < settings.ActionSequence.Count)
            {
                var nextAction = settings.ActionSequence[state.CurrentActionIndex];
                state.CurrentBehavior = nextAction.Behavior;
            }
            else if (settings.LoopActions)
            {
                state.CurrentActionIndex = 0;
                state.CurrentBehavior = settings.ActionSequence[0].Behavior;
            }
        }
    }
}
