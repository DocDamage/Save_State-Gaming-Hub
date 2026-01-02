using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Tools tab.
/// </summary>
public partial class ToolsViewModel : ObservableObject
{
    public ToolsViewModel()
    {
        // TODO: Implement tools functionality
    }

    /// <summary>
    /// Gets the display title for the tools tab.
    /// </summary>
    public string Title => "Tools";
}