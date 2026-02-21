# Project Governance & Quality Gates

This document defines the strict criteria for project progression and quality control.

---

[← Back to README](./README.md)

---

## **⚖️ Quality Gate Criteria**

Each phase has a "Stop/Go" gate. We do NOT proceed until all criteria are met.

### **Gate 0: Foundation (Phase 0 → Phase 1)**

- [ ] **Build Integrity**: 0 errors, 0 warnings (warnings-as-errors enabled).
- [ ] **Architecture**: All 10 ADRs (Architecture Decision Records) reviewed and signed off.
- [ ] **Walking Skeleton**: Can perform one full "Round Trip" (UI -> Application -> DB -> UI).
- [ ] **Linting**: 100% compliance with `.editorconfig`.

### **Gate 1: Infrastructure (Phase 1 → Phase 2)**

- [ ] **Test Coverage**: ≥ 90% instruction coverage in `SaveState.Core`.
- [ ] **DB Migrations**: All migrations tested for rollback capability.
- [ ] **Telemetry**: OpenTelemetry traces successfully captured for all Repository methods.
- [ ] **Security**: No secrets detected in git history (using `gitleaks` or similar).

### **Gate 2: Game Library (Phase 2 → Phase 3)**

- [ ] **Resilience**: `SteamProvider` and `MetadataService` must withstand 10% simulated API failure rate.
- [ ] **Performance**: ROM scan of 1,000 files completes in < 5 seconds.
- [ ] **Concurrency**: No "Database Locked" errors during simultaneous import and scan.

### **Gate 3: AI Integration (Phase 3 → Phase 4/5)**

- [ ] **Cost Control**: AI cost-per-query logged and within $0.01 threshold.
- [ ] **Latency**: P95 AI response time < 5 seconds.
- [ ] **RAG Accuracy**: AI correctly identifies cheat codes from local knowledge base 9/10 times.
- [ ] **Circuit Breaker**: Verified that `OpenAiProvider` fails over to `GroqProvider` in < 200ms.

---

## **🛠️ Automated Quality Gates (CI/CD)**

The pipeline will enforce these automatically on every Push to `main`.

| Check | Tool | Failure Action |
|:---|:---|:---|
| Unit Tests | `dotnet test` | Block Merge |
| Code Coverage | `coverlet` | Block Merge if < 90% |
| Static Analysis | `SonarLint / Roslyn` | Block Merge if "Code Smells" > 5 |
| Licensing | `dotnet-project-licenses` | Warning if GPLv3 detected |
| Vulnerabilities | `dotnet list package --vulnerable` | Block Merge if High/Critical |

---

## **📜 Architecture Decision Records (ADR) Index**

| ADR # | Status | Title | Decision |
|:---|:---|:---|:---|
| 001 | Accepted | Clean Architecture | Use 5-layer separation of concerns. |
| 002 | Accepted | MediatR / CQRS | Use MediatR for all Side-Effects; Direct Query for Reads. |
| 003 | Accepted | Result Pattern | Never throw exceptions for expected business failures. |
| 004 | Accepted | Native AOT | Target Native AOT for the CLI/Core components for fast startup. |
| 005 | Accepted | Local-First RAG | Prioritize local SQLite vector storage over cloud vector DBs. |
| 006 | Accepted | Reactive UI | Use `ReactiveUI` for ViewModel-to-View binding and throttling. |

---

## **🔒 Security Policy**

- **Secret Management**: All keys must reside in `UserSecrets` (Dev) or `EnvironmentVariables` (Prod).
- **Encryption**: sensitive game paths in the DB must be encrypted at rest if the user requests "Private Mode".
- **Sandbox**: All ROM execution logic must be isolated from the main app process.

---

## **🚀 Release Readiness Checklist (The "Master" Gate)**

- [ ] Startup time < 200ms on a clean machine.
- [ ] Memory footprint < 150MB after 1 hour of use.
- [ ] Full AOT compilation verified on Win11 x64.
- [ ] All "Specialist" personas correctly responding to their domain prompts.
- [ ] Velopack installer correctly bundles all dependencies (including 7zip/dependencies).
