using SaveState.Core.Common;
using SaveState.Application.Mugen.Models.Analytics;

namespace SaveState.Application.Mugen.Services.Analytics;

/// <summary>
/// Interface for advanced analytics service.
/// </summary>
public interface IAdvancedAnalyticsService
{
    Task<Result<BusinessAnalyticsReport>> GenerateAnalyticsReportAsync(AnalyticsReportRequest request, CancellationToken ct = default);
    Task<Result<PredictiveModel>> TrainPredictiveModelAsync(ModelTrainingRequest request, CancellationToken ct = default);
    Task<Result<PredictionResult>> GeneratePredictionAsync(PredictionRequest request, CancellationToken ct = default);
    Task<Result<BusinessIntelligenceReport>> GenerateBIReportAsync(BIReportRequest request, CancellationToken ct = default);
    Task<Result<DashboardData>> GenerateDashboardAsync(DashboardRequest request, CancellationToken ct = default);
    Task<Result<TrendAnalysis>> AnalyzeTrendsAsync(TrendAnalysisRequest request, CancellationToken ct = default);
    Task<Result<SegmentAnalysis>> AnalyzeSegmentsAsync(SegmentAnalysisRequest request, CancellationToken ct = default);
    Task<Result<AnalyticsPerformanceMetrics>> GetPerformanceMetricsAsync(string category, CancellationToken ct = default);
    Task<Result<AnomalyReport>> DetectAnomaliesAsync(string dataType, TimeSpan timePeriod, CancellationToken ct = default);
}

/// <summary>
/// Legacy alias for backward compatibility.
/// </summary>
public interface AdvancedAnalyticsServiceIAdvancedAnalyticsService : IAdvancedAnalyticsService { }
