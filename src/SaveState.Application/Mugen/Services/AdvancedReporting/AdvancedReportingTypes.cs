using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Advanced Reporting Service interface.
/// </summary>
public interface IAdvancedReportingService
{
    Task<Result<AdvancedReportingServiceReport>> GenerateReportAsync(AdvancedReportingServiceReportRequest request, CancellationToken ct = default);
    Task<Result<AdvancedReportingServiceDashboard>> CreateDashboardAsync(AdvancedReportingServiceDashboardRequest request, CancellationToken ct = default);
    Task<Result<AdvancedReportingServiceDashboardData>> GetDashboardDataAsync(string dashboardId, AdvancedReportingServiceDashboardQuery query, CancellationToken ct = default);
    Task<Result<AdvancedReportingServiceChartData>> GenerateChartAsync(AdvancedReportingServiceChartRequest request, CancellationToken ct = default);
    Task<Result<AdvancedReportingServiceReportTemplate>> CreateReportTemplateAsync(AdvancedReportingServiceReportTemplateRequest request, CancellationToken ct = default);
    Task<Result<AdvancedReportingServiceScheduledReport>> ScheduleReportAsync(AdvancedReportingServiceScheduledReportRequest request, CancellationToken ct = default);
    Task<Result<AdvancedReportingServiceReport>> ExportReportAsync(string reportId, AdvancedReportingServiceReportingExportFormat format, CancellationToken ct = default);
    Task<Result<AdvancedReportingServiceReportAnalytics>> GetReportAnalyticsAsync(TimeSpan period, CancellationToken ct = default);
    Task<Result<AdvancedReportingServiceDashboardTemplate>> CreateDashboardTemplateAsync(AdvancedReportingServiceDashboardTemplateRequest request, CancellationToken ct = default);
    Task<Result<AdvancedReportingServiceReportSharing>> ShareReportAsync(AdvancedReportingServiceReportSharingRequest request, CancellationToken ct = default);
}

/// <summary>
/// AdvancedReportingServiceReport data.
/// </summary>
public class AdvancedReportingServiceReport
{
    public string ReportId { get; set; } = default!;
    public AdvancedReportingServiceReportingReportType ReportType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime StartDate { get; set; } = default!;
    public DateTime EndDate { get; set; } = default!;
    public string GeneratedBy { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceReportPage> Pages { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Metadata { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceReport page data.
/// </summary>
public class AdvancedReportingServiceReportPage
{
    public int PageNumber { get; set; } = default!;
    public string Title { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Content { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceChartData> Charts { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceTableData> Tables { get; set; } = default!;
}

/// <summary>
/// Table data.
/// </summary>
public class AdvancedReportingServiceTableData
{
    public string TableId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public IReadOnlyList<string> Headers { get; set; } = default!;
    public IReadOnlyList<object[]> Rows { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceReport request.
/// </summary>
public class AdvancedReportingServiceReportRequest
{
    public ReportType ReportType { get; set; } = default!;
    public DateTime StartDate { get; set; } = default!;
    public DateTime EndDate { get; set; } = default!;
    public IReadOnlyList<string> Metrics { get; set; } = default!;
    public IReadOnlyList<string> Dimensions { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceDashboard data.
/// </summary>
public class AdvancedReportingServiceDashboard
{
    public string DashboardId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceDashboardWidget> Widgets { get; set; } = default!;
    public AdvancedReportingServiceDashboardLayout Layout { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime UpdatedAt { get; set; } = default!;
    public bool IsPublic { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
    public TimeSpan RefreshInterval { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceDashboard widget data.
/// </summary>
public class AdvancedReportingServiceDashboardWidget
{
    public string WidgetId { get; set; } = default!;
    public WidgetType Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public AdvancedReportingServiceWidgetPosition Position { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Config { get; set; } = default!;
}

/// <summary>
/// Widget position data.
/// </summary>
public class AdvancedReportingServiceWidgetPosition
{
    public int X { get; set; } = default!;
    public int Y { get; set; } = default!;
    public int Width { get; set; } = default!;
    public int Height { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceDashboard layout data.
/// </summary>
public class AdvancedReportingServiceDashboardLayout
{
    public int Columns { get; set; } = default!;
    public int RowHeight { get; set; } = default!;
    public IReadOnlyList<int> Margin { get; set; } = default!;
    public IReadOnlyList<int> ContainerPadding { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceDashboard request.
/// </summary>
public class AdvancedReportingServiceDashboardRequest
{
    public string UserId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceDashboardWidget> Widgets { get; set; } = default!;
    public bool IsPublic { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceDashboard data response.
/// </summary>
public class AdvancedReportingServiceDashboardData
{
    public string DashboardId { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceDashboardWidgetData> Widgets { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
    public TimeSpan CacheExpiry { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceDashboard widget data.
/// </summary>
public class AdvancedReportingServiceDashboardWidgetData
{
    public string WidgetId { get; set; } = default!;
    public WidgetType Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Data { get; set; } = default!;
    public DateTime LastUpdated { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceDashboard query.
/// </summary>
public class AdvancedReportingServiceDashboardQuery
{
    public DateTime? StartDate { get; set; } = default!;
    public DateTime? EndDate { get; set; } = default!;
    public IReadOnlyList<string> Filters { get; set; } = default!;
    public bool IncludeHistorical { get; set; } = default!;
}

/// <summary>
/// Chart data.
/// </summary>
public class AdvancedReportingServiceChartData
{
    public string ChartId { get; set; } = default!;
    public AdvancedReportingServiceChartType AdvancedReportingServiceChartType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceDataPoint> DataPoints { get; set; } = default!;
    public string? XAxisLabel { get; set; } = default!;
    public string? YAxisLabel { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Data point.
/// </summary>
public class AdvancedReportingServiceDataPoint
{
    public string X { get; set; } = default!;
    public double Y { get; set; } = default!;
}

/// <summary>
/// Chart request.
/// </summary>
public class AdvancedReportingServiceChartRequest
{
    public AdvancedReportingServiceChartType AdvancedReportingServiceChartType { get; set; } = default!;
    public string Metric { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateTime StartDate { get; set; } = default!;
    public DateTime EndDate { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Options { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceReport template data.
/// </summary>
public class AdvancedReportingServiceReportTemplate
{
    public string TemplateId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ReportType ReportType { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceReportSection> Sections { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime UpdatedAt { get; set; } = default!;
    public bool IsPublic { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceReport section data.
/// </summary>
public class AdvancedReportingServiceReportSection
{
    public string SectionId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public AdvancedReportingServiceSectionType Type { get; set; } = default!;
    public string Content { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceReport template request.
/// </summary>
public class AdvancedReportingServiceReportTemplateRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ReportType ReportType { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceReportSection> Sections { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public bool IsPublic { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
}

/// <summary>
/// Scheduled report data.
/// </summary>
public class AdvancedReportingServiceScheduledReport
{
    public string ScheduleId { get; set; } = default!;
    public string ReportName { get; set; } = default!;
    public ReportType ReportType { get; set; } = default!;
    public AdvancedReportingServiceScheduleType AdvancedReportingServiceScheduleType { get; set; } = default!;
    public IReadOnlyDictionary<string, object> ScheduleConfig { get; set; } = default!;
    public IReadOnlyList<string> Recipients { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public bool IsActive { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime? LastRun { get; set; } = default!;
    public DateTime? NextRun { get; set; } = default!;
    public int RunCount { get; set; } = default!;
}

/// <summary>
/// Scheduled report request.
/// </summary>
public class AdvancedReportingServiceScheduledReportRequest
{
    public string ReportName { get; set; } = default!;
    public ReportType ReportType { get; set; } = default!;
    public AdvancedReportingServiceScheduleType AdvancedReportingServiceScheduleType { get; set; } = default!;
    public IReadOnlyDictionary<string, object> ScheduleConfig { get; set; } = default!;
    public IReadOnlyList<string> Recipients { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceReport analytics data.
/// </summary>
public class AdvancedReportingServiceReportAnalytics
{
    public TimeSpan Period { get; set; } = default!;
    public int TotalReportsGenerated { get; set; } = default!;
    public int TotalDashboardsCreated { get; set; } = default!;
    public int TotalScheduledReports { get; set; } = default!;
    public IReadOnlyDictionary<ReportType, int> MostPopularReportTypes { get; set; } = default!;
    public IReadOnlyDictionary<DateTime, int> ReportGenerationTrends { get; set; } = default!;
    public TimeSpan AverageReportGenerationTime { get; set; } = default!;
    public IReadOnlyDictionary<string, double> UserEngagementMetrics { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceDashboard template data.
/// </summary>
public class AdvancedReportingServiceDashboardTemplate
{
    public string TemplateId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceWidgetTemplate> WidgetTemplates { get; set; } = default!;
    public AdvancedReportingServiceLayoutTemplate AdvancedReportingServiceLayoutTemplate { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public DateTime UpdatedAt { get; set; } = default!;
    public bool IsPublic { get; set; } = default!;
    public int UsageCount { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
}

/// <summary>
/// Widget template data.
/// </summary>
public class AdvancedReportingServiceWidgetTemplate
{
    public string TemplateId { get; set; } = default!;
    public WidgetType Type { get; set; } = default!;
    public string Name { get; set; } = default!;
    public IReadOnlyDictionary<string, object> DefaultConfig { get; set; } = default!;
}

/// <summary>
/// Layout template data.
/// </summary>
public class AdvancedReportingServiceLayoutTemplate
{
    public int Columns { get; set; } = default!;
    public int DefaultRowHeight { get; set; } = default!;
    public IReadOnlyList<int> DefaultMargin { get; set; } = default!;
    public IReadOnlyList<int> DefaultPadding { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceDashboard template request.
/// </summary>
public class AdvancedReportingServiceDashboardTemplateRequest
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
    public IReadOnlyList<AdvancedReportingServiceWidgetTemplate> WidgetTemplates { get; set; } = default!;
    public AdvancedReportingServiceLayoutTemplate AdvancedReportingServiceLayoutTemplate { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public bool IsPublic { get; set; } = default!;
    public IReadOnlyList<string> Tags { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceReport sharing data.
/// </summary>
public class AdvancedReportingServiceReportSharing
{
    public string SharingId { get; set; } = default!;
    public string ReportId { get; set; } = default!;
    public string SharedBy { get; set; } = default!;
    public IReadOnlyList<string> Recipients { get; set; } = default!;
    public AdvancedReportingServiceSharingPermissions Permissions { get; set; } = default!;
    public DateTime? ExpiresAt { get; set; } = default!;
    public DateTime SharedAt { get; set; } = default!;
    public int AccessCount { get; set; } = default!;
    public DateTime? LastAccessed { get; set; } = default!;
}

/// <summary>
/// AdvancedReportingServiceReport sharing request.
/// </summary>
public class AdvancedReportingServiceReportSharingRequest
{
    public string ReportId { get; set; } = default!;
    public string SharedBy { get; set; } = default!;
    public IReadOnlyList<string> Recipients { get; set; } = default!;
    public AdvancedReportingServiceSharingPermissions Permissions { get; set; } = default!;
    public DateTime? ExpiresAt { get; set; } = default!;
}

/// <summary>
/// Sharing permissions.
/// </summary>
public class AdvancedReportingServiceSharingPermissions
{
    public bool CanView { get; set; } = default!;
    public bool CanEdit { get; set; } = default!;
    public bool CanShare { get; set; } = default!;
    public bool CanExport { get; set; } = default!;
}

/// <summary>
/// Various enumeration types.
/// </summary>
public enum AdvancedReportingServiceReportingReportType { UserAnalytics, Financial, Performance, Security, Custom, Executive }
public enum AdvancedReportingServiceReportingWidgetType { MetricCard, Chart, Table, Gauge, Map, Text }
public enum AdvancedReportingServiceChartType { Line, Bar, Pie, Area, Scatter, Heatmap }
public enum AdvancedReportingServiceSectionType { Summary, Data, Charts, Tables, Text }
public enum AdvancedReportingServiceScheduleType { Hourly, Daily, Weekly, Monthly, Custom }
public enum AdvancedReportingServiceReportingExportFormat { PDF, Excel, CSV, JSON, PowerPoint }
