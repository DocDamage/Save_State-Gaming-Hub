# SaveState.Presentation

The UI layer of SaveState Reborn - an Avalonia UI application using MVVM pattern.

---

## Overview

This project contains all user interface components for SaveState Reborn:

- **255 Views** - XAML user controls and windows
- **149 ViewModels** - Presentation logic with MVVM pattern
- **20+ Converters** - Value converters for data binding
- **Custom Services** - Dialog, Navigation, Notification, Theme services

---

## Project Structure

```
SaveState.Presentation/
├── App.axaml                    # Application entry point
├── App.axaml.cs                 # Application initialization
├── ViewLocator.cs               # View-ViewModel auto-resolution
│
├── ViewModels/                  # All ViewModels
│   ├── MainViewModel.cs         # Root navigation coordinator
│   ├── Shell/                   # Main application shell
│   │   ├── MainShellViewModel.cs
│   │   ├── LibraryViewModel.cs
│   │   ├── DashboardViewModel.cs
│   │   └── ...
│   ├── Library/                 # Game library feature
│   │   ├── LibraryViewModel.cs
│   │   ├── GameGridViewModel.cs
│   │   └── GameDetail/
│   ├── Dialogs/                 # Dialog ViewModels
│   ├── BigPicture/              # 10-foot UI mode
│   ├── Overlays/                # Overlay ViewModels
│   └── ...
│
├── Views/                       # All Views
│   ├── MainWindow.axaml
│   ├── Shell/
│   ├── Library/
│   ├── Dialogs/
│   ├── BigPicture/
│   └── ...
│
├── Services/                    # UI-specific services
│   ├── IDialogService.cs
│   ├── INavigationService.cs
│   ├── IThemeService.cs
│   └── ...
│
├── Converters/                  # Value converters
├── Styles/                      # Theme resources
└── Resources/                   # Localization resources
```

---

## Getting Started

### Prerequisites

- .NET 9 SDK
- Avalonia UI development tools (optional but recommended)
- Visual Studio 2022 or VS Code with C# Dev Kit

### Running the Application

```bash
# From repository root
dotnet run --project src/SaveState.Presentation

# Or navigate to project
cd src/SaveState.Presentation
dotnet run
```

### Building

```bash
dotnet build src/SaveState.Presentation/SaveState.Presentation.csproj
```

---

## Adding New Views

### 1. Create ViewModel

```csharp
// ViewModels/MyFeature/MyFeatureViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace SaveState.Presentation.ViewModels.MyFeature;

public partial class MyFeatureViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "My Feature";
    
    [RelayCommand]
    private async Task LoadAsync()
    {
        // Implementation
    }
}
```

### 2. Create View

```xml
<!-- Views/MyFeature/MyFeatureView.axaml -->
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:SaveState.Presentation.ViewModels.MyFeature"
             x:Class="SaveState.Presentation.Views.MyFeature.MyFeatureView"
             x:DataType="vm:MyFeatureViewModel">
    
    <Grid>
        <TextBlock Text="{Binding Title}" Classes="Header" />
    </Grid>
</UserControl>
```

```csharp
// Views/MyFeature/MyFeatureView.axaml.cs
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SaveState.Presentation.Views.MyFeature;

public partial class MyFeatureView : UserControl
{
    public MyFeatureView()
    {
        InitializeComponent();
    }
}
```

### 3. Register in DI

```csharp
// In App.axaml.cs or a ServiceRegistration class
services.AddTransient<MyFeatureViewModel>();
```

### 4. Navigate to View

```csharp
// From another ViewModel
await _navigationService.NavigateToAsync<MyFeatureViewModel>();

// Or by tab name
await _navigationService.NavigateToAsync("MyFeature");
```

---

## Adding New Dialogs

### 1. Create Dialog ViewModel

```csharp
// ViewModels/Dialogs/MyDialogViewModel.cs
public partial class MyDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;
    
    [ObservableProperty]
    private string _message;
    
    public MyDialogViewModel(string title, string message)
    {
        _title = title;
        _message = message;
    }
    
    [RelayCommand]
    private void Confirm()
    {
        CloseDialog(new MyDialogResult(/* ... */));
    }
    
    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }
    
    private void CloseDialog(object? result)
    {
        if (Application.Current?.ApplicationLifetime is 
            IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}

// Result record
public record MyDialogResult(string Value);
```

### 2. Create Dialog View

```xml
<!-- Views/Dialogs/MyDialog.axaml -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:SaveState.Presentation.ViewModels.Dialogs"
        x:Class="SaveState.Presentation.Views.Dialogs.MyDialog"
        x:DataType="vm:MyDialogViewModel"
        Title="{Binding Title}"
        Width="400"
        Height="200"
        WindowStartupLocation="CenterOwner">
    
    <StackPanel Margin="20" Spacing="10">
        <TextBlock Text="{Binding Message}" TextWrapping="Wrap" />
        
        <StackPanel Orientation="Horizontal" 
                    HorizontalAlignment="Right" 
                    Spacing="10">
            <Button Content="Cancel" 
                    Command="{Binding CancelCommand}" 
                    Classes="Secondary" />
            <Button Content="OK" 
                    Command="{Binding ConfirmCommand}" 
                    Classes="Primary" />
        </StackPanel>
    </StackPanel>
</Window>
```

### 3. Add to IDialogService

```csharp
// Services/IDialogService.cs
public interface IDialogService
{
    Task<MyDialogResult?> ShowMyDialogAsync(string title, string message);
}
```

### 4. Implement in DialogService

```csharp
// Services/DialogService.cs
public async Task<MyDialogResult?> ShowMyDialogAsync(string title, string message)
{
    var viewModel = new MyDialogViewModel(title, message);
    var dialog = new MyDialog { DataContext = viewModel };
    
    if (Application.Current?.ApplicationLifetime is 
        IClassicDesktopStyleApplicationLifetime desktop &&
        desktop.MainWindow != null)
    {
        var result = await dialog.ShowDialog<MyDialogResult?>(desktop.MainWindow);
        return result;
    }
    
    return null;
}
```

---

## Styling Guidelines

### Using Theme Resources

Always use static resources for colors to maintain theme consistency:

```xml
<!-- Good -->
<Border Background="{StaticResource CardBackgroundBrush}" />
<TextBlock Foreground="{StaticResource TextPrimaryBrush}" />

<!-- Avoid -->
<Border Background="#25262B" />
<TextBlock Foreground="White" />
```

### Control Style Classes

Use predefined style classes for consistent appearance:

```xml
<!-- Button styles -->
<Button Classes="Primary" />      <!-- Main action -->
<Button Classes="Secondary" />    <!-- Secondary action -->
<Button Classes="Nav" />          <!-- Navigation -->
<Button Classes="Outline" />      <!-- Outline button -->

<!-- Container styles -->
<Border Classes="Card" />         <!-- Standard card -->
<Border Classes="GlassContainer" /> <!-- Semi-transparent -->
<Border Classes="GameCard" />     <!-- Game card with hover effect -->

<!-- Text styles -->
<TextBlock Classes="Header" />    <!-- Page header -->
<TextBlock Classes="H1" />        <!-- Large heading -->
<TextBlock Classes="Body" />      <!-- Body text -->
<TextBlock Classes="Caption" />   <!-- Small text -->
```

### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| Views | PascalCase + View | `LibraryView.axaml` |
| ViewModels | PascalCase + ViewModel | `LibraryViewModel.cs` |
| UserControls | PascalCase | `GameCard.axaml` |
| Styles | PascalCase + Style | `PrimaryButtonStyle` |
| Resources | PascalCase + Resource | `CardBackgroundBrush` |

---

## Testing UI Changes

### Hot Reload

Avalonia supports hot reload for XAML changes during development:

1. Run the application with `dotnet run` or F5 in VS
2. Edit XAML files
3. Save to see changes immediately

### Manual Testing Checklist

When adding new UI features, verify:

- [ ] UI renders correctly at different window sizes
- [ ] Keyboard navigation works (Tab order)
- [ ] High contrast mode compatibility
- [ ] Light/Dark theme switching
- [ ] Empty states handled
- [ ] Loading states shown
- [ ] Error states handled gracefully
- [ ] Tooltips provided for icon-only buttons

---

## Data Binding Patterns

### Basic Binding

```xml
<!-- One-way binding -->
<TextBlock Text="{Binding Title}" />

<!-- Two-way binding -->
<TextBox Text="{Binding SearchText, Mode=TwoWay}" />

<!-- Command binding -->
<Button Command="{Binding SaveCommand}" />

<!-- Command with parameter -->
<Button Command="{Binding SelectCommand}" 
        CommandParameter="{Binding}" />
```

### Collection Binding

```xml
<ListBox ItemsSource="{Binding Games}" 
         SelectedItem="{Binding SelectedGame}">
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType="vm:GameCardViewModel">
            <TextBlock Text="{Binding Title}" />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### Converter Usage

```xml
<!-- Using converter -->
<Border Background="{Binding IsActive, 
                 Converter={StaticResource BoolToBrushConverter}}" />

<!-- Converter with parameter -->
<TextBlock Foreground="{Binding Status, 
               Converter={StaticResource StatusToColorConverter},
               ConverterParameter='Active|Inactive'}" />
```

---

## Common Patterns

### View Visibility Toggle

```xml
<Grid>
    <views:GameGridView IsVisible="{Binding IsGridView}" />
    <views:GameListView IsVisible="{Binding IsListView}" />
</Grid>
```

### Empty State

```xml
<Border IsVisible="{Binding IsEmpty}">
    <StackPanel>
        <TextBlock Text="No items found" Classes="Header" />
        <Button Content="Add Item" Command="{Binding AddCommand}" />
    </StackPanel>
</Border>
```

### Loading State

```xml
<Grid>
    <ContentControl IsVisible="{Binding !IsLoading}" />
    <ProgressBar IsVisible="{Binding IsLoading}" 
                 IsIndeterminate="True" />
</Grid>
```

---

## Architecture References

- [UI Architecture Guide](../../docs/guides/UI_ARCHITECTURE.md) - Comprehensive architecture documentation
- [UI Component Catalog](../../docs/guides/UI_COMPONENT_CATALOG.md) - Complete component reference
- [Patterns Cookbook](../../docs/architecture/PATTERNS_COOKBOOK.md) - Copy-paste code patterns
- [Engineering Rules](../../docs/architecture/ENGINEERING_RULES.md) - Coding standards

---

## Troubleshooting

### Common Issues

**View not found**
- Check naming convention: `MyFeatureViewModel` → `MyFeatureView`
- Verify namespace matches folder structure
- Ensure View is registered in DI container

**Bindings not working**
- Add `x:DataType` to root element
- Check property names match exactly
- Verify DataContext is set correctly

**Converters not found**
- Register in `App.axaml` resources
- Use correct `x:Key` in binding

**Styles not applied**
- Check style class spelling matches exactly
- Verify resource dictionaries are merged in `App.axaml`

---

## Contributing

When contributing UI changes:

1. Follow the MVVM pattern
2. Use existing theme resources
3. Add XML documentation to public members
4. Test with both light and dark themes
5. Ensure keyboard accessibility
6. Update this README if adding new patterns
