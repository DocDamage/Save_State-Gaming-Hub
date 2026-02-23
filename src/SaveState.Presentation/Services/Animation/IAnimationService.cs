using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace SaveState.Presentation.Services.Animation;

/// <summary>
/// Direction options for slide animations.
/// </summary>
public enum SlideDirection
{
    Left,
    Right,
    Up,
    Down
}

/// <summary>
/// Types of page transitions available.
/// </summary>
public enum PageTransitionType
{
    Fade,
    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,
    Scale,
    Crossfade,
    Flip,
    SharedElement
}

/// <summary>
/// Service for managing UI animations and transitions throughout the application.
/// 
/// Responsibilities:
/// - Provides smooth transitions for UI elements (fade, slide, scale)
/// - Handles page navigation transitions
/// - Implements micro-interactions (pulse, shake, bounce)
/// - Manages loading states with skeletons and spinners
/// - Animates list operations (add, remove, reorder)
/// - Provides scroll animations
/// - Animates value changes (double, color)
/// - Respects reduced motion preferences
/// 
/// All animations are non-blocking async operations that run at 60fps.
/// Timing follows Material Design 3 specifications.
/// </summary>
public interface IAnimationService
{
    #region Transitions

    /// <summary>
    /// Fades in an element from transparent to fully opaque.
    /// </summary>
    /// <param name="element">The control to animate.</param>
    /// <param name="duration">Optional duration. Default is 300ms.</param>
    Task FadeInAsync(Control element, TimeSpan? duration = null);

    /// <summary>
    /// Fades out an element from fully opaque to transparent.
    /// </summary>
    /// <param name="element">The control to animate.</param>
    /// <param name="duration">Optional duration. Default is 200ms.</param>
    Task FadeOutAsync(Control element, TimeSpan? duration = null);

    /// <summary>
    /// Slides an element in from the specified direction.
    /// </summary>
    /// <param name="element">The control to animate.</param>
    /// <param name="direction">The direction to slide from.</param>
    /// <param name="duration">Optional duration. Default is 350ms.</param>
    Task SlideInAsync(Control element, SlideDirection direction, TimeSpan? duration = null);

    /// <summary>
    /// Slides an element out in the specified direction.
    /// </summary>
    /// <param name="element">The control to animate.</param>
    /// <param name="direction">The direction to slide to.</param>
    /// <param name="duration">Optional duration. Default is 300ms.</param>
    Task SlideOutAsync(Control element, SlideDirection direction, TimeSpan? duration = null);

    /// <summary>
    /// Scales an element in from 0 to 1 with fade.
    /// </summary>
    /// <param name="element">The control to animate.</param>
    /// <param name="duration">Optional duration. Default is 300ms.</param>
    Task ScaleInAsync(Control element, TimeSpan? duration = null);

    /// <summary>
    /// Scales an element out from 1 to 0 with fade.
    /// </summary>
    /// <param name="element">The control to animate.</param>
    /// <param name="duration">Optional duration. Default is 250ms.</param>
    Task ScaleOutAsync(Control element, TimeSpan? duration = null);

    #endregion

    #region Page Transitions

    /// <summary>
    /// Animates a forward navigation transition between two pages.
    /// </summary>
    /// <param name="fromPage">The page transitioning from.</param>
    /// <param name="toPage">The page transitioning to.</param>
    Task NavigateForwardAsync(Control fromPage, Control toPage);

    /// <summary>
    /// Animates a backward navigation transition between two pages.
    /// </summary>
    /// <param name="fromPage">The page transitioning from.</param>
    /// <param name="toPage">The page transitioning to.</param>
    Task NavigateBackAsync(Control fromPage, Control toPage);

    /// <summary>
    /// Animates a modal appearing over a background.
    /// </summary>
    /// <param name="background">The background overlay.</param>
    /// <param name="modal">The modal content.</param>
    Task NavigateModalAsync(Control background, Control modal);

    /// <summary>
    /// Animates a modal dismissing.
    /// </summary>
    /// <param name="background">The background overlay.</param>
    /// <param name="modal">The modal content.</param>
    Task DismissModalAsync(Control background, Control modal);

    #endregion

    #region Micro-interactions

    /// <summary>
    /// Creates a pulse animation on an element.
    /// </summary>
    /// <param name="element">The control to animate.</param>
    Task PulseAsync(Control element);

    /// <summary>
    /// Creates a shake animation on an element (useful for errors).
    /// </summary>
    /// <param name="element">The control to animate.</param>
    Task ShakeAsync(Control element);

    /// <summary>
    /// Creates a bounce animation on an element.
    /// </summary>
    /// <param name="element">The control to animate.</param>
    Task BounceAsync(Control element);

    /// <summary>
    /// Creates a highlight flash animation on an element.
    /// </summary>
    /// <param name="element">The control to animate.</param>
    Task HighlightAsync(Control element);

    /// <summary>
    /// Creates a ripple effect originating from a specific point.
    /// </summary>
    /// <param name="element">The control to animate.</param>
    /// <param name="origin">The origin point of the ripple.</param>
    Task RippleAsync(Control element, Point origin);

    #endregion

    #region Loading States

    /// <summary>
    /// Shows a skeleton loading state on a container.
    /// </summary>
    /// <param name="container">The container to show skeleton in.</param>
    Task ShowSkeletonAsync(Control container);

    /// <summary>
    /// Hides the skeleton loading state.
    /// </summary>
    /// <param name="container">The container to hide skeleton from.</param>
    Task HideSkeletonAsync(Control container);

    /// <summary>
    /// Shows a loading spinner on an element.
    /// </summary>
    /// <param name="element">The element to show spinner on.</param>
    /// <param name="message">Optional loading message.</param>
    Task ShowSpinnerAsync(Control element, string? message = null);

    /// <summary>
    /// Hides the loading spinner.
    /// </summary>
    /// <param name="element">The element to hide spinner from.</param>
    Task HideSpinnerAsync(Control element);

    #endregion

    #region List Animations

    /// <summary>
    /// Animates adding an item to a list.
    /// </summary>
    /// <param name="list">The items control.</param>
    /// <param name="item">The item being added.</param>
    Task AnimateListAddAsync(ItemsControl list, Control item);

    /// <summary>
    /// Animates removing an item from a list.
    /// </summary>
    /// <param name="list">The items control.</param>
    /// <param name="item">The item being removed.</param>
    Task AnimateListRemoveAsync(ItemsControl list, Control item);

    /// <summary>
    /// Animates reordering items in a list.
    /// </summary>
    /// <param name="list">The items control.</param>
    Task AnimateListReorderAsync(ItemsControl list);

    #endregion

    #region Scroll Animations

    /// <summary>
    /// Smoothly scrolls to a specific offset.
    /// </summary>
    /// <param name="scrollViewer">The scroll viewer.</param>
    /// <param name="offset">The target offset.</param>
    /// <param name="animated">Whether to animate the scroll.</param>
    Task ScrollToAsync(ScrollViewer scrollViewer, double offset, bool animated = true);

    /// <summary>
    /// Smoothly scrolls to bring an element into view.
    /// </summary>
    /// <param name="scrollViewer">The scroll viewer.</param>
    /// <param name="element">The element to scroll to.</param>
    Task ScrollToElementAsync(ScrollViewer scrollViewer, Control element);

    #endregion

    #region Value Animations

    /// <summary>
    /// Animates a double property from one value to another.
    /// </summary>
    /// <param name="element">The control owning the property.</param>
    /// <param name="property">The property to animate.</param>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The ending value.</param>
    /// <param name="duration">Optional duration. Default is 300ms.</param>
    Task AnimateDoubleAsync(Control element, AvaloniaProperty property, double from, double to, TimeSpan? duration = null);

    /// <summary>
    /// Animates a color property from one value to another.
    /// </summary>
    /// <param name="element">The control owning the property.</param>
    /// <param name="property">The property to animate.</param>
    /// <param name="from">The starting color.</param>
    /// <param name="to">The ending color.</param>
    /// <param name="duration">Optional duration. Default is 300ms.</param>
    Task AnimateColorAsync(Control element, AvaloniaProperty property, Color from, Color to, TimeSpan? duration = null);

    #endregion

    #region Easing Functions

    /// <summary>
    /// Gets the default easing function (CubicEaseOut).
    /// </summary>
    IEasing DefaultEasing { get; }

    /// <summary>
    /// Gets the bounce easing function.
    /// </summary>
    IEasing BounceEasing { get; }

    /// <summary>
    /// Gets the elastic easing function.
    /// </summary>
    IEasing ElasticEasing { get; }

    #endregion

    /// <summary>
    /// Gets whether reduced motion is preferred by the user.
    /// </summary>
    bool IsReducedMotionPreferred { get; }
}
