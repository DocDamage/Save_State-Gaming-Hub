using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the MUGEN tab.
/// </summary>
public partial class MugenViewModel : ObservableObject
{
    public MugenViewModel()
    {
        // TODO: Implement MUGEN functionality
    }

    /// <summary>
    /// Gets the display title for the MUGEN tab.
    /// </summary>
    public string Title => "MUGEN";
}