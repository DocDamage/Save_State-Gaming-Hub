using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Entities;

/// <summary>
/// Log entry for detected memory anomalies (MBAD)
/// </summary>
public class AnomalyLog
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the process where anomaly was detected
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string ProcessName { get; set; } = "";

    /// <summary>
    /// When the anomaly was detected
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Type of anomaly (RapidValueChange, ExternalWrite, PatternMatch, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string AnomalyType { get; set; } = "";

    /// <summary>
    /// ML confidence score (0-1)
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// JSON serialized details (feature contributions, addresses, etc.)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Whether user dismissed/acknowledged this anomaly
    /// </summary>
    public bool IsDismissed { get; set; } = false;
}
