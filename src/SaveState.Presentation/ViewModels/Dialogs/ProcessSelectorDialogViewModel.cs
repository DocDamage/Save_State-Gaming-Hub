using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SaveState.Presentation.ViewModels.Dialogs;

public partial class ProcessSelectorDialogViewModel : ObservableObject
{
    private Action<int?>? _closeAction;
    private List<ProcessItem> _allProcesses = new();

    [ObservableProperty]
    private ObservableCollection<ProcessItem> _processes = new();

    [ObservableProperty]
    private ProcessItem? _selectedProcess;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public ProcessSelectorDialogViewModel()
    {
        _ = LoadProcessesAsync();
    }

    public void SetCloseAction(Action<int?> closeAction)
    {
        _closeAction = closeAction;
    }

    [RelayCommand]
    private async Task LoadProcessesAsync()
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
                    // Filter out system processes usually irrelevant for gaming if desired,
                    // but for now list all that have a window title or reasonable looking name.
                    // Accessing MainModule might throw for some system processes.

                    var title = p.MainWindowTitle;

                    // Simple filter: skip processes with no title to reduce noise,
                    // unless user wants to attach to background process.
                    // Games usually have a window.
                    if (string.IsNullOrEmpty(title)) continue;

                    _allProcesses.Add(new ProcessItem(p.Id, p.ProcessName, title));
                }
                catch
                {
                    // Ignore inaccessible processes
                }
            }

            // Sort
            _allProcesses.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        });

        ApplyFilter();
        IsLoading = false;
    }

    partial void OnFilterTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(FilterText))
        {
            Processes = new ObservableCollection<ProcessItem>(_allProcesses);
        }
        else
        {
            var filtered = _allProcesses.Where(p =>
                p.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                p.Title.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                p.Id.ToString().Contains(FilterText));
            Processes = new ObservableCollection<ProcessItem>(filtered);
        }
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedProcess != null)
        {
            _closeAction?.Invoke(SelectedProcess.Id);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _closeAction?.Invoke(null);
    }
}

public record ProcessItem(int Id, string Name, string Title)
{
    public string DisplayText => $"{Name} ({Id})";
}
