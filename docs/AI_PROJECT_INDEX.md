# 🤖 SaveState Reborn — Project Knowledge Index

**Status**: ✅ Production Release v1.0.0 + Active Development
**Last Updated**: January 2, 2026 (Technical Debt Audit Complete)
**Maintained By**: Development Team
**Next Review**: January 15, 2026 (Bi-weekly review for ENGINEERING_RULES)
**Related Documents**: [All docs in this directory](./)

---

## 📋 Documentation Freshness Check

| Document | Last Updated | Review Due | Status |
|----------|--------------|-----------|--------|
| [AI_QUICK_START.md](AI_QUICK_START.md) | Jan 2 | Weekly | ✅ NEW |
| [ACTIVE_ISSUES.md](ACTIVE_ISSUES.md) | Jan 2 | Daily | ✅ NEW |
| [PATTERNS_COOKBOOK.md](PATTERNS_COOKBOOK.md) | Jan 2 | Monthly | ✅ NEW |
| [DECISIONS_LOG.md](DECISIONS_LOG.md) | Jan 2 | Monthly | ✅ NEW |
| [PROJECT_METRICS.md](PROJECT_METRICS.md) | Jan 2 | Jan 9 | ✅ Current |
| [AI_MASTER_CONTEXT.md](AI_MASTER_CONTEXT.md) | Jan 2 | Feb 1 | ✅ Current |
| [ENGINEERING_RULES.md](ENGINEERING_RULES.md) | Jan 2 | Jan 15 | ⚠️ DUE SOON |
| [DEVELOPMENT_STATUS.md](status/DEVELOPMENT_STATUS.md) | Jan 2 | Jan 16 | ✅ Current |
| [GLOSSARY.md](GLOSSARY.md) | Jan 2 | Feb 2 | ✅ Current |

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
> **January 2, 2026 Update**: Comprehensive Technical Debt Audit completed. Health Score: 91/100. AI Knowledge Base with web search integration now active. Phase 1, 2, and 4 of UI development complete. See [TECHNICAL_DEBT_AUDIT_2026-01-02.md](reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md) for full findings.

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
- Current implementation status (Phase 1, 2, 4 complete)
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
| **What are the core metrics?** | [PROJECT_METRICS.md](PROJECT_METRICS.md) | Full Metrics |
| **How do I build a new view?** | [FEATURE_SURFACING_PLAN.md](planning/FEATURE_SURFACING_PLAN.md) | Relevant tab section |
| How do I write code? | [AI_MASTER_CONTEXT.md](AI_MASTER_CONTEXT.md) | "Coding Standards" |
| Why does that rule exist? | [planning/LESSONS_LEARNED.md](planning/LESSONS_LEARNED.md) | Use Ctrl+F |
| What are the non-negotiables? | [ENGINEERING_RULES.md](ENGINEERING_RULES.md) | Top section |
| What's being built next? | [planning/FEATURE_SURFACING_PLAN.md](planning/FEATURE_SURFACING_PLAN.md) | "Implementation Phases" |
| What technical debt exists? | [TECHNICAL_DEBT_AUDIT_2026-01-02.md](reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md) | Full document |
| What was already fixed? | [TECHNICAL_DEBT_REMEDIATION_PLAN.md](reports/TECHNICAL_DEBT_REMEDIATION_PLAN.md) | Phases 0-7 |
| What terms mean what? | [GLOSSARY.md](GLOSSARY.md) | Term lookup |

---

## 📋 Document Maintenance

| Document | Last Updated | Confidence | Notes |
|:---------|:------|:--------|:------|
| [PROJECT_METRICS.md](PROJECT_METRICS.md) | Jan 2, 2026 | 100% | Single source of truth |
| [TECHNICAL_DEBT_AUDIT_2026-01-02.md](reports/TECHNICAL_DEBT_AUDIT_2026-01-02.md) | Jan 2, 2026 | 100% | ⭐ Latest scan |
| [AI_MASTER_CONTEXT.md](AI_MASTER_CONTEXT.md) | Jan 2, 2026 | 100% | AI KB integration added |
| [AI_PROJECT_INDEX.md](AI_PROJECT_INDEX.md) | Jan 2, 2026 | 100% | Reorganized for new audit |
| [ENGINEERING_RULES.md](ENGINEERING_RULES.md) | Jan 2, 2026 | 100% | Updated compliance status |
| [GLOSSARY.md](GLOSSARY.md) | Jan 2, 2026 | 100% | Expanded with AI terms |
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
| **What's currently broken** | TECHNICAL_DEBT_AUDIT_2026-01-02.md | AI_MASTER_CONTEXT.md |
| **Writing new code** | AI_MASTER_CONTEXT.md | ENGINEERING_RULES.md |
| **Architecture decisions** | ENGINEERING_RULES.md | AI_MASTER_CONTEXT.md |
| **Understanding why** | LESSONS_LEARNED.md | Any other |
| **Code review** | AI_MASTER_CONTEXT.md | ENGINEERING_RULES.md |

---

## 🔄 Reading Order by Task Type

### 🚨 **Fixing Technical Debt (START HERE)**

1. **TECHNICAL_DEBT_AUDIT_2026-01-02.md** - Current issues
2. **AI_MASTER_CONTEXT.md** - Understand patterns
3. Verify fix with `dotnet build`

### 🚀 **Starting a New Feature**

1. **TECHNICAL_DEBT_AUDIT_2026-01-02.md** - Check if build works first
2. **AI_MASTER_CONTEXT.md** - Learn current patterns
3. **ENGINEERING_RULES.md** - Understand constraints
4. **planning/LESSONS_LEARNED.md** - Understand why

### 🎨 **Building New UI Views**

1. **FEATURE_SURFACING_PLAN.md** - Find the view specification
2. **Relevant surfacing/*.md** - Get detailed wireframes & ViewModel
3. **AI_MASTER_CONTEXT.md** - Review MVVM patterns
4. **ENGINEERING_RULES.md** - Ensure architectural compliance

### 🤖 **Working with AI System**

1. **AI_MASTER_CONTEXT.md** - AI Architecture section
2. **AiOrchestrator.cs** - Core coordination logic
3. **WebSearchService.cs** - Internet fallback
4. **MarkdownKnowledgeBaseService.cs** - Knowledge storage

### 📋 **Code Review**

1. **AI_MASTER_CONTEXT.md** - Verify adherence to standards
2. **ENGINEERING_RULES.md** - Check architectural compliance
3. **TECHNICAL_DEBT_AUDIT_2026-01-02.md** - Ensure no new debt added

---

## 🔗 Cross-Reference Map

### Current Issues

- **TECHNICAL_DEBT_AUDIT_2026-01-02.md**: Latest scan results
- **AI_MASTER_CONTEXT.md**: Summary in "Current Technical Debt" section

### Result Pattern

- **AI_MASTER_CONTEXT.md**: Rule - "MANDATORY. Never return null"
- **ENGINEERING_RULES.md**: Law - "Must return Result<T>"
- **TECHNICAL_DEBT_AUDIT_2026-01-02.md**: 45+ violations listed

### Async Safety

- **AI_MASTER_CONTEXT.md**: Current violations count
- **ENGINEERING_RULES.md**: Law - "Must Not use async void"
- **TECHNICAL_DEBT_AUDIT_2026-01-02.md**: Specific instances

### AI Knowledge Base

- **AI_MASTER_CONTEXT.md**: AI Architecture section
- **GLOSSARY.md**: RAG, BMAD definitions

---

## 📚 AI Ingestion Order

### 🤖 **For AI Models (Recommended Reading Sequence)**

```
1. AI_QUICK_START.md - 30 seconds ⭐ START HERE
   → Build commands, current status, critical issues

2. ACTIVE_ISSUES.md - 2 minutes
   → What's broken right now, fix templates

3. PATTERNS_COOKBOOK.md - 5 minutes (reference)
   → Copy-paste code patterns

4. AI_MASTER_CONTEXT.md - 10 minutes
   → Architecture, full context

5. DECISIONS_LOG.md - 5 minutes (as needed)
   → Why we made key choices

6. ENGINEERING_RULES.md - 5 minutes
   → Non-negotiable constraints
```

### 👥 **For Human Contributors**

```
1. AI_QUICK_START.md - 30 seconds
2. ACTIVE_ISSUES.md - 2 minutes
3. AI_MASTER_CONTEXT.md - 10 minutes
4. ENGINEERING_RULES.md - 5 minutes
5. Begin development
```

---

## 📊 Codebase Quick Stats

| Category | Value |
|----------|-------|
| **Build Status** | ✅ PASSING (0 errors) |
| **Source Projects** | 25 (6 main + 19 plugins) |
| **Test Projects** | 13 |
| **Source Files** | 763+ C# files |
| **Test Files** | 148 C# files |
| **Source LOC** | 58,571+ lines |
| **Test LOC** | 11,056 lines |
| **Test Methods** | 529 |
| **Warnings** | ~1,220 (CS1591 docs) |
| **Health Score** | **91/100** |

### Technical Debt Summary (Jan 2, 2026)

| Priority | Issue | Count |
|----------|-------|-------|
| 🔴 Critical | `.Result` sync-over-async | 3 |
| 🟠 High | `async void` methods | 3 |
| 🟠 High | Silent exception handlers | 4 |
| 🟡 Medium | TODO comments | 68+ |
| 🟡 Medium | `return null` | 45+ |
| 🟡 Medium | Manual HttpClient | 2 |
| ✅ Resolved | Compilation errors | 0 |
| ✅ Resolved | CLI command groups | 12/12 |

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

**Last Audit**: January 2, 2026 - Technical Debt Audit Complete
**Next Review**: February 1, 2026
