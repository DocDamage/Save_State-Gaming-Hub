using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for the process selector dialog.
/// Allows users to select a running process to attach to for memory inspection.
/// </summary>
public partial class ProcessSelectorDialogViewModel : ObservableObject
{
    private Action<int?>? _closeAction;
    private List<ProcessInfoViewModel> _allProcesses = new();

    /// <summary>
    /// Collection of filtered processes displayed in the dialog.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProcessInfoViewModel> _processes = new();

    /// <summary>
    /// The currently selected process.
    /// </summary>
    [ObservableProperty]
    private ProcessInfoViewModel? _selectedProcess;

    /// <summary>
    /// Search text for filtering the process list.
    /// </summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Indicates whether the process list is being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Initializes a new instance of the process selector dialog view model.
    /// Automatically loads the process list.
    /// </summary>
    public ProcessSelectorDialogViewModel()
    {
        _ = RefreshProcessesAsync();
    }

    /// <summary>
    /// Sets the action to be called when the dialog closes.
    /// </summary>
    /// <param name="closeAction">Action that receives the selected process ID or null if cancelled.</param>
    public void SetCloseAction(Action<int?> closeAction)
    {
        _closeAction = closeAction;
    }

    /// <summary>
    /// Refreshes the list of running processes.
    /// </summary>
    [RelayCommand]
    private async Task RefreshProcessesAsync()
    {
        IsLoading = true;
        await Task.Run(() =>
        {
            _allProcesses.Clear();
            var systemProcesses = Process.GetProcesses();
            foreach (var p in systemProcesses)
            {
                try
                {
                    // Accessing process properties might throw for system processes
                    var title = p.MainWindowTitle;
                    var memoryBytes = p.WorkingSet64;

                    // Filter: include processes with window titles OR significant memory usage (>100MB)
                    // This helps identify games while still allowing background process selection
                    if (string.IsNullOrEmpty(title) && memoryBytes < 100 * 1024 * 1024)
                        continue;

                    _allProcesses.Add(new ProcessInfoViewModel
                    {
                        Id = p.Id,
                        Name = p.ProcessName,
                        WindowTitle = title,
                        MemoryUsage = $"{memoryBytes / (1024 * 1024)} MB",
                        IsLikelyGame = memoryBytes > 200 * 1024 * 1024 && !string.IsNullOrEmpty(title)
                    });
                }
                catch
                {
                    // Ignore inaccessible processes (system processes, etc.)
                }
            }

            // Sort by name alphabetically
            _allProcesses.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        });

        ApplyFilter();
        IsLoading = false;
    }

    /// <summary>
    /// Called when SearchText changes to apply the filter.
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    /// <summary>
    /// Applies the current search filter to the process list.
    /// </summary>
    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Processes = new ObservableCollection<ProcessInfoViewModel>(_allProcesses);
        }
        else
        {
            var filtered = _allProcesses.Where(p =>
                p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.WindowTitle.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                p.Id.ToString().Contains(SearchText));
            Processes = new ObservableCollection<ProcessInfoViewModel>(filtered);
        }
    }

    /// <summary>
    /// Confirms the selection and closes the dialog with the selected process ID.
    /// </summary>
    [RelayCommand]
    private void Attach()
    {
        if (SelectedProcess != null)
        {
            _closeAction?.Invoke(SelectedProcess.Id);
        }
    }

    /// <summary>
    /// Cancels the dialog and returns null.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }
}

/// <summary>
/// Represents a process item displayed in the selector dialog.
/// </summary>
public class ProcessInfoViewModel
{
    /// <summary>
    /// The process ID (PID).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The process name (executable name without extension).
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The main window title of the process.
    /// </summary>
    public string WindowTitle { get; set; } = "";

    /// <summary>
    /// Formatted memory usage string (e.g., "512 MB").
    /// </summary>
    public string MemoryUsage { get; set; } = "";

    /// <summary>
    /// Indicates whether this process is likely a game based on memory usage and window presence.
    /// </summary>
    public bool IsLikelyGame { get; set; }

    /// <summary>
    /// Display text combining name and ID for compact representation.
    /// </summary>
    public string DisplayText => $"{Name} ({Id})";
}
