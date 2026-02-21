# UI Architecture Guide

**Project:** SaveState Reborn  
**Framework:** Avalonia UI 11.2.6 with .NET 9  
**Pattern:** MVVM (Model-View-ViewModel) with ReactiveUI  
**Last Updated:** February 21, 2026

---

## Table of Contents

1. [Overview](#overview)
2. [MVVM Pattern Implementation](#mvvm-pattern-implementation)
3. [Project Structure](#project-structure)
4. [ViewModel Base Classes](#viewmodel-base-classes)
5. [Navigation System](#navigation-system)
6. [Dialog System](#dialog-system)
7. [Theme and Styling System](#theme-and-styling-system)
8. [Data Binding Patterns](#data-binding-patterns)
9. [Common UI Patterns](#common-ui-patterns)
10. [Best Practices](#best-practices)

---

## Overview

SaveState Reborn uses **Avalonia UI** with the **MVVM (Model-View-ViewModel)** pattern. The UI layer is organized into:

- **Views**: XAML files defining the visual structure
- **ViewModels**: C# classes containing presentation logic
- **Services**: UI-specific services (dialogs, navigation, theming)
- **Converters**: Value converters for binding transformations

### Key Technologies

| Component | Technology |
|-----------|------------|
| UI Framework | Avalonia UI 11.2.6 |
| MVVM Toolkit | CommunityToolkit.Mvvm 8.4.0 |
| DI Container | Microsoft.Extensions.DependencyInjection + Splat |
| Reactive Programming | System.Reactive + ReactiveUI |
| Message Bus | CommunityToolkit.Mvvm.Messaging |

---

## MVVM Pattern Implementation

### ViewModel Base Class

All ViewModels inherit from `ObservableObject` (CommunityToolkit.Mvvm):

```csharp
/// <summary>
/// Base class for all ViewModels using CommunityToolkit.Mvvm.
/// Provides property change notification and automatic command generation.
/// </summary>
public partial class MyViewModel : ObservableObject
{
    // Auto-implemented property with change notification
    [ObservableProperty]
    private string _title = string.Empty;
    
    // Auto-generated command via source generators
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        // Implementation
    }
}
```

### View-ViewModel Relationship

The `ViewLocator` class automatically resolves Views from ViewModels using naming convention:

```
ViewModels.Library.LibraryViewModel → Views.Library.LibraryView
ViewModels.Dialogs.MessageDialogViewModel → Views.Dialogs.MessageDialogView
```

```csharp
// ViewLocator.cs - Automatic View resolution
public Control Build(object? data)
{
    var vmType = data.GetType();
    var viewName = fullName.Replace("ViewModels", "Views")
                           .Replace("ViewModel", "View");
    var type = vmType.Assembly.GetType(viewName);
    return (Control)Activator.CreateInstance(type)!;
}
```

### DataContext Binding

Views set their DataContext via binding to parent or DI:

```xml
<!-- Explicit DataContext from parent -->
<views:LibraryToolbar DataContext="{Binding ToolbarViewModel}" />

<!-- DataType for compile-time checking -->
<UserControl x:DataType="vm:LibraryViewModel">
```

---

## Project Structure

```
src/SaveState.Presentation/
├── App.axaml                     # Application entry point, global resources
├── App.axaml.cs                  # Application initialization
├── ViewLocator.cs                # View-ViewModel resolution
│
├── ViewModels/                   # 149 ViewModels
│   ├── MainViewModel.cs          # Root navigation coordinator
│   ├── Shell/                    # Main shell components
│   │   ├── MainShellViewModel.cs # Main window shell
│   │   ├── LibraryViewModel.cs   # Library tab
│   │   ├── DashboardViewModel.cs # Dashboard tab
│   │   ├── MugenHubViewModel.cs  # MUGEN hub
│   │   └── ...
│   ├── Library/                  # Library feature
│   │   ├── LibraryViewModel.cs   # Library container
│   │   ├── GameGridViewModel.cs  # Grid display
│   │   ├── GameListViewModel.cs  # List display
│   │   └── GameDetail/           # Detail view tabs
│   ├── Dialogs/                  # Dialog ViewModels
│   │   ├── MessageDialogViewModel.cs
│   │   ├── ConfirmationDialogViewModel.cs
│   │   └── ...
│   ├── BigPicture/               # 10-foot UI mode
│   ├── Overlays/                 # Overlay ViewModels
│   └── ...
│
├── Views/                        # 255 Views
│   ├── MainWindow.axaml          # Main window
│   ├── Shell/                    # Main shell views
│   ├── Library/                  # Library views
│   ├── Dialogs/                  # Dialog views
│   ├── BigPicture/               # Big picture mode
│   └── ...
│
├── Services/                     # UI Services
│   ├── IDialogService.cs         # Dialog management
│   ├── INavigationService.cs     # Navigation
│   ├── IThemeService.cs          # Theme switching
│   ├── INotificationService.cs   # Toast notifications
│   └── ...
│
├── Converters/                   # Value converters
│   ├── GameLibraryConverters.cs
│   └── ...
│
└── Styles/                       # Theme resources
    ├── Brushes.axaml             # Color palette
    ├── Controls.axaml            # Control styles
    └── Animations.axaml          # Animation definitions
```

---

## ViewModel Base Classes

### ObservableObject Pattern

Using CommunityToolkit.Mvvm source generators:

```csharp
public partial class LibraryViewModel : ObservableObject
{
    // Generates: public string SearchText { get; set; } with INotifyPropertyChanged
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSearchActive))]  // Notify dependent property
    private string _searchText = string.Empty;
    
    // Computed property (not auto-generated)
    public bool IsSearchActive => !string.IsNullOrEmpty(_searchText);
    
    // Generates: public ICommand SearchCommand { get; }
    [RelayCommand(CanExecute = nameof(IsSearchActive))]
    private async Task SearchAsync()
    {
        // Implementation
    }
}
```

### Messaging Between ViewModels

Use the weak reference messenger for cross-ViewModel communication:

```csharp
// Define message
public record NaturalLanguageSearchRequestedMessage(string Value);

// Subscribe in constructor
WeakReferenceMessenger.Default.Register<NaturalLanguageSearchRequestedMessage>(this);

// Handle message
public void Receive(NaturalLanguageSearchRequestedMessage message)
{
    _ = ProcessNaturalLanguageSearchAsync(message.Value);
}

// Send message from another ViewModel
WeakReferenceMessenger.Default.Send(
    new NaturalLanguageSearchRequestedMessage(query));
```

---

## Navigation System

### NavigationService Interface

```csharp
/// <summary>
/// Service for managing navigation between different application views and tabs.
/// </summary>
public interface INavigationService
{
    ObservableObject CurrentViewModel { get; }
    string CurrentTab { get; }
    ReadOnlyObservableCollection<NavigationEntry> History { get; }
    bool CanGoBack { get; }
    
    Task NavigateToAsync<TViewModel>() where TViewModel : ObservableObject;
    Task NavigateToAsync(string tabName);
    Task NavigateToAsync(string tabName, object parameter);
    void GoBack();
    
    event EventHandler<NavigationEventArgs>? Navigated;
}
```

### Tab Registration

Tabs are registered in `TabRegistry`:

```csharp
public static class TabRegistry
{
    public static void RegisterTabs(IServiceCollection services)
    {
        services.AddTransient<LibraryViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<CloudSyncViewModel>();
        services.AddTransient<SettingsViewModel>();
        // ...
    }
}
```

### Navigation in ViewModels

```csharp
public partial class LibraryViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    
    [RelayCommand]
    private async Task OpenSettings()
    {
        await _navigationService.NavigateToAsync("Settings");
    }
    
    [RelayCommand]
    private void NavigateBack()
    {
        if (_navigationService.CanGoBack)
            _navigationService.GoBack();
    }
}
```

### Navigation Parameters

Pass data between ViewModels:

```csharp
// Navigate with parameter
await _navigationService.NavigateToAsync("GameDetail", gameId);

// ViewModel receives parameter via initialization
public class GameDetailViewModel : ObservableObject
{
    public GameDetailViewModel(GameId gameId, /* ... */)
    {
        _gameId = gameId;
    }
}
```

---

## Dialog System

### IDialogService Interface

```csharp
/// <summary>
/// Service for showing dialogs and overlays.
/// </summary>
public interface IDialogService
{
    // Simple dialogs
    Task ShowInformationAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task ShowWarningAsync(string title, string message);
    Task<bool> ShowConfirmationAsync(string title, string message, 
        string confirmText = "OK", string cancelText = "Cancel");
    
    // Input dialogs
    Task<string?> ShowInputDialogAsync(string title, string message, 
        string? placeholder = null, bool isSensitive = false);
    
    // Complex dialogs with results
    Task<AddGameResult?> ShowAddGameWizardAsync();
    Task<NoteEditorResult?> ShowNoteEditorAsync(...);
    Task<GoalCreationResult?> ShowGoalCreationDialogAsync();
}
```

### Dialog Result Pattern

```csharp
// Dialog result record
public record AddGameResult(
    string Title, 
    string? Path, 
    string? Platform, 
    bool ScanAutomatically);

// Usage in ViewModel
[RelayCommand]
private async Task AddGame()
{
    var result = await _dialogService.ShowAddGameWizardAsync();
    if (result != null)  // User confirmed
    {
        // Process result
        await CreateGameAsync(result);
    }
}
```

### Dialog ViewModel Pattern

```csharp
public partial class ConfirmationDialogViewModel : ObservableObject
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _message;
    
    [RelayCommand]
    private void Confirm()
    {
        // Close dialog with result
        if (Application.Current?.ApplicationLifetime is 
            IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(true);  // Return true
        }
    }
}
```

### Closing Dialogs

```csharp
// Close with result (from ViewModel)
[RelayCommand]
private void CloseDialog()
{
    if (Application.Current?.ApplicationLifetime is 
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
        window?.Close(result);  // Pass result back
    }
}
```

---

## Theme and Styling System

### Brush Resources

Defined in `Styles/Brushes.axaml`:

```xml
<!-- Base Colors -->
<SolidColorBrush x:Key="BackgroundBrush" Color="#121212" />
<SolidColorBrush x:Key="SurfaceBrush" Color="#1A1B1E" />
<SolidColorBrush x:Key="CardBackgroundBrush" Color="#25262B" />

<!-- Text Colors -->
<SolidColorBrush x:Key="TextPrimaryBrush" Color="#FFFFFF" />
<SolidColorBrush x:Key="TextSecondaryBrush" Color="#A1A1AA" />
<SolidColorBrush x:Key="TextTertiaryBrush" Color="#71717A" />

<!-- Accent Colors -->
<SolidColorBrush x:Key="AccentBrush" Color="#10B981" />
<SolidColorBrush x:Key="AccentHoverBrush" Color="#059669" />

<!-- Status Colors -->
<SolidColorBrush x:Key="SuccessBrush" Color="#10B981" />
<SolidColorBrush x:Key="WarningBrush" Color="#F59E0B" />
<SolidColorBrush x:Key="ErrorBrush" Color="#EF4444" />
```

### Control Styles

Defined in `Styles/Controls.axaml`:

```xml
<!-- Primary Button (Pill Shape) -->
<Style Selector="Button.Primary">
    <Setter Property="Background" Value="{StaticResource PrimaryActionGradient}" />
    <Setter Property="Padding" Value="32,10" />
    <Setter Property="CornerRadius" Value="24" />
    <Setter Property="FontWeight" Value="Bold" />
</Style>

<!-- Navigation Button -->
<Style Selector="Button.Nav">
    <Setter Property="Background" Value="Transparent" />
    <Setter Property="Foreground" Value="{StaticResource TextSecondaryBrush}" />
</Style>

<!-- Card Container -->
<Style Selector="Border.Card">
    <Setter Property="Background" Value="{StaticResource CardBackgroundBrush}" />
    <Setter Property="CornerRadius" Value="12" />
    <Setter Property="BoxShadow" Value="0 4 12 #20000000" />
</Style>
```

### Using Resources

```xml
<Border Background="{StaticResource CardBackgroundBrush}"
        BorderBrush="{StaticResource BorderBrush}">
    <TextBlock Foreground="{StaticResource TextPrimaryBrush}" />
</Border>
```

### Theme Service

```csharp
public interface IThemeService
{
    ThemeType CurrentTheme { get; }
    void SetTheme(ThemeType theme);
    IReadOnlyList<ThemeType> AvailableThemes { get; }
    event EventHandler<ThemeType>? ThemeChanged;
}
```

---

## Data Binding Patterns

### Basic Binding

```xml
<!-- One-way binding -->
<TextBlock Text="{Binding Title}" />

<!-- Two-way binding -->
<TextBox Text="{Binding SearchText, Mode=TwoWay}" />

<!-- Binding with converter -->
<Button IsVisible="{Binding IsLoading, 
          Converter={StaticResource InverseBoolConverter}}" />
```

### Command Binding

```xml
<Button Content="Save" 
        Command="{Binding SaveCommand}" 
        CommandParameter="{Binding SelectedItem}" />

<!-- Command with CanExecute (automatically disables button) -->
<Button Content="Search" 
        Command="{Binding SearchCommand}" 
        IsEnabled="{Binding CanSearch}" />
```

### Collection Binding

```xml
<ListBox ItemsSource="{Binding Games}" 
         SelectedItem="{Binding SelectedGame}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Title}" />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### Multi-Binding

```xml
<!-- Using multi-value converter -->
<MultiBinding Converter="{StaticResource TabContentConverter}">
    <Binding Path="SelectedTabIndex" />
    <Binding Path="." />
</MultiBinding>
```

---

## Common UI Patterns

### View Visibility Toggling

```xml
<!-- Multiple views, only one visible -->
<Grid>
    <views:GameGridView IsVisible="{Binding IsGridView}" />
    <views:GameListView IsVisible="{Binding IsListView}" />
    <views:GameCompactView IsVisible="{Binding IsCompactView}" />
</Grid>
```

### Empty State Pattern

```xml
<Border IsVisible="{Binding IsEmpty}">
    <StackPanel>
        <TextBlock Text="No games found" Classes="Header" />
        <TextBlock Text="{Binding EmptyStateMessage}" />
        <Button Content="Add Your First Game" 
                Command="{Binding AddGameCommand}" />
    </StackPanel>
</Border>
```

### Loading State Pattern

```xml
<Grid>
    <!-- Content (hidden when loading) -->
    <ContentControl IsVisible="{Binding !IsLoading}" />
    
    <!-- Loading indicator -->
    <ProgressBar IsVisible="{Binding IsLoading}" 
                 IsIndeterminate="True" />
</Grid>
```

### Tab Interface Pattern

```xml
<TabControl SelectedIndex="{Binding SelectedTabIndex}">
    <TabItem Header="Overview">
        <views:GameOverviewTabView DataContext="{Binding OverviewTab}" />
    </TabItem>
    <TabItem Header="Save States">
        <views:GameSaveStatesTabView DataContext="{Binding SaveStatesTab}" />
    </TabItem>
</TabControl>
```

---

## Best Practices

### ViewModel Design

1. **Keep ViewModels focused**: Each ViewModel should have a single responsibility
2. **Use constructor injection**: Inject services via constructor, not service locator
3. **Avoid UI logic in ViewModels**: Use behaviors or code-behind for UI-only concerns
4. **Dispose subscriptions**: Unsubscribe from events in `Dispose()` method

```csharp
public partial class MyViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscription;
    
    public MyViewModel(IMediator mediator)
    {
        // Subscribe to events
        _subscription = someObservable.Subscribe(OnNext);
    }
    
    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
```

### Async Patterns

```csharp
// Use async commands
[RelayCommand]
private async Task LoadDataAsync(CancellationToken cancellationToken)
{
    IsLoading = true;
    try
    {
        var data = await _service.GetDataAsync(cancellationToken);
        Items = new ObservableCollection<Item>(data);
    }
    finally
    {
        IsLoading = false;
    }
}

// Fire-and-forget with proper exception handling
_ = InitializeAsync();

private async Task InitializeAsync()
{
    try
    {
        await LoadDataAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to initialize");
    }
}
```

### XAML Guidelines

1. **Use `x:DataType`** for compile-time binding checking
2. **Name conventions**: Use descriptive names for named elements
3. **Extract large DataTemplates** to resources
4. **Use StaticResource for theme brushes**

```xml
<UserControl x:DataType="vm:LibraryViewModel">
    <Grid>
        <!-- Good: Named for clarity -->
        <TextBlock x:Name="TitleTextBlock" Text="{Binding Title}" />
        
        <!-- Good: Theme-aware colors -->
        <Border Background="{StaticResource CardBackgroundBrush}" />
    </Grid>
</UserControl>
```

### Error Handling

```csharp
[RelayCommand]
private async Task PerformActionAsync()
{
    try
    {
        await _service.DoWorkAsync();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Action failed");
        await _dialogService.ShowErrorAsync(
            "Error", 
            "Failed to perform action. Please try again.");
    }
}
```

### Performance Considerations

1. **Virtualize long lists** using `VirtualizingStackPanel`
2. **Debounce search input** using ReactiveUI's `Throttle`
3. **Lazy-load tab content** when possible
4. **Dispose heavy resources** (images, streams) properly

```csharp
// Debounced search
this.WhenAnyValue(x => x.SearchText)
    .Throttle(TimeSpan.FromMilliseconds(300))
    .Subscribe(async text => await SearchAsync(text));
```

---

## Related Documentation

- [UI Component Catalog](./UI_COMPONENT_CATALOG.md) - Complete component reference
- [Presentation README](../../src/SaveState.Presentation/README.md) - Project setup and development
- [Patterns Cookbook](../architecture/PATTERNS_COOKBOOK.md) - Copy-paste code patterns
- [Engineering Rules](../architecture/ENGINEERING_RULES.md) - Coding standards
