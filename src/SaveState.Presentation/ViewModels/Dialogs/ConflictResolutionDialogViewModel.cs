using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for sync conflict resolution dialog.
/// </summary>
public partial class ConflictResolutionDialogViewModel : ObservableObject
{
    public ConflictResolutionDialogViewModel(SyncConflictViewModel[] conflicts)
    {
        Conflicts = new ObservableCollection<ConflictItemViewModel>(
            conflicts.Select(c => new ConflictItemViewModel(c)));

        ResolutionStrategies = new ObservableCollection<string>
        {
            "Keep Local",
            "Keep Cloud",
            "Keep Both",
            "Skip"
        };
    }

    /// <summary>
    /// Gets the list of conflicts to resolve.
    /// </summary>
    public ObservableCollection<ConflictItemViewModel> Conflicts { get; }

    /// <summary>
    /// Gets the available resolution strategies.
    /// </summary>
    public ObservableCollection<string> ResolutionStrategies { get; }

    /// <summary>
    /// Command to resolve all conflicts.
    /// </summary>
    [RelayCommand]
    private void ResolveAll()
    {
        var resolutions = new Dictionary<string, string>();
        foreach (var conflict in Conflicts)
        {
            resolutions[conflict.FilePath] = conflict.SelectedResolution;
        }

        Result = new ConflictResolutionResult(resolutions);
    }

    /// <summary>
    /// Command to cancel the dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Result = null;
    }

    /// <summary>
    /// Gets the result of the dialog.
    /// </summary>
    public ConflictResolutionResult? Result { get; private set; }
}

/// <summary>
/// View model for a single conflict item.
/// </summary>
public partial class ConflictItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedResolution = "Keep Local";

    public ConflictItemViewModel(SyncConflictViewModel conflict)
    {
        FilePath = conflict.FilePath;
        LocalModified = conflict.LocalModified;
        CloudModified = conflict.CloudModified;
        LocalSize = conflict.LocalSize;
        CloudSize = conflict.CloudSize;
    }

    /// <summary>
    /// Gets the file path of the conflicting file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the local modification date.
    /// </summary>
    public DateTime LocalModified { get; }

    /// <summary>
    /// Gets the cloud modification date.
    /// </summary>
    public DateTime CloudModified { get; }

    /// <summary>
    /// Gets the local file size in bytes.
    /// </summary>
    public long LocalSize { get; }

    /// <summary>
    /// Gets the cloud file size in bytes.
    /// </summary>
    public long CloudSize { get; }

    /// <summary>
    /// Gets a formatted string for local info.
    /// </summary>
    public string LocalInfo => $"Modified: {LocalModified:g}, Size: {LocalSize:N0} bytes";

    /// <summary>
    /// Gets a formatted string for cloud info.
    /// </summary>
    public string CloudInfo => $"Modified: {CloudModified:g}, Size: {CloudSize:N0} bytes";
}
