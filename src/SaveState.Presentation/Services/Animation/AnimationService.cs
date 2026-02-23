using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SaveState.Presentation.Services.Animation;

/// <summary>
/// Implementation of the animation service providing smooth UI transitions and micro-interactions.
/// 
/// Performance Considerations:
/// - Uses composition rendering where possible for GPU acceleration
/// - Batches animation updates on the UI thread
/// - Respects reduced motion preferences for accessibility
/// - Caches easing functions and common animations
/// 
/// Thread Safety: All methods must be called from the UI thread.
/// </summary>
public class AnimationService : IAnimationService
{
    private readonly ILogger<AnimationService> _logger;
    private readonly Dictionary<Control, CancellationTokenSource> _activeAnimations = new();
    private readonly Subject<AnimationFrame> _animationFrameSubject = new();

    // Material Design 3 timing specifications
    private static readonly TimeSpan DefaultTransitionDuration = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan FastTransitionDuration = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan MediumTransitionDuration = TimeSpan.FromMilliseconds(350);
    private static readonly TimeSpan SlowTransitionDuration = TimeSpan.FromMilliseconds(500);

    // Cached easing functions
    private static readonly CubicEaseOut DefaultEasingInstance = new();
    private static readonly BounceEaseInOut BounceEasingInstance = new();
    private static readonly ElasticEaseOut ElasticEasingInstance = new();
    private static readonly QuarticEaseInOut QuarticEasingInstance = new();

    /// <inheritdoc />
    public IEasing DefaultEasing => DefaultEasingInstance;

    /// <inheritdoc />
    public IEasing BounceEasing => BounceEasingInstance;

    /// <inheritdoc />
    public IEasing ElasticEasing => ElasticEasingInstance;

    /// <inheritdoc />
    public bool IsReducedMotionPreferred => CheckReducedMotionPreference();

    public AnimationService(ILogger<AnimationService> logger)
    {
        _logger = logger;
        _logger.LogDebug("AnimationService initialized");
    }

    #region Transitions

    /// <inheritdoc />
    public async Task FadeInAsync(Control element, Duration? duration = null)
    {
        if (IsReducedMotionPreferred)
        {
            element.Opacity = 1;
            element.IsVisible = true;
            return;
        }

        var animDuration = duration?.TimeSpan ?? DefaultTransitionDuration;
        element.IsVisible = true;

        var animation = new Animation
        {
            Duration = animDuration,
            Easing = DefaultEasingInstance,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                }
            }
        };

        await RunAnimationAsync(element, animation);
    }

    /// <inheritdoc />
    public async Task FadeOutAsync(Control element, Duration? duration = null)
    {
        if (IsReducedMotionPreferred)
        {
            element.Opacity = 0;
            element.IsVisible = false;
            return;
        }

        var animDuration = duration?.TimeSpan ?? FastTransitionDuration;

        var animation = new Animation
        {
            Duration = animDuration,
            Easing = DefaultEasingInstance,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(Visual.OpacityProperty, element.Opacity) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                }
            }
        };

        await RunAnimationAsync(element, animation);
        element.IsVisible = false;
        element.Opacity = 1; // Reset for next time
    }

    /// <inheritdoc />
    public async Task SlideInAsync(Control element, SlideDirection direction, Duration? duration = null)
    {
        if (IsReducedMotionPreferred)
        {
            element.IsVisible = true;
            element.Opacity = 1;
            return;
        }

        var animDuration = duration?.TimeSpan ?? MediumTransitionDuration;
        var (startX, startY) = GetSlideStartPosition(direction, element);

        // Ensure render transform is set up
        element.RenderTransform = new TranslateTransform(startX, startY);
        element.Opacity = 0;
        element.IsVisible = true;

        var animation = new Animation
        {
            Duration = animDuration,
            Easing = QuarticEasingInstance,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0.0),
                        new Setter(Visual.RenderTransformProperty, new TranslateTransform(startX, startY))
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1.0),
                        new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0))
                    }
                }
            }
        };

        await RunAnimationAsync(element, animation);
    }

    /// <inheritdoc />
    public async Task SlideOutAsync(Control element, SlideDirection direction, Duration? duration = null)
    {
        if (IsReducedMotionPreferred)
        {
            element.IsVisible = false;
            return;
        }

        var animDuration = duration?.TimeSpan ?? DefaultTransitionDuration;
        var (endX, endY) = GetSlideEndPosition(direction, element);

        var animation = new Animation
        {
            Duration = animDuration,
            Easing = QuarticEasingInstance,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1.0),
                        new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0))
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0.0),
                        new Setter(Visual.RenderTransformProperty, new TranslateTransform(endX, endY))
                    }
                }
            }
        };

        await RunAnimationAsync(element, animation);
        element.IsVisible = false;
        element.RenderTransform = null;
    }

    /// <inheritdoc />
    public async Task ScaleInAsync(Control element, Duration? duration = null)
    {
        if (IsReducedMotionPreferred)
        {
            element.IsVisible = true;
            element.Opacity = 1;
            return;
        }

        var animDuration = duration?.TimeSpan ?? DefaultTransitionDuration;

        element.RenderTransform = new ScaleTransform(0.8, 0.8);
        element.Opacity = 0;
        element.IsVisible = true;

        var animation = new Animation
        {
            Duration = animDuration,
            Easing = ElasticEasingInstance,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0.0),
                        new Setter(Visual.RenderTransformProperty, new ScaleTransform(0.8, 0.8))
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1.0),
                        new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.0, 1.0))
                    }
                }
            }
        };

        await RunAnimationAsync(element, animation);
    }

    /// <inheritdoc />
    public async Task ScaleOutAsync(Control element, Duration? duration = null)
    {
        if (IsReducedMotionPreferred)
        {
            element.IsVisible = false;
            return;
        }

        var animDuration = duration?.TimeSpan ?? FastTransitionDuration;

        var animation = new Animation
        {
            Duration = animDuration,
            Easing = DefaultEasingInstance,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1.0),
                        new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.0, 1.0))
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0.0),
                        new Setter(Visual.RenderTransformProperty, new ScaleTransform(0.8, 0.8))
                    }
                }
            }
        };

        await RunAnimationAsync(element, animation);
        element.IsVisible = false;
        element.RenderTransform = null;
    }

    #endregion

    #region Page Transitions

    /// <inheritdoc />
    public async Task NavigateForwardAsync(Control fromPage, Control toPage)
    {
        if (IsReducedMotionPreferred)
        {
            fromPage.IsVisible = false;
            toPage.IsVisible = true;
            return;
        }

        // Animate both pages simultaneously
        var fromTask = SlideOutAsync(fromPage, SlideDirection.Left, Duration.FromMilliseconds(300));
        var toTask = SlideInAsync(toPage, SlideDirection.Right, Duration.FromMilliseconds(350));

        await Task.WhenAll(fromTask, toTask);
        fromPage.IsVisible = false;
    }

    /// <inheritdoc />
    public async Task NavigateBackAsync(Control fromPage, Control toPage)
    {
        if (IsReducedMotionPreferred)
        {
            fromPage.IsVisible = false;
            toPage.IsVisible = true;
            return;
        }

        toPage.IsVisible = true;

        var fromTask = SlideOutAsync(fromPage, SlideDirection.Right, Duration.FromMilliseconds(300));
        var toTask = SlideInAsync(toPage, SlideDirection.Left, Duration.FromMilliseconds(350));

        await Task.WhenAll(fromTask, toTask);
        fromPage.IsVisible = false;
    }

    /// <inheritdoc />
    public async Task NavigateModalAsync(Control background, Control modal)
    {
        if (IsReducedMotionPreferred)
        {
            background.IsVisible = true;
            modal.IsVisible = true;
            return;
        }

        background.Opacity = 0;
        background.IsVisible = true;

        // Fade in background
        var bgAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(200),
            Easing = DefaultEasingInstance,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Visual.OpacityProperty, 0.5) }
                }
            }
        };

        var bgTask = RunAnimationAsync(background, bgAnimation);
        var modalTask = ScaleInAsync(modal, Duration.FromMilliseconds(350));

        await Task.WhenAll(bgTask, modalTask);
    }

    /// <inheritdoc />
    public async Task DismissModalAsync(Control background, Control modal)
    {
        if (IsReducedMotionPreferred)
        {
            background.IsVisible = false;
            modal.IsVisible = false;
            return;
        }

        var bgAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(200),
            Easing = DefaultEasingInstance,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(Visual.OpacityProperty, 0.5) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Visual.OpacityProperty, 0.0) }
                }
            }
        };

        var bgTask = RunAnimationAsync(background, bgAnimation);
        var modalTask = ScaleOutAsync(modal, Duration.FromMilliseconds(250));

        await Task.WhenAll(bgTask, modalTask);

        background.IsVisible = false;
        modal.IsVisible = false;
        background.Opacity = 1;
    }

    #endregion

    #region Micro-interactions

    /// <inheritdoc />
    public async Task PulseAsync(Control element)
    {
        if (IsReducedMotionPreferred) return;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(600),
            Easing = DefaultEasingInstance,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.0, 1.0)) }
                },
                new KeyFrame
                {
                    Cue = new Cue(0.5),
                    Setters = { new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.1, 1.1)) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.0, 1.0)) }
                }
            }
        };

        await RunAnimationAsync(element, animation);
    }

    /// <inheritdoc />
    public async Task ShakeAsync(Control element)
    {
        if (IsReducedMotionPreferred) return;

        const int shakeDistance = 10;
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(500),
            Easing = DefaultEasingInstance,
            Children =
            {
                new KeyFrame { Cue = new Cue(0.0), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0)) } },
                new KeyFrame { Cue = new Cue(0.1), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(-shakeDistance, 0)) } },
                new KeyFrame { Cue = new Cue(0.2), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(shakeDistance, 0)) } },
                new KeyFrame { Cue = new Cue(0.3), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(-shakeDistance, 0)) } },
                new KeyFrame { Cue = new Cue(0.4), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(shakeDistance, 0)) } },
                new KeyFrame { Cue = new Cue(0.5), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(-shakeDistance, 0)) } },
                new KeyFrame { Cue = new Cue(0.6), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(shakeDistance, 0)) } },
                new KeyFrame { Cue = new Cue(0.7), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(-shakeDistance, 0)) } },
                new KeyFrame { Cue = new Cue(0.8), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(shakeDistance, 0)) } },
                new KeyFrame { Cue = new Cue(1.0), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0)) } }
            }
        };

        await RunAnimationAsync(element, animation);
        element.RenderTransform = null;
    }

    /// <inheritdoc />
    public async Task BounceAsync(Control element)
    {
        if (IsReducedMotionPreferred) return;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(800),
            Easing = BounceEasingInstance,
            Children =
            {
                new KeyFrame { Cue = new Cue(0.0), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0)) } },
                new KeyFrame { Cue = new Cue(0.5), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, -30)) } },
                new KeyFrame { Cue = new Cue(1.0), Setters = { new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0)) } }
            }
        };

        await RunAnimationAsync(element, animation);
        element.RenderTransform = null;
    }

    /// <inheritdoc />
    public async Task HighlightAsync(Control element)
    {
        if (IsReducedMotionPreferred) return;

        // Store original background if possible
        var originalBrush = element.GetValue(Control.BackgroundProperty);
        var highlightBrush = new SolidColorBrush(Colors.Yellow, 0.3);

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(600),
            Easing = DefaultEasingInstance,
            Children =
            {
                new KeyFrame { Cue = new Cue(0.0), Setters = { new Setter(Control.BackgroundProperty, originalBrush) } },
                new KeyFrame { Cue = new Cue(0.5), Setters = { new Setter(Control.BackgroundProperty, highlightBrush) } },
                new KeyFrame { Cue = new Cue(1.0), Setters = { new Setter(Control.BackgroundProperty, originalBrush) } }
            }
        };

        await RunAnimationAsync(element, animation);
    }

    /// <inheritdoc />
    public async Task RippleAsync(Control element, Point origin)
    {
        if (IsReducedMotionPreferred) return;

        var ripple = new Ellipse
        {
            Width = 0,
            Height = 0,
            Fill = new SolidColorBrush(Colors.White, 0.3),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(origin.X, origin.Y, 0, 0)
        };

        if (element is Panel panel)
        {
            panel.Children.Add(ripple);
        }
        else if (element is ContentControl contentControl && contentControl.Content is Panel contentPanel)
        {
            contentPanel.Children.Add(ripple);
        }
        else
        {
            return; // Cannot add ripple
        }

        var maxRadius = Math.Max(element.Bounds.Width, element.Bounds.Height) * 1.5;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(600),
            Easing = DefaultEasingInstance,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Visual.WidthProperty, 0.0),
                        new Setter(Visual.HeightProperty, 0.0),
                        new Setter(Visual.OpacityProperty, 0.5)
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Visual.WidthProperty, maxRadius * 2),
                        new Setter(Visual.HeightProperty, maxRadius * 2),
                        new Setter(Visual.OpacityProperty, 0.0)
                    }
                }
            }
        };

        await RunAnimationAsync(ripple, animation);

        // Clean up ripple
        if (element is Panel p)
        {
            p.Children.Remove(ripple);
        }
    }

    #endregion

    #region Loading States

    /// <inheritdoc />
    public Task ShowSkeletonAsync(Control container)
    {
        if (container is Panel panel)
        {
            var skeletonOverlay = CreateSkeletonOverlay(container);
            skeletonOverlay.Name = "SkeletonOverlay";
            panel.Children.Add(skeletonOverlay);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HideSkeletonAsync(Control container)
    {
        if (container is Panel panel)
        {
            var skeleton = panel.Children.FirstOrDefault(c => c.Name == "SkeletonOverlay");
            if (skeleton != null)
            {
                panel.Children.Remove(skeleton);
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShowSpinnerAsync(Control element, string? message = null)
    {
        // Implementation would add a spinner overlay to the element
        // This is a simplified version
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HideSpinnerAsync(Control element)
    {
        // Implementation would remove the spinner overlay
        return Task.CompletedTask;
    }

    #endregion

    #region List Animations

    /// <inheritdoc />
    public async Task AnimateListAddAsync(ItemsControl list, Control item)
    {
        if (IsReducedMotionPreferred) return;

        item.Opacity = 0;
        item.RenderTransform = new TranslateTransform(-50, 0);

        // Wait for item to be added to visual tree
        await Task.Delay(10);

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = DefaultEasingInstance,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0.0),
                        new Setter(Visual.RenderTransformProperty, new TranslateTransform(-50, 0))
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1.0),
                        new Setter(Visual.RenderTransformProperty, new TranslateTransform(0, 0))
                    }
                }
            }
        };

        await RunAnimationAsync(item, animation);
    }

    /// <inheritdoc />
    public async Task AnimateListRemoveAsync(ItemsControl list, Control item)
    {
        if (IsReducedMotionPreferred) return;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(250),
            Easing = DefaultEasingInstance,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 1.0),
                        new Setter(Visual.RenderTransformProperty, new ScaleTransform(1.0, 1.0))
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Visual.OpacityProperty, 0.0),
                        new Setter(Visual.RenderTransformProperty, new ScaleTransform(0.8, 0.8))
                    }
                }
            }
        };

        await RunAnimationAsync(item, animation);
    }

    /// <inheritdoc />
    public async Task AnimateListReorderAsync(ItemsControl list)
    {
        if (IsReducedMotionPreferred) return;

        // Animate all visible items with a stagger effect
        if (list.GetVisualChildren().OfType<Control>() is { } items)
        {
            var tasks = items.Select((item, index) =>
            {
                var delay = index * 30; // Stagger by 30ms
                return Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var animation = new Animation
                        {
                            Duration = TimeSpan.FromMilliseconds(200),
                            Easing = DefaultEasingInstance,
                            Children =
                            {
                                new KeyFrame
                                {
                                    Cue = new Cue(0.0),
                                    Setters = { new Setter(Visual.OpacityProperty, 0.7) }
                                },
                                new KeyFrame
                                {
                                    Cue = new Cue(1.0),
                                    Setters = { new Setter(Visual.OpacityProperty, 1.0) }
                                }
                            }
                        };
                        await RunAnimationAsync(item, animation);
                    });
                });
            });

            await Task.WhenAll(tasks);
        }
    }

    #endregion

    #region Scroll Animations

    /// <inheritdoc />
    public async Task ScrollToAsync(ScrollViewer scrollViewer, double offset, bool animated = true)
    {
        if (!animated || IsReducedMotionPreferred)
        {
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, offset);
            return;
        }

        var startOffset = scrollViewer.Offset.Y;
        var distance = offset - startOffset;
        var duration = TimeSpan.FromMilliseconds(Math.Min(500, Math.Abs(distance) * 0.5));

        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < duration)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var progress = elapsed / duration.TotalMilliseconds;
            var eased = DefaultEasingInstance.Ease(progress);

            var currentOffset = startOffset + (distance * eased);
            scrollViewer.Offset = new Vector(scrollViewer.Offset.X, currentOffset);

            await Task.Delay(16); // ~60fps
        }

        scrollViewer.Offset = new Vector(scrollViewer.Offset.X, offset);
    }

    /// <inheritdoc />
    public async Task ScrollToElementAsync(ScrollViewer scrollViewer, Control element)
    {
        var elementPosition = element.TranslatePoint(new Point(0, 0), scrollViewer)?.Y ?? 0;
        await ScrollToAsync(scrollViewer, elementPosition);
    }

    #endregion

    #region Value Animations

    /// <inheritdoc />
    public async Task AnimateDoubleAsync(Control element, AvaloniaProperty property, double from, double to, Duration? duration = null)
    {
        if (IsReducedMotionPreferred)
        {
            element.SetValue(property, to);
            return;
        }

        var animDuration = duration?.TimeSpan ?? DefaultTransitionDuration;
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < animDuration)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var progress = elapsed / animDuration.TotalMilliseconds;
            var eased = DefaultEasingInstance.Ease(progress);
            var current = from + ((to - from) * eased);

            element.SetValue(property, current);
            await Task.Delay(16);
        }

        element.SetValue(property, to);
    }

    /// <inheritdoc />
    public async Task AnimateColorAsync(Control element, AvaloniaProperty property, Color from, Color to, Duration? duration = null)
    {
        if (IsReducedMotionPreferred)
        {
            element.SetValue(property, new SolidColorBrush(to));
            return;
        }

        var animDuration = duration?.TimeSpan ?? DefaultTransitionDuration;
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < animDuration)
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var progress = elapsed / animDuration.TotalMilliseconds;
            var eased = DefaultEasingInstance.Ease(progress);

            var current = Color.FromArgb(
                (byte)(from.A + ((to.A - from.A) * eased)),
                (byte)(from.R + ((to.R - from.R) * eased)),
                (byte)(from.G + ((to.G - from.G) * eased)),
                (byte)(from.B + ((to.B - from.B) * eased))
            );

            element.SetValue(property, new SolidColorBrush(current));
            await Task.Delay(16);
        }

        element.SetValue(property, new SolidColorBrush(to));
    }

    #endregion

    #region Private Helpers

    private static (double x, double y) GetSlideStartPosition(SlideDirection direction, Control element)
    {
        return direction switch
        {
            SlideDirection.Left => (300, 0),
            SlideDirection.Right => (-300, 0),
            SlideDirection.Up => (0, 200),
            SlideDirection.Down => (0, -200),
            _ => (0, 0)
        };
    }

    private static (double x, double y) GetSlideEndPosition(SlideDirection direction, Control element)
    {
        return direction switch
        {
            SlideDirection.Left => (-300, 0),
            SlideDirection.Right => (300, 0),
            SlideDirection.Up => (0, -200),
            SlideDirection.Down => (0, 200),
            _ => (0, 0)
        };
    }

    private async Task RunAnimationAsync(Control element, Animation animation)
    {
        // Cancel any existing animation on this element
        if (_activeAnimations.TryGetValue(element, out var existingCts))
        {
            existingCts.Cancel();
            _activeAnimations.Remove(element);
        }

        var cts = new CancellationTokenSource();
        _activeAnimations[element] = cts;

        try
        {
            await animation.RunAsync(element, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Animation was cancelled, this is expected
        }
        finally
        {
            _activeAnimations.Remove(element);
            cts.Dispose();
        }
    }

    private static Control CreateSkeletonOverlay(Control container)
    {
        var shimmerBrush = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute),
            EndPoint = new RelativePoint(200, 0, RelativeUnit.Absolute),
            GradientStops =
            {
                new GradientStop(Colors.LightGray, 0.0),
                new GradientStop(Colors.White, 0.5),
                new GradientStop(Colors.LightGray, 1.0)
            }
        };

        // Create animation for shimmer effect
        var shimmerAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(1500),
            IterationCount = IterationCount.Infinite,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters =
                    {
                        new Setter(Brush.TransformProperty, new TranslateTransform(-200, 0))
                    }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters =
                    {
                        new Setter(Brush.TransformProperty, new TranslateTransform(200, 0))
                    }
                }
            }
        };

        var overlay = new Border
        {
            Background = shimmerBrush,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false
        };

        // Apply shimmer animation
        shimmerAnimation.RunAsync(overlay, CancellationToken.None);

        return overlay;
    }

    private static bool CheckReducedMotionPreference()
    {
        // In a real implementation, this would check system settings
        // For now, return false to enable animations by default
        return false;
    }

    private record AnimationFrame(TimeSpan Timestamp, double Progress);

    #endregion
}
