using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for selecting a branch from available branches.
/// </summary>
public partial class BranchSelectionDialogViewModel : ObservableObject
{
    private readonly ILogger<BranchSelectionDialogViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<BranchOptionViewModel> _branches = new();

    [ObservableProperty]
    private BranchOptionViewModel? _selectedBranch;

    [ObservableProperty]
    private string _currentBranchName = "main";

    [ObservableProperty]
    private bool _canConfirm;

    public BranchSelectionDialogViewModel(ILogger<BranchSelectionDialogViewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the dialog with available branches.
    /// </summary>
    public void Initialize(string currentBranchName, BranchOptionViewModel[] availableBranches)
    {
        CurrentBranchName = currentBranchName;
        Branches.Clear();

        foreach (var branch in availableBranches)
        {
            branch.IsCurrent = branch.Name == currentBranchName;
            Branches.Add(branch);
        }

        // Select current branch by default
        SelectedBranch = Branches.FirstOrDefault(b => b.IsCurrent);
        UpdateCanConfirm();
    }

    partial void OnSelectedBranchChanged(BranchOptionViewModel? value)
    {
        UpdateCanConfirm();
    }

    private void UpdateCanConfirm()
    {
        CanConfirm = SelectedBranch != null && SelectedBranch.Name != CurrentBranchName;
    }

    [RelayCommand]
    private void SelectBranch(BranchOptionViewModel branch)
    {
        SelectedBranch = branch;
    }

    [RelayCommand(CanExecute = nameof(CanConfirm))]
    private void Confirm()
    {
        _logger.LogInformation("Branch '{BranchName}' selected for switching", SelectedBranch?.Name);
        // Dialog result will be handled by the caller
    }

    [RelayCommand]
    private void Cancel()
    {
        _logger.LogDebug("Branch selection cancelled");
        // Dialog result will be handled by the caller
    }
}

/// <summary>
/// Represents a branch option in the selection dialog.
/// </summary>
public partial class BranchOptionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _branchType = "Feature";

    [ObservableProperty]
    private int _saveStateCount;

    [ObservableProperty]
    private DateTime _lastModified;

    [ObservableProperty]
    private bool _isCurrent;

    [ObservableProperty]
    private string _icon = "📁";

    public string LastModifiedText => LastModified == default
        ? "Never"
        : (DateTime.UtcNow - LastModified).TotalDays < 1
            ? $"Today {LastModified:HH:mm}"
            : $"{LastModified:MMM d, yyyy}";

    public string SaveCountText => $"{SaveStateCount} save{(SaveStateCount == 1 ? "" : "s")}";

    public string StatusText => IsCurrent ? "Current Branch" : "Available";

    public string StatusColor => IsCurrent ? "#4CAF50" : "#666666";

    public string BranchTypeIcon => BranchType switch
    {
        "Main" => "🌟",
        "Feature" => "🔧",
        "Experiment" => "🧪",
        "Backup" => "💾",
        _ => "📁"
    };
}
