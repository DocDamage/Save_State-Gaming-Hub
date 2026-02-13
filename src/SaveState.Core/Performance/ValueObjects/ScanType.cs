namespace SaveState.Core.Performance.ValueObjects;

/// <summary>
/// Type of memory scan to perform.
/// </summary>
public enum ScanType
{
    /// <summary>Exact value search.</summary>
    ExactValue,
    /// <summary>Value is greater than X.</summary>
    GreaterThan,
    /// <summary>Value is less than X.</summary>
    LessThan,
    /// <summary>Value is between X and Y.</summary>
    ValueBetween,
    /// <summary>Initial scan for unknown value.</summary>
    UnknownInitialValue,
    /// <summary>Subsequent scan for values that increased since last scan.</summary>
    Increased,
    /// <summary>Subsequent scan for values that decreased since last scan.</summary>
    Decreased,
    /// <summary>Subsequent scan for values that changed since last scan.</summary>
    Changed,
    /// <summary>Subsequent scan for values that remained the same since last scan.</summary>
    Unchanged
}
