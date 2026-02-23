namespace SaveState.Presentation.Services.Accessibility;

/// <summary>
/// Specifies the direction for focus navigation.
/// </summary>
public enum FocusNavigationDirection
{
    /// <summary>
    /// Move focus up.
    /// </summary>
    Up,

    /// <summary>
    /// Move focus down.
    /// </summary>
    Down,

    /// <summary>
    /// Move focus left.
    /// </summary>
    Left,

    /// <summary>
    /// Move focus right.
    /// </summary>
    Right,

    /// <summary>
    /// Move focus to the next control.
    /// </summary>
    Next,

    /// <summary>
    /// Move focus to the previous control.
    /// </summary>
    Previous,

    /// <summary>
    /// Move focus to the first control.
    /// </summary>
    First,

    /// <summary>
    /// Move focus to the last control.
    /// </summary>
    Last
}
