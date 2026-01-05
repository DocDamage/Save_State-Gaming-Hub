using CommunityToolkit.Mvvm.ComponentModel;

namespace SaveState.Presentation.ViewModels.BigPicture;

/// <summary>
/// Represents a game collection in the Big Picture mode.
/// </summary>
public partial class CollectionViewModel : ObservableObject
{
    /// <summary>
    /// Gets or sets the unique identifier for the collection.
    /// </summary>
    [ObservableProperty]
    private string id = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the collection.
    /// </summary>
    [ObservableProperty]
    private string name = string.Empty;

    /// <summary>
    /// Gets or sets the icon identifier for the collection.
    /// </summary>
    [ObservableProperty]
    private string icon = string.Empty;

    /// <summary>
    /// Gets or sets the number of games in this collection.
    /// </summary>
    [ObservableProperty]
    private int gameCount;
}
