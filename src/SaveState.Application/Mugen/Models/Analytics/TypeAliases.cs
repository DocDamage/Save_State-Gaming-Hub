// Type aliases for backward compatibility

namespace SaveState.Application.Mugen.Services;

using SaveState.Application.Mugen.Models.Analytics;

// Model aliases
public class AdvancedAnalyticsServiceBusinessAnalyticsReport : BusinessAnalyticsReport { }
public class AdvancedAnalyticsServiceReportSummary : ReportSummary { }
public class AdvancedAnalyticsServiceAnalyticsReportRequest : AnalyticsReportRequest { }
public class AdvancedAnalyticsServicePredictiveModel : PredictiveModel { }
public class AdvancedAnalyticsServiceModelTrainingRequest : ModelTrainingRequest { }
public class AdvancedAnalyticsServiceTrainingDataPoint : TrainingDataPoint { }
public class AdvancedAnalyticsServicePredictionRequest : PredictionRequest { }
public class AdvancedAnalyticsServicePredictionResult : PredictionResult { }
public class AdvancedAnalyticsServicePredictionInterval : PredictionInterval { }
public class AdvancedAnalyticsServiceBusinessIntelligenceReport : BusinessIntelligenceReport { }
public class AdvancedAnalyticsServiceBusinessInsight : BusinessInsight { }
public class AdvancedAnalyticsServiceAnalyticsRiskAssessment : AnalyticsRiskAssessment { }
public class AdvancedAnalyticsServiceBIReportRequest : BIReportRequest { }
public class AdvancedAnalyticsServiceDashboardData : DashboardData { }
public class AdvancedAnalyticsServiceDashboardWidget : DashboardWidget { }
public class AdvancedAnalyticsServiceWidgetPosition : WidgetPosition { }
public class AdvancedAnalyticsServiceDashboardRequest : DashboardRequest { }
public class AdvancedAnalyticsServiceAnalyticsTrendAnalysis : TrendAnalysis { }
public class AdvancedAnalyticsServiceTrendForecast : TrendForecast { }
public class AdvancedAnalyticsServiceTrendAnalysisRequest : TrendAnalysisRequest { }
public class AdvancedAnalyticsServiceSegmentAnalysis : SegmentAnalysis { }
public class AdvancedAnalyticsServiceUserSegment : UserSegment { }
public class AdvancedAnalyticsServiceSegmentAnalysisRequest : SegmentAnalysisRequest { }
public class AdvancedAnalyticsServiceAnalyticsPerformanceMetrics : AnalyticsPerformanceMetrics { }
public class AdvancedAnalyticsServiceMetricData : MetricData { }
public class AdvancedAnalyticsServicePerformanceMetricsRequest : AnalyticsPerformanceMetricsRequest { }
public class AdvancedAnalyticsServiceAnomalyReport : AnomalyReport { }
public class AdvancedAnalyticsServiceAnomaly : Anomaly { }
public class AdvancedAnalyticsServiceDataExportReport : DataExportReport { }
public class AdvancedAnalyticsServiceDataExportRequest : DataExportRequest { }

// Enum aliases
public enum AdvancedAnalyticsServiceReportType { UserBehavior = ReportType.UserBehavior, Revenue = ReportType.Revenue, Performance = ReportType.Performance, Engagement = ReportType.Engagement, Custom = ReportType.Custom }
public enum AdvancedAnalyticsServiceModelType { ChurnPrediction = ModelType.ChurnPrediction, RevenueForecast = ModelType.RevenueForecast, UserSegmentation = ModelType.UserSegmentation, ContentRecommendation = ModelType.ContentRecommendation }
public enum AdvancedAnalyticsServiceModelStatus { Training = ModelStatus.Training, Active = ModelStatus.Active, Inactive = ModelStatus.Inactive, Failed = ModelStatus.Failed }
public enum AdvancedAnalyticsServiceValidationStatus { Passed = ValidationStatus.Passed, Warning = ValidationStatus.Warning, Failed = ValidationStatus.Failed }
public enum AdvancedAnalyticsServiceWidgetType { MetricCard = WidgetType.MetricCard, Chart = WidgetType.Chart, Table = WidgetType.Table, Gauge = WidgetType.Gauge, Map = WidgetType.Map }
public enum AdvancedAnalyticsServiceMetricTrend { Increasing = MetricTrend.Increasing, Decreasing = MetricTrend.Decreasing, Stable = MetricTrend.Stable }
public enum AdvancedAnalyticsServiceAnomalyType { Spike = AnomalyType.Spike, Drop = AnomalyType.Drop, TrendChange = AnomalyType.TrendChange, Outlier = AnomalyType.Outlier }
public enum AdvancedAnalyticsServiceAnomalySeverity { Low = AnomalySeverity.Low, Medium = AnomalySeverity.Medium, High = AnomalySeverity.High, Critical = AnomalySeverity.Critical }
public enum AdvancedAnalyticsServiceInsightImpact { Low = InsightImpact.Low, Medium = InsightImpact.Medium, High = InsightImpact.High, Critical = InsightImpact.Critical }
public enum AdvancedAnalyticsServiceRiskSeverity { Low = RiskSeverity.Low, Medium = RiskSeverity.Medium, High = RiskSeverity.High, Critical = RiskSeverity.Critical }
public enum AdvancedAnalyticsServiceExportFormat { CSV = ExportFormat.CSV, JSON = ExportFormat.JSON, XML = ExportFormat.XML, Parquet = ExportFormat.Parquet }

// TrendDirection is named differently
public enum AdvancedAnalyticsServiceTrendDirection { Upward = 0, Downward = 1, Stable = 2 }
