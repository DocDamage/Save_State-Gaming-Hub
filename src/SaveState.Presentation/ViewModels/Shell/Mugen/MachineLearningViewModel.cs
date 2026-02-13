using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Application.Mugen.DTOs;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;
using SaveState.Presentation.ViewModels.Shell.Mugen;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Shell.Mugen;

/// <summary>
/// View model for machine learning and predictive analytics in MUGEN.
/// Provides AI training, match prediction, and performance analysis features.
/// </summary>
public partial class MachineLearningViewModel : MugenSectionViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IMachineLearningService _mlService;
    private readonly IMatchPredictionEngine _predictionEngine;
    private readonly IMugenCollectionService _collectionService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private bool _isTraining;

    [ObservableProperty]
    private bool _isPredicting;

    [ObservableProperty]
    private bool _isAnalyzing;

    [ObservableProperty]
    private double _trainingProgress;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _modelName = string.Empty;

    [ObservableProperty]
    private int _trainingEpochs = 100;

    [ObservableProperty]
    private double _learningRate = 0.001;

    [ObservableProperty]
    private int _batchSize = 32;

    [ObservableProperty]
    private string _selectedAlgorithm = "Neural Network";

    [ObservableProperty]
    private MugenCharacter? _character1;

    [ObservableProperty]
    private MugenCharacter? _character2;

    [ObservableProperty]
    private string _predictionResult = string.Empty;

    [ObservableProperty]
    private double _predictionConfidence;

    [ObservableProperty]
    private string _winnerName = string.Empty;

    [ObservableProperty]
    private SaveState.Core.Mugen.DTOs.TrainingModel? _selectedModel;

    public ObservableCollection<MugenCharacter> AvailableCharacters { get; } = new();
    public ObservableCollection<string> Algorithms { get; } = new()
    {
        "Neural Network",
        "Decision Tree",
        "Random Forest",
        "Support Vector Machine",
        "Gradient Boosting",
        "Deep Learning"
    };
    public ObservableCollection<SaveState.Core.Mugen.DTOs.TrainingModel> TrainedModels { get; } = new();
    public ObservableCollection<TrainingMetric> TrainingMetrics { get; } = new();
    public ObservableCollection<PredictionHistory> PredictionHistoryList { get; } = new();

    public MachineLearningViewModel(
        IMediator mediator,
        IMachineLearningService mlService,
        IMatchPredictionEngine predictionEngine,
        IMugenCollectionService collectionService,
        INotificationService notificationService)
    {
        _mediator = mediator;
        _mlService = mlService;
        _predictionEngine = predictionEngine;
        _collectionService = collectionService;
        _notificationService = notificationService;
        Title = "Machine Learning & Predictive Analytics";
    }

    public override async Task InitializeAsync()
    {
        await LoadCharactersAsync();
        await LoadTrainedModelsAsync();
        await base.InitializeAsync();
    }

    [RelayCommand]
    private async Task LoadCharactersAsync()
    {
        try
        {
            var result = await _collectionService.GetRosterAsync();
            if (result.IsSuccess && result.Value != null)
            {
                AvailableCharacters.Clear();
                foreach (var character in result.Value)
                {
                    AvailableCharacters.Add(character);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load characters: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadTrainedModelsAsync()
    {
        try
        {
            var result = await _mlService.GetTrainedModelsAsync();
            if (result.IsSuccess && result.Value != null)
            {
                TrainedModels.Clear();
                foreach (var model in result.Value)
                {
                    TrainedModels.Add(model);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load models: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task TrainModelAsync()
    {
        if (string.IsNullOrWhiteSpace(ModelName))
        {
            StatusMessage = "Model name is required.";
            _notificationService.ShowWarning("Please enter a model name.");
            return;
        }

        IsTraining = true;
        TrainingProgress = 0;
        StatusMessage = "Initializing training...";

        try
        {
            var trainingConfig = new TrainingConfiguration(
                ModelName,
                SelectedAlgorithm,
                TrainingEpochs,
                LearningRate,
                BatchSize,
                useGpu: false
            );

            var progress = new Progress<TrainingProgress>(p =>
            {
                TrainingProgress = p.Percentage;
                StatusMessage = $"Training: Epoch {p.CurrentEpoch}/{p.TotalEpochs} - Loss: {p.Loss:F4} - Accuracy: {p.Accuracy:P2}";
                
                TrainingMetrics.Add(new TrainingMetric(
                    p.CurrentEpoch,
                    p.Loss,
                    p.Accuracy,
                    p.ValidationLoss,
                    p.ValidationAccuracy
                ));
            });

            var result = await _mlService.TrainModelAsync(trainingConfig, progress);
            
            if (result.IsSuccess && result.Value != null)
            {
                StatusMessage = $"Training completed! Model accuracy: {result.Value.Accuracy:P2}";
                _notificationService.ShowSuccess($"Model '{ModelName}' trained successfully!");
                await LoadTrainedModelsAsync();
                TrainingProgress = 100;
            }
            else
            {
                StatusMessage = $"Training failed: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Training failed");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during training: {ex.Message}";
            _notificationService.ShowError($"Training error: {ex.Message}");
        }
        finally
        {
            IsTraining = false;
        }
    }

    [RelayCommand]
    private async Task PredictMatchAsync()
    {
        if (Character1 == null || Character2 == null)
        {
            StatusMessage = "Please select two characters for prediction.";
            _notificationService.ShowWarning("Select two characters.");
            return;
        }

        if (Character1.Id == Character2.Id)
        {
            StatusMessage = "Please select two different characters.";
            _notificationService.ShowWarning("Characters must be different.");
            return;
        }

        IsPredicting = true;
        StatusMessage = "Analyzing matchup...";

        try
        {
            var result = await _predictionEngine.PredictMatchAsync(Character1, Character2);
            
            if (result.IsSuccess && result.Value != null)
            {
                var prediction = result.Value;
                // Determine winner based on win probabilities
                var isPlayer1Winner = prediction.WinProbabilityPlayer1 > prediction.WinProbabilityPlayer2;
                WinnerName = isPlayer1Winner ? Character1.DisplayName : Character2.DisplayName;
                PredictionConfidence = (isPlayer1Winner ? prediction.WinProbabilityPlayer1 : prediction.WinProbabilityPlayer2) * 100;
                PredictionResult = $"{WinnerName} is predicted to win with {PredictionConfidence:F1}% confidence.";
                
                PredictionHistoryList.Insert(0, new PredictionHistory(
                    Character1.DisplayName,
                    Character2.DisplayName,
                    WinnerName,
                    PredictionConfidence,
                    DateTime.UtcNow
                ));

                if (PredictionHistoryList.Count > 50)
                {
                    PredictionHistoryList.RemoveAt(PredictionHistoryList.Count - 1);
                }

                StatusMessage = "Prediction complete.";
                _notificationService.ShowSuccess("Match prediction complete!");
            }
            else
            {
                StatusMessage = $"Prediction failed: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Prediction failed");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during prediction: {ex.Message}";
            _notificationService.ShowError($"Prediction error: {ex.Message}");
        }
        finally
        {
            IsPredicting = false;
        }
    }

    [RelayCommand]
    private async Task AnalyzeCharacterAsync(MugenCharacterSummaryDto? character)
    {
        if (character == null)
        {
            StatusMessage = "Please select a character to analyze.";
            return;
        }

        IsAnalyzing = true;
        StatusMessage = $"Analyzing {character.DisplayName}...";

        try
        {
            var result = await _mlService.AnalyzeCharacterPerformanceAsync(character.Id);
            
            if (result.IsSuccess && result.Value != null)
            {
                var analysis = result.Value;
                StatusMessage = $"Analysis complete: Strength={analysis.OverallStrength:F2}, Weaknesses={string.Join(", ", analysis.Weaknesses)}";
                _notificationService.ShowSuccess($"Analysis complete for {character.DisplayName}!");
            }
            else
            {
                StatusMessage = $"Analysis failed: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Analysis failed");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error during analysis: {ex.Message}";
            _notificationService.ShowError($"Analysis error: {ex.Message}");
        }
        finally
        {
            IsAnalyzing = false;
        }
    }

    [RelayCommand]
    private async Task DeleteModelAsync(TrainingModel? model)
    {
        if (model == null)
        {
            _notificationService.ShowWarning("Please select a model to delete.");
            return;
        }

        try
        {
            var result = await _mlService.DeleteModelAsync(model.Id);
            
            if (result.IsSuccess)
            {
                StatusMessage = $"Model '{model.Name}' deleted.";
                _notificationService.ShowSuccess($"Model deleted successfully!");
                await LoadTrainedModelsAsync();
            }
            else
            {
                StatusMessage = $"Failed to delete model: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Deletion failed");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting model: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ExportModelAsync(TrainingModel? model)
    {
        if (model == null)
        {
            _notificationService.ShowWarning("Please select a model to export.");
            return;
        }

        try
        {
            var result = await _mlService.ExportModelAsync(model.Id);
            
            if (result.IsSuccess)
            {
                StatusMessage = $"Model exported to: {result.Value}";
                _notificationService.ShowSuccess($"Model exported successfully!");
            }
            else
            {
                StatusMessage = $"Failed to export model: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Export failed");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exporting model: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ClearMetrics()
    {
        TrainingMetrics.Clear();
        StatusMessage = "Metrics cleared.";
    }

    [RelayCommand]
    private void ClearPredictionHistory()
    {
        PredictionHistoryList.Clear();
        StatusMessage = "Prediction history cleared.";
    }
}

/// <summary>
/// Represents a trained machine learning model.
/// </summary>
public sealed class TrainingModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Algorithm { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public DateTime TrainedAt { get; set; }
    public int TotalEpochs { get; set; }
    public long ModelSize { get; set; }
}

/// <summary>
/// Represents a training metric snapshot.
/// </summary>
public sealed record TrainingMetric(
    int Epoch,
    double Loss,
    double Accuracy,
    double ValidationLoss,
    double ValidationAccuracy
);

/// <summary>
/// Represents a prediction history entry.
/// </summary>
public sealed record PredictionHistory(
    string Character1,
    string Character2,
    string PredictedWinner,
    double Confidence,
    DateTime PredictedAt
);
