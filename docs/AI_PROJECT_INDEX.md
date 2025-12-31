# 🤖 SaveState Reborn — Project Knowledge Index

**Status**: ✅ Active
**Last Updated**: December 31, 2025
**Maintained By**: Development Team
**Next Review**: January 15, 2026
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
- Coding standards & behavioral handbook
- Domain truth & invariants ("sacred" rules)
- Gold standard examples with references
- Current status & roadmap

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
- Process lessons (technical debt tracking, CI/CD reliability)

---

### ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md
**Role**: Execution roadmap.
**Priority**: Contextual and time-bound.

**Used when:**
- Implementing new features
- Planning milestones
- Estimating effort
- Understanding current development status
- Contributing to feature development

**Contains:**
- Implementation progress for advanced gaming features
- Technical debt reports and remediation status
- Build status and error tracking
- Phase-by-phase implementation details

---

## 🔍 Quick Find Guide

**Looking for...**
| Question | Go To | Section |
|:---------|:------|:---------|
| How do I write code? | AI_MASTER_CONTEXT.md | "Coding Standards" |
| Why does that rule exist? | LESSONS_LEARNED.md | Use Ctrl+F |
| What are the non-negotiables? | ENGINEERING_RULES.md | Top section |
| What's being built next? | ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md | "Completed Features" |
| I just broke the build | ENGINEERING_RULES.md | "Architecture Rules" |
| My code works but feels wrong | LESSONS_LEARNED.md | "Anti-Patterns" |

**Why**: Reduces document surfacing time from 5 min to 30 sec.

---

## 📋 Document Maintenance

| Document | Last Updated | Reviewer | Confidence |
|:---------|:-------------|:---------|:-----------|
| AI_PROJECT_INDEX.md | Dec 31, 2025 | Auto | 100% |
| AI_MASTER_CONTEXT.md | Dec 31, 2025 | Dev | 95% |
| ENGINEERING_RULES.md | Dec 31, 2025 | Arch | 100% |
| LESSONS_LEARNED.md | Dec 30, 2025 | Team | 100% |
| ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md | Dec 31, 2025 | PM | 85% |

**Confidence**: How well this document reflects current code.
**Next Review**: January 15, 2026

**Why**: Prevents relying on stale documentation.

---

## ⚖️ Conflict Resolution Matrix (EXPANDED)

### Precedence Rules
1. **Architecture Rules** (ENGINEERING_RULES.md) - Non-negotiable
2. **Context & Patterns** (AI_MASTER_CONTEXT.md) - Standard practice
3. **Decision History** (LESSONS_LEARNED.md) - Learn why
4. **Implementation Details** (ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md) - Tactical

### Example: Conflicting Guidance?
**Situation**: Rule says "pagination always" but feature request says "load all for dashboard"
- **Primary**: ENGINEERING_RULES.md § "Data Access" → Must paginate
- **Secondary**: LESSONS_LEARNED.md § "N+1 Queries" → Why it matters
- **Resolution**: Implement pagination with sensible defaults (e.g., 1000 items)
- **Decision**: Document exception in technical debt register if pagination breaks UX

**Why**: Removes ambiguity when docs appear to conflict.

| Situation | Primary Source | Secondary Check | Rationale |
|-----------|----------------|-----------------|-----------|
| **Writing new code** | AI_MASTER_CONTEXT.md | ENGINEERING_RULES.md | Context first, then constraints |
| **Architecture decisions** | ENGINEERING_RULES.md | AI_MASTER_CONTEXT.md | Rules first, then patterns |
| **Understanding why** | LESSONS_LEARNED.md | Any other | Historical context for current rules |
| **Implementation planning** | ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md | AI_MASTER_CONTEXT.md | Roadmap first, then technical foundation |
| **Code review** | AI_MASTER_CONTEXT.md | ENGINEERING_RULES.md | Standards first, then constraints |
| **Debugging issues** | LESSONS_LEARNED.md | AI_MASTER_CONTEXT.md | Learn from past mistakes |
| **Feature development** | ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md | ENGINEERING_RULES.md | Plan first, then follow rules |

---

## 🔄 Reading Order by Task Type

### 🚀 **Starting a New Feature**
1. **ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md** - Check if feature is planned, understand scope
2. **ENGINEERING_RULES.md** - Understand architectural constraints
3. **AI_MASTER_CONTEXT.md** - Learn coding patterns and standards
4. **LESSONS_LEARNED.md** - Understand why certain patterns exist

### 🔧 **Refactoring Existing Code**
1. **AI_MASTER_CONTEXT.md** - Review current patterns and standards
2. **ENGINEERING_RULES.md** - Ensure compliance with architectural rules
3. **LESSONS_LEARNED.md** - Learn from past refactoring experiences
4. **ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md** - Check for ongoing work

### 🐛 **Debugging Issues**
1. **LESSONS_LEARNED.md** - Check for similar past issues and solutions
2. **AI_MASTER_CONTEXT.md** - Understand expected behavior patterns
3. **ENGINEERING_RULES.md** - Verify compliance with current rules
4. **ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md** - Check for known issues

### 📋 **Code Review**
1. **AI_MASTER_CONTEXT.md** - Verify adherence to coding standards
2. **ENGINEERING_RULES.md** - Check architectural compliance
3. **ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md** - Ensure alignment with current work
4. **LESSONS_LEARNED.md** - Reference past experiences

### 🏗️ **System Design**
1. **ENGINEERING_RULES.md** - Understand architectural constraints
2. **AI_MASTER_CONTEXT.md** - Learn existing patterns and tech stack
3. **LESSONS_LEARNED.md** - Understand why certain designs were chosen
4. **ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md** - Check integration requirements

### 🎓 **Onboarding New Contributors**
1. **AI_MASTER_CONTEXT.md** - Get overview of architecture and standards
2. **ENGINEERING_RULES.md** - Learn the non-negotiable constraints
3. **ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md** - Understand current development status
4. **LESSONS_LEARNED.md** - Learn from the team's experiences

---

## 🔗 Cross-Reference Map

### Result Pattern
- **AI_MASTER_CONTEXT.md**: Rule - "MANDATORY. Never return null or throw business exceptions"
- **ENGINEERING_RULES.md**: Law - "Must return Result<T> or Result for all service and command methods"
- **LESSONS_LEARNED.md**: Why - "Result Pattern > Return Null" section with examples
- **ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md**: Applied - Used throughout all implemented features

### Clean Architecture
- **AI_MASTER_CONTEXT.md**: Rule - "Domain at the center, Infrastructure on the outside"
- **ENGINEERING_RULES.md**: Law - "Strict 4-layer separation: Core → Application → Infrastructure → Presentation"
- **LESSONS_LEARNED.md**: Why - "Clean Architecture Pays Dividends" with performance metrics
- **ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md**: Applied - Maintained throughout all phases

### CQRS Pattern
- **AI_MASTER_CONTEXT.md**: Rule - "Command-Query Responsibility Segregation via IMediator"
- **ENGINEERING_RULES.md**: Law - "Must separate Read and Write models. Use Projections for data retrieval"
- **LESSONS_LEARNED.md**: Why - "CQRS Enables Scalability" with memory/performance improvements
- **ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md**: Applied - Query handlers with optimized projections

### IHttpClientFactory
- **AI_MASTER_CONTEXT.md**: Rule - "Always use IHttpClientFactory. Manual instantiation is banned"
- **ENGINEERING_RULES.md**: Law - "Must Always use IHttpClientFactory. Must Not manually instantiate HttpClient()"
- **LESSONS_LEARNED.md**: Why - "IHttpClientFactory, Always" with socket exhaustion explanation
- **ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md**: Applied - All external API integrations

---

## 📚 AI Ingestion Order

### 🤖 **For AI Models (Recommended Reading Sequence)**
```
1. AI_PROJECT_INDEX.md (This file) - 2 minutes
2. AI_MASTER_CONTEXT.md - 15 minutes
3. ENGINEERING_RULES.md - 10 minutes
4. LESSONS_LEARNED.md - 30 minutes (as needed)
5. ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md - 20 minutes (for feature work)
```

### 👥 **For Human Contributors**
```
1. AI_PROJECT_INDEX.md (This file) - 3 minutes
2. AI_MASTER_CONTEXT.md - 10 minutes
3. ENGINEERING_RULES.md - 10 minutes
4. ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md - 15 minutes
5. LESSONS_LEARNED.md - As needed for deeper understanding
```

---

## 🎯 Quick Reference Guide

| I need to... | Read this first | Then this | Notes |
|-------------|----------------|-----------|-------|
| Write code | AI_MASTER_CONTEXT.md | ENGINEERING_RULES.md | Patterns, then constraints |
| Fix a bug | LESSONS_LEARNED.md | AI_MASTER_CONTEXT.md | Learn from past, understand current |
| Add a feature | ADVANCED_GAMING_FEATURES_IMPLEMENTATION_PLAN.md | ENGINEERING_RULES.md | Plan, then rules |
| Review code | AI_MASTER_CONTEXT.md | ENGINEERING_RULES.md | Standards, then compliance |
| Design system | ENGINEERING_RULES.md | AI_MASTER_CONTEXT.md | Constraints, then patterns |
| Understand project | AI_MASTER_CONTEXT.md | This index | Overview, then navigation |

---

*This index ensures no document is read in isolation. Each document reinforces the others, creating a cohesive knowledge system that scales with the project's complexity.*