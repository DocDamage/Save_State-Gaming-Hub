# SaveState Reborn: White Paper
## A Modern Game Library Platform

**Document ID:** SS-WP-001  
**Revision:** 1.0  
**Classification:** Engineering Reference  
**Date:** 2024-12-20

---

## 1. Executive Summary

SaveState Reborn is a next-generation game library management platform designed to unify PC gaming across all major storefronts while providing comprehensive retro-gaming support. Built from the ground up using cutting-edge technologies, it prioritizes performance, maintainability, and cross-platform capability.

### Mission Statement
> Deliver the fastest, most reliable game library experience by leveraging Native AOT compilation, modern UI frameworks, and first-party integrations with zero third-party plugin dependencies.

---

## 2. Requirements Hierarchy

### 2.1 Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-001 | Import game libraries from Steam, GOG, Epic, Xbox, EA, Ubisoft, Amazon, itch.io, Humble, PlayStation | Critical |
| FR-002 | Fetch metadata (artwork, descriptions, playtime estimates) from IGDB and SteamGridDB | Critical |
| FR-003 | Manage ROM collections with automatic organization and scraping | Critical |
| FR-004 | Track achievements via RetroAchievements integration | High |
| FR-005 | Launch games with automatic store client handling | Critical |
| FR-006 | BIOS management for emulator configuration | High |
| FR-007 | Single-instance enforcement with command passing | Medium |

### 2.2 Non-Functional Requirements

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-001 | Application startup time | < 200ms |
| NFR-002 | Memory footprint (idle) | < 100MB |
| NFR-003 | Binary size (installed) | < 50MB |
| NFR-004 | Platform support | Windows 10+, Linux, macOS |
| NFR-005 | Framework support lifecycle | .NET 9 → .NET 10 LTS migration path |

---

## 3. Technology Stack

### 3.1 Decision Matrix

| Component | Selected Technology | Alternatives Considered | Rationale |
|-----------|---------------------|------------------------|-----------|
| **Runtime** | .NET 9 + C# 13 | .NET 8 LTS | Latest features, Native AOT improvements, 18-month support window acceptable |
| **Compilation** | Native AOT | JIT | 10x faster startup, smaller binaries, no runtime dependency |
| **UI Framework** | Avalonia UI 11 | WPF, MAUI, Blazor | True cross-platform, XAML familiarity, AOT-compatible, active development |
| **Database** | SQLite + EF Core 9 | PostgreSQL + Marten, LiteDB | Embedded, zero-install, portable, modern ORM |
| **IPC** | gRPC over Named Pipes | WCF, raw Named Pipes, SignalR | Strongly-typed contracts, binary efficiency, local-only optimization |
| **Browser Control** | WebView2 | CefSharp, Photino | Native .NET support, ships with Windows, Edge-based |

### 3.2 Dependency Graph

```mermaid
graph TB
    subgraph "Presentation Layer"
        UI[Avalonia UI 11]
        WV[WebView2]
    end
    
    subgraph "Application Layer"
        VM[ViewModels]
        SVC[Services]
    end
    
    subgraph "Integration Layer"
        STEAM[Steam API]
        GOG[GOG Galaxy]
        EPIC[Epic Games]
        XBOX[Xbox/Game Pass]
        EA[EA App]
        UBI[Ubisoft Connect]
        IGDB[IGDB API]
    end
    
    subgraph "Data Layer"
        EF[EF Core 9]
        DB[(SQLite)]
    end
    
    subgraph "Infrastructure"
        IPC[gRPC Named Pipes]
        AOT[Native AOT Runtime]
    end
    
    UI --> VM
    WV --> VM
    VM --> SVC
    SVC --> STEAM & GOG & EPIC & XBOX & EA & UBI & IGDB
    SVC --> EF
    EF --> DB
    SVC --> IPC
    AOT --> UI & SVC & EF
```

---

## 4. Architecture Overview

### 4.1 Layer Definitions

| Layer | Responsibility | Key Components |
|-------|---------------|----------------|
| **Presentation** | User interface rendering, input handling | Avalonia Views, WebView2 panels |
| **ViewModel** | UI state management, command binding | CommunityToolkit.Mvvm ViewModels |
| **Service** | Business logic, integrations | Store importers, metadata fetchers, ROM managers |
| **Data** | Persistence, caching | EF Core DbContext, SQLite |
| **Infrastructure** | Cross-cutting concerns | IPC, logging, configuration |

### 4.2 Store Integration Architecture

Each store integration follows the **Provider Pattern**:

```
IGameProvider
├── SteamProvider
├── GogProvider
├── EpicProvider
├── XboxProvider
├── EaProvider
├── UbisoftProvider
├── AmazonProvider
├── ItchProvider
├── HumbleProvider
└── PlayStationProvider
```

Each provider implements:
- `GetInstalledGamesAsync()` - Discover locally installed games
- `GetOwnedGamesAsync()` - Fetch cloud library via API
- `LaunchGameAsync(Game game)` - Start game with proper client

---

## 5. Risk Assessment

| Risk ID | Description | Likelihood | Impact | Mitigation |
|---------|-------------|------------|--------|------------|
| R-001 | .NET 9 EOL in 18 months | Certain | Medium | Plan .NET 10 LTS migration for Q4 2025 |
| R-002 | Store API changes break integrations | Medium | High | Abstract behind provider interface, automated testing |
| R-003 | Native AOT limits dynamic scenarios | Low | Medium | Design all integrations as compile-time; no plugins |
| R-004 | WebView2 runtime not installed | Low | Medium | Include WebView2 bootstrapper in installer |
| R-005 | Cross-platform parity issues | Medium | Medium | Prioritize Windows, validate Linux/macOS quarterly |

---

## 6. Trade-Off Analysis

### 6.1 Plugin System vs. Native AOT

| Factor | Plugin System (JIT) | Native AOT (No Plugins) |
|--------|---------------------|-------------------------|
| Startup time | ~1-2 seconds | ~100-200ms |
| Binary size | ~150MB | ~40MB |
| Extensibility | Community plugins | First-party only |
| Maintenance | Plugin compatibility burden | Full control |
| **Decision** | — | ✅ Selected |

**Rationale:** With first-party integrations for all major stores, the plugin ecosystem provides diminishing returns while adding startup latency and maintenance burden.

### 6.2 WPF vs. Avalonia

| Factor | WPF | Avalonia |
|--------|-----|----------|
| Platform | Windows only | Windows, Linux, macOS, Web |
| Minimum OS | Windows 7 | Windows 10 |
| AOT support | Limited | Full |
| Development velocity | Mature but stagnant | Active, modern patterns |
| **Decision** | — | ✅ Selected |

---

## 7. Standards Compliance

| Standard | Applicability | Status |
|----------|--------------|--------|
| .NET Code Style (editorconfig) | All C# code | Enforced |
| MVVM Pattern | UI/ViewModel separation | Required |
| SOLID Principles | Service layer design | Required |
| Semantic Versioning | Release management | Required |
| SPDX License Identifiers | Dependency management | Required |

---

## 8. Glossary

| Term | Definition |
|------|------------|
| **Native AOT** | Ahead-of-Time compilation producing native executables without JIT |
| **Provider** | Implementation of store-specific game discovery and launch logic |
| **ROM** | Read-Only Memory dump of a game cartridge/disc for emulation |
| **IPC** | Inter-Process Communication for single-instance coordination |
| **IGDB** | Internet Game Database - metadata source |

---

## Appendix A: Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2024-12-20 | Antigravity | Initial specification |

---

## Appendix B: Approval

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Technical Lead | _________________ | ________ | ________ |
| Project Manager | _________________ | ________ | ________ |
