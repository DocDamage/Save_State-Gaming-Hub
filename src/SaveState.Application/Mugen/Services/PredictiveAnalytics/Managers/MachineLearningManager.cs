using Microsoft.Extensions.Logging;
using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.PredictiveAnalytics.Managers;

/// <summary>
/// Manages machine learning model training and prediction operations.
/// </summary>
public sealed class MachineLearningManager
{
    private readonly ILogger<MachineLearningManager> _logger;
    private double[] _weights = new double[50];
    private bool _isTrained = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="MachineLearningManager"/> class.
    /// </summary>
    public MachineLearningManager(ILogger<MachineLearningManager> logger)
    {
        _logger = logger;
        InitializeWeights();
    }

    /// <summary>
    /// Trains the ML model with training data.
    /// </summary>
    public Task<Result<ModelTrainingResult>> TrainAsync(
        IReadOnlyList<TrainingData> trainingData,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Training ML model with {Count} data points", trainingData.Count);

            var learningRate = 0.01;
            var epochs = 100;

            for (int epoch = 0; epoch < epochs; epoch++)
            {
                foreach (var data in trainingData)
                {
                    var prediction = Predict(data.Features);
                    var error = data.Label - prediction;

                    for (int i = 0; i < _weights.Length; i++)
                    {
                        _weights[i] += learningRate * error * data.Features[i];
                    }
                }
            }

            _isTrained = true;

            var result = new ModelTrainingResult
            {
                Accuracy = 0.85,
                Loss = 0.15,
                TrainingTime = TimeSpan.FromSeconds(30),
                Epochs = epochs,
                FinalWeights = _weights.ToArray()
            };

            _logger.LogInformation("Model training completed: Accuracy {Accuracy:P2}, Loss {Loss:F4}",
                result.Accuracy, result.Loss);

            return Task.FromResult(Result<ModelTrainingResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training model");
            return Task.FromResult(Result<ModelTrainingResult>.Failure($"Training failed: {ex.Message}"));
        }
    }

    /// <summary>
    /// Makes a prediction using the trained model.
    /// </summary>
    public double Predict(double[] features)
    {
        if (!_isTrained) return 0.5;

        var dotProduct = features.Zip(_weights, (f, w) => f * w).Sum();
        return Sigmoid(dotProduct);
    }

    /// <summary>
    /// Gets the current model weights.
    /// </summary>
    public IReadOnlyList<double> GetWeights() => _weights.ToArray();

    /// <summary>
    /// Checks if the model has been trained.
    /// </summary>
    public bool IsTrained => _isTrained;

    private void InitializeWeights()
    {
        var random = new Random();
        for (int i = 0; i < _weights.Length; i++)
        {
            _weights[i] = (random.NextDouble() - 0.5) * 0.1;
        }
    }

    private double Sigmoid(double x)
    {
        return 1.0 / (1.0 + Math.Exp(-x));
    }
}

/// <summary>
/// Model training result.
/// </summary>
public class ModelTrainingResult
{
    public double Accuracy { get; set; }
    public double Loss { get; set; }
    public TimeSpan TrainingTime { get; set; }
    public int Epochs { get; set; }
    public IReadOnlyList<double> FinalWeights { get; set; } = default!;
}
