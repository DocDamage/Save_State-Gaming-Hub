using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.RomManagement.RomValidation.Commands;
using SaveState.Application.RomManagement.RomValidation.Queries;
using SaveState.Core.RomManagement.RomValidation;
using SaveState.Core.RomManagement.RomValidation.Services;

namespace SaveState.Presentation.ViewModels.RomValidation;

/// <summary>
/// ViewModel for ROM validation and integrity checking.
/// </summary>
public partial class RomValidationViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private double _validationProgress;

    [ObservableProperty]
    private bool _isValidating;

    [ObservableProperty]
    private RomValidationReport? _selectedReport;

    [ObservableProperty]
    private RomValidationStatistics? _statistics;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private Guid? _selectedPlatformId;

    // Collections
    public ObservableCollection<RomValidationReport> ValidationReports { get; } = new();
    public ObservableCollection<DuplicateRomInfoViewModel> DuplicateRoms { get; } = new();
    public ObservableCollection<BadDumpInfoViewModel> BadDumps { get; } = new();
    public ObservableCollection<PlatformViewModel> Platforms { get; } = new();

    // Validation options
    [ObservableProperty]
    private bool _calculateCrc32 = true;

    [ObservableProperty]
    private bool _calculateMd5 = true;

    [ObservableProperty]
    private bool _calculateSha1 = true;

    [ObservableProperty]
    private bool _matchAgainstDatFiles = true;

    [ObservableProperty]
    private string _datFilePath = string.Empty;

    public RomValidationViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Load validation statistics.
    /// </summary>
    [RelayCommand]
    private async Task LoadStatisticsAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading statistics...";

        try
        {
            var result = await _mediator.Send(new GetRomValidationStatisticsQuery());

            if (result.IsSuccess)
            {
                Statistics = result.Value;
                StatusMessage = $"Loaded statistics: {Statistics?.ValidatedRoms ?? 0} validated ROMs";
            }
            else
            {
                StatusMessage = $"Failed to load statistics: {result.Error}";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Validate a specific ROM file.
    /// </summary>
    [RelayCommand]
    private async Task ValidateRomAsync(Guid romFileId)
    {
        IsValidating = true;
        StatusMessage = "Validating ROM...";

        try
        {
            var options = new RomValidationOptions
            {
                CalculateCrc32 = CalculateCrc32,
                CalculateMd5 = CalculateMd5,
                CalculateSha1 = CalculateSha1,
                CalculateSha256 = false,
                MatchAgainstDatFiles = MatchAgainstDatFiles && !string.IsNullOrEmpty(DatFilePath),
                DatFilePaths = MatchAgainstDatFiles && !string.IsNullOrEmpty(DatFilePath)
                    ? new List<string> { DatFilePath }
                    : new List<string>()
            };

            var result = await _mediator.Send(new ValidateRomCommand(romFileId, options));

            if (result.IsSuccess)
            {
                SelectedReport = result.Value;
                StatusMessage = $"Validation completed: {result.Value.Status}";

                // Refresh the reports list
                await LoadValidationReportsAsync();
            }
            else
            {
                StatusMessage = $"Validation failed: {result.Error}";
            }
        }
        finally
        {
            IsValidating = false;
        }
    }

    /// <summary>
    /// Batch validate ROMs for a platform.
    /// </summary>
    [RelayCommand]
    private async Task BatchValidateAsync()
    {
        if (!SelectedPlatformId.HasValue)
        {
            StatusMessage = "Please select a platform first";
            return;
        }

        IsValidating = true;
        StatusMessage = "Starting batch validation...";

        try
        {
            var options = new RomValidationOptions
            {
                CalculateCrc32 = CalculateCrc32,
                CalculateMd5 = CalculateMd5,
                CalculateSha1 = CalculateSha1,
                CalculateSha256 = false,
                MatchAgainstDatFiles = MatchAgainstDatFiles && !string.IsNullOrEmpty(DatFilePath),
                DatFilePaths = MatchAgainstDatFiles && !string.IsNullOrEmpty(DatFilePath)
                    ? new List<string> { DatFilePath }
                    : new List<string>()
            };

            var command = new BatchValidateRomsCommand(
                "Batch Validation",
                null,
                new List<Guid> { SelectedPlatformId.Value },
                options);

            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                StatusMessage = $"Batch validation completed: {result.Value.ProcessedRoms}/{result.Value.TotalRoms} ROMs processed";
                await LoadStatisticsAsync();
            }
            else
            {
                StatusMessage = $"Batch validation failed: {result.Error}";
            }
        }
        finally
        {
            IsValidating = false;
        }
    }

    /// <summary>
    /// Find duplicate ROM files.
    /// </summary>
    [RelayCommand]
    private async Task FindDuplicatesAsync()
    {
        IsLoading = true;
        StatusMessage = "Finding duplicates...";

        try
        {
            var result = await _mediator.Send(new GetDuplicateRomsQuery(SelectedPlatformId, HashAlgorithmType.Sha1));

            if (result.IsSuccess)
            {
                DuplicateRoms.Clear();
                foreach (var dup in result.Value)
                {
                    DuplicateRoms.Add(new DuplicateRomInfoViewModel(dup));
                }
                StatusMessage = $"Found {result.Value.Count} sets of duplicates";
            }
            else
            {
                StatusMessage = $"Failed to find duplicates: {result.Error}";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Identify bad dump ROMs.
    /// </summary>
    [RelayCommand]
    private async Task IdentifyBadDumpsAsync()
    {
        IsLoading = true;
        StatusMessage = "Identifying bad dumps...";

        try
        {
            var result = await _mediator.Send(new GetBadDumpsQuery(SelectedPlatformId));

            if (result.IsSuccess)
            {
                BadDumps.Clear();
                foreach (var dump in result.Value)
                {
                    BadDumps.Add(new BadDumpInfoViewModel(dump));
                }
                StatusMessage = $"Found {result.Value.Count} bad dumps";
            }
            else
            {
                StatusMessage = $"Failed to identify bad dumps: {result.Error}";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Load validation reports.
    /// </summary>
    [RelayCommand]
    private async Task LoadValidationReportsAsync()
    {
        // This would typically load recent validation reports
        // For now, we'll just refresh statistics
        await LoadStatisticsAsync();
    }

    /// <summary>
    /// Export validation results.
    /// </summary>
    [RelayCommand]
    private async Task ExportResultsAsync(string outputPath)
    {
        IsLoading = true;
        StatusMessage = "Exporting results...";

        try
        {
            var result = await _mediator.Send(new ExportValidationResultsCommand(
                outputPath,
                ValidationExportFormat.Html,
                SelectedPlatformId));

            if (result.IsSuccess)
            {
                StatusMessage = $"Exported to: {result.Value}";
            }
            else
            {
                StatusMessage = $"Export failed: {result.Error}";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

}

/// <summary>
/// ViewModel wrapper for DuplicateRomInfo.
/// </summary>
public class DuplicateRomInfoViewModel
{
    private readonly DuplicateRomInfo _info;

    public DuplicateRomInfoViewModel(DuplicateRomInfo info)
    {
        _info = info;
    }

    public string Hash => _info.Hash.Substring(0, Math.Min(16, _info.Hash.Length)) + "...";
    public string HashType => _info.HashType.ToString();
    public int Count => _info.Count;
    public long WastedSpace => _info.WastedSpace;
    public bool AreInDifferentLocations => _info.AreInDifferentLocations;
    public List<RomDuplicateEntry> Duplicates => _info.Duplicates;
}

/// <summary>
/// ViewModel wrapper for BadDumpInfo.
/// </summary>
public class BadDumpInfoViewModel
{
    private readonly BadDumpInfo _info;

    public BadDumpInfoViewModel(BadDumpInfo info)
    {
        _info = info;
    }

    public Guid RomFileId => _info.RomFileId;
    public string FileName => _info.FileName;
    public string PlatformName => _info.PlatformName;
    public RomDumpStatus DumpStatus => _info.DumpStatus;
    public string IssueDescription => _info.IssueDescription;
    public string RecommendedAction => _info.RecommendedAction;
}

/// <summary>
/// ViewModel for platform selection.
/// </summary>
public class PlatformViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
}
