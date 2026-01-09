using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for comparing two branches.
/// </summary>
public partial class BranchComparisonDialogViewModel : ObservableObject
{
    private readonly ILogger<BranchComparisonDialogViewModel> _logger;

    [ObservableProperty]
    private string _leftBranchName = string.Empty;

    [ObservableProperty]
    private string _rightBranchName = string.Empty;

    [ObservableProperty]
    private int _leftSaveCount;

    [ObservableProperty]
    private int _rightSaveCount;

    [ObservableProperty]
    private ObservableCollection<SaveStateDiffViewModel> _differences = new();

    [ObservableProperty]
    private string _summaryText = string.Empty;

    [ObservableProperty]
    private bool _hasConflicts;

    [ObservableProperty]
    private int _conflictCount;

    [ObservableProperty]
    private int _additionsInLeft;

    [ObservableProperty]
    private int _additionsInRight;

    [ObservableProperty]
    private int _commonSaves;

    public BranchComparisonDialogViewModel(ILogger<BranchComparisonDialogViewModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the comparison with two branches.
    /// </summary>
    public void Initialize(
        string leftBranchName,
        string rightBranchName,
        SaveStateDiffViewModel[] differences)
    {
        LeftBranchName = leftBranchName;
        RightBranchName = rightBranchName;

        Differences.Clear();
        foreach (var diff in differences)
        {
            Differences.Add(diff);
        }

        // Calculate statistics
        AdditionsInLeft = Differences.Count(d => d.Status == DiffStatus.OnlyInLeft);
        AdditionsInRight = Differences.Count(d => d.Status == DiffStatus.OnlyInRight);
        CommonSaves = Differences.Count(d => d.Status == DiffStatus.InBoth);
        ConflictCount = Differences.Count(d => d.Status == DiffStatus.Conflict);

        LeftSaveCount = AdditionsInLeft + CommonSaves;
        RightSaveCount = AdditionsInRight + CommonSaves;

        HasConflicts = ConflictCount > 0;

        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (HasConflicts)
        {
            SummaryText = $"⚠️ {ConflictCount} conflict{(ConflictCount == 1 ? "" : "s")} detected. " +
                         $"{CommonSaves} common, {AdditionsInLeft} only in {LeftBranchName}, " +
                         $"{AdditionsInRight} only in {RightBranchName}.";
        }
        else
        {
            SummaryText = $"✓ No conflicts. {CommonSaves} common save{(CommonSaves == 1 ? "" : "s")}, " +
                         $"{AdditionsInLeft} only in {LeftBranchName}, " +
                         $"{AdditionsInRight} only in {RightBranchName}.";
        }
    }

    [RelayCommand]
    private void ViewDetails(SaveStateDiffViewModel diff)
    {
        _logger.LogInformation("Viewing details for save state: {SaveName}", diff.Name);
        // Could open a detailed comparison view
    }

    [RelayCommand]
    private void Close()
    {
        _logger.LogDebug("Branch comparison dialog closed");
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }
}

/// <summary>
/// Represents a save state difference between two branches.
/// </summary>
public partial class SaveStateDiffViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private DiffStatus _status;

    [ObservableProperty]
    private DateTime? _leftTimestamp;

    [ObservableProperty]
    private DateTime? _rightTimestamp;

    [ObservableProperty]
    private long? _leftSize;

    [ObservableProperty]
    private long? _rightSize;

    public string StatusIcon => Status switch
    {
        DiffStatus.InBoth => "✓",
        DiffStatus.OnlyInLeft => "←",
        DiffStatus.OnlyInRight => "→",
        DiffStatus.Conflict => "⚠️",
        _ => "?"
    };

    public string StatusText => Status switch
    {
        DiffStatus.InBoth => "In Both",
        DiffStatus.OnlyInLeft => "Only in Left",
        DiffStatus.OnlyInRight => "Only in Right",
        DiffStatus.Conflict => "Conflict",
        _ => "Unknown"
    };

    public string StatusColor => Status switch
    {
        DiffStatus.InBoth => "#4CAF50",
        DiffStatus.OnlyInLeft => "#2196F3",
        DiffStatus.OnlyInRight => "#FF9800",
        DiffStatus.Conflict => "#F44336",
        _ => "#666666"
    };

    public string LeftTimestampText => LeftTimestamp.HasValue
        ? LeftTimestamp.Value.ToString("MMM d, yyyy HH:mm")
        : "-";

    public string RightTimestampText => RightTimestamp.HasValue
        ? RightTimestamp.Value.ToString("MMM d, yyyy HH:mm")
        : "-";

    public string LeftSizeText => LeftSize.HasValue
        ? FormatSize(LeftSize.Value)
        : "-";

    public string RightSizeText => RightSize.HasValue
        ? FormatSize(RightSize.Value)
        : "-";

    private static string FormatSize(long bytes)
    {
        var mb = bytes / (1024.0 * 1024.0);
        return mb < 1024 ? $"{mb:F1} MB" : $"{mb / 1024.0:F2} GB";
    }
}

public enum DiffStatus
{
    InBoth,
    OnlyInLeft,
    OnlyInRight,
    Conflict
}
