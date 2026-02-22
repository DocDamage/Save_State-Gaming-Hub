# EnterpriseSecurityService Refactoring Plan

## Manager Pattern Implementation

**Target File:** `src/SaveState.Application/Mugen/Services/EnterpriseSecurityService.cs`  
**Current Size:** 1,044 lines  
**Target Size:** ~110 lines (coordinator) + 6 manager classes (~130 lines each)  
**Estimated Reduction:** 25% (1,044 → ~890 total, with proper separation of concerns)

---

## 1. Current Analysis

### Statistics
| Metric | Value |
|--------|-------|
| Total Lines | 1,044 |
| Public Methods | 9 |
| Private Methods | 2 |
| Nested Classes | 20 (5 engines + 15 data classes) |
| State Dictionaries | 3 (security policies, audit logs, compliance reports) |
| Engine Classes | 5 |

### Current Architecture
```
EnterpriseSecurityService (1,044 lines)
├── 5 Engine Classes (private nested)
│   ├── AccessControlEngine (60 lines)
│   ├── EncryptionEngine (95 lines)
│   ├── ComplianceMonitor (105 lines)
│   ├── ThreatDetectionEngine (70 lines)
│   └── AuditTrailManager (60 lines)
├── 15 Data Classes
└── 3 State Dictionaries (in-memory)
```

### Responsibilities Currently Mixed
1. **Access Control** - Permission evaluation, access decisions
2. **Encryption** - Data encryption/decryption, key management
3. **Compliance** - Regulatory compliance monitoring, report generation
4. **Threat Detection** - Security assessments, vulnerability scanning
5. **Audit Trail** - Security event logging, audit log querying
6. **Security Policy Management** - Policy creation, storage, initialization
7. **Incident Management** - Security incident reporting and tracking
8. **Data Classification** - Data sensitivity classification
9. **Security Metrics** - Overall security metrics generation

---

## 2. Proposed Manager Structure

```
EnterpriseSecurityService (~110 lines) - Coordinator
├── AccessControlManager (~120 lines)
├── EncryptionManager (~140 lines)
├── ComplianceManager (~150 lines)
├── ThreatDetectionManager (~120 lines)
├── AuditTrailManager (~130 lines)
└── SecurityPolicyManager (~140 lines)
```

### Manager Classes

#### 2.1 AccessControlManager
**Responsibilities:**
- Access control evaluation
- Permission checking
- Access decision generation
- Role-based access control (RBAC)
- Resource access evaluation
- Access control logging

**Public Methods:**
```csharp
Task<Result<AccessControlDecision>> EvaluateAccessAsync(
    string userId, string resourceId, Permission permission, CancellationToken ct);
    
Task<bool> HasPermissionAsync(string userId, string resourceId, Permission permission, CancellationToken ct);
Task<IReadOnlyList<string>> GetUserRolesAsync(string userId, CancellationToken ct);
Task<IReadOnlyList<string>> GetResourcePermissionsAsync(string resourceId, CancellationToken ct);

// Access decision helpers
AccessDecision EvaluateRoleBasedAccess(string userId, string resourceId, Permission permission);
AccessDecision EvaluateAttributeBasedAccess(string userId, string resourceId, Permission permission);
string GenerateAccessReason(AccessDecision decision, string userId, string resourceId);
```

**State:** None (stateless - would query role/permission store in production)

---

#### 2.2 EncryptionManager
**Responsibilities:**
- Data encryption with various levels
- Data decryption
- Key generation and management
- Algorithm selection
- Encryption result tracking

**Public Methods:**
```csharp
Task<Result<EncryptionResult>> EncryptAsync(string data, EncryptionLevel level, CancellationToken ct);
Task<Result<string>> DecryptAsync(string encryptedData, string keyId, CancellationToken ct);
Task<string> GenerateKeyAsync(EncryptionLevel level, CancellationToken ct);
Task<bool> ValidateKeyAsync(string keyId, CancellationToken ct);
Task RevokeKeyAsync(string keyId, CancellationToken ct);

// Encryption helpers
byte[] EncryptData(string data, EncryptionLevel level);
string DecryptData(byte[] encryptedData, string keyId);
string SelectAlgorithm(EncryptionLevel level);
int GetKeySize(EncryptionLevel level);
```

**State:** None (keys would be stored in secure key vault in production)

---

#### 2.3 ComplianceManager
**Responsibilities:**
- Compliance report generation
- Regulatory framework monitoring
- Compliance requirement tracking
- Compliance finding management
- Data classification
- Compliance scoring

**Public Methods:**
```csharp
Task<Result<ComplianceReport>> GenerateReportAsync(
    ComplianceFramework framework, DateTime startDate, DateTime endDate, CancellationToken ct);
    
Task<Result<DataClassification>> ClassifyDataAsync(string data, DataSensitivity sensitivity, CancellationToken ct);
Task<ComplianceStatus> CheckComplianceStatusAsync(ComplianceFramework framework, CancellationToken ct);
Task<IReadOnlyList<ComplianceFinding>> GetOpenFindingsAsync(ComplianceFramework framework, CancellationToken ct);
Task<double> CalculateComplianceScoreAsync(ComplianceFramework framework, CancellationToken ct);

// Compliance helpers
ComplianceStatus DetermineOverallStatus(IReadOnlyList<ComplianceRequirement> requirements);
IReadOnlyList<ComplianceRequirement> GenerateRequirements(ComplianceFramework framework);
DataClassificationLevel MapSensitivityToLevel(DataSensitivity sensitivity);
```

**State:** None (compliance data would come from external store in production)

---

#### 2.4 ThreatDetectionManager
**Responsibilities:**
- Security assessment execution
- Threat detection and analysis
- Vulnerability scanning
- Risk level assessment
- Security finding generation
- Security recommendations

**Public Methods:**
```csharp
Task<Result<SecurityAssessment>> PerformAssessmentAsync(
    string targetId, AssessmentType assessmentType, CancellationToken ct);
    
Task<SecurityRiskLevel> AssessRiskLevelAsync(string targetId, CancellationToken ct);
Task<IReadOnlyList<SecurityFinding>> ScanForVulnerabilitiesAsync(string targetId, CancellationToken ct);
Task<IReadOnlyList<SecurityFinding>> DetectThreatsAsync(string targetId, CancellationToken ct);
Task<IReadOnlyList<string>> GenerateRecommendationsAsync(SecurityAssessment assessment, CancellationToken ct);

// Threat detection helpers
IReadOnlyList<SecurityFinding> AnalyzeConfiguration(string targetId);
IReadOnlyList<SecurityFinding> ScanVulnerabilities(string targetId);
double CalculateRiskScore(IReadOnlyList<SecurityFinding> findings);
SecurityRiskLevel DetermineRiskLevel(double riskScore);
```

**State:** None (stateless threat detection service)

---

#### 2.5 AuditTrailManager
**Responsibilities:**
- Security event logging
- Audit trail management
- Audit log querying and filtering
- Audit log storage
- Security incident audit logging

**Public Methods:**
```csharp
Task LogSecurityEventAsync(SecurityEvent securityEvent, CancellationToken ct);
Task<Result<IReadOnlyList<AuditLog>>> QueryLogsAsync(AuditQuery query, CancellationToken ct);
Task<AuditLog> GetLogByIdAsync(string logId, CancellationToken ct);
Task<IReadOnlyList<AuditLog>> GetLogsForUserAsync(string userId, int limit, CancellationToken ct);
Task<IReadOnlyList<AuditLog>> GetLogsForResourceAsync(string resourceId, int limit, CancellationToken ct);
Task ArchiveOldLogsAsync(TimeSpan retentionPeriod, CancellationToken ct);

// Audit helpers
AuditLog CreateAuditLog(SecurityEvent securityEvent);
IQueryable<AuditLog> ApplyQueryFilters(IQueryable<AuditLog> logs, AuditQuery query);
string SerializeEventDetails(IReadOnlyDictionary<string, object> details);
```

**State:**
- `Dictionary<string, AuditLog>` - In-memory audit log storage (would be database in production)

---

#### 2.6 SecurityPolicyManager
**Responsibilities:**
- Security policy creation
- Security policy storage
- Policy rule management
- Default policy initialization
- Policy enforcement validation
- Security incident management

**Public Methods:**
```csharp
Task<Result<SecurityPolicy>> CreatePolicyAsync(SecurityPolicyRequest request, CancellationToken ct);
Task<Result<SecurityPolicy>> GetPolicyAsync(string policyId, CancellationToken ct);
Task<IReadOnlyList<SecurityPolicy>> GetActivePoliciesAsync(CancellationToken ct);
Task<Result<SecurityPolicy>> UpdatePolicyAsync(string policyId, SecurityPolicyRequest request, CancellationToken ct);
Task<Result> DeletePolicyAsync(string policyId, CancellationToken ct);
Task<bool> ValidatePolicyAsync(SecurityPolicy policy, CancellationToken ct);

// Incident management
Task<Result<SecurityIncident>> ReportIncidentAsync(SecurityIncidentReport report, CancellationToken ct);
Task<Result<SecurityIncident>> GetIncidentAsync(string incidentId, CancellationToken ct);
Task<Result<SecurityIncident>> UpdateIncidentAsync(string incidentId, string resolution, CancellationToken ct);

// Policy helpers
void InitializeDefaultPolicies();
SecurityRule CreatePasswordLengthRule(int minLength);
SecurityRule CreatePasswordComplexityRule(bool requireComplexity);
SecurityPolicy CreatePasswordPolicy();
```

**State:**
- `Dictionary<string, SecurityPolicy>` - Policy storage
- `Dictionary<string, SecurityIncident>` - Incident tracking (simplified)

---

## 3. Before/After Code Structure

### Before (Current - Monolithic)
```csharp
public class EnterpriseSecurityService : IEnterpriseSecurityService
{
    private readonly Dictionary<string, SecurityPolicy> _securityPolicies = new();
    private readonly Dictionary<string, AuditLog> _auditLogs = new();
    private readonly Dictionary<string, ComplianceReport> _complianceReports = new();
    private readonly AccessControlEngine _accessControl;
    private readonly EncryptionEngine _encryptionEngine;
    private readonly ComplianceMonitor _complianceMonitor;
    private readonly ThreatDetectionEngine _threatDetection;
    private readonly AuditTrailManager _auditManager;

    public async Task<Result<SecurityAssessment>> PerformSecurityAssessmentAsync(...)
    {
        var assessment = await _threatDetection.PerformAssessmentAsync(targetId, assessmentType, ct);
        await _auditManager.LogSecurityEventAsync(new SecurityEvent { ... }, ct);
        return Result.Success(assessment);
    }

    public async Task<Result<AccessControlDecision>> CheckAccessControlAsync(...)
    {
        var decision = await _accessControl.EvaluateAccessAsync(userId, resourceId, permission, ct);
        await _auditManager.LogSecurityEventAsync(new SecurityEvent { ... }, ct);
        return Result.Success(decision);
    }

    // 9 public methods, 2 private methods, 20 nested classes = 1,044 lines
}
```

### After (Refactored - Manager Pattern)
```csharp
/// <summary>
/// Coordinator service for enterprise security operations.
/// Provides comprehensive security measures, compliance monitoring, and audit trails.
/// </summary>
public class EnterpriseSecurityService : IEnterpriseSecurityService
{
    private readonly ILogger<EnterpriseSecurityService> _logger;
    private readonly AccessControlManager _accessControlManager;
    private readonly EncryptionManager _encryptionManager;
    private readonly ComplianceManager _complianceManager;
    private readonly ThreatDetectionManager _threatDetectionManager;
    private readonly AuditTrailManager _auditTrailManager;
    private readonly SecurityPolicyManager _securityPolicyManager;

    public EnterpriseSecurityService(
        ILogger<EnterpriseSecurityService> logger,
        AccessControlManager accessControlManager,
        EncryptionManager encryptionManager,
        ComplianceManager complianceManager,
        ThreatDetectionManager threatDetectionManager,
        AuditTrailManager auditTrailManager,
        SecurityPolicyManager securityPolicyManager)
    {
        _logger = logger;
        _accessControlManager = accessControlManager;
        _encryptionManager = encryptionManager;
        _complianceManager = complianceManager;
        _threatDetectionManager = threatDetectionManager;
        _auditTrailManager = auditTrailManager;
        _securityPolicyManager = securityPolicyManager;

        _logger.LogInformation("Enterprise security service initialized");
    }

    // Security Assessment
    public async Task<Result<SecurityAssessment>> PerformSecurityAssessmentAsync(
        string targetId, AssessmentType assessmentType, CancellationToken ct = default)
    {
        var result = await _threatDetectionManager.PerformAssessmentAsync(targetId, assessmentType, ct);
        
        if (result.IsSuccess)
        {
            await LogSecurityEventAsync(SecurityEventType.AssessmentPerformed, targetId, "system", 
                new Dictionary<string, object>
                {
                    ["assessment_type"] = assessmentType,
                    ["risk_level"] = result.Value.OverallRisk,
                    ["findings_count"] = result.Value.Findings.Count
                }, ct);
        }

        return result;
    }

    // Access Control
    public async Task<Result<AccessControlDecision>> CheckAccessControlAsync(
        string userId, string resourceId, Permission permission, CancellationToken ct = default)
    {
        var result = await _accessControlManager.EvaluateAccessAsync(userId, resourceId, permission, ct);
        
        if (result.IsSuccess)
        {
            await LogSecurityEventAsync(SecurityEventType.AccessControlCheck, resourceId, userId,
                new Dictionary<string, object>
                {
                    ["permission"] = permission,
                    ["decision"] = result.Value.Decision,
                    ["reason"] = result.Value.Reason
                }, ct);
        }

        return result;
    }

    // Encryption
    public Task<Result<EncryptionResult>> EncryptDataAsync(
        string data, EncryptionLevel level, CancellationToken ct = default)
        => _encryptionManager.EncryptAsync(data, level, ct);

    public Task<Result<string>> DecryptDataAsync(
        string encryptedData, string keyId, CancellationToken ct = default)
        => _encryptionManager.DecryptAsync(encryptedData, keyId, ct);

    // Compliance
    public Task<Result<ComplianceReport>> GenerateComplianceReportAsync(
        ComplianceFramework framework, DateTime startDate, DateTime endDate, CancellationToken ct = default)
        => _complianceManager.GenerateReportAsync(framework, startDate, endDate, ct);

    public Task<Result<DataClassification>> ClassifyDataAsync(
        string data, DataSensitivity sensitivity, CancellationToken ct = default)
        => _complianceManager.ClassifyDataAsync(data, sensitivity, ct);

    // Security Policy
    public async Task<Result<SecurityPolicy>> CreateSecurityPolicyAsync(
        SecurityPolicyRequest request, CancellationToken ct = default)
    {
        var result = await _securityPolicyManager.CreatePolicyAsync(request, ct);
        
        if (result.IsSuccess)
        {
            await LogSecurityEventAsync(SecurityEventType.PolicyCreated, result.Value.PolicyId, request.CreatedBy,
                new Dictionary<string, object>
                {
                    ["policy_name"] = result.Value.Name,
                    ["category"] = result.Value.Category
                }, ct);
        }

        return result;
    }

    // Audit Logs
    public Task<Result<IReadOnlyList<AuditLog>>> GetAuditLogsAsync(
        AuditQuery query, CancellationToken ct = default)
        => _auditTrailManager.QueryLogsAsync(query, ct);

    // Security Incidents
    public async Task<Result<SecurityIncident>> ReportSecurityIncidentAsync(
        SecurityIncidentReport report, CancellationToken ct = default)
    {
        var result = await _securityPolicyManager.ReportIncidentAsync(report, ct);
        
        if (result.IsSuccess)
        {
            await LogSecurityEventAsync(SecurityEventType.IncidentReported, result.Value.IncidentId, report.ReportedBy,
                new Dictionary<string, object>
                {
                    ["incident_type"] = result.Value.IncidentType,
                    ["severity"] = result.Value.Severity
                }, ct);
        }

        return result;
    }

    // Security Metrics
    public async Task<Result<SecurityMetrics>> GetSecurityMetricsAsync(
        TimeSpan period, CancellationToken ct = default)
    {
        // Aggregate metrics from various managers
        var metrics = new SecurityMetrics
        {
            Period = period,
            ThreatMetrics = await GetThreatMetricsAsync(period, ct),
            AccessMetrics = await GetAccessMetricsAsync(period, ct),
            ComplianceMetrics = await GetComplianceMetricsAsync(ct),
            GeneratedAt = DateTime.UtcNow
        };

        return Result.Success(metrics);
    }

    #region Private Helpers

    private async Task LogSecurityEventAsync(
        SecurityEventType eventType, string targetId, string userId, 
        Dictionary<string, object> details, CancellationToken ct)
    {
        var securityEvent = new SecurityEvent
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = eventType,
            TargetId = targetId,
            UserId = userId,
            Details = details,
            Timestamp = DateTime.UtcNow,
            IpAddress = "unknown",
            UserAgent = "EnterpriseSecurityService"
        };

        await _auditTrailManager.LogSecurityEventAsync(securityEvent, ct);
    }

    private async Task<ThreatMetrics> GetThreatMetricsAsync(TimeSpan period, CancellationToken ct)
    {
        // Would query from threat detection manager in production
        return new ThreatMetrics
        {
            TotalIncidents = 12,
            ResolvedIncidents = 10,
            ActiveIncidents = 2,
            AverageResponseTime = TimeSpan.FromHours(2.5)
        };
    }

    private async Task<AccessMetrics> GetAccessMetricsAsync(TimeSpan period, CancellationToken ct)
    {
        // Would query from access control manager in production
        return new AccessMetrics
        {
            TotalAccessRequests = 15420,
            ApprovedRequests = 15200,
            DeniedRequests = 220,
            AverageApprovalTime = TimeSpan.FromMinutes(3.2)
        };
    }

    private async Task<ComplianceMetrics> GetComplianceMetricsAsync(CancellationToken ct)
    {
        // Aggregate from compliance manager
        return new ComplianceMetrics
        {
            OverallComplianceScore = 0.96,
            FrameworksMonitored = new[] { "GDPR", "SOC2", "ISO27001" },
            OpenFindings = 5,
            CriticalFindings = 0
        };
    }

    #endregion
}
```

---

## 4. File Structure After Refactoring

```
src/SaveState.Application/Mugen/Services/EnterpriseSecurity/
├── EnterpriseSecurityService.cs                 (110 lines - coordinator)
├── Managers/
│   ├── AccessControlManager.cs                  (120 lines)
│   ├── EncryptionManager.cs                     (140 lines)
│   ├── ComplianceManager.cs                     (150 lines)
│   ├── ThreatDetectionManager.cs                (120 lines)
│   ├── AuditTrailManager.cs                     (130 lines)
│   └── SecurityPolicyManager.cs                 (140 lines)
├── Models/
│   ├── SecurityAssessment.cs
│   ├── SecurityFinding.cs
│   ├── AccessControlDecision.cs
│   ├── EncryptionResult.cs
│   ├── ComplianceReport.cs
│   ├── ComplianceRequirement.cs
│   ├── ComplianceFinding.cs
│   ├── SecurityPolicy.cs
│   ├── SecurityRule.cs
│   ├── SecurityPolicyRequest.cs
│   ├── AuditLog.cs
│   ├── AuditQuery.cs
│   ├── SecurityEvent.cs
│   ├── SecurityIncident.cs
│   ├── SecurityIncidentReport.cs
│   ├── DataClassification.cs
│   ├── SecurityMetrics.cs
│   ├── ThreatMetrics.cs
│   ├── AccessMetrics.cs
│   ├── ComplianceMetrics.cs
│   ├── DateRange.cs
│   ├── PerformanceZone.cs
│   └── Enums.cs                                 (All enum definitions)
└── Interfaces/
    ├── IAccessControlManager.cs
    ├── IEncryptionManager.cs
    ├── IComplianceManager.cs
    ├── IThreatDetectionManager.cs
    ├── IAuditTrailManager.cs
    ├── ISecurityPolicyManager.cs
    └── IEnterpriseSecurityService.cs
```

---

## 5. Edge Cases and Migration Challenges

### 5.1 State Management Migration
**Challenge:** Current service has 3 state dictionaries that need to move to appropriate managers.

**Solution:** 
- `_securityPolicies` → `SecurityPolicyManager`
- `_auditLogs` → `AuditTrailManager`
- `_complianceReports` → Can be removed or moved to `ComplianceManager` if needed

### 5.2 Audit Logging Integration
**Challenge:** Many methods log security events after operations, requiring audit manager access.

**Solution:** Coordinator handles audit logging orchestration. Managers return results, coordinator logs events.

```csharp
// Coordinator pattern
var result = await _threatDetectionManager.PerformAssessmentAsync(...);
if (result.IsSuccess)
{
    await _auditTrailManager.LogSecurityEventAsync(...);
}
return result;
```

### 5.3 Security Metrics Aggregation
**Challenge:** `GetSecurityMetricsAsync` aggregates data from multiple systems.

**Solution:** Coordinator aggregates from managers. Each manager provides its own metrics method.

### 5.4 Incident vs Policy Management
**Challenge:** Security incidents are currently mixed with policy management.

**Solution:** Keep incidents in `SecurityPolicyManager` for now, or extract to `IncidentManager` if it grows.

### 5.5 Encryption Key Management
**Challenge:** Current encryption uses in-memory keys (simplified).

**Solution:** `EncryptionManager` owns this. In production, would integrate with key vault service.

### 5.6 Cache Usage
**Challenge:** Current service has `ICacheService` injected but doesn't appear to use it.

**Solution:** Can be removed from coordinator, or used in appropriate manager if needed.

---

## 6. Implementation Phases

### Phase 1: Preparation (1-2 hours)
1. Create directory structure (`EnterpriseSecurity/Managers`, `EnterpriseSecurity/Models`)
2. Extract all data classes to separate files in `Models/`
3. Create manager interfaces in `Interfaces/`
4. Verify project builds after file moves

### Phase 2: Manager Implementation (4-6 hours)
1. Implement `AccessControlManager` with tests
2. Implement `EncryptionManager` with tests
3. Implement `ComplianceManager` with tests
4. Implement `ThreatDetectionManager` with tests
5. Implement `AuditTrailManager` with tests
6. Implement `SecurityPolicyManager` with tests

### Phase 3: Coordinator Refactoring (2-3 hours)
1. Refactor `EnterpriseSecurityService` to coordinator pattern
2. Update DI registration
3. Run all existing tests
4. Verify backward compatibility

### Phase 4: Cleanup (1 hour)
1. Remove old nested engine classes
2. Clean up using statements
3. Update XML documentation
4. Run full test suite

---

## 7. DI Registration Updates

```csharp
// In Program.cs or DI configuration
services.AddScoped<AccessControlManager>();
services.AddScoped<EncryptionManager>();
services.AddScoped<ComplianceManager>();
services.AddScoped<ThreatDetectionManager>();
services.AddScoped<AuditTrailManager>();
services.AddScoped<SecurityPolicyManager>();

// Keep existing registration
services.AddScoped<IEnterpriseSecurityService, EnterpriseSecurityService>();
```

---

## 8. Testing Strategy

### Unit Tests Per Manager
- `AccessControlManagerTests` - Access evaluation, permission checking
- `EncryptionManagerTests` - Encryption/decryption, key management
- `ComplianceManagerTests` - Report generation, data classification
- `ThreatDetectionManagerTests` - Assessment, vulnerability scanning
- `AuditTrailManagerTests` - Event logging, log querying
- `SecurityPolicyManagerTests` - Policy CRUD, incident management

### Integration Tests
- `EnterpriseSecurityServiceTests` - Coordinator integration, backward compatibility

---

## 9. Success Metrics

| Metric | Before | After | Target |
|--------|--------|-------|--------|
| Service Lines | 1,044 | ~110 | 89% reduction |
| Max Class Size | 1,044 | ~150 | 86% reduction |
| Testability | Low | High | Improved |
| Responsibilities/Class | 9 | 1 | SRP compliance |
| Public Methods/Class | 9 | 2-4 avg | Reduced API surface |

---

## 10. Summary

This refactoring will transform the monolithic `EnterpriseSecurityService` (1,044 lines) into a clean coordinator service (~110 lines) that delegates to 6 focused managers. Each manager handles a single responsibility:

1. **AccessControlManager** - Access control and permission evaluation
2. **EncryptionManager** - Data encryption and decryption
3. **ComplianceManager** - Regulatory compliance monitoring
4. **ThreatDetectionManager** - Security assessments and threat detection
5. **AuditTrailManager** - Security event logging and audit trails
6. **SecurityPolicyManager** - Security policies and incident management

**Benefits:**
- Single Responsibility Principle compliance
- Improved testability (test managers independently)
- Reduced cognitive load per file
- Easier maintenance and debugging
- Clear separation of concerns
- Consistent with established Manager Pattern in codebase

**Security Considerations:**
- All managers maintain the same security levels
- Encryption algorithms unchanged
- Access control logic preserved
- Audit logging remains comprehensive
- Compliance frameworks remain supported
