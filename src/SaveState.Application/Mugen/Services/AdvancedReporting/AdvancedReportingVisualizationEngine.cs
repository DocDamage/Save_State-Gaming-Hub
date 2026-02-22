using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Data visualization engine for charts and graphs.
/// </summary>
internal class AdvancedReportingVisualizationEngine
{
    private readonly ILogger<AdvancedReportingVisualizationEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public AdvancedReportingVisualizationEngine(ILogger<AdvancedReportingVisualizationEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<AdvancedReportingServiceChartData> GenerateChartAsync(AdvancedReportingServiceChartRequest request, CancellationToken ct)
    {
        // Generate chart data
        var dataPoints = new List<AdvancedReportingServiceDataPoint>();

        // Generate sample data points
        for (int i = 0; i < 30; i++)
        {
            dataPoints.Add(new AdvancedReportingServiceDataPoint
            {
                X = _timeProvider.UtcNow.AddDays(-30 + i).ToString("yyyy-MM-dd"),
                Y = new Random().Next(100, 1000)
            });
        }

        return new AdvancedReportingServiceChartData
        {
            ChartId = Guid.NewGuid().ToString(),
            AdvancedReportingServiceChartType = request.AdvancedReportingServiceChartType,
            Title = request.Title,
            DataPoints = dataPoints,
            XAxisLabel = "Date",
            YAxisLabel = request.Metric,
            GeneratedAt = _timeProvider.UtcNow
        };
    }
}
