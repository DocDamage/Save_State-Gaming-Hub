using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Terminal tab.
/// </summary>
public partial class TerminalViewModel : ObservableObject
{
    public TerminalViewModel()
    {
        // TODO: Implement terminal functionality
    }

    /// <summary>
    /// Gets the display title for the terminal tab.
    /// </summary>
    public string Title => "Terminal";
}