using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for the Library tab.
/// </summary>
public partial class LibraryViewModel : ObservableObject
{
    public LibraryViewModel()
    {
    }

    /// <summary>
    /// Gets the display title for the library.
    /// </summary>
    public string Title => "Library";
}
