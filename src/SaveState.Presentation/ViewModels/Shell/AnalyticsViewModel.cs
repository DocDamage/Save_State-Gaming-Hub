using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Analytics tab.
/// </summary>
public partial class AnalyticsViewModel : ObservableObject
{
    public AnalyticsViewModel()
    {
        // TODO: Implement analytics functionality
    }

    /// <summary>
    /// Gets the display title for the analytics tab.
    /// </summary>
    public string Title => "Analytics";
}