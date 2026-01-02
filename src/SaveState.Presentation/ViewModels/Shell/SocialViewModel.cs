using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Social tab.
/// </summary>
public partial class SocialViewModel : ObservableObject
{
    public SocialViewModel()
    {
        // TODO: Implement social functionality
    }

    /// <summary>
    /// Gets the display title for the social tab.
    /// </summary>
    public string Title => "Social";
}