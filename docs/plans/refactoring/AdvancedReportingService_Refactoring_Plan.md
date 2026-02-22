# AdvancedReportingService Refactoring Plan

## Overview

**File:** `src/SaveState.Application/Mugen/Services/AdvancedReportingService.cs`  
**Current Lines:** 1,063  
**Current Methods:** 14 public + 3 private + 4 nested classes  
**Target:** Split into Manager Pattern following project conventions

---

## Current Structure Analysis

### Service Breakdown

```
AdvancedReportingService (Coordinator - 383 lines)
├── ReportEngine (nested class - 112 lines)
├── DashboardBuilder (nested class - 87 lines)
├── DataVisualizationEngine (nested class - 37 lines)
├── ReportScheduler (nested class - 13 lines - mostly empty)
├── Data Models (~450 lines of DTOs/Enums)
└── Interface Definition (~20 lines)
```

### Responsibility Areas Identified

1. **Report Generation** - Creating, exporting reports
2. **Dashboard Management** - Creating dashboards, managing widgets
3. **Data Visualization** - Chart generation, data points
4. **Template Management** - Report templates, dashboard templates
5. **Scheduling** - Automated report scheduling (currently stub)
6. **Analytics** - Report usage analytics, performance metrics
7. **Sharing** - Report sharing with permissions

---

## Proposed Manager Structure

### After Refactoring

```
AdvancedReportingService (Coordinator - ~150 lines)
├── IAdvancedReportingService (interface - split to separate file)
├── ReportGenerationManager (ReportEngine → Manager)
├── DashboardManager (DashboardBuilder → Manager)
├── VisualizationManager (DataVisualizationEngine → Manager)
├── ReportTemplateManager (new - from coordinator methods)
├── ReportSchedulingManager (ReportScheduler → Manager)
└── Data Models (split to separate files)
```

### Manager Classes

#### 1. ReportGenerationManager
**Responsibilities:**
- Generate reports from templates
- Export reports to various formats (PDF, Excel, CSV, etc.)
- Report page composition
- Report metadata management

**Methods:**
```csharp
public Task<Report> GenerateReportAsync(ReportRequest request, CancellationToken ct)
public Task<Report> ExportReportAsync(string reportId, ExportFormat format, CancellationToken ct)
public Task<ReportPage> CreateReportPageAsync(PageRequest request, CancellationToken ct)
public Task<ReportAnalytics> GetReportAnalyticsAsync(TimeSpan period, CancellationToken ct)
```

**Estimated Lines:** ~180

---

#### 2. DashboardManager
**Responsibilities:**
- Create and manage dashboards
- Dashboard widget management
- Dashboard data retrieval
- Layout configuration

**Methods:**
```csharp
public Task<Dashboard> CreateDashboardAsync(DashboardRequest request, CancellationToken ct)
public Task<DashboardData> GetDashboardDataAsync(Dashboard dashboard, DashboardQuery query, CancellationToken ct)
public Task UpdateDashboardLayoutAsync(string dashboardId, DashboardLayout layout, CancellationToken ct)
public Task<IReadOnlyList<Dashboard>> GetUserDashboardsAsync(string userId, CancellationToken ct)
```

**Estimated Lines:** ~160

---

#### 3. VisualizationManager
**Responsibilities:**
- Chart generation (Line, Bar, Pie, Area, Scatter, Heatmap)
- Data point generation
- Chart configuration
- Time-series data visualization

**Methods:**
```csharp
public Task<ChartData> GenerateChartAsync(ChartRequest request, CancellationToken ct)
public Task<IReadOnlyList<DataPoint>> GenerateDataPointsAsync(DataPointRequest request, CancellationToken ct)
public Task<TableData> GenerateTableAsync(TableRequest request, CancellationToken ct)
```

**Estimated Lines:** ~100

---

#### 4. ReportTemplateManager
**Responsibilities:**
- Report template CRUD operations
- Dashboard template management
- Template validation
- Default template initialization

**Methods:**
```csharp
public Task<ReportTemplate> CreateReportTemplateAsync(ReportTemplateRequest request, CancellationToken ct)
public Task<DashboardTemplate> CreateDashboardTemplateAsync(DashboardTemplateRequest request, CancellationToken ct)
public Task<IReadOnlyList<ReportTemplate>> GetTemplatesAsync(string? category, CancellationToken ct)
public Task InitializeDefaultTemplatesAsync(CancellationToken ct)
```

**Estimated Lines:** ~140

---

#### 5. ReportSchedulingManager
**Responsibilities:**
- Scheduled report creation
- Schedule calculation (Daily, Weekly, Monthly, Custom)
- Report delivery to recipients
- Schedule execution tracking

**Methods:**
```csharp
public Task<ScheduledReport> ScheduleReportAsync(ScheduledReportRequest request, CancellationToken ct)
public Task ExecuteScheduledReportAsync(string scheduleId, CancellationToken ct)
public Task<DateTime> CalculateNextRunAsync(ScheduleType type, IReadOnlyDictionary<string, object> config)
public Task<IReadOnlyList<ScheduledReport>> GetActiveSchedulesAsync(CancellationToken ct)
```

**Estimated Lines:** ~120

---

#### 6. ReportSharingManager
**Responsibilities:**
- Report sharing with users
- Permission management
- Access tracking
- Share expiration handling

**Methods:**
```csharp
public Task<ReportSharing> ShareReportAsync(ReportSharingRequest request, CancellationToken ct)
public Task TrackAccessAsync(string sharingId, CancellationToken ct)
public Task<bool> ValidateAccessAsync(string sharingId, string userId, CancellationToken ct)
public Task RevokeSharingAsync(string sharingId, CancellationToken ct)
```

**Estimated Lines:** ~100

---

## Before/After Code Structure

### Before (Current)

```csharp
// AdvancedReportingService.cs - 1,063 lines
public class AdvancedReportingService : IAdvancedReportingService
{
    private readonly ReportEngine _reportEngine;
    private readonly DashboardBuilder _dashboardBuilder;
    // ... other fields
    
    public async Task<Result<Report>> GenerateReportAsync(ReportRequest request, CancellationToken ct = default)
    {
        // 20+ lines of logging, validation, delegation
        var report = await _reportEngine.GenerateReportAsync(request, ct);
        return Result.Success(report);
    }
    
    // 13 more public methods...
    // 3 private methods...
    // 4 nested classes (ReportEngine, DashboardBuilder, etc.)...
    // 40+ data model classes...
}
```

### After (Target)

```csharp
// AdvancedReportingService.cs - ~150 lines
public class AdvancedReportingService : IAdvancedReportingService
{
    private readonly ReportGenerationManager _reportGenerationManager;
    private readonly DashboardManager _dashboardManager;
    private readonly VisualizationManager _visualizationManager;
    private readonly ReportTemplateManager _templateManager;
    private readonly ReportSchedulingManager _schedulingManager;
    private readonly ReportSharingManager _sharingManager;
    
    public AdvancedReportingService(
        ReportGenerationManager reportGenerationManager,
        DashboardManager dashboardManager,
        VisualizationManager visualizationManager,
        ReportTemplateManager templateManager,
        ReportSchedulingManager schedulingManager,
        ReportSharingManager sharingManager)
    {
        _reportGenerationManager = reportGenerationManager;
        _dashboardManager = dashboardManager;
        _visualizationManager = visualizationManager;
        _templateManager = templateManager;
        _schedulingManager = schedulingManager;
        _sharingManager = sharingManager;
    }
    
    public Task<Result<Report>> GenerateReportAsync(ReportRequest request, CancellationToken ct = default)
        => _reportGenerationManager.GenerateReportAsync(request, ct);
    
    public Task<Result<Dashboard>> CreateDashboardAsync(DashboardRequest request, CancellationToken ct = default)
        => _dashboardManager.CreateDashboardAsync(request, ct);
    
    // Other methods delegate to appropriate managers...
}

// Managers/ReportGenerationManager.cs - ~180 lines
public class ReportGenerationManager
{
    private readonly ILogger<ReportGenerationManager> _logger;
    private readonly ITimeProvider _timeProvider;
    
    public async Task<Report> GenerateReportAsync(ReportRequest request, CancellationToken ct)
    {
        // Full implementation
    }
}

// Similar for other managers...
```

---

## Data Model Restructuring

### New File Structure

```
AdvancedReporting/
├── Services/
│   ├── AdvancedReportingService.cs (coordinator)
│   ├── ReportGenerationManager.cs
│   ├── DashboardManager.cs
│   ├── VisualizationManager.cs
│   ├── ReportTemplateManager.cs
│   ├── ReportSchedulingManager.cs
│   └── ReportSharingManager.cs
├── Models/
│   ├── Report.cs
│   ├── ReportPage.cs
│   ├── ReportTemplate.cs
│   ├── Dashboard.cs
│   ├── DashboardWidget.cs
│   ├── ChartData.cs
│   ├── DataPoint.cs
│   ├── ScheduledReport.cs
│   └── [Other models...]
├── Enums/
│   ├── ReportType.cs
│   ├── ChartType.cs
│   ├── ScheduleType.cs
│   ├── ExportFormat.cs
│   └── [Other enums...]
└── Interfaces/
    └── IAdvancedReportingService.cs
```

---

## Edge Cases and Challenges

### 1. Nested Class Dependencies
**Challenge:** `ReportEngine`, `DashboardBuilder`, and other nested classes have tight coupling with parent service state.

**Solution:** 
- Pass required dependencies (ITimeProvider, ILogger) via constructor
- Move shared state (cache, dictionaries) to appropriate managers
- Use DI container for manager registration

### 2. Dictionary State Management
**Challenge:** Current service uses in-memory dictionaries (`_reportTemplates`, `_dashboards`, `_scheduledReports`).

**Solution:**
- Each manager owns its respective dictionary
- Consider extracting to a `IReportingRepository` interface for future database migration
- Keep in-memory for now (as per current implementation)

### 3. Cross-Manager Dependencies
**Challenge:** Some operations may need data from multiple managers.

**Solution:**
- Coordinator service orchestrates cross-manager operations
- Use events/messaging for loosely coupled communication
- Example: Report scheduling triggers report generation

### 4. Template Initialization
**Challenge:** `InitializeReportTemplates()` currently called in constructor.

**Solution:**
- Move to `ReportTemplateManager.InitializeDefaultTemplatesAsync()`
- Call from coordinator or during app startup

### 5. Shared Enums and Types
**Challenge:** Many enums (ReportType, ChartType, etc.) used across managers.

**Solution:**
- Keep enums in shared `Models/Enums` folder
- Ensure no circular dependencies between managers

---

## Implementation Steps

### Phase 1: Extract Data Models
1. Create `Models/` directory structure
2. Move all DTO classes to appropriate files
3. Move all enums to `Enums/` directory
4. Update using statements

### Phase 2: Create Managers
1. Create `ReportGenerationManager` from `ReportEngine`
2. Create `DashboardManager` from `DashboardBuilder`
3. Create `VisualizationManager` from `DataVisualizationEngine`
4. Create `ReportTemplateManager` (new)
5. Create `ReportSchedulingManager` from `ReportScheduler`
6. Create `ReportSharingManager` (new)

### Phase 3: Refactor Coordinator
1. Update `AdvancedReportingService` to use managers
2. Remove nested classes
3. Update constructor to accept managers via DI
4. Simplify methods to delegate calls

### Phase 4: Update DI Registration
1. Register all managers in DI container
2. Update service registration
3. Ensure proper lifetime scopes (Scoped recommended)

### Phase 5: Testing
1. Update unit tests to test managers independently
2. Add integration tests for coordinator
3. Verify all existing functionality preserved

---

## Statistics Summary

| Metric | Before | After | Change |
|--------|--------|-------|--------|
| Files | 1 | 7 | +6 |
| Lines per file | 1,063 | ~150 avg | -86% |
| Classes per file | 5 | 1 | Clean separation |
| Public methods per class | 14 | ~2-4 | Focused |
| Testability | Low | High | Isolated units |

---

## References

- [AGENTS.md](../../../AGENTS.md) - Manager Pattern guidelines
- [Interface Segregation ADR](../../../docs/architecture/adrs/) - Interface splitting patterns
- Existing Manager implementations: `IkemenGoService`, `CharacterDiscoveryService`
