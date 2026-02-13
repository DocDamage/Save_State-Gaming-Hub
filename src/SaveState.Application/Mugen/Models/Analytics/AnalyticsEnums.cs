namespace SaveState.Application.Mugen.Models.Analytics;

/// <summary>
/// Report type enumeration.
/// </summary>
public enum ReportType { UserBehavior, Revenue, Performance, Engagement, Custom }

/// <summary>
/// Model type enumeration.
/// </summary>
public enum ModelType { ChurnPrediction, RevenueForecast, UserSegmentation, ContentRecommendation }

/// <summary>
/// Model status enumeration.
/// </summary>
public enum ModelStatus { Training, Active, Inactive, Failed }

/// <summary>
/// Validation status enumeration.
/// </summary>
public enum ValidationStatus { Passed, Warning, Failed }

/// <summary>
/// Widget type enumeration.
/// </summary>
public enum WidgetType { MetricCard, Chart, Table, Gauge, Map }

/// <summary>
/// Metric trend enumeration.
/// </summary>
public enum MetricTrend { Increasing, Decreasing, Stable }

/// <summary>
/// Anomaly type enumeration.
/// </summary>
public enum AnomalyType { Spike, Drop, TrendChange, Outlier }

/// <summary>
/// Anomaly severity enumeration.
/// </summary>
public enum AnomalySeverity { Low, Medium, High, Critical }

/// <summary>
/// Insight impact enumeration.
/// </summary>
public enum InsightImpact { Low, Medium, High, Critical }

/// <summary>
/// Risk severity enumeration.
/// </summary>
public enum RiskSeverity { Low, Medium, High, Critical }

/// <summary>
/// Export format enumeration.
/// </summary>
public enum ExportFormat { CSV, JSON, XML, Parquet }

/// <summary>
/// Trend direction enumeration.
/// </summary>
public enum TrendDirection { Upward, Downward, Stable }
