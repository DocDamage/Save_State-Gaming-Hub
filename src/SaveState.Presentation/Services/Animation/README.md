# Animation System

A comprehensive animation and transition system for SaveStateReborn built on Avalonia UI.

## Features

- **60fps smooth animations** - GPU-accelerated where possible
- **Reduced motion support** - Respects user accessibility preferences
- **Material Design 3 timing** - Consistent animation durations
- **Non-blocking async** - All animations are awaitable tasks
- **Performance optimized** - Composition rendering, minimal allocations

## Quick Start

### 1. Register Services

```csharp
// In Program.cs or App.axaml.cs
services.AddAnimationServices();
```

### 2. Use in ViewModels

```csharp
public class GameLibraryViewModel : ObservableObject, ILoadingStateAware
{
    private readonly IAnimationService _animationService;
    
    public bool IsLoading { get; set; }
    public string? LoadingMessage { get; set; }
    
    public async Task LoadGamesAsync()
    {
        var games = await this.WithLoadingAsync(
            async () => await _gameService.GetGamesAsync(),
            loadingMessage: "Loading games...");
        
        Games = games;
    }
}
```

### 3. Use in Views

```xml
<Button Content="Click Me">
    <i:Interaction.Behaviors>
        <behaviors:RippleBehavior RippleColor="White" />
        <behaviors:ScaleOnPressBehavior />
    </i:Interaction.Behaviors>
</Button>
```

## Components

### Animation Service

The `IAnimationService` provides programmatic control over animations:

```csharp
// Transitions
await _animationService.FadeInAsync(myControl);
await _animationService.SlideInAsync(myControl, SlideDirection.Right);
await _animationService.ScaleInAsync(myControl);

// Page Navigation
await _animationService.NavigateForwardAsync(fromPage, toPage);
await _animationService.NavigateModalAsync(overlay, modal);

// Micro-interactions
await _animationService.PulseAsync(notificationBadge);
await _animationService.ShakeAsync(errorField);
await _animationService.BounceAsync(successIcon);
await _animationService.RippleAsync(button, clickPosition);

// Loading States
await _animationService.ShowSkeletonAsync(container);
await _animationService.ShowSpinnerAsync(button, "Loading...");

// List Animations
await _animationService.AnimateListAddAsync(list, newItem);
await _animationService.AnimateListRemoveAsync(list, removedItem);

// Scroll Animations
await _animationService.ScrollToElementAsync(scrollViewer, targetElement);

// Value Animations
await _animationService.AnimateDoubleAsync(
    progressBar, 
    RangeBase.ValueProperty, 
    0, 
    100);
```

### Transition Host

A container control for page transitions:

```xml
<local:TransitionHost x:Name="TransitionHost" 
                      Transition="SlideRight" 
                      Duration="0:0:0.3">
    <views:CurrentView />
</local:TransitionHost>
```

```csharp
// Navigate to new page
await TransitionHost.NavigateAsync(new GameDetailView { DataContext = game });

// Go back
await TransitionHost.GoBackAsync();

// Replace without history
await TransitionHost.ReplaceAsync(newView);
```

### Loading Controls

#### SkeletonContainer

```xml
<local:SkeletonContainer IsLoading="{Binding IsLoading}" 
                         LoadingMessage="Loading games...">
    <views:GameListView />
</local:SkeletonContainer>
```

#### LoadingSpinner

```xml
<local:LoadingSpinner Size="Large" Message="Loading..." />
```

#### ProgressIndicator

```xml
<local:ProgressIndicator CurrentStep="{Binding CurrentStep}"
                         TotalSteps="4"
                         StepLabels="Download,Install,Configure,Launch"
                         Message="Installing..." />
```

### Behaviors

Attach interactive animations to controls:

| Behavior | Description |
|----------|-------------|
| `RippleBehavior` | Material design ripple effect on click |
| `ScaleOnPressBehavior` | Scales down when pressed |
| `HoverGlowBehavior` | Glow effect on hover |
| `MagneticButtonBehavior` | Button moves towards cursor |
| `FocusAnimationBehavior` | Scale animation on focus |
| `TiltBehavior` | 3D tilt effect based on mouse position |

### Converters

Animate value changes automatically:

```xml
<!-- Count up animation -->
<TextBlock Text="{Binding Score, Converter={StaticResource CountUpConverter}}" />

<!-- Progress to color gradient -->
<Border Background="{Binding Progress, Converter={StaticResource ProgressToColorConverter}}" />

<!-- Smooth value for real-time data -->
<TextBlock Text="{Binding Fps, Converter={StaticResource SmoothValueConverter}}" />
```

## Timing Standards

Following Material Design 3 specifications:

| Duration | Use Case |
|----------|----------|
| 100-200ms | Micro-interactions (press, hover) |
| 250-300ms | Standard transitions (fade, scale) |
| 350ms | Page transitions |
| 500ms | Emphasis transitions (modals) |
| 600-800ms | Complex animations (ripple, count up) |

## Easing Functions

| Easing | Use Case |
|--------|----------|
| `CubicEaseOut` | UI element entrance |
| `CubicEaseIn` | UI element exit |
| `QuarticEaseOut` | Page transitions |
| `ElasticEaseOut` | Playful entrances (modals) |
| `BounceEaseOut` | Playful effects |

## Accessibility

The animation system automatically respects reduced motion preferences:

```csharp
if (_animationService.IsReducedMotionPreferred)
{
    // Animations are disabled, instant transitions
}
```

To force reduced motion for testing:

```xml
<Window Classes="reduced-motion">
```

## Extension Methods

### Loading State Extensions

```csharp
// Simple loading wrapper
await viewModel.WithLoadingAsync(
    async () => await LoadDataAsync(),
    loadingMessage: "Loading...");

// With skeleton overlay
await viewModel.WithSkeletonLoadingAsync(
    async () => await LoadDataAsync(),
    container,
    _animationService);

// With progress tracking
await viewModel.WithProgressAsync(
    async progress => await LoadWithProgressAsync(progress),
    progressIndicator,
    totalSteps: 5);

// With retry logic
await viewModel.WithRetryAndLoadingAsync(
    async () => await LoadDataAsync(),
    maxRetries: 3,
    loadingMessage: "Loading...");

// With debouncing
await viewModel.WithDebounceAsync(
    async () => await SearchAsync(query),
    debounceMs: 300);

// With throttling
await viewModel.WithThrottleAsync(
    async () => await SaveAsync(),
    throttleMs: 1000);
```

## XAML Styles

Include the page transitions in your App.axaml:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://SaveState.Presentation/Styles/PageTransitions.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

Apply transition classes:

```xml
<Border Classes="fade-transition" />
<Border Classes="slide-in-right" />
<Border Classes="scale-in" />
<Border Classes="pulse-animation" />
```

## Best Practices

1. **Always check IsReducedMotionPreferred** before custom animations
2. **Use the AnimationService** for consistent timing
3. **Keep animations under 500ms** for UI feedback
4. **Use micro-interactions** for button/element feedback
5. **Use skeleton loading** for content-heavy views
6. **Chain animations** with `await` for sequential effects
7. **Run animations in parallel** with `Task.WhenAll` for simultaneous effects

## Performance Tips

- Animations run on the UI thread - keep them short
- Use `ClipToBounds="True"` for ripple effects
- Prefer `RenderTransform` over `LayoutTransform`
- Use `Opacity` animations for showing/hiding
- Avoid animating layout properties (Width, Height)

## Examples

### Game Card Entrance

```csharp
// Staggered entrance for game cards
var cards = GameItemsControl.GetVisualChildren().OfType<GameCardView>();
var tasks = cards.Select((card, index) => 
    Task.Delay(index * 50)
        .ContinueWith(_ => _animationService.SlideInAsync(card, SlideDirection.Up)));

await Task.WhenAll(tasks);
```

### Modal Dialog

```csharp
public async Task ShowGameDetailsAsync(Game game)
{
    var modal = new GameDetailsView { DataContext = game };
    
    await _animationService.NavigateModalAsync(
        BackgroundOverlay, 
        modal);
    
    // Wait for user interaction...
    
    await _animationService.DismissModalAsync(
        BackgroundOverlay, 
        modal);
}
```

### Error Shake

```csharp
public async Task ValidateAndSubmitAsync()
{
    if (!IsValid)
    {
        await _animationService.ShakeAsync(SubmitButton);
        return;
    }
    
    // Submit...
}
```
