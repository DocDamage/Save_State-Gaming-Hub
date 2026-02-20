using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.GameLibrary.Testing;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Tools;

/// <summary>
/// ViewModel for the signature testing tool.
/// </summary>
public partial class SignatureTesterViewModel : ObservableObject, IDisposable
{
    private readonly ISignatureVerificationService _verificationService;
    private readonly IMemoryPatternDatabase _patternDatabase;
    private readonly IGameMemoryReader _memoryReader;
    private readonly IDialogService _dialogService;
    private readonly ILogger<SignatureTesterViewModel> _logger;
    private readonly ITimeProvider _timeProvider;
    private SignatureTestRunner? _testRunner;

    #region Observable Properties

    [ObservableProperty]
    private ObservableCollection<SelectableSignatureViewModel> _availableSignatures = new();

    [ObservableProperty]
    private ObservableCollection<SignatureTestResultViewModel> _testResults = new();

    [ObservableProperty]
    private string _selectedGameTitle = "";

    [ObservableProperty]
    private ObservableCollection<string> _availableGames = new();

    [ObservableProperty]
    private int? _selectedProcessId;

    [ObservableProperty]
    private bool _isTesting;

    [ObservableProperty]
    private double _testProgress;

    [ObservableProperty]
    private string _currentTestStatus = "Ready";

    [ObservableProperty]
    private string _searchFilter = "";

    [ObservableProperty]
    private bool _showPassedOnly;

    [ObservableProperty]
    private bool _showFailedOnly;

    [ObservableProperty]
    private SignatureTestResultViewModel? _selectedResult;

    [ObservableProperty]
    private ObservableCollection<PatternFixSuggestionViewModel> _suggestedFixes = new();

    [ObservableProperty]
    private bool _hasTestResults;

    [ObservableProperty]
    private TestSummaryStatsViewModel _summaryStats = new();

    [ObservableProperty]
    private bool _isProcessAttached;

    [ObservableProperty]
    private bool _skipDynamicTests = true;

    [ObservableProperty]
    private bool _runInParallel = true;

    [ObservableProperty]
    private int _stabilitySampleCount = 5;

    [ObservableProperty]
    private double _minimumConfidence = 0.7;

    #endregion

    public SignatureTesterViewModel(
        ISignatureVerificationService verificationService,
        IMemoryPatternDatabase patternDatabase,
        IGameMemoryReader memoryReader,
        IDialogService dialogService,
        ILogger<SignatureTesterViewModel> logger,
        ITimeProvider timeProvider)
    {
        _verificationService = verificationService;
        _patternDatabase = patternDatabase;
        _memoryReader = memoryReader;
        _dialogService = dialogService;
        _logger = logger;
        _timeProvider = timeProvider;

        // Initialize test runner - logger will be injected properly via DI in real implementation
        _testRunner = new SignatureTestRunner(
            verificationService,
            NullLogger<SignatureTestRunner>.Instance,
            timeProvider);

        // Load available games
        _ = LoadAvailableGamesAsync();
    }

    #region Commands

    [RelayCommand]
    private async Task LoadAvailableGamesAsync()
    {
        try
        {
            // Get games with memory signatures from the database
            var gamesResult = await _patternDatabase.GetAllGamesWithSignaturesAsync();
            if (gamesResult.IsSuccess && gamesResult.Value != null)
            {
                AvailableGames.Clear();
                foreach (var game in gamesResult.Value.OrderBy(g => g))
                {
                    AvailableGames.Add(game);
                }
            }

            CurrentTestStatus = $"Loaded {AvailableGames.Count} games with signatures";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load available games");
            CurrentTestStatus = "Failed to load games";
        }
    }

    [RelayCommand]
    private async Task SelectGameAsync(string gameTitle)
    {
        SelectedGameTitle = gameTitle;
        await LoadSignaturesForGameAsync(gameTitle);
    }

    [RelayCommand]
    private async Task LoadSignaturesForGameAsync(string gameTitle)
    {
        try
        {
            AvailableSignatures.Clear();

            var signaturesResult = await _patternDatabase.GetSignaturesForGameAsync(gameTitle);
            if (signaturesResult.IsSuccess && signaturesResult.Value != null)
            {
                foreach (var signature in signaturesResult.Value)
                {
                    AvailableSignatures.Add(new SelectableSignatureViewModel
                    {
                        Signature = signature,
                        IsSelected = true,
                        HealthScore = "-",
                        LastVerified = "Never"
                    });
                }
            }

            CurrentTestStatus = $"Loaded {AvailableSignatures.Count} signatures for {gameTitle}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load signatures for game {Game}", gameTitle);
            CurrentTestStatus = $"Failed to load signatures for {gameTitle}";
        }
    }

    [RelayCommand]
    private async Task AttachToProcessAsync()
    {
        var processId = await _dialogService.ShowProcessSelectorAsync();
        if (processId.HasValue)
        {
            var result = await _memoryReader.AttachToProcessAsync(processId.Value);
            if (result.IsSuccess)
            {
                SelectedProcessId = processId;
                IsProcessAttached = true;
                CurrentTestStatus = $"Attached to process {processId}";
            }
            else
            {
                await _dialogService.ShowErrorAsync("Attachment Failed", result.Error ?? "Unknown error");
                CurrentTestStatus = $"Failed to attach: {result.Error}";
            }
        }
    }

    [RelayCommand]
    private async Task DetachFromProcessAsync()
    {
        await _memoryReader.DetachAsync();
        IsProcessAttached = false;
        SelectedProcessId = null;
        CurrentTestStatus = "Detached from process";
    }

    [RelayCommand]
    private async Task RunTestsAsync()
    {
        if (!SelectedProcessId.HasValue)
        {
            await _dialogService.ShowErrorAsync("No Process", "Please attach to a game process first.");
            return;
        }

        var selectedSignatures = AvailableSignatures
            .Where(s => s.IsSelected)
            .Select(s => s.Signature)
            .ToList();

        if (selectedSignatures.Count == 0)
        {
            await _dialogService.ShowErrorAsync("No Signatures", "Please select at least one signature to test.");
            return;
        }

        IsTesting = true;
        TestProgress = 0;
        TestResults.Clear();
        SuggestedFixes.Clear();
        HasTestResults = false;

        try
        {
            CurrentTestStatus = $"Testing {selectedSignatures.Count} signatures...";

            var options = new TestSuiteOptions
            {
                RunInParallel = RunInParallel,
                SkipDynamicTests = SkipDynamicTests,
                StabilitySampleCount = StabilitySampleCount,
                MinimumConfidenceThreshold = MinimumConfidence,
                ProgressReporter = new Progress<TestProgress>(OnTestProgress)
            };

            var results = await _testRunner!.RunTestSuiteAsync(
                selectedSignatures,
                SelectedProcessId.Value,
                options);

            // Update UI with results
            foreach (var result in results.Results)
            {
                TestResults.Add(new SignatureTestResultViewModel
                {
                    SignatureName = result.SignatureName,
                    GameTitle = result.GameTitle,
                    Status = result.OverallPassed ? TestStatusIcon.Passed : TestStatusIcon.Failed,
                    StatusText = result.OverallPassed ? "Passed" : "Failed",
                    Confidence = result.Confidence,
                    HealthScore = result.HealthScore,
                    HealthRating = result.HealthRating,
                    Duration = result.Duration,
                    FailureReason = result.FailureReason,
                    TestDetails = result.Tests.Select(t => new TestDetailViewModel
                    {
                        TestType = t.TestType.ToString(),
                        Passed = t.Passed,
                        Message = t.Message
                    }).ToList(),
                    IsSelected = false
                });

                // Update available signatures with health info
                var availableSig = AvailableSignatures.FirstOrDefault(s =>
                    s.Signature.Name == result.SignatureName);
                if (availableSig != null)
                {
                    availableSig.HealthScore = result.HealthScore.ToString();
                    availableSig.LastVerified = _timeProvider.Now.ToString("g");
                }
            }

            // Update summary stats
            UpdateSummaryStats(results);
            HasTestResults = true;

            CurrentTestStatus = $"Testing complete: {results.PassedCount}/{results.TotalSignatures} passed";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running signature tests");
            CurrentTestStatus = $"Test error: {ex.Message}";
            await _dialogService.ShowErrorAsync("Test Error", ex.Message);
        }
        finally
        {
            IsTesting = false;
            TestProgress = 100;
        }
    }

    [RelayCommand]
    private async Task TestSingleSignatureAsync(GameMemorySignature signature)
    {
        if (!SelectedProcessId.HasValue)
        {
            await _dialogService.ShowErrorAsync("No Process", "Please attach to a game process first.");
            return;
        }

        IsTesting = true;
        CurrentTestStatus = $"Testing {signature.Name}...";

        try
        {
            var options = new VerificationOptions
            {
                SkipDynamicTests = SkipDynamicTests,
                StabilitySampleCount = StabilitySampleCount,
                MinimumConfidenceThreshold = MinimumConfidence
            };

            var result = await _verificationService.VerifySignatureAsync(
                signature,
                SelectedProcessId.Value,
                options);

            if (result.IsSuccess)
            {
                var vr = result.Value;

                // Add or update result in the list
                var existingResult = TestResults.FirstOrDefault(r => r.SignatureName == signature.Name);
                if (existingResult != null)
                {
                    TestResults.Remove(existingResult);
                }

                TestResults.Add(new SignatureTestResultViewModel
                {
                    SignatureName = signature.Name,
                    GameTitle = signature.GameTitle,
                    Status = vr.IsValid ? TestStatusIcon.Passed : TestStatusIcon.Failed,
                    StatusText = vr.IsValid ? "Passed" : "Failed",
                    Confidence = vr.Confidence,
                    HealthScore = vr.HealthScore.OverallScore,
                    HealthRating = vr.HealthScore.GetRating(),
                    Duration = vr.VerificationDuration,
                    FailureReason = vr.FailureReason,
                    TestDetails = vr.TestResults.Select(t => new TestDetailViewModel
                    {
                        TestType = t.TestType.ToString(),
                        Passed = t.Passed,
                        Message = t.Message
                    }).ToList()
                });

                HasTestResults = true;
                CurrentTestStatus = $"{signature.Name}: {vr.HealthScore.GetRatingDescription()}";

                // If failed, suggest fixes
                if (!vr.IsValid)
                {
                    await LoadFixSuggestionsAsync(signature, vr);
                }
            }
            else
            {
                CurrentTestStatus = $"Test failed: {result.Error}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing signature {Signature}", signature.Name);
            CurrentTestStatus = $"Test error: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task LoadFixSuggestionsAsync(GameMemorySignature signature, VerificationResult failureResult)
    {
        try
        {
            var suggestions = await _verificationService.SuggestFixesAsync(signature, failureResult);
            if (suggestions.IsSuccess && suggestions.Value != null)
            {
                SuggestedFixes.Clear();
                foreach (var suggestion in suggestions.Value.OrderByDescending(s => s.Confidence))
                {
                    SuggestedFixes.Add(new PatternFixSuggestionViewModel
                    {
                        Type = suggestion.Type.ToString(),
                        Description = suggestion.Description,
                        Confidence = suggestion.Confidence,
                        Reasoning = suggestion.Reasoning,
                        SuggestedPattern = suggestion.SuggestedPattern,
                        SuggestedOffset = suggestion.SuggestedOffset,
                        SuggestedValueType = suggestion.SuggestedValueType,
                        CanApply = suggestion.SuggestedPattern != null || suggestion.SuggestedOffset.HasValue
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading fix suggestions");
        }
    }

    [RelayCommand]
    private async Task ApplyFixAsync(PatternFixSuggestionViewModel fix)
    {
        // This would apply the suggested fix to the signature
        await _dialogService.ShowInformationAsync(
            "Apply Fix",
            $"This would apply the fix: {fix.Description}\n\n(In a full implementation, this would update the signature in the database)");
    }

    [RelayCommand]
    private async Task ExportResultsAsync()
    {
        if (!HasTestResults)
        {
            await _dialogService.ShowWarningAsync("No Results", "Please run tests before exporting.");
            return;
        }

        var path = await _dialogService.ShowFilePickerAsync(
            "Export Test Results",
            new[] { "json", "csv", "md" });

        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            // Reconstruct TestSuiteResult from UI state
            var suiteResult = new TestSuiteResult
            {
                Results = TestResults.Select(r => new SignatureTestResult
                {
                    SignatureName = r.SignatureName,
                    GameTitle = r.GameTitle,
                    OverallPassed = r.Status == TestStatusIcon.Passed,
                    Confidence = r.Confidence,
                    HealthScore = r.HealthScore,
                    HealthRating = r.HealthRating,
                    Duration = r.Duration,
                    FailureReason = r.FailureReason
                }).ToList(),
                TotalSignatures = TestResults.Count,
                ProcessId = SelectedProcessId ?? 0,
                StartedAt = _timeProvider.UtcNow - TimeSpan.FromTicks(TestResults.Sum(r => r.Duration.Ticks)),
                CompletedAt = _timeProvider.UtcNow
            };

            var format = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".csv" => ExportFormat.Csv,
                ".md" => ExportFormat.Markdown,
                _ => ExportFormat.Json
            };

            var result = await _testRunner!.ExportResultsAsync(suiteResult, path, format);

            if (result.IsSuccess)
            {
                await _dialogService.ShowInformationAsync(
                    "Export Complete",
                    $"Results exported to:\n{path}");
            }
            else
            {
                await _dialogService.ShowErrorAsync("Export Failed", result.Error ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting results");
            await _dialogService.ShowErrorAsync("Export Error", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ViewDetailsAsync(SignatureTestResultViewModel result)
    {
        SelectedResult = result;

        // If failed, try to get fix suggestions
        if (result.Status == TestStatusIcon.Failed)
        {
            var signature = AvailableSignatures
                .FirstOrDefault(s => s.Signature.Name == result.SignatureName)?.Signature;

            if (signature != null)
            {
                var failureResult = new VerificationResult
                {
                    IsValid = false,
                    FailureReason = result.FailureReason,
                    Confidence = result.Confidence,
                    HealthScore = new SignatureHealthScore { OverallScore = result.HealthScore }
                };

                await LoadFixSuggestionsAsync(signature, failureResult);
            }
        }
    }

    [RelayCommand]
    private void SelectAllSignatures()
    {
        foreach (var sig in AvailableSignatures)
        {
            sig.IsSelected = true;
        }
    }

    [RelayCommand]
    private void DeselectAllSignatures()
    {
        foreach (var sig in AvailableSignatures)
        {
            sig.IsSelected = false;
        }
    }

    [RelayCommand]
    private void FilterSignatures()
    {
        // The filtering is handled by the view binding to FilteredSignatures property
        OnPropertyChanged(nameof(FilteredSignatures));
    }

    [RelayCommand]
    private async Task RefreshSignaturesAsync()
    {
        if (!string.IsNullOrEmpty(SelectedGameTitle))
        {
            await LoadSignaturesForGameAsync(SelectedGameTitle);
        }
    }

    [RelayCommand]
    private async Task ReportWorkingAsync(SignatureTestResultViewModel result)
    {
        await _dialogService.ShowInformationAsync(
            "Report Working",
            $"Reported that '{result.SignatureName}' is working for the current game version.\n\n(In a full implementation, this would update community statistics)");
    }

    [RelayCommand]
    private async Task ReportBrokenAsync(SignatureTestResultViewModel result)
    {
        var reason = await _dialogService.ShowInputDialogAsync(
            "Report Broken Signature",
            $"Please describe the issue with '{result.SignatureName}':",
            result.FailureReason);

        if (!string.IsNullOrEmpty(reason))
        {
            await _dialogService.ShowInformationAsync(
                "Report Submitted",
                "Thank you for your report. This will help improve signature accuracy.");
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Filtered signatures based on search and filter settings.
    /// </summary>
    public IEnumerable<SelectableSignatureViewModel> FilteredSignatures
    {
        get
        {
            var query = AvailableSignatures.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchFilter))
            {
                var filter = SearchFilter.ToLowerInvariant();
                query = query.Where(s =>
                    s.Signature.Name.ToLowerInvariant().Contains(filter) ||
                    (s.Signature.Description?.ToLowerInvariant().Contains(filter) ?? false));
            }

            if (ShowPassedOnly && HasTestResults)
            {
                var passedNames = TestResults
                    .Where(r => r.Status == TestStatusIcon.Passed)
                    .Select(r => r.SignatureName)
                    .ToHashSet();
                query = query.Where(s => passedNames.Contains(s.Signature.Name));
            }

            if (ShowFailedOnly && HasTestResults)
            {
                var failedNames = TestResults
                    .Where(r => r.Status == TestStatusIcon.Failed)
                    .Select(r => r.SignatureName)
                    .ToHashSet();
                query = query.Where(s => failedNames.Contains(s.Signature.Name));
            }

            return query;
        }
    }

    /// <summary>
    /// Number of selected signatures.
    /// </summary>
    public int SelectedSignatureCount => AvailableSignatures.Count(s => s.IsSelected);

    #endregion

    #region Private Methods

    private void OnTestProgress(TestProgress progress)
    {
        TestProgress = progress.PercentComplete;
        CurrentTestStatus = $"Testing {progress.CurrentSignature}... ({progress.CompletedCount}/{progress.TotalCount})";
    }

    private void UpdateSummaryStats(TestSuiteResult results)
    {
        SummaryStats = new TestSummaryStatsViewModel
        {
            TotalTested = results.TotalSignatures,
            PassedCount = results.PassedCount,
            FailedCount = results.FailedCount,
            SuccessRate = results.SuccessRate,
            TotalDuration = results.Duration,
            ExcellentCount = results.Results.Count(r => r.HealthRating == HealthRating.Excellent),
            GoodCount = results.Results.Count(r => r.HealthRating == HealthRating.Good),
            FairCount = results.Results.Count(r => r.HealthRating == HealthRating.Fair),
            PoorCount = results.Results.Count(r => r.HealthRating == HealthRating.Poor),
            BrokenCount = results.Results.Count(r => r.HealthRating == HealthRating.Broken)
        };
    }

    partial void OnSearchFilterChanged(string value) => OnPropertyChanged(nameof(FilteredSignatures));
    partial void OnShowPassedOnlyChanged(bool value) => OnPropertyChanged(nameof(FilteredSignatures));
    partial void OnShowFailedOnlyChanged(bool value) => OnPropertyChanged(nameof(FilteredSignatures));

    #endregion

    public void Dispose()
    {
        _testRunner = null;
    }
}

/// <summary>
/// View model for a selectable signature.
/// </summary>
public partial class SelectableSignatureViewModel : ObservableObject
{
    [ObservableProperty]
    private GameMemorySignature _signature = null!;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private string _healthScore = "-";

    [ObservableProperty]
    private string _lastVerified = "Never";
}

/// <summary>
/// View model for a signature test result.
/// </summary>
public partial class SignatureTestResultViewModel : ObservableObject
{
    [ObservableProperty]
    private string _signatureName = "";

    [ObservableProperty]
    private string _gameTitle = "";

    [ObservableProperty]
    private TestStatusIcon _status;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private double _confidence;

    [ObservableProperty]
    private int _healthScore;

    [ObservableProperty]
    private HealthRating _healthRating;

    [ObservableProperty]
    private TimeSpan _duration;

    [ObservableProperty]
    private string? _failureReason;

    [ObservableProperty]
    private List<TestDetailViewModel> _testDetails = new();

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// Formatted confidence percentage.
    /// </summary>
    public string ConfidenceText => $"{Confidence:P0}";

    /// <summary>
    /// Formatted duration.
    /// </summary>
    public string DurationText => $"{Duration.TotalMilliseconds:F0}ms";

    /// <summary>
    /// Health rating color for UI.
    /// </summary>
    public string HealthRatingColor => HealthRating switch
    {
        HealthRating.Excellent => "#4CAF50",
        HealthRating.Good => "#8BC34A",
        HealthRating.Fair => "#FFC107",
        HealthRating.Poor => "#FF9800",
        HealthRating.Broken => "#F44336",
        _ => "#9E9E9E"
    };
}

/// <summary>
/// Test status icon enumeration.
/// </summary>
public enum TestStatusIcon
{
    Pending,
    Passed,
    Failed,
    Skipped
}

/// <summary>
/// View model for a test detail.
/// </summary>
public class TestDetailViewModel
{
    public string TestType { get; set; } = "";
    public bool Passed { get; set; }
    public string Message { get; set; } = "";
    public string StatusIcon => Passed ? "✓" : "✗";
}

/// <summary>
/// View model for a pattern fix suggestion.
/// </summary>
public partial class PatternFixSuggestionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _type = "";

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private double _confidence;

    [ObservableProperty]
    private string _reasoning = "";

    [ObservableProperty]
    private string? _suggestedPattern;

    [ObservableProperty]
    private int? _suggestedOffset;

    [ObservableProperty]
    private string? _suggestedValueType;

    [ObservableProperty]
    private bool _canApply;

    public string ConfidenceText => $"{Confidence:P0}";
}

/// <summary>
/// View model for test summary statistics.
/// </summary>
public partial class TestSummaryStatsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _totalTested;

    [ObservableProperty]
    private int _passedCount;

    [ObservableProperty]
    private int _failedCount;

    [ObservableProperty]
    private double _successRate;

    [ObservableProperty]
    private TimeSpan _totalDuration;

    [ObservableProperty]
    private int _excellentCount;

    [ObservableProperty]
    private int _goodCount;

    [ObservableProperty]
    private int _fairCount;

    [ObservableProperty]
    private int _poorCount;

    [ObservableProperty]
    private int _brokenCount;

    public string SuccessRateText => $"{SuccessRate:P0}";
    public string DurationText => $"{TotalDuration.TotalSeconds:F1}s";
}
