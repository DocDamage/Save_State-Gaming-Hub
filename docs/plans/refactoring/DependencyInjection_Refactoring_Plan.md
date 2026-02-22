# DependencyInjection Refactoring Plan

## Current State Analysis

### File Statistics
- **File**: `src/SaveState.Infrastructure/DependencyInjection.cs`
- **Lines**: 1,051
- **Status**: ⚠️ **Partially decomposed** - needs completion

### Current Decomposition (Good)

Already extracted to extension methods:

```csharp
✅ AddOpenApiDocumentation()      // Lines 944-949
✅ AddMetrics()                   // Lines 954-967
✅ AddInfrastructureLogging()     // Lines 975-990
✅ AddSmartLauncherServices()     // Lines 995-1019
✅ AddRetroArchServices()         // Lines 1024-1049
```

### Main Method Analysis (AddInfrastructure)

The main `AddInfrastructure` method (lines 97-939) is **843 lines** and contains:

| Section | Lines | Content |
|---------|-------|---------|
| Database | 15 | DbContext registration |
| Repositories | 22 | 20+ repository registrations |
| ROM Management | 4 | Verification service |
| Session Tracking | 2 | Session tracking service |
| Game Detection | 6 | Scanner services |
| Social Services | 8 | Discord, reviews, collections |
| Achievement/Mod/Media | 3 | Core services |
| Plugin System | 3 | Plugin manager |
| User Services | 4 | Preferences, TimeProvider, TaskRunner |
| MUGEN Services | 50 | Fusion, Move, Graphics, Sound |
| **Manager Patterns** | **~120** | **8 services with managers** |
| IKEMEN GO | 10 | Installation, config, launch, etc. |
| OpenMK | 7 | Character, progress, match state |
| ML Services | 3 | Machine learning |
| Game Providers | 3 | Steam, GOG, Epic |
| **HTTP Clients** | **~60** | **12+ external APIs with resilience** |
| Cloud Sync | 3 | Sync service |
| Caching/Rate Limiting | 4 | Memory cache, rate limiter |
| Authentication | 4 | JWT, password hasher, user context |
| User Repositories | 4 | User, role, API key |
| Metadata/Cover Art | 4 | IGDB, image resizer |
| Analytics | 6 | Analytics, streak, prediction, goals |
| Backlog/Collections | 3 | Backlog, virtual collections |
| Phase 2 Intelligence | 30 | Recommendations, DNA, thumbnails, search |
| OpenAI Clients | 2 | Image, embedding |
| Recommendations | 2 | Core recommendation services |
| Plugin Services | 2 | Dependency resolver, settings |
| Assistant | 2 | Game assistant, eye tracking |
| AI/ML Services | 20 | Difficulty analyzer, session monitor, preferences |
| Save State Services | 10 | Manager, branching, auto-save, cloud |
| Input Services | 4 | Input, controller, Steam Deck, touch |
| Performance | 6 | Monitor, battery, resources, display |
| Launch Experience | 4 | Launch manager, game briefing |
| Cloud Gaming | 8 | Catalog, gaming manager, network quality |
| Voice Commands | 2 | Voice service, speech recognition |
| Phase 7 Services | 40 | Query optimizer, ML, theming, multiplayer, etc. |
| Automation | 6 | Macro recorder/player, backup, workflow |
| MUGEN Tournament | 10 | Repositories, simulators, services |
| HTTP Clients (more) | 4 | Twitch, IGDB, SteamGridDB |
| Health Checks | 8 | Database, metrics, APIs, resources |
| Monitoring | 6 | Metrics, performance, database, cache, errors |
| AI Providers | 15 | OpenAI, Groq, Local with resilience |
| **Options Validation** | **~150** | **15+ options with validation** |
| AI Services | 8 | Knowledge, memory, orchestrator, voice |
| Image Analysis | 1 | Screenshot scanning |
| Cloud Sync | 4 | Authentication, providers |
| RetroArch | 1 | Extension method call |
| Subscriptions/Deals | 2 | Extension method calls |
| Smart Launcher | 1 | Extension method call |
| Metrics | 1 | Extension method call |
| AI Co-Op | 1 | Companion service |
| OpenAPI | 1 | Extension method call |

---

## Refactoring Goals

1. **Extract logical groups** into extension methods
2. **Reduce main method** to ~200 lines (coordinator)
3. **Improve organization** by feature/domain
4. **Maintain registration order** where important
5. **Enable parallel work** - teams can own different feature DI

---

## Recommended Refactoring Strategy

### Target Architecture

```
DependencyInjection.cs (coordinator - ~200 lines)
├── AddInfrastructure() - orchestrates registration
├── AddDatabaseServices()
├── AddRepositoryServices()
├── AddCoreInfrastructureServices()
├── AddMugenServices() - includes managers
├── AddRetroArchServices() ✅
├── AddAiServices()
├── AddExternalApiClients()
├── AddCloudSyncServices()
├── AddAnalyticsServices()
├── AddIntelligenceServices() - Phase 2
├── AddAutomationServices()
├── AddMonitoringServices()
├── AddConfigurationValidation()
├── AddSmartLauncherServices() ✅
└── AddOpenApiDocumentation() ✅
```

---

## Implementation Plan

### Phase 1: Extract Database & Repositories

**New Method**: `AddDatabaseServices()`

```csharp
private static IServiceCollection AddDatabaseServices(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // Database (lines 99-113)
    services.AddDbContext<ISaveStateDbContext, SaveStateDbContext>(...)
        .AddDbContextFactory<SaveStateDbContext>(...);
    
    return services;
}
```

**New Method**: `AddRepositoryServices()`

```csharp
private static IServiceCollection AddRepositoryServices(
    this IServiceCollection services)
{
    // Lines 115-136 + 371-373
    services.AddScoped<IGameRepository, GameRepository>();
    services.AddScoped<IPlatformRepository, PlatformRepository>();
    // ... 20+ more repositories
    
    return services;
}
```

### Phase 2: Extract Core Infrastructure

**New Method**: `AddCoreInfrastructureServices()`

```csharp
private static IServiceCollection AddCoreInfrastructureServices(
    this IServiceCollection services)
{
    // Session Tracking (138)
    // Game Detection (140-145)
    // Social Services (147-155)
    // Achievement/Mod/Media (156-158)
    // Plugin System (160-162)
    // User Services (164-167)
    // Infrastructure Logging (169)
    // Culture/Localization (172-173)
    // Accessibility (175-176)
    
    return services;
}
```

### Phase 3: Extract MUGEN Services

**New Method**: `AddMugenServices()`

Already partially extracted (managers), but consolidate:

```csharp
private static IServiceCollection AddMugenServices(
    this IServiceCollection services)
{
    // Fusion, Move, Graphics, Sound (178-182)
    // Sprite Animation - Manager Pattern (184-191)
    // Predictive Analytics - Manager Pattern (193-199)
    // Combo Database - Manager Pattern (201-210)
    // Blockchain - Manager Pattern (212-217)
    // Advanced Graphics - Manager Pattern (219-225)
    // Sound Design Studio - Manager Pattern (227-235)
    // Story Mode - Manager Pattern (236-245)
    // Performance Profiler - Manager Pattern (247-254)
    // Symbiotic Partner - Manager Pattern (256-263)
    // Replay Analysis - Manager Pattern (264-269)
    // IKEMEN GO - Manager Pattern (271-280)
    // OpenMK (282-289)
    // ML Services (291-293)
    // Tournament Features (642-658)
    
    return services;
}
```

### Phase 4: Extract AI Services

**New Method**: `AddAiServices()`

```csharp
private static IServiceCollection AddAiServices(
    this IServiceCollection services)
{
    // AI Providers (706-721)
    // AI Services (884-894)
    // Image Analysis (896)
    // Phase 1.2 AI Assistant (451-480)
    // AI Co-Op Companion (933)
    
    return services;
}

private static IServiceCollection AddAiProviderServices(
    this IServiceCollection services)
{
    // OpenAI LLM (707-712)
    // Groq (714-719)
    // Local Embedded (721)
    
    return services;
}
```

### Phase 5: Extract External APIs

**New Method**: `AddExternalApiClients()`

```csharp
private static IServiceCollection AddExternalApiClients(
    this IServiceCollection services)
{
    // Game Providers (295-298)
    // Steam (300-304)
    // GOG (306-310)
    // Epic (312-316)
    // OneDrive (318-322)
    // Google Drive (324-325)
    // Cloud Auth (327-328)
    // Mod Updates (330-331)
    // RetroAchievements (334-338)
    // HowLongToBeat (340-345)
    // IsThereAnyDeal (347-352)
    // Twitch/IGDB (661-668)
    // SteamGridDB (671-678)
    // Azure Speech (537-544)
    // Google Cloud (546-562)
    // OpenAI HTTP (564-571)
    // Cloud Catalog (525-526)
    // Web Search (891-892)
    
    return services;
}
```

### Phase 6: Extract Cloud & Sync Services

**New Method**: `AddCloudSyncServices()`

```csharp
private static IServiceCollection AddCloudSyncServices(
    this IServiceCollection services)
{
    // Cloud Sync Core (355-356)
    // Network Optimizer (522)
    // Cloud Gaming (516-518)
    // Storage Providers (902-912)
    
    return services;
}
```

### Phase 7: Extract Analytics & Intelligence

**New Method**: `AddAnalyticsServices()`

```csharp
private static IServiceCollection AddAnalyticsServices(
    this IServiceCollection services)
{
    // Analytics (387-392)
    // Backlog (395-396)
    // Collections (398-399)
    // Smart Categorization (401-402)
    
    return services;
}

private static IServiceCollection AddIntelligenceServices(
    this IServiceCollection services)
{
    // Phase 2 Intelligence (404-433)
    // Recommendation Engine V2 (405-407)
    // Gamer DNA (409-410)
    // AI Content Generation (412-423)
    // Universal Search (425-434)
    // OpenAI Clients (436-437)
    // Recommendations (439-442)
    
    return services;
}
```

### Phase 8: Extract Save State & Input Services

**New Method**: `AddSaveStateServices()`

```csharp
private static IServiceCollection AddSaveStateServices(
    this IServiceCollection services)
{
    // Save State (482-492)
    // Input (494-498)
    // Performance (500-506)
    
    return services;
}
```

### Phase 9: Extract Phase Services

**New Method**: `AddLaunchExperienceServices()`

```csharp
private static IServiceCollection AddLaunchExperienceServices(
    this IServiceCollection services)
{
    // Phase 4 (511-513)
    // Phase 5 (515-526)
    // Phase 6 (528-530)
    // Phase 7 (532-625)
    // Phase 8 (630-641)
    
    return services;
}
```

### Phase 10: Extract Automation Services

**New Method**: `AddAutomationServices()`

```csharp
private static IServiceCollection AddAutomationServices(
    this IServiceCollection services)
{
    // Automation (626-641)
    
    return services;
}
```

### Phase 11: Extract Monitoring & Health

**New Method**: `AddMonitoringServices()`

```csharp
private static IServiceCollection AddMonitoringServices(
    this IServiceCollection services)
{
    // Health Checks (681-688)
    // Application Metrics (691)
    // Performance Monitoring (693-695)
    // Database Monitoring (697-698)
    // Cache Monitoring (700-701)
    // Error Tracking (703-704)
    
    return services;
}
```

### Phase 12: Extract Options Validation

**New Method**: `AddConfigurationValidation()`

```csharp
private static IServiceCollection AddConfigurationValidation(
    this IServiceCollection services)
{
    // OpenAI Options (723-734)
    // Groq Options (736-746)
    // AI Options (748-752)
    // Steam Options (754-763)
    // GOG Options (765-773)
    // Epic Options (775-783)
    // IGDB Options (785-793)
    // SteamGridDB Options (795-805)
    // Resilience Config (807-819)
    // Memory Options (821-825)
    // Application Options (827-830)
    // Database Options (832-835)
    // Launch Options (837-840)
    // Cheat Detection Options (842-845)
    // Rate Limiting Options (847-850)
    // JWT Options (852-856)
    // Authentication Options (858-861)
    // Localization Options (863-872)
    // Mugen Options (874-877)
    // CloudSync Options (879-882)
    
    return services;
}
```

---

## Refactored Main Method Structure

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    // 1. Foundation
    services.AddDatabaseServices(configuration);
    services.AddRepositoryServices();
    services.AddCoreInfrastructureServices();
    
    // 2. Feature Domains
    services.AddMugenServices();
    services.AddAiServices();
    services.AddExternalApiClients();
    services.AddCloudSyncServices();
    services.AddAnalyticsServices();
    services.AddIntelligenceServices();
    services.AddSaveStateServices();
    services.AddLaunchExperienceServices();
    services.AddAutomationServices();
    
    // 3. Operations
    services.AddMonitoringServices();
    services.AddConfigurationValidation();
    
    // 4. Extensions (already extracted)
    services.AddSmartLauncherServices();
    services.AddRetroArchServices();
    services.AddOpenApiDocumentation();
    services.AddMetrics();
    
    return services;
}
```

**Estimated lines**: ~50 lines (down from 843)

---

## Benefits After Refactoring

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| Main Method Lines | 843 | ~50 | **94% reduction** |
| Total File Lines | 1,051 | ~100 + extension files | Better organization |
| Methods per File | 1 giant + 5 small | 12 focused | Better maintainability |
| Feature Isolation | Poor | Excellent | Parallel development |
| Testability | Hard | Easy | Mock specific groups |
| Code Review Scope | Large | Focused | Faster reviews |

---

## Implementation Order

1. **Phase 1** (Database, Repositories) - Low risk, foundation
2. **Phase 8** (Save State, Input) - Clear boundaries
3. **Phase 4** (AI Services) - Well-defined scope
4. **Phase 5** (External APIs) - Many similar registrations
5. **Phase 3** (MUGEN) - Complex, many managers
6. **Phase 7** (Analytics, Intelligence) - Related services
7. **Phase 6** (Cloud Sync) - Distinct domain
8. **Phase 9** (Launch Experience) - Phase-based grouping
9. **Phase 10** (Automation) - Small, distinct
10. **Phase 11** (Monitoring) - Cross-cutting
11. **Phase 12** (Options Validation) - Repetitive pattern

---

## Effort Estimate

| Phase | Effort | Risk |
|-------|--------|------|
| Phase 1 | 30 min | Low |
| Phase 2 | 20 min | Low |
| Phase 3 | 45 min | Medium |
| Phase 4 | 30 min | Low |
| Phase 5 | 40 min | Low |
| Phase 6 | 20 min | Low |
| Phase 7 | 25 min | Low |
| Phase 8 | 20 min | Low |
| Phase 9 | 30 min | Medium |
| Phase 10 | 15 min | Low |
| Phase 11 | 20 min | Low |
| Phase 12 | 45 min | Low |
| Testing | 60 min | - |

**Total**: ~6-7 hours

---

## Risk Mitigation

1. **Registration Order**: Some services may depend on registration order. Test thoroughly after each phase.

2. **Scoped vs Singleton**: Verify lifetimes are preserved during extraction.

3. **HttpClient Registration**: `AddHttpClient` with named clients needs careful handling.

4. **Options Validation**: `ValidateOnStart()` behavior must be maintained.

5. **Decorators**: `services.Decorate<IMetadataService>()` order must be preserved.

---

## Verification Checklist

- [ ] All 12 extraction phases complete
- [ ] Main method reduced to ~50 lines
- [ ] All service registrations preserved
- [ ] Registration order verified for dependencies
- [ ] Scoped/Singleton lifetimes correct
- [ ] HttpClient configurations intact
- [ ] Options validation working
- [ ] Decorator order preserved
- [ ] Build succeeds with 0 errors
- [ ] All tests pass
- [ ] Application starts correctly
- [ ] DI container resolves all services

---

## Related Files

Files that may need updates if method signatures change:
- `src/SaveState.Presentation/App.axaml.cs` - Calls `AddInfrastructure()`
- `tests/SaveState.IntegrationTests/` - May use specific service registrations

---

*Plan created: February 21, 2026*
*Status: Ready for implementation*
