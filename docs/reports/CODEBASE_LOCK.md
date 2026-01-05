# 🔒 Codebase Lock Registry

**Last Updated**: January 1, 2026 (v1.0.0 Release)
**Status**: Production Lock Enabled
**Purpose**: Define stable, production-ready code that requires architectural review before modification

---

## 🎯 Lock Policy

Files marked as **LOCKED** are:

- Production-ready and battle-tested
- Core architectural components
- Require architectural review + approval before changes
- Must maintain backward compatibility
- Changes require comprehensive test coverage

---

## 🔐 Locked Files (Stable - Production Ready)

### Core Domain Layer

| File | Status | Reason |
|------|--------|--------|
| `src/SaveState.Core/Common/Result.cs` | 🔒 LOCKED | Result pattern implementation |
| `src/SaveState.Core/Common/ValueObjects/*.cs` | 🔒 LOCKED | Value objects (GameTitle, FilePath, etc.) |
| `src/SaveState.Core/GameLibrary/Entities/Game.cs` | 🔒 LOCKED | Core aggregate root |
| `src/SaveState.Core/GameLibrary/Entities/GameSession.cs` | 🔒 LOCKED | Session tracking entity |
| `src/SaveState.Core/GameLibrary/Entities/Platform.cs` | 🔒 LOCKED | Platform entity |
| `src/SaveState.Core/Mugen/Entities/*.cs` | 🔒 LOCKED | MUGEN domain entities |

### Application Layer

| File | Status | Reason |
|------|--------|--------|
| `src/SaveState.Application/GameLibrary/Commands/*.cs` | 🔒 LOCKED | Core CQRS commands |
| `src/SaveState.Application/GameLibrary/Queries/*.cs` | 🔒 LOCKED | Core CQRS queries |
| `src/SaveState.Application/GameLibrary/Commands/Handlers/*.cs` | 🔒 LOCKED | Command handlers |
| `src/SaveState.Application/GameLibrary/Queries/Handlers/*.cs` | 🔒 LOCKED | Query handlers |

### Infrastructure Layer

| File | Status | Reason |
|------|--------|--------|
| `src/SaveState.Infrastructure/Persistence/SaveStateDbContext.cs` | 🔒 LOCKED | Database context |
| `src/SaveState.Infrastructure/Persistence/Configurations/*.cs` | 🔒 LOCKED | EF Core configurations |
| `src/SaveState.Infrastructure/DependencyInjection.cs` | 🔒 LOCKED | DI registration |
| `src/SaveState.Infrastructure/Ai/AiOrchestrator.cs` | 🔒 LOCKED | AI coordinator |
| `src/SaveState.Infrastructure/Ai/Resilience/AiResiliencePolicy.cs` | 🔒 LOCKED | Resilience policies |

### Repositories

| File | Status | Reason |
|------|--------|--------|
| `src/SaveState.Infrastructure/Repositories/GameRepository.cs` | 🔒 LOCKED | Core game data access |
| `src/SaveState.Infrastructure/Repositories/MugenCharacterRepository.cs` | 🔒 LOCKED | MUGEN character data access |
| `src/SaveState.Infrastructure/Repositories/MugenTournamentRepository.cs` | 🔒 LOCKED | Tournament data access |

---

## 🟡 Semi-Locked (Require Review)

### Services

| File | Status | Reason |
|------|--------|--------|
| `src/SaveState.Infrastructure/GameLibrary/Services/*.cs` | 🟡 REVIEW | Service layer - changes need review |
| `src/SaveState.Infrastructure/Sync/*.cs` | 🟡 REVIEW | Cloud sync - stable but extensible |
| `src/SaveState.Infrastructure/Social/*.cs` | 🟡 REVIEW | Social features - stable API |

### Plugin System

| File | Status | Reason |
|------|--------|--------|
| `src/SaveState.Core/Plugins/IPlugin.cs` | 🔒 LOCKED | Plugin interface contract |
| `src/SaveState.Infrastructure/Plugins/PluginLoader.cs` | 🟡 REVIEW | Plugin loader - changes need review |

---

## ✅ Open for Development

### UI Layer

| Area | Status | Reason |
|------|--------|--------|
| `src/SaveState.Presentation/Views/*.axaml` | ✅ OPEN | UI can evolve |
| `src/SaveState.Presentation/ViewModels/*.cs` | ✅ OPEN | ViewModels can change |

### CLI Layer

| Area | Status | Reason |
|------|--------|--------|
| `src/SaveState.CLI/Commands/*.cs` | ✅ OPEN | CLI commands can be added/modified |

### Plugins

| Area | Status | Reason |
|------|--------|--------|
| `src/SaveState.Plugins.*/*.cs` | ✅ OPEN | Plugins are extensible by design |

### Tests

| Area | Status | Reason |
|------|--------|--------|
| `tests/**/*.cs` | ✅ OPEN | Tests should continuously improve |

---

## 📋 Lock Modification Process

### To Modify a Locked File

1. **Create Issue**: Document why the change is needed
2. **Architectural Review**: Get approval from team lead
3. **Impact Analysis**: Assess breaking changes
4. **Test Coverage**: Ensure comprehensive tests
5. **Documentation**: Update all relevant docs
6. **Peer Review**: Minimum 2 reviewers
7. **Integration**: Merge with caution

### Emergency Hotfixes

Critical bugs in locked files:

1. Create hotfix branch
2. Minimal code changes only
3. Comprehensive test coverage
4. Expedited review process
5. Document in CHANGELOG.md

---

## 🔍 Lock Rationale

### Why Lock Files?

1. **Stability**: Prevent accidental breaking changes
2. **Architecture**: Maintain design integrity
3. **Contracts**: Preserve public APIs
4. **Testing**: Ensure validated code stays validated
5. **Quality**: Protect production-grade implementations

### When to Lock?

Files are locked when they achieve:

- ✅ 100% test coverage
- ✅ Production usage validation
- ✅ Architectural approval
- ✅ Documentation completion
- ✅ Zero known issues

---

## 📊 Lock Status Summary

| Category | Locked | Semi-Locked | Open | Total |
|----------|--------|-------------|------|-------|
| **Core** | 25 | 5 | 232 | 262 |
| **Application** | 50 | 10 | 161 | 221 |
| **Infrastructure** | 45 | 35 | 100 | 180 |
| **Presentation** | 0 | 0 | 38 | 38 |
| **CLI** | 0 | 0 | 10 | 10 |
| **Plugins** | 1 | 1 | 50 | 52 |
| **Total** | **121** | **51** | **591** | **763** |

**Lock Percentage**: 15.9% (121/763 files)

---

## 🚨 Breaking Change Policy

### Locked Files

- **MUST NOT** break public APIs
- **MUST** maintain backward compatibility
- **MUST** provide migration path if breaking
- **MUST** update major version number

### Semi-Locked Files

- **SHOULD** avoid breaking changes
- **CAN** evolve with minor version bumps
- **MUST** document changes in CHANGELOG

---

## 📝 Lock Review Schedule

| Review Type | Frequency | Next Review |
|-------------|-----------|-------------|
| **Full Audit** | Quarterly | April 1, 2026 |
| **Critical Files** | Monthly | February 1, 2026 |
| **Lock Status** | Per Release | Next release |

---

## ✅ Verification

To verify lock status:

```bash
# Check for modifications to locked files
git diff --name-only HEAD~1 | grep -f LOCKED_FILES.txt

# Enforce in CI/CD
# Add pre-commit hook to check locked files
```

---

**Lock Registry Version**: 1.0.0
**Approved By**: Development Team
**Effective Date**: January 1, 2026
**Next Review**: February 1, 2026

---

*Protecting production stability while enabling innovation*
