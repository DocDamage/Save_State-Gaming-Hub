using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class RetroArchTabViewModel : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
}
