# 📚 SaveState Reborn Glossary

**Status**: ✅ Active Reference
**Last Updated**: January 2, 2026
**Maintained By**: Development Team
**Audience**: New team members, contributors, AI assistants

---

## Architecture & Patterns

| Term | Definition | Used In |
|:-----|:-----------|:--------|
| **Result<T>** | Type-safe error handling pattern; never returns null. Use `Result<T>.Success(value)` or `Result<T>.Failure(msg)` | All projects |
| **CQRS** | Command Query Responsibility Segregation - separates read and write operations | Architecture layer |
| **Clean Architecture** | Layered architecture with Core at center, Infrastructure outside | Project structure |
| **Value Object** | Immutable type that enforces domain rules (e.g., `GameTitle`, `FilePath`) | Core layer |
| **Aggregate Root** | Entity that owns and controls access to related entities (e.g., `Game`) | Domain design |
| **MediatR** | CQRS implementation using the mediator pattern for decoupled handlers | Application layer |
| **Projection** | Read-optimized DTO separate from write model; used for efficient queries | CQRS queries |
| **Bounded Context** | Logical boundary within the domain with its own models and language | Core layer (22 contexts) |
| **Soft Delete** | Marking entities as deleted without removing from database | Entity behavior |
| **Guard Clause** | Validation at method/constructor entry using `Guard.Against` (Ardalis) | All layers |

---

## AI & Knowledge System

| Term | Definition | Used In |
|:-----|:-----------|:--------|
| **RAG** | Retrieval-Augmented Generation - enhances AI responses with relevant knowledge base context | AI orchestration |
| **BMAD** | Bounded Memory Associative Dynamics - short-term conversational memory | AI orchestration |
| **AiOrchestrator** | Central coordinator for all AI operations with RAG, BMAD, and web search | Infrastructure |
| **Knowledge Base** | Collection of Markdown files in `%LOCALAPPDATA%/SaveStateReborn/KnowledgeBase/` | AI services |
| **Semantic Cache** | Caches AI responses based on semantic similarity of queries | AI optimization |
| **Web Search Fallback** | Automatic internet search when local knowledge is insufficient | AI orchestration |
| **Auto-Save to KB** | Automatically persists search results as Markdown for future queries | Knowledge service |
| **Vector Store** | SQLite-based semantic search index for knowledge retrieval | `SqliteVectorStore` |
| **Resilience Policy** | Polly-based retry, circuit breaker, and timeout handling for AI calls | AI infrastructure |

---

## Gaming & MUGEN

| Term | Definition | Used In |
|:-----|:-----------|:--------|
| **MUGEN** | Customizable 2D fighting game engine with character/stage importing | MUGEN module |
| **Character Stats** | Win/loss/draw records for MUGEN characters | `MugenStatsService` |
| **Match History** | Record of all fights between characters | `MugenMatchHistory` |
| **Tournament** | Organized competition bracket for MUGEN characters | Tournament system |
| **Death Match** | Automated fight simulation for AI predictions | `DeathMatchSimulator` |
| **ROM** | Read-Only Memory - game files for emulators | Emulator detection |
| **RetroAchievements** | Achievement system for retro games via retroachievements.org | Achievements module |

---

## UI & Presentation

| Term | Definition | Used In |
|:-----|:-----------|:--------|
| **Avalonia** | Cross-platform .NET UI framework similar to WPF | Presentation layer |
| **ReactiveUI** | MVVM framework with reactive programming support | ViewModels |
| **Big Picture Mode** | Controller-friendly fullscreen UI mode for TV usage | Planned feature |
| **Widget** | Modular dashboard component with auto-refresh capability | Dashboard system |
| **Command Palette** | Quick-access command interface (Ctrl+Shift+P) | Shell navigation |
| **Performance HUD** | Real-time CPU/GPU/FPS overlay (F3) | Overlay system |
| **$parent Binding** | Avalonia XAML binding to parent control's DataContext | XAML patterns |

---

## Infrastructure

| Term | Definition | Used In |
|:-----|:-----------|:--------|
| **IHttpClientFactory** | Factory pattern for creating HttpClient instances with proper lifecycle | HTTP communication |
| **Polly** | .NET resilience library for retries, circuit breakers, timeouts | Resilience policies |
| **EF Core** | Entity Framework Core - ORM for database access | Persistence |
| **SQLite** | Lightweight file-based relational database | Data storage |
| **WAL** | Write-Ahead Logging - SQLite mode for concurrent access | Database config |
| **DI** | Dependency Injection - IoC container pattern | `DependencyInjection.cs` |
| **ConfigureAwait(false)** | Async optimization to avoid capturing synchronization context | Library code |

---

## Testing

| Term | Definition | Used In |
|:-----|:-----------|:--------|
| **xUnit** | .NET testing framework | All test projects |
| **FluentAssertions** | Readable assertion library for expressive test assertions | Test assertions |
| **Moq** | Mocking library for creating test doubles | Unit tests |
| **Bogus** | Fake data generator for test fixtures | Test data |
| **Fake** | Simple test implementation of an interface | Integration tests |
| **Mock** | Configurable test double with verification capabilities | Unit tests |

---

## CLI

| Term | Definition | Used In |
|:-----|:-----------|:--------|
| **Spectre.Console** | Rich console UI library with tables, charts, prompts | CLI output |
| **System.CommandLine** | .NET library for building command-line applications | CLI parsing |
| **ICommandGroup** | Interface for organizing related CLI commands | Command organization |

---

## Common Abbreviations

| Abbreviation | Full Form |
|:-------------|:----------|
| **AI** | Artificial Intelligence |
| **API** | Application Programming Interface |
| **CLI** | Command-Line Interface |
| **CQRS** | Command Query Responsibility Segregation |
| **DI** | Dependency Injection |
| **DTO** | Data Transfer Object |
| **EF** | Entity Framework |
| **IGDB** | Internet Game Database |
| **KB** | Knowledge Base |
| **LOC** | Lines of Code |
| **MVVM** | Model-View-ViewModel |
| **ORM** | Object-Relational Mapping |
| **RAG** | Retrieval-Augmented Generation |
| **UI** | User Interface |
| **VM** | ViewModel |
| **WAL** | Write-Ahead Logging |

---

## Error Types (Result Pattern)

| ErrorType | When to Use |
|:----------|:------------|
| `Validation` | Input doesn't meet business rules |
| `NotFound` | Requested entity doesn't exist |
| `Conflict` | Operation conflicts with existing state |
| `Unauthorized` | User lacks permission |
| `External` | Third-party service failure |
| `Internal` | Unexpected internal error |

---

**Format**: Alphabetical within categories, searchable
**Updates**: Add new terms as they're introduced to the codebase
