# 🤖 SaveState Reborn — Project Knowledge Index

**Status**: ✅ Production Release v1.0.0 + UI Planning
**Last Updated**: January 1, 2026 (Feature Surfacing Plan Complete)
**Maintained By**: Development Team
**Next Review**: February 1, 2026
**Related Documents**: [All docs in this directory](./)

---

## Table of Contents

- [Document Roles](#-document-roles-non-overlapping)
- [Quick Find Guide](#-quick-find-guide)
- [Document Maintenance](#-document-maintenance)
- [Conflict Resolution Matrix](#-conflict-resolution-matrix-expanded)
- [Reading Order by Task Type](#-reading-order-by-task-type)
- [Cross-Reference Map](#-cross-reference-map)
- [AI Ingestion Order](#-ai-ingestion-order)
- [Codebase Quick Stats](#-codebase-quick-stats)

---

> [!NOTE]
> **UI Development Active** (January 1, 2026): Phase 1 (Shell & Navigation) and Phase 2 (Dashboard Hub) complete! 5/5 dashboard widgets implemented with real-time data. **494/494 tests passing (100%)**. Next: Phase 3 (Library Enhancement). See [FEATURE_SURFACING_PLAN.md](planning/FEATURE_SURFACING_PLAN.md) for 16-week roadmap.

---

This document defines how all project documentation is intended to be read, interpreted, and applied by humans and AI systems.

---

## 📋 Document Roles (Non-Overlapping)

### AI_MASTER_CONTEXT.md

**Role**: Canonical runtime context.
**Priority**: HIGHEST.
**If conflicts exist, this document wins.**

**Used when:**

- Writing code
- Reviewing PRs
- Generating new features
- Refactoring
- Understanding current architecture patterns
- Learning coding standards and behavioral handbook

**Contains:**

- Technical foundation (Clean Architecture, CQRS, Result Pattern)
- Core project structure and tech stack
- **Current codebase metrics (800+ files, 65K+ LOC, UI components added)**
- Coding standards & behavioral handbook
- **Current technical debt status**
- Domain truth & invariants ("sacred" rules)
- Gold standard examples with references

---

### ENGINEERING_RULES.md

**Role**: Non-negotiable constraints.
**Priority**: Equal to AI_MASTER_CONTEXT.md.

**Used when:**

- Designing systems
- Reviewing architecture
- Evaluating correctness
- Implementing new features
- Writing tests
- Setting up infrastructure

**Contains:**

- Architecture rules (layers, CQRS, Result Pattern)
- AI & automation rules (orchestration, resilience policies)
- CLI & presentation rules (stability, async safety)
- Infrastructure rules (HTTP communication, logging, configuration)
- Testing rules (isolation, test doubles, reliability)

---

### TECHNICAL_DEBT_REMEDIATION_PLAN.md ⭐ CRITICAL

**Role**: Active technical debt tracking and remediation.
**Priority**: **HIGHEST when fixing issues**.

**Used when:**

- **Fixing the build (START HERE)**
- Addressing### 🛡️ Code Consistency
**- **Health Score**: 🟢 95/100 (Target Achieved)
- **Primary Focus**: Phase 6: Code Quality & Warning Reduction
- **Compilation**: ✅ 0 Errors
- **Architecture**: 90/100 (Clean Architecture enforced)
- **Technical Debt**: 5/100 (Low - Main technical debt is now warnings and incomplete method stubs)
- **TODO Comments**: 0 (All resolved)

### 📈 Immediate Action Items

1. **Phase 4: MUGEN Persistence Layer (High Priority)**
   - Implement `MugenCharacterRepository` (`dapper` + `sqlite`)
   - Implement `MugenTournamentRepository`
   - Implement `MugenMatchHistoryRepository`

2. **Phase 5: Convert "Return Null" to Pattern**
   - Refactor `IMugenCharacterRepository.GetAsync`
   - Refactor `IMugenTournamentRepository.FindMatchAsync`
   - Systematic replacement across Infrastructure layer

3. **Phase 6: Warning Reduction**
   - Address 400+ compilation warnings (primarily CS8618/CS1998).

---

### LESSONS_LEARNED.md

**Role**: Historical justification.
**Priority**: Informational.

**Used when:**

- Understanding *why* rules exist
- Onboarding contributors
- Avoiding repeated mistakes
- Learning from past decisions
- Evaluating architectural choices

**Contains:**

- Architecture lessons (Clean Architecture, CQRS, Value Objects)
- Code quality lessons (Result Pattern, async safety, logging)
- Infrastructure lessons (IHttpClientFactory, configuration validation)
- Testing lessons (test infrastructure, mocking limits)
- Performance lessons (N+1 queries, pagination)

---

### ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md

**Role**: Execution roadmap.
**Priority**: Contextual and time-bound.

**Used when:**

- Implementing new features
- Planning milestones
- Estimating effort
- Understanding current development status

**Contains:**

- Implementation progress for advanced gaming features
- Phase-by-phase implementation details
- Plugin system architecture

---

### FEATURE_SURFACING_PLAN.md ⭐ NEW

**Role**: UI Implementation Plan - Surface all backend services.
**Priority**: **HIGHEST for UI development**.

**Used when:**

- **Building new UI views (START HERE)**
- Implementing widgets and components
- Understanding where services should be exposed
- Planning Big Picture Mode features

**Contains:**

- 118 views across 7 primary tabs
- 20 dashboard widget specifications
- Service-to-view mappings for 90+ services
- 16-week implementation timeline
- Detailed specifications in 9 sub-documents:
  - [01_SHELL_AND_NAVIGATION.md](planning/surfacing/01_SHELL_AND_NAVIGATION.md)
  - [02_DASHBOARD_HUB.md](planning/surfacing/02_DASHBOARD_HUB.md)
  - [03_LIBRARY_TAB.md](planning/surfacing/03_LIBRARY_TAB.md)
  - [04_MUGEN_TAB.md](planning/surfacing/04_MUGEN_TAB.md)
  - [05_ANALYTICS_SOCIAL.md](planning/surfacing/05_ANALYTICS_SOCIAL.md)
  - [06_TOOLS_TAB.md](planning/surfacing/06_TOOLS_TAB.md)
  - [07_TERMINAL_SETTINGS.md](planning/surfacing/07_TERMINAL_SETTINGS.md)
  - [08_OVERLAYS_BIGPICTURE.md](planning/surfacing/08_OVERLAYS_BIGPICTURE.md)
  - [09_IMPLEMENTATION_TIMELINE.md](planning/surfacing/09_IMPLEMENTATION_TIMELINE.md)

---

## 🔍 Quick Find Guide

| Question | Go To | Section |
|:---------|:------|:--------|
| **Why won't it build?** | TECHNICAL_DEBT_REMEDIATION_PLAN.md | **Phase 0** |
| **How do I build a new view?** | FEATURE_SURFACING_PLAN.md | Relevant tab section |
| How do I write code? | AI_MASTER_CONTEXT.md | "Coding Standards" |
| What are the metrics? | AI_MASTER_CONTEXT.md | "Codebase Metrics" |
| Why does that rule exist? | planning/LESSONS_LEARNED.md | Use Ctrl+F |
| What are the non-negotiables? | ENGINEERING_RULES.md | Top section |
| What's being built next? | planning/FEATURE_SURFACING_PLAN.md | "Implementation Phases" |
| Where should this service appear? | planning/FEATURE_SURFACING_PLAN.md | Service mappings |
| What widgets exist? | planning/surfacing/02_DASHBOARD_HUB.md | Widget catalog |
| How does Big Picture work? | planning/surfacing/08_OVERLAYS_BIGPICTURE.md | Controller mapping |
| What technical debt exists? | TECHNICAL_DEBT_REMEDIATION_PLAN.md | "Phase 1-4" |

---

## 📋 Document Maintenance

| Document | Last Updated | Confidence | Notes |
|:---------|:-------------|:-----------|:------|
| AI_PROJECT_INDEX.md | Jan 1, 2026 | 100% | Feature Surfacing added |
| AI_MASTER_CONTEXT.md | Jan 1, 2026 | 100% | Feature Surfacing added |
| ENGINEERING_RULES.md | Dec 31, 2025 | 95% | Rules still accurate |
| **FEATURE_SURFACING_PLAN.md** | Jan 1, 2026 | 100% | ⭐ 118 views, 9 sub-docs |
| TECHNICAL_DEBT_REMEDIATION_PLAN.md | Jan 1, 2026 | 100% | All phases complete |
| planning/LESSONS_LEARNED.md | Jan 1, 2026 | 100% | Updated with UI lessons |
| planning/ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md | Dec 31, 2025 | 85% | Backend complete |
| status/DEVELOPMENT_STATUS.md | Dec 31, 2025 | 90% | Production release |
| planning/V2_FEATURE_ROADMAP.md | Dec 30, 2025 | 80% | Features accurate |

**Confidence**: How well this document reflects current code (verified via codebase scan).

---

## ⚖️ Conflict Resolution Matrix

### Precedence Rules

1. **Architecture Rules** (ENGINEERING_RULES.md) - Non-negotiable
2. **Context & Patterns** (AI_MASTER_CONTEXT.md) - Standard practice
3. **Technical Debt** (TECHNICAL_DEBT_REMEDIATION_PLAN.md) - Current issues
4. **Decision History** (LESSONS_LEARNED.md) - Learn why
5. **Implementation Details** (ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md) - Tactical

### Document Status Conflicts

**Situation**: DEVELOPMENT_STATUS.md claims "0 errors" but build fails

- **Resolution**: AI_MASTER_CONTEXT.md and TECHNICAL_DEBT_REMEDIATION_PLAN.md are authoritative
- **Reason**: January 1, 2026 comprehensive audit supersedes December 31 status

| Situation | Primary Source | Secondary Check |
|-----------|----------------|-----------------|
| **Build won't compile** | TECHNICAL_DEBT_REMEDIATION_PLAN.md | AI_MASTER_CONTEXT.md |
| **Writing new code** | AI_MASTER_CONTEXT.md | ENGINEERING_RULES.md |
| **Architecture decisions** | ENGINEERING_RULES.md | AI_MASTER_CONTEXT.md |
| **Understanding why** | LESSONS_LEARNED.md | Any other |
| **Code review** | AI_MASTER_CONTEXT.md | ENGINEERING_RULES.md |

---

## 🔄 Reading Order by Task Type

### 🚨 **Fixing the Build (START HERE)**

1. **TECHNICAL_DEBT_REMEDIATION_PLAN.md** - Phase 0 has exact fixes
2. **AI_MASTER_CONTEXT.md** - Understand what's broken
3. Verify fix with `dotnet build`

### 🚀 **Starting a New Feature**

1. **TECHNICAL_DEBT_REMEDIATION_PLAN.md** - Check if build works first
2. **AI_MASTER_CONTEXT.md** - Learn current patterns
3. **ENGINEERING_RULES.md** - Understand constraints
4. **planning/LESSONS_LEARNED.md** - Understand why

### 🔧 **Refactoring Existing Code**

1. **TECHNICAL_DEBT_REMEDIATION_PLAN.md** - Check for related debt
2. **AI_MASTER_CONTEXT.md** - Review current patterns
3. **ENGINEERING_RULES.md** - Ensure compliance

### 🎨 **Building New UI Views** ⭐ NEW

1. **FEATURE_SURFACING_PLAN.md** - Find the view specification
2. **Relevant surfacing/*.md** - Get detailed wireframes & ViewModel
3. **AI_MASTER_CONTEXT.md** - Review MVVM patterns
4. **ENGINEERING_RULES.md** - Ensure architectural compliance

### 🐛 **Debugging Issues**

1. **planning/LESSONS_LEARNED.md** - Check for similar past issues
2. **AI_MASTER_CONTEXT.md** - Understand expected behavior
3. **TECHNICAL_DEBT_REMEDIATION_PLAN.md** - Check if it's known debt

### 📋 **Code Review**

1. **AI_MASTER_CONTEXT.md** - Verify adherence to standards
2. **ENGINEERING_RULES.md** - Check architectural compliance
3. **FEATURE_SURFACING_PLAN.md** - Verify view matches specification

---

## 🔗 Cross-Reference Map

### Compilation Issues

- **AI_MASTER_CONTEXT.md**: Current status - "11 compilation errors"
- **TECHNICAL_DEBT_REMEDIATION_PLAN.md**: Phase 0 - Exact files and fixes
- **DEVELOPMENT_STATUS.md**: ⚠️ OUTDATED - Claims 0 errors

### Result Pattern

- **AI_MASTER_CONTEXT.md**: Rule - "MANDATORY. Never return null"
- **ENGINEERING_RULES.md**: Law - "Must return Result<T>"
- **TECHNICAL_DEBT_REMEDIATION_PLAN.md**: 50+ violations listed
- **planning/LESSONS_LEARNED.md**: Why - Historical context

### Async Safety

- **AI_MASTER_CONTEXT.md**: Stats - ".Result: 10, .Wait(): 4"
- **ENGINEERING_RULES.md**: Law - "Must Not use async void"
- **TECHNICAL_DEBT_REMEDIATION_PLAN.md**: Phase 1 - Specific instances

### CLI Architecture

- **AI_MASTER_CONTEXT.md**: Status - "3/12 command groups complete"
- **TECHNICAL_DEBT_REMEDIATION_PLAN.md**: Phase 3 - Implementation plan

---

## 📚 AI Ingestion Order

### 🤖 **For AI Models (Recommended Reading Sequence)**

```
1. AI_PROJECT_INDEX.md (This file) - 3 minutes
   → Understand document structure, current blockers

2. TECHNICAL_DEBT_REMEDIATION_PLAN.md - 10 minutes
   → CRITICAL: Understand compilation issues first

3. AI_MASTER_CONTEXT.md - 15 minutes
   → Architecture, patterns, current metrics

4. ENGINEERING_RULES.md - 10 minutes
   → Non-negotiable constraints

5. planning/LESSONS_LEARNED.md - 30 minutes (as needed)
   → Historical context
```

### 👥 **For Human Contributors**

```
1. AI_PROJECT_INDEX.md (This file) - 2 minutes
2. TECHNICAL_DEBT_REMEDIATION_PLAN.md - 5 minutes (if build fails)
3. AI_MASTER_CONTEXT.md - 10 minutes
4. ENGINEERING_RULES.md - 10 minutes
5. Begin development
```

---

## 📊 Codebase Quick Stats

| Category | Value |
|----------|-------|
| **Build Status** | ✅ PASSING (0 errors) |
| **Source Projects** | 25 (6 main + 19 plugins) |
| **Test Projects** | 13 |
| **Source Files** | 763 C# files |
| **Test Files** | 148 C# files |
| **Source LOC** | 58,571 lines |
| **Test LOC** | 11,056 lines |
| **Test Methods** | 529 |
| **Warnings** | **1** (99.8% reduction) ✅ |
| **Health Score** | **95/100** ✅ |

### File Distribution

| Project | Files |
|---------|-------|
| Core | 262 |
| Application | 221 |
| Infrastructure | 180 |
| Presentation | 38 |
| CLI | 10 |
| Plugins | 52 |

### Technical Debt Summary

| Priority | Count |
|----------|-------|
| ✅ Compilation Errors | 0 (Fixed!) |
| ✅ Sync-over-async | 0 (Fixed!) |
| ✅ return null | 0 (Fixed!) |
| ✅ TODO comments | 0 (Fixed!) |
| ✅ CLI Groups | 12/12 (Complete) |
| ✅ Warnings | 1 (99.8% reduction) |

 ---

## 🎯 Immediate Action Items

### ✅ Completed (January 1, 2026)

 **Phase 0-1:**
 1. ~~**DELETE** Duplicate Classes~~ ✅ Done
 2. ~~**FIX** Sync-over-async violations~~ ✅ Done
 3. ~~**VERIFY** build~~ ✅ Build passing

 **Phase 3:**
 4. ~~**IMPLEMENT** 12/12 CLI Command Groups~~ ✅ Done
 5. ~~**RESOLVE** 29 remaining TODOs~~ ✅ Done

 **Phase 4:**
 6. ~~**IMPLEMENT** MUGEN Persistence (Repositories)~~ ✅ Done

 **UI Phase 1 (Shell & Navigation):**
 7. ~~**IMPLEMENT** MainShell with 7-tab navigation~~ ✅ Done
 8. ~~**IMPLEMENT** Command palette, AI assistant, performance HUD~~ ✅ Done
 9. ~~**IMPLEMENT** Global shortcuts (Ctrl+1-7, Ctrl+Shift+P)~~ ✅ Done

 **UI Phase 2 (Dashboard Hub):**
 10. ~~**IMPLEMENT** Complete widget system (IWidget, WidgetBase, WidgetSize)~~ ✅ Done
 11. ~~**IMPLEMENT** 5 dashboard widgets with real-time data~~ ✅ Done
 12. ~~**SURFACE** 5 backend services via UI widgets~~ ✅ Done

  **Phase 5:**
  7. ~~**REFACTOR** MUGEN Repositories to Result<T>~~ ✅ Done (Character, Tournament, MatchHistory)
  8. ~~**REFACTOR** RetroAchievementsClient to Result<T>~~ ✅ Done
  9. ~~**REFACTOR** GameMemoryReader to Result<T>~~ ✅ Done
  10. ~~**REFACTOR** NetworkQualityMonitor to Result<T>~~ ✅ Done
  11. ~~**REFACTOR** VoiceCommandService to Result<T>~~ ✅ Done

### ✅ All Technical Debt Phases Complete (January 1, 2026)

### 🎨 UI Development Phases Complete (January 1, 2026)

  1. **Phase 6: Warning Reduction ✅ COMPLETE**
     - ~~Address CS8618 (Non-nullable property uninitialized)~~ ✅ Done (0 remaining)
     - ~~Address CS1998 (Async method lacks await)~~ ✅ Done (0 remaining in src)
     - ~~Address Missing XML Comments (CS1591)~~ ✅ Done (100% documented)

 **Phase 7: Project Completion ✅ COMPLETE**
     - ~~Fix 14 failing tests~~ ✅ Done (14/14 resolved - all tests now pass)
     - ~~Complete XML documentation~~ ✅ Done (Infrastructure layer: 0 warnings)
     - ~~Enhance test coverage~~ ✅ Done (100% pass rate achieved)
     - ~~Update status documents~~ ✅ Done (All documents updated)

  **UI Development Result**: Phase 1 & 2 complete - Complete shell architecture with 7 tabs, 5 dashboard widgets surfacing 5 backend services. Widget system with auto-refresh, error handling, and responsive design. Ready for Phase 3 (Library Enhancement).

---

*This index ensures no document is read in isolation. Each document reinforces the others, creating a cohesive knowledge system that scales with the project's complexity.*

**Last Audit**: January 1, 2026 - UI Phase 1 & 2 Complete
**Next Review**: February 1, 2026 - Phase 3 Planning
