using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using SaveState.Presentation.Services.Animation;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.Controls;

/// <summary>
/// A content control that animates transitions between different content.
/// 
/// Usage:
/// <code>
/// &lt;local:TransitionHost x:Name="TransitionHost" Transition="Slide" Duration="0:0:0.3"&gt;
///     &lt;views:HomeView /&gt;
/// &lt;/local:TransitionHost&gt;
/// 
/// // In code-behind:
/// await TransitionHost.NavigateAsync(new GameDetailView { DataContext = game });
/// await TransitionHost.GoBackAsync();
/// </code>
/// </summary>
public partial class TransitionHost : ContentControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="Transition"/> property.
    /// </summary>
    public static readonly StyledProperty<PageTransitionType> TransitionProperty =
        AvaloniaProperty.Register<TransitionHost, PageTransitionType>(
            nameof(Transition),
            PageTransitionType.SlideRight);

    /// <summary>
    /// Defines the <see cref="Duration"/> property.
    /// </summary>
    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<TransitionHost, TimeSpan>(
            nameof(Duration),
            TimeSpan.FromMilliseconds(350));

    /// <summary>
    /// Defines the <see cref="Easing"/> property.
    /// </summary>
    public static readonly StyledProperty<Easing> EasingProperty =
        AvaloniaProperty.Register<TransitionHost, Easing>(
            nameof(Easing),
            new QuarticEaseOut());

    /// <summary>
    /// Defines the <see cref="OldContent"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> OldContentProperty =
        AvaloniaProperty.Register<TransitionHost, object?>(nameof(OldContent));

    /// <summary>
    /// Defines the <see cref="IsTransitioning"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsTransitioningProperty =
        AvaloniaProperty.Register<TransitionHost, bool>(nameof(IsTransitioning));

    /// <summary>
    /// Defines the <see cref="EnableHistory"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> EnableHistoryProperty =
        AvaloniaProperty.Register<TransitionHost, bool>(
            nameof(EnableHistory),
            true);

    /// <summary>
    /// Defines the <see cref="DisableTransitions"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> DisableTransitionsProperty =
        AvaloniaProperty.Register<TransitionHost, bool>(nameof(DisableTransitions));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the type of transition animation to use.
    /// </summary>
    public PageTransitionType Transition
    {
        get => GetValue(TransitionProperty);
        set => SetValue(TransitionProperty, value);
    }

    /// <summary>
    /// Gets or sets the duration of the transition animation.
    /// </summary>
    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    /// <summary>
    /// Gets or sets the easing function for the transition animation.
    /// </summary>
    public Easing Easing
    {
        get => GetValue(EasingProperty);
        set => SetValue(EasingProperty, value);
    }

    /// <summary>
    /// Gets the old content that is being transitioned out.
    /// </summary>
    public object? OldContent
    {
        get => GetValue(OldContentProperty);
        private set => SetValue(OldContentProperty, value);
    }

    /// <summary>
    /// Gets whether a transition is currently in progress.
    /// </summary>
    public bool IsTransitioning
    {
        get => GetValue(IsTransitioningProperty);
        private set => SetValue(IsTransitioningProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to maintain navigation history.
    /// </summary>
    public bool EnableHistory
    {
        get => GetValue(EnableHistoryProperty);
        set => SetValue(EnableHistoryProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to disable all transitions.
    /// </summary>
    public bool DisableTransitions
    {
        get => GetValue(DisableTransitionsProperty);
        set => SetValue(DisableTransitionsProperty, value);
    }

    /// <summary>
    /// Gets the navigation history stack.
    /// </summary>
    public ReadOnlyCollection<object> History => new(_history.ToList());

    /// <summary>
    /// Gets whether navigation back is possible.
    /// </summary>
    public bool CanGoBack => _history.Count > 0 && !IsTransitioning;

    #endregion

    private readonly Stack<object> _history = new();
    private ContentPresenter? _oldContentPresenter;
    private ContentPresenter? _contentPresenter;

    public TransitionHost()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _oldContentPresenter = e.NameScope.Find<ContentPresenter>("PART_OldContentPresenter");
        _contentPresenter = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
    }

    #region Navigation Methods

    /// <summary>
    /// Navigates to new content with an animation.
    /// </summary>
    /// <param name="content">The new content to display.</param>
    public async Task NavigateAsync(object content)
    {
        if (IsTransitioning) return;

        if (DisableTransitions)
        {
            PushToHistory(Content);
            Content = content;
            return;
        }

        IsTransitioning = true;

        try
        {
            // Store old content for transition
            OldContent = Content;

            // Push current to history before changing
            if (Content != null)
            {
                PushToHistory(Content);
            }

            // Set new content
            Content = content;

            // Perform transition
            await PerformTransitionAsync(isForward: true);
        }
        finally
        {
            IsTransitioning = false;
            OldContent = null;
        }
    }

    /// <summary>
    /// Navigates back to the previous content in history.
    /// </summary>
    public async Task GoBackAsync()
    {
        if (!CanGoBack) return;

        if (DisableTransitions)
        {
            Content = _history.Pop();
            return;
        }

        IsTransitioning = true;

        try
        {
            // Store old content
            OldContent = Content;

            // Get previous content from history
            var previousContent = _history.Pop();

            // Set new content (without pushing to history)
            Content = previousContent;

            // Perform reverse transition
            await PerformTransitionAsync(isForward: false);
        }
        finally
        {
            IsTransitioning = false;
            OldContent = null;
        }
    }

    /// <summary>
    /// Clears the navigation history.
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
    }

    /// <summary>
    /// Replaces the current content without adding to history.
    /// </summary>
    /// <param name="content">The new content to display.</param>
    public async Task ReplaceAsync(object content)
    {
        if (IsTransitioning) return;

        // Don't push current to history, just replace
        if (DisableTransitions)
        {
            Content = content;
            return;
        }

        IsTransitioning = true;

        try
        {
            OldContent = Content;
            Content = content;
            await PerformTransitionAsync(isForward: true);
        }
        finally
        {
            IsTransitioning = false;
            OldContent = null;
        }
    }

    #endregion

    #region Private Methods

    private void PushToHistory(object content)
    {
        if (!EnableHistory) return;
        _history.Push(content);
    }

    private async Task PerformTransitionAsync(bool isForward)
    {
        if (_oldContentPresenter is null || _contentPresenter is null) return;

        // Prepare old content for exit animation
        _oldContentPresenter.IsVisible = true;
        _oldContentPresenter.Opacity = 1;
        _oldContentPresenter.RenderTransform = new TransformGroup();

        // Prepare new content for enter animation
        _contentPresenter.Opacity = 0;
        _contentPresenter.RenderTransform = GetEnterTransform(isForward);

        // Create exit animation for old content
        var exitAnimation = CreateExitAnimation(isForward);

        // Create enter animation for new content
        var enterAnimation = CreateEnterAnimation(isForward);

        // Run both animations
        var exitTask = exitAnimation.RunAsync(_oldContentPresenter, System.Threading.CancellationToken.None);
        var enterTask = enterAnimation.RunAsync(_contentPresenter, System.Threading.CancellationToken.None);

        await Task.WhenAll(exitTask, enterTask);

        // Clean up old content presenter
        _oldContentPresenter.IsVisible = false;
        _oldContentPresenter.RenderTransform = null;
        _contentPresenter.RenderTransform = null;
    }

    private Transform GetEnterTransform(bool isForward)
    {
        var transition = isForward ? Transition : GetReverseTransition();

        return transition switch
        {
            PageTransitionType.SlideLeft => new TranslateTransform(isForward ? 300 : -300, 0),
            PageTransitionType.SlideRight => new TranslateTransform(isForward ? -300 : 300, 0),
            PageTransitionType.SlideUp => new TranslateTransform(0, isForward ? 200 : -200),
            PageTransitionType.SlideDown => new TranslateTransform(0, isForward ? -200 : 200),
            PageTransitionType.Scale => new ScaleTransform(0.8, 0.8),
            PageTransitionType.Fade => new TransformGroup(),
            PageTransitionType.Crossfade => new ScaleTransform(0.95, 0.95),
            PageTransitionType.Flip => new Rotate3DTransform { AngleY = isForward ? -90 : 90 },
            _ => new TransformGroup()
        };
    }

    private Animation CreateEnterAnimation(bool isForward)
    {
        var transition = isForward ? Transition : GetReverseTransition();
        var animation = new Animation { Duration = Duration, Easing = Easing };

        var startFrame = new KeyFrame { Cue = new Cue(0.0) };
        var endFrame = new KeyFrame { Cue = new Cue(1.0) };

        // Always fade in
        startFrame.Setters.Add(new Setter(OpacityProperty, 0.0));
        endFrame.Setters.Add(new Setter(OpacityProperty, 1.0));

        // Add transform animation based on transition type
        switch (transition)
        {
            case PageTransitionType.SlideLeft:
            case PageTransitionType.SlideRight:
                startFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(isForward ? 300 : -300, 0)));
                endFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(0, 0)));
                break;

            case PageTransitionType.SlideUp:
            case PageTransitionType.SlideDown:
                startFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(0, isForward ? 200 : -200)));
                endFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(0, 0)));
                break;

            case PageTransitionType.Scale:
            case PageTransitionType.Crossfade:
                startFrame.Setters.Add(new Setter(RenderTransformProperty, new ScaleTransform(0.8, 0.8)));
                endFrame.Setters.Add(new Setter(RenderTransformProperty, new ScaleTransform(1.0, 1.0)));
                break;

            case PageTransitionType.Fade:
                // Just opacity, no transform
                break;
        }

        animation.Children.Add(startFrame);
        animation.Children.Add(endFrame);

        return animation;
    }

    private Animation CreateExitAnimation(bool isForward)
    {
        var transition = isForward ? Transition : GetReverseTransition();
        var animation = new Animation
        {
            Duration = Duration,
            Easing = Easing,
            FillMode = FillMode.Forward
        };

        var startFrame = new KeyFrame { Cue = new Cue(0.0) };
        var endFrame = new KeyFrame { Cue = new Cue(1.0) };

        // Always fade out
        startFrame.Setters.Add(new Setter(OpacityProperty, 1.0));
        endFrame.Setters.Add(new Setter(OpacityProperty, 0.0));

        // Add transform animation based on transition type
        switch (transition)
        {
            case PageTransitionType.SlideLeft:
                startFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(0, 0)));
                endFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(isForward ? -300 : 300, 0)));
                break;

            case PageTransitionType.SlideRight:
                startFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(0, 0)));
                endFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(isForward ? 300 : -300, 0)));
                break;

            case PageTransitionType.SlideUp:
                startFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(0, 0)));
                endFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(0, isForward ? -200 : 200)));
                break;

            case PageTransitionType.SlideDown:
                startFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(0, 0)));
                endFrame.Setters.Add(new Setter(RenderTransformProperty, new TranslateTransform(0, isForward ? 200 : -200)));
                break;

            case PageTransitionType.Scale:
            case PageTransitionType.Crossfade:
                startFrame.Setters.Add(new Setter(RenderTransformProperty, new ScaleTransform(1.0, 1.0)));
                endFrame.Setters.Add(new Setter(RenderTransformProperty, new ScaleTransform(0.95, 0.95)));
                break;

            case PageTransitionType.Fade:
                // Just opacity, no transform
                break;
        }

        animation.Children.Add(startFrame);
        animation.Children.Add(endFrame);

        return animation;
    }

    private PageTransitionType GetReverseTransition()
    {
        return Transition switch
        {
            PageTransitionType.SlideLeft => PageTransitionType.SlideRight,
            PageTransitionType.SlideRight => PageTransitionType.SlideLeft,
            PageTransitionType.SlideUp => PageTransitionType.SlideDown,
            PageTransitionType.SlideDown => PageTransitionType.SlideUp,
            _ => Transition
        };
    }

    #endregion
}
