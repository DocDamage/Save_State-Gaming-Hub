# Character Discovery Service Refactoring Plan

**Document Version:** 1.0  
**Created:** February 20, 2026  
**Status:** ✅ COMPLETED - February 20, 2026  
**Estimated Effort:** 3-4 hours  
**Priority:** Medium (Technical Debt)

---

## 📋 Executive Summary

The `CharacterDiscoveryService` has grown to **1,109 lines** and violates the Single Responsibility Principle. This plan outlines the extraction of 6 specialized manager classes to reduce complexity and improve maintainability.

### Current State
- **File:** `src/SaveState.Infrastructure/Mugen/CharacterDiscovery/CharacterDiscoveryService.cs`
- **Lines:** 1,109
- **Methods:** 30+ public methods
- **Responsibilities:** 6 distinct areas

### Target State
- **Coordinator Service:** ~180 lines (delegates to managers)
- **6 Manager Classes:** Each <350 lines
- **Single Responsibility:** Each manager handles one domain

---

## 🏗️ Architecture Overview

```
CharacterDiscoveryService (Coordinator)
├── CharacterSearchManager
├── CharacterDetailsManager
├── UserInteractionManager
├── CollectionsManager
├── CharacterComparisonManager
└── DiscoveryAnalyticsManager
```

---

## 📦 Manager Specifications

### 1. CharacterSearchManager
**Responsibility:** Search, recommendations, trending, categories

**File:** `src/SaveState.Infrastructure/Mugen/CharacterDiscovery/Managers/CharacterSearchManager.cs`

**Methods:**
```csharp
Task<Result<CharacterSearchResult>> SearchCharactersAsync(CharacterSearchQuery query, CancellationToken ct)
Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetRecommendationsAsync(string userId, int count, CancellationToken ct)
Task<Result<IReadOnlyList<TrendingCharacter>>> GetTrendingCharactersAsync(int count, TimeSpan? timeWindow, CancellationToken ct)
Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetRecentlyAddedAsync(int count, CancellationToken ct)
Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByCategoryAsync(string category, int count, CancellationToken ct)
Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> GetSimilarCharactersAsync(Guid characterId, int count, CancellationToken ct)
Task<Result<IReadOnlyList<CharacterCombination>>> GetPopularCombinationsAsync(int count, CancellationToken ct)
Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetByAuthorAsync(string author, int count, CancellationToken ct)
Task<Result<FeaturedCharacter>> GetFeaturedCharacterAsync(CancellationToken ct)
```

**Estimated Lines:** ~280

---

### 2. CharacterDetailsManager
**Responsibility:** Character details, reviews, matchups, showcases

**File:** `src/SaveState.Infrastructure/Mugen/CharacterDiscovery/Managers/CharacterDetailsManager.cs`

**Methods:**
```csharp
Task<Result<CharacterDetail>> GetCharacterDetailsAsync(Guid characterId, CancellationToken ct)
Task<Result<CharacterReviews>> GetCharacterReviewsAsync(Guid characterId, int page, int pageSize, CancellationToken ct)
Task<Result<IReadOnlyList<CharacterMatchup>>> GetCharacterMatchupsAsync(Guid characterId, CancellationToken ct)
Task<Result<IReadOnlyList<CharacterShowcase>>> GetShowcasesAsync(Guid characterId, int count, CancellationToken ct)
Task<Result<DownloadHistory>> GetDownloadHistoryAsync(Guid characterId, CancellationToken ct)
```

**Estimated Lines:** ~180

---

### 3. UserInteractionManager
**Responsibility:** Ratings, favorites, reports, sharing

**File:** `src/SaveState.Infrastructure/Mugen/CharacterDiscovery/Managers/UserInteractionManager.cs`

**Methods:**
```csharp
Task<Result> RateCharacterAsync(Guid characterId, string userId, int rating, string? review, CancellationToken ct)
Task<Result> AddToFavoritesAsync(Guid characterId, string userId, CancellationToken ct)
Task<Result> RemoveFromFavoritesAsync(Guid characterId, string userId, CancellationToken ct)
Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetFavoritesAsync(string userId, int page, int pageSize, CancellationToken ct)
Task<Result> ReportCharacterAsync(Guid characterId, string userId, string reason, string? details, CancellationToken ct)
Task<Result<string>> ShareCharacterAsync(Guid characterId, string userId, ShareOptions options, CancellationToken ct)
```

**Estimated Lines:** ~220

---

### 4. CollectionsManager
**Responsibility:** Collections, lists, curation

**File:** `src/SaveState.Infrastructure/Mugen/CharacterDiscovery/Managers/CollectionsManager.cs`

**Methods:**
```csharp
Task<Result<CharacterCollection>> CreateCollectionAsync(string userId, string name, string? description, bool isPublic, CancellationToken ct)
Task<Result> AddToCollectionAsync(Guid collectionId, Guid characterId, string userId, CancellationToken ct)
Task<Result> RemoveFromCollectionAsync(Guid collectionId, Guid characterId, string userId, CancellationToken ct)
Task<Result<IReadOnlyList<CharacterCollection>>> GetCollectionsAsync(string userId, CancellationToken ct)
Task<Result<IReadOnlyList<CharacterCollection>>> GetPublicCollectionsAsync(int page, int pageSize, CancellationToken ct)
Task<Result<IReadOnlyList<DiscoveredCharacter>>> GetCollectionCharactersAsync(Guid collectionId, CancellationToken ct)
Task<Result> DeleteCollectionAsync(Guid collectionId, string userId, CancellationToken ct)
```

**Estimated Lines:** ~200

---

### 5. CharacterComparisonManager
**Responsibility:** Comparisons, compatibility, roster suggestions

**File:** `src/SaveState.Infrastructure/Mugen/CharacterDiscovery/Managers/CharacterComparisonManager.cs`

**Methods:**
```csharp
Task<Result<CharacterComparison>> CompareCharactersAsync(IReadOnlyList<Guid> characterIds, CancellationToken ct)
Task<Result<CompatibilityMatrix>> GetCompatibilityMatrixAsync(Guid characterId, int depth, CancellationToken ct)
Task<Result<IReadOnlyList<DiscoveredCharacterRecommendation>>> SuggestRosterCompletionAsync(IReadOnlyList<Guid> currentRoster, string playStyle, CancellationToken ct)
```

**Estimated Lines:** ~180

---

### 6. DiscoveryAnalyticsManager
**Responsibility:** Statistics, trends, user activity

**File:** `src/SaveState.Infrastructure/Mugen/CharacterDiscovery/Managers/DiscoveryAnalyticsManager.cs`

**Methods:**
```csharp
Task<Result<DiscoveryStatistics>> GetStatisticsAsync(TimeSpan? period, CancellationToken ct)
Task<Result<UserDiscoveryActivity>> GetUserActivityAsync(string userId, TimeSpan? period, CancellationToken ct)
Task<Result<IReadOnlyList<PopularityTrend>>> GetPopularityTrendsAsync(Guid characterId, TimeSpan? period, CancellationToken ct)
Task<Result<CharacterStats>> GetCharacterStatsAsync(Guid characterId, CancellationToken ct)
```

**Estimated Lines:** ~180

---

## 📝 Implementation Steps

### Phase 1: Foundation
1. Create `Managers` directory
2. Create all 6 manager classes with constructors
3. Update DI registration

### Phase 2: Migrate Logic
1. Extract Search and Discovery methods
2. Extract Character Details methods
3. Extract User Interaction methods
4. Extract Collections methods
5. Extract Comparison methods
6. Extract Analytics methods

### Phase 3: Refactor Service
1. Update `CharacterDiscoveryService` to delegate to managers
2. Remove `CharacterDiscoveryServiceOperations` class
3. Update tests

---

## ✅ Success Metrics

| Metric | Before | After | Target |
|--------|--------|-------|--------|
| Lines in Service | 1,109 | ~180 | <200 |
| Max Lines per Manager | - | ~280 | <350 |
| Test Coverage | ? | ? | Maintain |
| Build Status | ✅ | ✅ | Pass |

---

*This plan follows the same pattern as the IKEMEN GO Service refactoring.*
