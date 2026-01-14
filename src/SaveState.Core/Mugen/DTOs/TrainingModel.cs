namespace SaveState.Core.Mugen.DTOs;

/// <summary>
/// Represents a trained machine learning model for MUGEN character analysis.
/// </summary>
public class TrainingModel
{
    /// <summary>
    /// Gets or sets the unique identifier for the model.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the model.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the character name this model was trained for.
    /// </summary>
    public string? CharacterName { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the model was trained.
    /// </summary>
    public DateTime TrainedAt { get; set; }

    /// <summary>
    /// Gets or sets the model's accuracy score (0.0-1.0).
    /// </summary>
    public double Accuracy { get; set; }

    /// <summary>
    /// Gets or sets the number of samples used for training.
    /// </summary>
    public int SampleCount { get; set; }

    /// <summary>
    /// Gets or sets the file path where the model is stored.
    /// </summary>
    public string? ModelPath { get; set; }

    /// <summary>
    /// Gets or sets whether this model is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the algorithm used for the model (e.g., "Neural Network", "Random Forest").
    /// </summary>
    public string? Algorithm { get; set; }

    /// <summary>
    /// Gets or sets the total number of training epochs.
    /// </summary>
    public int TotalEpochs { get; set; }

    /// <summary>
    /// Gets or sets the model size in bytes.
    /// </summary>
    public long ModelSize { get; set; }
}
