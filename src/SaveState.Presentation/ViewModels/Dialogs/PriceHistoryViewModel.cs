using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class PriceHistoryViewModel : ObservableObject
{
    public Action? RequestClose { get; set; }

    public void Initialize(string gameTitle)
    {
        // Implementation
    }
}
