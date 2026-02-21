using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Overlays;

public abstract partial class OverlayViewModelBase : ObservableObject
{
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private string? _title;
    
    [RelayCommand]
    protected virtual void Close()
    {
        IsVisible = false;
    }
    
    public virtual Task ShowAsync()
    {
        IsVisible = true;
        return Task.CompletedTask;
    }
    
    public virtual Task HideAsync()
    {
        IsVisible = false;
        return Task.CompletedTask;
    }
}
