namespace SaveState.Application.Mugen.Models.Analytics;

/// <summary>
/// Predictive model data.
/// </summary>
public class PredictiveModel
{
    public string ModelId { get; set; } = default!;
    public ModelType ModelType { get; set; } = default!;
    public string Algorithm { get; set; } = default!;
    public double Accuracy { get; set; } = default!;
    public double Precision { get; set; } = default!;
    public double Recall { get; set; } = default!;
    public double F1Score { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime LastTrained { get; set; } = default!;
    public int TrainingDataSize { get; set; } = default!;
    public ModelStatus Status { get; set; } = default!;
    public IReadOnlyDictionary<string, double> FeatureImportance { get; set; } = default!;
}

/// <summary>
/// Model training request.
/// </summary>
public class ModelTrainingRequest
{
    public ModelType ModelType { get; set; } = default!;
    public IReadOnlyList<TrainingDataPoint> TrainingData { get; set; } = default!;
    public IReadOnlyDictionary<string, string> Parameters { get; set; } = default!;
}

/// <summary>
/// Training data point.
/// </summary>
public class TrainingDataPoint
{
    public IReadOnlyDictionary<string, object> Features { get; set; } = default!;
    public object Target { get; set; } = default!;
}

/// <summary>
/// Prediction request.
/// </summary>
public class PredictionRequest
{
    public string ModelId { get; set; } = default!;
    public IReadOnlyDictionary<string, object> InputData { get; set; } = default!;
}

/// <summary>
/// Prediction result data.
/// </summary>
public class PredictionResult
{
    public string PredictionId { get; set; } = default!;
    public string ModelId { get; set; } = default!;
    public double PredictedValue { get; set; } = default!;
    public double Confidence { get; set; } = default!;
    public PredictionInterval PredictionInterval { get; set; } = default!;
    public IReadOnlyDictionary<string, double> FeatureContributions { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
    public DateTime ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Prediction interval data.
/// </summary>
public class PredictionInterval
{
    public double LowerBound { get; set; } = default!;
    public double UpperBound { get; set; } = default!;
    public double ConfidenceLevel { get; set; } = default!;
}
