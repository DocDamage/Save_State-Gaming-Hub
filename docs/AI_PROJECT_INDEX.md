# 🤖 SaveState Reborn — Project Knowledge Index

**Status**: ✅ Production Release v2.3.9 + Active Development
**Last Updated**: January 8, 2026 (Dialog System Complete - All Placeholders Eliminated)
**Maintained By**: Development Team
**Next Review**: January 15, 2026 (Bi-weekly review for ENGINEERING_RULES)
**Related Documents**: [All docs in this directory](./)

---

## 📋 Documentation Freshness Check

| Document | Last Updated | Review Due | Status |
|----------|--------------|-----------|--------|
| [AI_QUICK_START.md](guides/AI_QUICK_START.md) | Jan 2 | Weekly | ✅ NEW |
| [ACTIVE_ISSUES.md](planning/ACTIVE_ISSUES.md) | Jan 2 | Daily | ✅ NEW |
| [PATTERNS_COOKBOOK.md](architecture/PATTERNS_COOKBOOK.md) | Jan 2 | Monthly | ✅ NEW |
| [DECISIONS_LOG.md](architecture/DECISIONS_LOG.md) | Jan 8 | Monthly | ✅ Updated |
| [PROJECT_METRICS.md](reports/PROJECT_METRICS.md) | Jan 3 | Jan 10 | ✅ Current |
| [AI_MASTER_CONTEXT.md](ai/AI_MASTER_CONTEXT.md) | Jan 8 | Feb 1 | ✅ Updated |
| [ENGINEERING_RULES.md](architecture/ENGINEERING_RULES.md) | Jan 8 | Jan 15 | ✅ Updated |
| [DEVELOPMENT_STATUS.md](status/DEVELOPMENT_STATUS.md) | Jan 3 | Jan 17 | ✅ Current |
| [GLOSSARY.md](resources/GLOSSARY.md) | Jan 3 | Feb 3 | ✅ Updated |
| [LESSONS_LEARNED.md](planning/LESSONS_LEARNED.md) | Jan 8 | Feb 8 | ✅ Updated |
| [WHATS_LEFT_TO_CODE.md](planning/WHATS_LEFT_TO_CODE.md) | Jan 8 | Feb 8 | ✅ Updated |
| [Character Development Integration Plan](planning/character_development_integration_plan.md) | Jan 3 | - | ✅ NEW |
| [Ikemen Repository Analysis](planning/ikemen_repositories_analysis.md) | Jan 2 | - | ✅ Current |
| [MUGEN Repository Evaluation](planning/mugen_repositories_evaluation.md) | Jan 3 | - | ✅ Current |
| [MUGEN Character Repositories Analysis](planning/mugen_character_repositories_analysis.md) | Jan 3 | - | ✅ Current |
| [Visual Resources Integration Plan](planning/visual_resources_integration_plan.md) | Jan 2 | - | ✅ Current |

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
> **January 8, 2026 Update**: Dialog System Complete v2.3.9. Eliminated all placeholder implementations in `IDialogService`. Created `TextInputDialog`, `BranchCreationDialog`, `BranchMergeDialog`, `SaveStateSettingsDialog` with full ViewModels, Views, and code-behinds. Build errors reduced from 15 → **0**. Warnings reduced from 995 → **117** (88% reduction). Health Score: **98/100**.

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
- **Current codebase metrics (763+ files, 58K+ LOC)**
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

### TECHNICAL_DEBT_AUDIT_2026-01-02.md ⭐ NEW

**Role**: Latest comprehensive debt scan.
**Priority**: **HIGHEST when assessing current issues**.

**Used when:**

- **Understanding what needs fixing now (START HERE)**
- Prioritizing remediation work
- Understanding current health score

**Contains:**

- Health score breakdown (91/100)
- Critical issues (3 `.Result` in JwtTokenService)
- High priority issues (3 `async void`, 4 silent catches)
- Medium priority issues (68+ TODOs, 45+ `return null`)
- Prioritized remediation plan

---

### TECHNICAL_DEBT_REMEDIATION_PLAN.md

**Role**: Historical remediation tracking.
**Priority**: Reference for completed work.

**Used when:**

- Understanding what debt was already fixed
- Reviewing Phase 0-7 remediation history
- Tracking completion status

**Contains:**

- Completed Phase 0-7 remediation details
- Historical violation counts
- Resolution approaches

---

### [FEATURE_SURFACING_PLAN.md](planning/FEATURE_SURFACING_PLAN.md)

**Role**: UI Implementation Plan - Surface all backend services.
**Priority**: **HIGHEST for UI development**.

**Used when:**

- **Building new UI views (START HERE)**
- Implementing widgets and components
- Understanding where services should be exposed
- Planning Big Picture Mode features

**Contains:**

- 118 views across 7 primary tabs planned
- 20 dashboard widget specifications
- Current implementation status (Phase 1-6 complete)
- 16-week implementation timeline reference

---

### GLOSSARY.md

**Role**: Domain terminology reference.
**Priority**: Reference for new contributors.

**Used when:**

- Onboarding new team members
- Understanding domain-specific terms
- Clarifying architectural concepts

**Contains:**

- Domain terms and definitions
- Architectural pattern explanations
- Cross-reference to usage locations

---

## 🔍 Quick Find Guide

| Question | Go To | Section |
|:---------|:------|:--------|
| **What's broken right now?** | [TECHNICAL_DEBT_AUDIT_2026-01-02.md](reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md) | Critical Issues |
| **What are the core metrics?** | [PROJECT_METRICS.md](reports/PROJECT_METRICS.md) | Full Metrics |
| **How do I build a new view?** | [FEATURE_SURFACING_PLAN.md](planning/FEATURE_SURFACING_PLAN.md) | Relevant tab section |
| How do I write code? | [ai/AI_MASTER_CONTEXT.md](ai/AI_MASTER_CONTEXT.md) | "Coding Standards" |
| Why does that rule exist? | [planning/LESSONS_LEARNED.md](planning/LESSONS_LEARNED.md) | Use Ctrl+F |
| What are the non-negotiables? | [ENGINEERING_RULES.md](architecture/ENGINEERING_RULES.md) | Top section |
| What's being built next? | [planning/FEATURE_SURFACING_PLAN.md](planning/FEATURE_SURFACING_PLAN.md) | "Implementation Phases" |
| What technical debt exists? | [TECHNICAL_DEBT_AUDIT_2026-01-02.md](reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md) | Full document |
| What was already fixed? | [TECHNICAL_DEBT_REMEDIATION_PLAN.md](reports/TECHNICAL_DEBT_REMEDIATION_PLAN.md) | Phases 0-7 |
| What terms mean what? | [GLOSSARY.md](resources/GLOSSARY.md) | Term lookup |
| How do I integrate character development tools? | [Character Development Integration Plan](planning/character_development_integration_plan.md) | Full 12-week plan |
| What character development repositories are available? | [Ikemen Repository Analysis](planning/ikemen_repositories_analysis.md) | Repository evaluation |

---

## 📋 Document Maintenance

| Document | Last Updated | Confidence | Notes |
|:---------|:------|:--------|:------|
| [PROJECT_METRICS.md](reports/PROJECT_METRICS.md) | Jan 2, 2026 | 100% | Single source of truth |
| [TECHNICAL_DEBT_AUDIT_2026-01-02.md](reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md) | Jan 2, 2026 | 100% | ⭐ Latest scan |
| [AI_MASTER_CONTEXT.md](ai/AI_MASTER_CONTEXT.md) | Jan 3, 2026 | 100% | Character development integration added |
| [AI_PROJECT_INDEX.md](AI_PROJECT_INDEX.md) | Jan 3, 2026 | 100% | Character development docs added |
| [ENGINEERING_RULES.md](architecture/ENGINEERING_RULES.md) | Jan 3, 2026 | 100% | Updated for character development |
| [GLOSSARY.md](resources/GLOSSARY.md) | Jan 3, 2026 | 100% | Character development terms added |
| [FEATURE_SURFACING_PLAN.md](planning/FEATURE_SURFACING_PLAN.md) | Jan 2, 2026 | 100% | 118 views planned |
| [TECHNICAL_DEBT_REMEDIATION_PLAN.md](reports/TECHNICAL_DEBT_REMEDIATION_PLAN.md) | Jan 2, 2026 | 95% | Historical reference |
| [planning/LESSONS_LEARNED.md](planning/LESSONS_LEARNED.md) | Jan 1, 2026 | 100% | Updated with UI lessons |

**Confidence**: How well this document reflects current code (verified via codebase scan).

---

## ⚖️ Conflict Resolution Matrix

### Precedence Rules

1. **Latest Audit** (TECHNICAL_DEBT_AUDIT_2026-01-02.md) - Current state of code
2. **Architecture Rules** (ENGINEERING_RULES.md) - Non-negotiable
3. **Context & Patterns** (AI_MASTER_CONTEXT.md) - Standard practice
4. **Historical Debt** (TECHNICAL_DEBT_REMEDIATION_PLAN.md) - What was fixed
5. **Decision History** (LESSONS_LEARNED.md) - Learn why

### Document Status Conflicts

| Situation | Primary Source | Secondary Check |
|-----------|----------------|-----------------|
| **What's currently broken** | reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md | ai/AI_MASTER_CONTEXT.md |
| **Writing new code** | ai/AI_MASTER_CONTEXT.md | architecture/ENGINEERING_RULES.md |
| **Architecture decisions** | architecture/ENGINEERING_RULES.md | ai/AI_MASTER_CONTEXT.md |
| **Understanding why** | architecture/DECISIONS_LOG.md | Any other |
| **Code review** | ai/AI_MASTER_CONTEXT.md | architecture/ENGINEERING_RULES.md |

---

## 🔄 Reading Order by Task Type

### 🚨 **Fixing Technical Debt (START HERE)**

1. **reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md** - Current issues
2. **ai/AI_MASTER_CONTEXT.md** - Understand patterns
3. Verify fix with `dotnet build`

### 🚀 **Starting a New Feature**

1. **reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md** - Check if build works first
2. **ai/AI_MASTER_CONTEXT.md** - Learn current patterns
3. **architecture/ENGINEERING_RULES.md** - Understand constraints
4. **planning/LESSONS_LEARNED.md** - Understand why

### 🎨 **Building New UI Views**

1. **planning/FEATURE_SURFACING_PLAN.md** - Find the view specification
2. **Relevant surfacing/*.md** - Get detailed wireframes & ViewModel
3. **ai/AI_MASTER_CONTEXT.md** - Review MVVM patterns
4. **architecture/ENGINEERING_RULES.md** - Ensure architectural compliance

### 🤖 **Working with AI System**

1. **ai/AI_MASTER_CONTEXT.md** - AI Architecture section
2. **src/SaveState.Application/AI/Services/AiOrchestrator.cs** - Core coordination logic
3. **src/SaveState.Infrastructure/AI/Services/WebSearchService.cs** - Internet fallback
4. **src/SaveState.Infrastructure/AI/Services/MarkdownKnowledgeBaseService.cs** - Knowledge storage

### 🎮 **Character Development & Modification**

1. **[Character Development Integration Plan](planning/character_development_integration_plan.md)** - Complete implementation roadmap
2. **[Ikemen Repository Analysis](planning/ikemen_repositories_analysis.md)** - Repository evaluation and integration strategies
3. **[MUGEN Repository Evaluation](planning/mugen_repositories_evaluation.md)** - Evaluation framework for character repositories
4. **ai/AI_MASTER_CONTEXT.md** - Character development tools section

### 📋 **Code Review**

1. **ai/AI_MASTER_CONTEXT.md** - Verify adherence to standards
2. **architecture/ENGINEERING_RULES.md** - Check architectural compliance
3. **reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md** - Ensure no new debt added

---

## 🔗 Cross-Reference Map

### Current Issues

- **reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md**: Latest scan results
- **ai/AI_MASTER_CONTEXT.md**: Summary in "Current Technical Debt" section

### Result Pattern

- **ai/AI_MASTER_CONTEXT.md**: Rule - "MANDATORY. Never return null"
- **architecture/ENGINEERING_RULES.md**: Law - "Must return Result<T>"
- **reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md**: 45+ violations listed

### Async Safety

- **ai/AI_MASTER_CONTEXT.md**: Current violations count
- **architecture/ENGINEERING_RULES.md**: Law - "Must Not use async void"
- **reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md**: Specific instances

### AI Knowledge Base

- **ai/AI_MASTER_CONTEXT.md**: AI Architecture section
- **resources/GLOSSARY.md**: RAG, BMAD definitions

---

## 📚 AI Ingestion Order

### 🤖 **For AI Models (Recommended Reading Sequence)**

```
1. **guides/AI_QUICK_START.md** - 30 seconds ⭐ START HERE
   → Build commands, current status, critical issues

2. **planning/ACTIVE_ISSUES.md** - 2 minutes
   → What's broken right now, fix templates

3. **architecture/PATTERNS_COOKBOOK.md** - 5 minutes (reference)
   → Copy-paste code patterns

4. **ai/AI_MASTER_CONTEXT.md** - 10 minutes
   → Architecture, full context

5. **architecture/DECISIONS_LOG.md** - 5 minutes (as needed)
   → Why we made key choices

6. **architecture/ENGINEERING_RULES.md** - 5 minutes
   → Non-negotiable constraints
```

### 👥 **For Human Contributors**

```
1. **guides/AI_QUICK_START.md** - 30 seconds
2. **planning/ACTIVE_ISSUES.md** - 2 minutes
3. **ai/AI_MASTER_CONTEXT.md** - 10 minutes
4. **architecture/ENGINEERING_RULES.md** - 5 minutes
5. Begin development
```

---

## 📊 Codebase Quick Stats

| Category | Value |
|----------|-------|
| **Build Status** | ✅ **PASSING (0 errors, 117 warnings)** |
| **Source Projects** | 25 (6 main + 19 plugins) |
| **Test Projects** | 13 |
| **Source Files** | 763+ C# files |
| **Test Files** | 148 C# files |
| **Source LOC** | 58,571+ lines |
| **Test LOC** | 11,056 lines |
| **Test Methods** | 300+ |
| **Warnings** | **117** (down from 4,746 - 98% reduction) |
| **Health Score** | **98/100** ✅ |

### Technical Debt Summary (Jan 8, 2026)

| Priority | Issue | Count |
|----------|-------|-------|
| 🔴 Critical | `.Result` sync-over-async | 0 ✅ |
| 🟠 High | `async void` methods | 3 |
| 🟠 High | Silent exception handlers | 2 |
| 🟡 Medium | TODO comments | 110+ |
| 🟡 Medium | `return null` | 30+ |
| 🟡 Medium | Manual HttpClient | 2 |
| ✅ Resolved | Compilation errors | 0 |
| ✅ Resolved | Dialog placeholders | 0 (v2.3.9) |
| ✅ Resolved | CLI command groups | 12/12 (UI Integrated) |

---

## 🎯 Immediate Action Items

### Priority 1: Critical Fixes

1. **Fix JwtTokenService.cs** - 3 `.Result` calls causing sync-over-async
2. **Wrap async void ViewModels** - Add try-catch to prevent crashes

### Priority 2: High Priority

1. **Add logging to silent catches** - 4 files need structured logging
2. **Complete StatusBar bindings** - Wire real-time data

### Priority 3: Documentation

1. **Reduce CS1591 warnings** - Document public APIs

---

*This index ensures no document is read in isolation. Each document reinforces the others, creating a cohesive knowledge system that scales with the project's complexity.*

**Last Audit**: January 8, 2026 - Build Error Hotfix v2.3.1
**Next Review**: February 1, 2026
