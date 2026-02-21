# Phase 0: Foundation & Governance (Weeks 1-2)

---

[← Back to README](./README.md) | [Phase 1 →](./phase-1-core-infrastructure.md)

---

## **🏗️ Phase 0: Foundation & Governance (Weeks 1-2)**

### **0.1 Project Setup & Infrastructure**

#### **Task T-0.1.1: Repository Initialization**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 4 hours |
| **Dependencies** | None |
| **AI Turns** | 2-3 |
| **Files Created** | 8 projects + solution |

**Assumes Exists:** Nothing - this is the first task.

**Steps:**

1. **Create Repository Structure**

📁 Run in: `C:\Projects\` (or your projects directory)

```bash
mkdir SaveStateReborn && cd SaveStateReborn
git init

# Create solution
dotnet new sln -n SaveStateReborn

# Create source projects
mkdir src
cd src
dotnet new classlib -n SaveState.Core -o SaveState.Core --framework net9.0
dotnet new classlib -n SaveState.Application -o SaveState.Application --framework net9.0
dotnet new classlib -n SaveState.Infrastructure -o SaveState.Infrastructure --framework net9.0
dotnet new classlib -n SaveState.Presentation -o SaveState.Presentation --framework net9.0
cd ..

# Create test projects
mkdir tests
cd tests
dotnet new xunit -n SaveState.Core.Tests -o SaveState.Core.Tests --framework net9.0
dotnet new xunit -n SaveState.Application.Tests -o SaveState.Application.Tests --framework net9.0
dotnet new xunit -n SaveState.IntegrationTests -o SaveState.IntegrationTests --framework net9.0
dotnet new xunit -n SaveState.EndToEndTests -o SaveState.EndToEndTests --framework net9.0
cd ..

# Add all projects to solution
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\SaveState.Core\SaveState.Core.csproj" />
  </ItemGroup>
</Project>
```

📁 Create: `src/SaveState.Infrastructure/SaveState.Infrastructure.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\SaveState.Application\SaveState.Application.csproj" />
    <ProjectReference Include="..\SaveState.Core\SaveState.Core.csproj" />
  </ItemGroup>
</Project>
```

📁 Create: `src/SaveState.Presentation/SaveState.Presentation.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\SaveState.Application\SaveState.Application.csproj" />
  </ItemGroup>
</Project>
```

1. **Create Directory Structure**

📁 Run: Create folder structure

```bash
# Core layer folders
mkdir src/SaveState.Core/Common
mkdir src/SaveState.Core/Common/Base
mkdir src/SaveState.Core/Common/Interfaces
mkdir src/SaveState.Core/GameLibrary
mkdir src/SaveState.Core/GameLibrary/Entities
mkdir src/SaveState.Core/GameLibrary/ValueObjects
mkdir src/SaveState.Core/GameLibrary/Events

# Application layer folders
mkdir src/SaveState.Application/Common
mkdir src/SaveState.Application/Common/Behaviors
mkdir src/SaveState.Application/GameLibrary
mkdir src/SaveState.Application/GameLibrary/Commands
mkdir src/SaveState.Application/GameLibrary/Queries

# Infrastructure layer folders
mkdir src/SaveState.Infrastructure/Persistence
mkdir src/SaveState.Infrastructure/Repositories
mkdir src/SaveState.Infrastructure/External

# Presentation layer folders
mkdir src/SaveState.Presentation/ViewModels
mkdir src/SaveState.Presentation/Views
```

✅ **Verify:**

```bash
dotnet build SaveStateReborn.sln
```

**Expected:** Build succeeded. 0 Warning(s). 0 Error(s).

🔧 **If Fails:**

- `error MSB4025: Project file not found` → Check paths in `dotnet sln add` commands
- `error NU1101: Unable to find package` → Run `dotnet restore`
- Reference errors → Verify `.csproj` ProjectReference paths use `..\` not `/`

---

   tests/
   ├── SaveState.Core.Tests/
   ├── SaveState.Application.Tests/
   ├── SaveState.IntegrationTests/
   └── SaveState.EndToEndTests/

   ```

#### **Task T-0.1.2: Development Environment Setup**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 8 hours |
| **Dependencies** | T-0.1.1 |
| **AI Turns** | 2 |
| **Files Created** | 4 |

**Assumes Exists:**
- Solution structure from T-0.1.1

**Steps:**

1. **Configure .editorconfig**

📁 Create: `.editorconfig` (root directory)
```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 4

[*.cs]
# Code quality rules
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_predefined_type_for_locals_parameters_members = true:warning
dotnet_style_predefined_type_for_member_access = true:warning

# Complexity limits (ENFORCED)
max_lines_per_file = 200:error
dotnet_diagnostic.CA1502.severity = error
dotnet_diagnostic.CA1505.severity = error
dotnet_diagnostic.CA1822.severity = error
dotnet_diagnostic.CA2007.severity = error

# Naming conventions
dotnet_naming_style.pascal_case.capitalization = pascal_case
dotnet_naming_rule.public_members_must_be_capitalized.severity = error
dotnet_naming_rule.public_members_must_be_capitalized.symbols = public_symbols
dotnet_naming_rule.public_members_must_be_capitalized.style = pascal_case
dotnet_naming_symbols.public_symbols.applicable_kinds = property,method,field,event,delegate
dotnet_naming_symbols.public_symbols.applicable_accessibilities = public
```

1. **Setup Global Usings**

📁 Create: `src/SaveState.Core/GlobalUsings.cs`

```csharp
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Ardalis.GuardClauses;
   ```

1. **Configure Development Tools**

   ```json
   // .vscode/settings.json
   {
     "dotnet.defaultSolution": "SaveStateReborn.sln",
     "editor.formatOnSave": true,
     "editor.codeActionsOnSave": {
       "source.fixAll": "explicit"
     },
     "csharp.format.enable": true,
     "omnisharp.enableRoslynAnalyzers": true,
     "omnisharp.enableEditorConfigSupport": true
   }
   ```

#### **Task T-0.1.3: CI/CD Pipeline Implementation**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 12 hours |
| **Dependencies** | T-0.1.1, T-0.1.2 |
| **AI Turns** | 3 |
| **Files Created** | 3 |

**Assumes Exists:**

- Solution structure from T-0.1.1
- `.editorconfig` from T-0.1.2

**Steps:**

1. **GitHub Actions Workflow**

📁 Create: `.github/workflows/ci.yml`

```yaml
   # .github/workflows/ci.yml
   name: Continuous Integration
   on:
     push:
       branches: [ main, develop ]
     pull_request:
       branches: [ main, develop ]

   env:
     DOTNET_VERSION: '9.0.x'

jobs:
     build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
             dotnet-version: ${{ env.DOTNET_VERSION }}

         - name: Cache NuGet packages
           uses: actions/cache@v4
           with:
             path: ~/.nuget/packages
             key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
             restore-keys: |
               ${{ runner.os }}-nuget-

      - name: Restore dependencies
           run: dotnet restore --locked-mode

      - name: Build
           run: dotnet build --no-restore --configuration Release -p:ContinuousIntegrationBuild=true

         - name: Run unit tests
           run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage" --results-directory ./coverage --filter "Category!=IntegrationTest"

         - name: Run integration tests
           run: dotnet test --no-build --configuration Release --filter "Category=IntegrationTest"

         - name: Upload coverage
        uses: codecov/codecov-action@v4
        with:
          file: ./coverage/**/coverage.cobertura.xml
          fail_ci_if_error: true
          threshold: 95%

         - name: Complexity check
        run: |
             echo "Checking file complexity..."
             find src -name "*.cs" -exec wc -l {} \; | awk '$1 > 200 {print "ERROR: " $2 " has " $1 " lines"; exit 1}'

      - name: Security scan
        uses: github/super-linter@v6
        env:
          VALIDATE_ALL_CODEBASE: false
          VALIDATE_CSHARP: true
             GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

     performance-test:
       runs-on: ubuntu-latest
       needs: build-and-test
       steps:
         - uses: actions/checkout@v4
         - name: Setup .NET
           uses: actions/setup-dotnet@v4
           with:
             dotnet-version: ${{ env.DOTNET_VERSION }}

         - name: Run performance benchmarks
           run: dotnet run --project tools/SaveState.Benchmarks --configuration Release

         - name: Upload benchmark results
           uses: actions/upload-artifact@v4
           with:
             name: benchmark-results
             path: BenchmarkDotNet.Artifacts/
   ```

1. **Quality Gates Configuration**

   ```xml
   <!-- Directory.Build.props -->
   <Project>
     <PropertyGroup>
       <TargetFramework>net9.0</TargetFramework>
       <Nullable>enable</Nullable>
       <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
       <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
       <AnalysisMode>AllEnabledByDefault</AnalysisMode>
       <CodeAnalysisTreatWarningsAsErrors>true</CodeAnalysisTreatWarningsAsErrors>
     </PropertyGroup>

     <ItemGroup>
       <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" PrivateAssets="All" />
       <PackageReference Include="Roslynator.Analyzers" Version="4.12.0" PrivateAssets="All" />
       <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" PrivateAssets="All" />
       <PackageReference Include="Ardalis.GuardClauses" Version="4.5.0" />
     </ItemGroup>
   </Project>
   ```

#### **Task T-0.1.4: Architecture Decision Records Setup**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 6 hours |
| **Dependencies** | T-0.1.1 |
| **AI Turns** | 1-2 |
| **Files Created** | 10 |

**Assumes Exists:**

- Repository from T-0.1.1

**Steps:**

1. **Create ADR Structure**

📁 Run: Create folder structure

```bash
mkdir -p docs/architecture/adrs
mkdir -p docs/architecture/decisions
```

1. **Create ADR Template**

📁 Create: `docs/architecture/adrs/000-template.md`

```markdown
# ADR [Number]: [Title]

## Status
[Proposed | Accepted | Deprecated | Superseded]

## Context
[Describe the context and problem statement]

## Decision
[Describe the decision made]

## Consequences
[List positive and negative consequences]

## Alternatives Considered
[List alternatives and why they were rejected]

## References
[Links to relevant documentation, issues, etc.]
```

1. **Create Initial ADRs**

📁 Create: `docs/architecture/adrs/001-clean-architecture.md`

```markdown
# ADR 001: Clean Architecture

## Status
Accepted

## Context
We need a maintainable, testable architecture that separates concerns and allows independent development of layers.

## Decision
Adopt Clean Architecture with 5 layers: Core, Application, Infrastructure, Presentation, App.

## Consequences
- ✅ Clear separation of concerns
- ✅ Easy to test each layer independently
- ⚠️ More files and boilerplate initially
```

1. **Create Remaining ADRs**

- ADR-002: CQRS for write operations, direct queries for reads
- ADR-003: Event-driven communication with MediatR
- ADR-004: Zero singletons policy - all dependencies injected
- ADR-005: Strong typing over primitive obsession
- ADR-006: Repository pattern with EF Core
- ADR-007: Comprehensive error handling with Result pattern
- ADR-008: Vertical slice development over horizontal layer completion
- ADR-009: Feature freeze policy after Week 4

✅ **Verify:**

```bash
ls docs/architecture/adrs/
```

**Expected:** 10 files (000-template.md through 009-feature-freeze.md)

#### **Task T-0.1.5: Walking Skeleton Milestone**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 8 hours |
| **Dependencies** | T-0.1.1, T-0.1.3, T-0.2.1 |
| **AI Turns** | 4-5 |
| **Files Created** | 8 |

**Assumes Exists:**

- Solution structure from T-0.1.1
- Base classes (EntityBase, ValueObject) from T-0.2.1
- CI/CD pipeline from T-0.1.3

**Definition**: A Walking Skeleton is the smallest possible implementation that connects ALL architectural layers:

```
[UI] GameCard displays "Test Game"
       ↓
[Presentation] GameLibraryViewModel
       ↓
[Application] GetGameDetailsQuery → Handler
       ↓
[Infrastructure] GameRepository → SQLite
       ↓
[Core] Game Entity
```

**Steps:**

1. **Create Game Entity (Core Layer)**

📁 Create: `src/SaveState.Core/GameLibrary/Entities/Game.cs`

```csharp
namespace SaveState.Core.GameLibrary.Entities;

public class Game
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? CoverImagePath { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Game() { } // EF Core

    public static Game Create(string title, string? coverImagePath = null)
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            Title = title,
            CoverImagePath = coverImagePath,
            CreatedAt = DateTime.UtcNow
        };
    }
}
```

1. **Create Repository Interface (Core Layer)**

📁 Create: `src/SaveState.Core/GameLibrary/IGameRepository.cs`

```csharp
namespace SaveState.Core.GameLibrary;

using SaveState.Core.GameLibrary.Entities;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Game game, CancellationToken ct = default);
}
```

1. **Create Query and Handler (Application Layer)**

📁 Create: `src/SaveState.Application/GameLibrary/Queries/GetAllGamesQuery.cs`

```csharp
namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Core.GameLibrary.Entities;

public record GetAllGamesQuery : IRequest<IReadOnlyList<Game>>;

public class GetAllGamesQueryHandler : IRequestHandler<GetAllGamesQuery, IReadOnlyList<Game>>
{
    private readonly IGameRepository _repository;

    public GetAllGamesQueryHandler(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Game>> Handle(GetAllGamesQuery request, CancellationToken ct)
    {
        return await _repository.GetAllAsync(ct);
    }
}
```

1. **Create DbContext and Repository (Infrastructure Layer)**

📁 Create: `src/SaveState.Infrastructure/Persistence/SaveStateDbContext.cs`

```csharp
namespace SaveState.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.GameLibrary.Entities;

public class SaveStateDbContext : DbContext
{
    public DbSet<Game> Games => Set<Game>();

    public SaveStateDbContext(DbContextOptions<SaveStateDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
        });
    }
}
```

📁 Create: `src/SaveState.Infrastructure/Repositories/GameRepository.cs`

```csharp
namespace SaveState.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

public class GameRepository : IGameRepository
{
    private readonly SaveStateDbContext _context;

    public GameRepository(SaveStateDbContext context)
    {
        _context = context;
    }

    public async Task<Game?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Games.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default)
        => await _context.Games.ToListAsync(ct);

    public async Task AddAsync(Game game, CancellationToken ct = default)
    {
        await _context.Games.AddAsync(game, ct);
        await _context.SaveChangesAsync(ct);
    }
}
```

1. **Create Unit Test (Verify Core Logic)**

📁 Create: `tests/SaveState.Core.Tests/GameLibrary/GameTests.cs`

```csharp
namespace SaveState.Core.Tests.GameLibrary;

using SaveState.Core.GameLibrary.Entities;
using Xunit;

public class GameTests
{
    [Fact]
    public void Create_WithValidTitle_SetsProperties()
    {
        var game = Game.Create("Test Game", "/images/cover.png");

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal("Test Game", game.Title);
        Assert.Equal("/images/cover.png", game.CoverImagePath);
        Assert.True(game.CreatedAt <= DateTime.UtcNow);
    }
}
```

✅ **Verify:**

```bash
# Build all projects
dotnet build SaveStateReborn.sln

# Run unit test
dotnet test tests/SaveState.Core.Tests --filter "GameTests"
```

**Expected:**

- Build succeeded. 0 Error(s).
- Test passed: 1 total.

🔧 **If Fails:**

- `CS0246: IGameRepository not found` → Add `using SaveState.Core.GameLibrary;` to Application project
- `CS0234: MediatR does not exist` → Run `dotnet add src/SaveState.Application package MediatR`
- `CS0234: EntityFrameworkCore` → Run `dotnet add src/SaveState.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite`

**Checkpoint**: Do not proceed to Phase 1 until Walking Skeleton passes.

### **0.2 Core Architecture Design**

#### **Task T-0.2.1: Clean Architecture Implementation**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 16 hours |
| **Dependencies** | T-0.1.1 |
| **AI Turns** | 3-4 |
| **Files Created** | 6 |

**Assumes Exists:**

- Solution structure from T-0.1.1
- Folder structure: `src/SaveState.Core/Common/Interfaces/` and `src/SaveState.Core/Common/Base/`

**Steps:**

1. **Define Core Interfaces**

📁 Create: `src/SaveState.Core/Common/Interfaces/IDomainEvent.cs`

```csharp
namespace SaveState.Core.Common.Interfaces;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
    Guid EventId { get; }
}
```

📁 Create: `src/SaveState.Core/Common/Interfaces/IAggregateRoot.cs`

```csharp
namespace SaveState.Core.Common.Interfaces;

public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

📁 Create: `src/SaveState.Core/Common/Interfaces/IEntity.cs`

```csharp
namespace SaveState.Core.Common.Interfaces;

public interface IEntity
{
    object Id { get; }
}
```

📁 Create: `src/SaveState.Core/Common/Interfaces/IValueObject.cs`

```csharp
namespace SaveState.Core.Common.Interfaces;

public interface IValueObject
{
    IEnumerable<object> GetEqualityComponents();
}
```

1. **Implement Base Classes**

📁 Create: `src/SaveState.Core/Common/Base/EntityBase.cs`

```csharp
namespace SaveState.Core.Common.Base;

using SaveState.Core.Common.Interfaces;

public abstract class EntityBase : IEntity
{
    public virtual object Id { get; protected set; } = default!;

    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id.Equals(other.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
```

📁 Create: `src/SaveState.Core/Common/Base/ValueObject.cs`

```csharp
namespace SaveState.Core.Common.Base;

using SaveState.Core.Common.Interfaces;

public abstract class ValueObject : IValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public IEnumerable<object> IValueObject.GetEqualityComponents() => GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
}
```

✅ **Verify (T-0.2.1):**

```bash
dotnet build src/SaveState.Core
```

**Expected:** Build succeeded. 0 Error(s).

🔧 **If Fails:**

- `CS0246: IDomainEvent not found` → Check namespace `using SaveState.Core.Common.Interfaces;`
- `CS0535: does not implement interface` → Ensure protected abstract matches interface signature

1. **Create Application Layer Foundation**

   ```csharp
   // SaveState.Application/Common/Behaviors/ValidationBehavior.cs
   public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
       where TRequest : IRequest<TResponse>
   {
       private readonly IEnumerable<IValidator<TRequest>> _validators;

       public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
       {
           _validators = validators;
       }

       public async Task<TResponse> Handle(
           TRequest request,
           RequestHandlerDelegate<TResponse> next,
           CancellationToken cancellationToken)
       {
           var failures = _validators
               .Select(v => v.Validate(request))
               .SelectMany(result => result.Errors)
               .Where(f => f != null)
               .ToList();

           if (failures.Any())
               throw new ValidationException(failures);

           return await next();
       }
   }

   // SaveState.Application/Common/Result.cs
   public class Result
   {
       public bool IsSuccess { get; }
       public bool IsFailure => !IsSuccess;
       public string? Error { get; }
       public ErrorType ErrorType { get; }

       protected Result(bool isSuccess, string? error = null, ErrorType errorType = ErrorType.None)
       {
           IsSuccess = isSuccess;
           Error = error;
           ErrorType = errorType;
       }

       public static Result Success() => new(true);
       public static Result Failure(string error, ErrorType errorType = ErrorType.Validation) =>
           new(false, error, errorType);
   }

   public class Result<T> : Result
   {
       public T? Value { get; }

       private Result(bool isSuccess, T? value = default, string? error = null, ErrorType errorType = ErrorType.None)
           : base(isSuccess, error, errorType)
       {
           Value = value;
       }

       public static Result<T> Success(T value) => new(true, value);
       public new static Result<T> Failure(string error, ErrorType errorType = ErrorType.Validation) =>
           new(false, default, error, errorType);
   }

```

#### **Task T-0.2.2: Bounded Context Design**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 20 hours |
| **Dependencies** | T-0.2.1 |
| **AI Turns** | 2-3 |
| **Files Created** | 6 |

**Assumes Exists:**
- Clean Architecture interfaces from T-0.2.1

**Steps:**

1. **Define Context Boundaries**

📁 Create: `src/SaveState.Core/GameLibrary/BoundedContext.cs`
```csharp
   // SaveState.Core/GameLibrary/BoundedContext.cs
   public static class GameLibraryContext
   {
       // This context handles:
       // - Game discovery and import
       // - Game metadata management
       // - Platform and genre organization
       // - Game library organization

       public const string Name = "GameLibrary";

       // Entities owned by this context
       public static readonly Type[] Entities = {
           typeof(Game),
           typeof(Platform),
           typeof(Genre),
           typeof(Developer),
           typeof(Publisher)
       };

       // Domain services
       public interface IGameImportService { /* ... */ }
       public interface IMetadataEnrichmentService { /* ... */ }
       public interface IGameOrganizationService { /* ... */ }
   }

   // SaveState.Core/RomManagement/BoundedContext.cs
   public static class RomManagementContext
   {
       // This context handles:
       // - ROM file discovery and scanning
       // - Emulator management
       // - BIOS file handling
       // - ROM organization and metadata

       public const string Name = "RomManagement";

       public static readonly Type[] Entities = {
           typeof(RomFile),
           typeof(Emulator),
           typeof(BiosFile),
           typeof(RomMetadata)
       };
   }

   // SaveState.Core/AiGaming/BoundedContext.cs
   public static class AiGamingContext
   {
       // This context handles:
       // - AI-assisted gaming features
       // - Cheat detection and prevention
       // - Trainer generation
       // - Memory scanning and analysis

       public const string Name = "AiGaming";

       public static readonly Type[] Entities = {
           typeof(AiModel),
           typeof(CheatPattern),
           typeof(Trainer),
           typeof(MemoryScan)
       };
   }
   ```

1. **Implement Context-Specific Value Objects**

   ```csharp
   // GameLibrary context
   public class GameTitle : ValueObject
   {
       public string Value { get; }

       public GameTitle(string value)
       {
           Value = Guard.Against.NullOrWhiteSpace(value, nameof(value))
               .Trim();
           if (Value.Length < 1 || Value.Length > 200)
               throw new ArgumentException("Title must be 1-200 characters", nameof(value));
       }

       protected override IEnumerable<object> GetEqualityComponents()
       {
           yield return Value.ToLowerInvariant();
       }

       public static implicit operator string(GameTitle title) => title.Value;
       public static explicit operator GameTitle(string value) => new(value);
   }

   // RomManagement context
   public class FilePath : ValueObject
   {
       public string Value { get; }

       public FilePath(string value)
       {
           Value = Guard.Against.NullOrWhiteSpace(value, nameof(value));
           if (!Path.IsPathRooted(Value))
               throw new ArgumentException("Path must be absolute", nameof(value));
       }

       protected override IEnumerable<object> GetEqualityComponents()
       {
           yield return Value.ToLowerInvariant();
       }

       public string GetDirectory() => Path.GetDirectoryName(Value)!;
       public string GetFileName() => Path.GetFileName(Value);
       public string GetExtension() => Path.GetExtension(Value);
   }

   // AiGaming context
   public class MemoryAddress : ValueObject
   {
       public long Value { get; }

       public MemoryAddress(long value)
       {
           if (value < 0)
               throw new ArgumentException("Memory address cannot be negative", nameof(value));
           Value = value;
       }

       protected override IEnumerable<object> GetEqualityComponents()
       {
           yield return Value;
       }

       public static MemoryAddress operator +(MemoryAddress left, int offset)
           => new(left.Value + offset);

       public override string ToString() => $"0x{Value:X}";

}

```

#### **Task 0.2.3: Event-Driven Architecture Foundation**
**Estimated Time**: 12 hours
**Dependencies**: Clean Architecture layers defined
**Deliverables**: Complete eventing infrastructure

**Steps**:
1. **Core Event Infrastructure**
```csharp
   // SaveState.Core/Common/Events/IEvent.cs
   public interface IEvent : INotification
   {
       Guid EventId { get; }
       DateTime OccurredOn { get; }
       string EventType => GetType().Name;
   }

   // SaveState.Core/Common/Events/EventBase.cs
   public abstract class EventBase : IEvent
   {
       public Guid EventId { get; } = Guid.NewGuid();
       public DateTime OccurredOn { get; } = DateTime.UtcNow;
   }

   // SaveState.Core/Common/Events/IEventHandler.cs
   public interface IEventHandler<in TEvent> : INotificationHandler<TEvent>
       where TEvent : IEvent
   {
   }
   ```

1. **Domain Events**

   ```csharp
   // GameLibrary domain events
   public class GameImportedEvent : EventBase

{
    public Guid GameId { get; }
    public string Source { get; }
       public string? SourceId { get; }
    public DateTime ImportedAt { get; }

       public GameImportedEvent(Guid gameId, string source, string? sourceId = null)
       {
           GameId = Guard.Against.Default(gameId, nameof(gameId));
           Source = Guard.Against.NullOrWhiteSpace(source, nameof(source));
           SourceId = sourceId;
           ImportedAt = DateTime.UtcNow;
       }
   }

   public class GameMetadataUpdatedEvent : EventBase
   {
       public Guid GameId { get; }
       public string? Description { get; }
       public IReadOnlyList<string> Tags { get; }

       public GameMetadataUpdatedEvent(Guid gameId, string? description, IEnumerable<string> tags)
       {
           GameId = Guard.Against.Default(gameId, nameof(gameId));
           Description = description;
           Tags = tags.ToList().AsReadOnly();
       }
   }

   // RomManagement domain events
   public class RomFileScannedEvent : EventBase
   {
       public Guid RomFileId { get; }
       public string FilePath { get; }
       public long FileSize { get; }
       public string Platform { get; }

       public RomFileScannedEvent(Guid romFileId, string filePath, long fileSize, string platform)
       {
           RomFileId = Guard.Against.Default(romFileId, nameof(romFileId));
           FilePath = Guard.Against.NullOrWhiteSpace(filePath, nameof(filePath));
           FileSize = Guard.Against.Negative(fileSize, nameof(fileSize));
           Platform = Guard.Against.NullOrWhiteSpace(platform, nameof(platform));
       }
   }

   // AiGaming domain events
   public class CheatDetectedEvent : EventBase
{
    public Guid ProcessId { get; }
    public string CheatType { get; }
    public float Confidence { get; }
       public IReadOnlyList<long> AffectedAddresses { get; }

       public CheatDetectedEvent(Guid processId, string cheatType, float confidence, IEnumerable<long> affectedAddresses)
       {
           ProcessId = Guard.Against.Default(processId, nameof(processId));
           CheatType = Guard.Against.NullOrWhiteSpace(cheatType, nameof(cheatType));
           Confidence = Guard.Against.OutOfRange(confidence, nameof(confidence), 0f, 1f);
           AffectedAddresses = affectedAddresses.ToList().AsReadOnly();
       }
   }

   ```

3. **Event Publishing Infrastructure**
   ```csharp
   // SaveState.Application/Common/Events/IEventPublisher.cs
   public interface IEventPublisher
   {
       Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
           where TEvent : IEvent;
       Task PublishAsync(IEnumerable<IEvent> events, CancellationToken ct = default);
   }

   // SaveState.Application/Common/Events/EventPublisher.cs
   public class EventPublisher : IEventPublisher
   {
       private readonly IMediator _mediator;
       private readonly ILogger<EventPublisher> _logger;

       public EventPublisher(IMediator mediator, ILogger<EventPublisher> logger)
       {
           _mediator = mediator;
           _logger = logger;
       }

       public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
           where TEvent : IEvent
       {
           _logger.LogInformation("Publishing event {EventType} with ID {EventId}",
               @event.EventType, @event.EventId);

           await _mediator.Publish(@event, ct);
       }

       public async Task PublishAsync(IEnumerable<IEvent> events, CancellationToken ct = default)
       {
           foreach (var @event in events)
           {
               await PublishAsync(@event, ct);
           }
       }
}
```

### **0.3 Configuration & Infrastructure**

#### **Task T-0.3.1: Configuration Architecture**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 10 hours |
| **Dependencies** | T-0.2.1 |
| **AI Turns** | 2-3 |
| **Files Created** | 5 |

**Assumes Exists:**

- Clean Architecture foundation from T-0.2.1

**Steps:**

1. **Core Configuration Classes**

📁 Create: `src/SaveState.Core/Common/Configuration/ApplicationOptions.cs`

```csharp
   // SaveState.Core/Common/Configuration/ApplicationOptions.cs
   public class ApplicationOptions : IValidatableObject
{
    public const string Section = "Application";

    public string ApplicationName { get; set; } = "SaveState Reborn";
    public string Version { get; set; } = "1.0.0";
    public string Environment { get; set; } = "Development";
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
       public bool EnableDetailedLogging { get; set; } = false;
       public string DataDirectory { get; set; } = "./data";

       public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
       {
           var results = new List<ValidationResult>();

           if (string.IsNullOrWhiteSpace(ApplicationName))
               results.Add(new ValidationResult("Application name is required", new[] { nameof(ApplicationName) }));

           if (DefaultTimeout <= TimeSpan.Zero)
               results.Add(new ValidationResult("Default timeout must be positive", new[] { nameof(DefaultTimeout) }));

           return results;
       }
   }

   // SaveState.Core/Common/Configuration/DatabaseOptions.cs
   public class DatabaseOptions : IValidatableObject
{
    public const string Section = "Database";

    public string ConnectionString { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
       public bool EnableSensitiveDataLogging { get; set; } = false;
       public bool EnableDetailedErrors { get; set; } = true;

       public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
       {
           var results = new List<ValidationResult>();

           if (string.IsNullOrWhiteSpace(ConnectionString))
               results.Add(new ValidationResult("Connection string is required", new[] { nameof(ConnectionString) }));

           if (CommandTimeoutSeconds <= 0)
               results.Add(new ValidationResult("Command timeout must be positive", new[] { nameof(CommandTimeoutSeconds) }));

           if (MaxRetryCount < 0)
               results.Add(new ValidationResult("Max retry count cannot be negative", new[] { nameof(MaxRetryCount) }));

           return results;
       }
   }

   // SaveState.Core/Common/Configuration/AiOptions.cs
   public class AiOptions : IValidatableObject
{
    public const string Section = "AI";

    public string PrimaryProvider { get; set; } = "OpenAI";
    public Dictionary<string, AiProviderOptions> Providers { get; set; } = new();
    public int MaxTokens { get; set; } = 2048;
    public float Temperature { get; set; } = 0.7f;
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConcurrentRequests { get; set; } = 5;
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
       public bool EnableFallbackProviders { get; set; } = true;

       public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
       {
           var results = new List<ValidationResult>();

           if (MaxTokens <= 0)
               results.Add(new ValidationResult("Max tokens must be positive", new[] { nameof(MaxTokens) }));

           if (Temperature < 0 || Temperature > 2)
               results.Add(new ValidationResult("Temperature must be between 0 and 2", new[] { nameof(Temperature) }));

           if (MaxConcurrentRequests <= 0)
               results.Add(new ValidationResult("Max concurrent requests must be positive", new[] { nameof(MaxConcurrentRequests) }));

           return results;
       }
   }

   // SaveState.Core/Common/Configuration/MemoryOptions.cs
   public class MemoryOptions : IValidatableObject
{
    public const string Section = "Memory";

    public int MaxEntries { get; set; } = 500;
    public int MaxTokens { get; set; } = 50000;
       public int PruneBatchSize { get; set; } = 50;
       public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromHours(1);
    public int MaxConcurrentScans { get; set; } = 3;
       public long MaxMemoryPressureBytes { get; set; } = 100 * 1024 * 1024; // 100MB
       public float MemoryPressureThreshold { get; set; } = 0.8f;

       public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
       {
           var results = new List<ValidationResult>();

           if (MaxEntries <= 0)
               results.Add(new ValidationResult("Max entries must be positive", new[] { nameof(MaxEntries) }));

           if (MaxTokens <= 0)
               results.Add(new ValidationResult("Max tokens must be positive", new[] { nameof(MaxTokens) }));

           if (DefaultTtl <= TimeSpan.Zero)
               results.Add(new ValidationResult("Default TTL must be positive", new[] { nameof(DefaultTtl) }));

           return results;
       }
   }
   ```

1. **Configuration Files**

   ```json
   // appsettings.json
   {
     "Application": {
       "ApplicationName": "SaveState Reborn",
       "Version": "1.0.0",
       "Environment": "Development",
       "DefaultTimeout": "00:00:30",
       "EnableDetailedLogging": false,
       "DataDirectory": "./data"
     },
     "Database": {
       "ConnectionString": "Data Source=savestate.db",
       "CommandTimeoutSeconds": 30,
       "MaxRetryCount": 3,
       "MaxRetryDelay": "00:00:30",
       "EnableSensitiveDataLogging": false,
       "EnableDetailedErrors": true
     },
     "AI": {
       "PrimaryProvider": "OpenAI",
       "MaxTokens": 2048,
       "Temperature": 0.7,
       "RequestTimeout": "00:00:30",
       "MaxConcurrentRequests": 5,
       "EnableFallbackProviders": true,
       "CircuitBreaker": {
         "Threshold": 5,
         "DurationMs": 60000,
         "TimeoutMs": 30000
       },
       "Providers": {
         "OpenAI": {
           "ApiKey": "${OPENAI_API_KEY}",
           "BaseUrl": "https://api.openai.com/v1",
           "Models": {
             "gpt-4": { "MaxTokens": 8192, "CostPerToken": 0.00003 },
             "gpt-3.5-turbo": { "MaxTokens": 4096, "CostPerToken": 0.000002 }
           }
         }
       }
     },
     "Memory": {
       "MaxEntries": 500,
       "MaxTokens": 50000,
       "PruneBatchSize": 50,
       "DefaultTtl": "01:00:00",
       "MaxConcurrentScans": 3,
       "MaxMemoryPressureBytes": 104857600,
       "MemoryPressureThreshold": 0.8
     }
   }

// appsettings.Development.json
   {
     "Application": {
       "EnableDetailedLogging": true
     },
     "Database": {
       "EnableSensitiveDataLogging": true
     }
   }

// appsettings.Production.json
   {
     "Application": {
       "Environment": "Production"
     },
     "Database": {
       "EnableDetailedErrors": false
     }
   }

```

#### **Task T-0.3.2: Dependency Injection Setup**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 8 hours |
| **Dependencies** | T-0.3.1 |
| **AI Turns** | 2 |
| **Files Created** | 3 |

**Assumes Exists:**
- Configuration system from T-0.3.1

**Steps:**

1. **Service Registration Infrastructure**

📁 Create: `src/SaveState.Application/Common/DependencyInjection/ServiceCollectionExtensions.cs`
```csharp
   // SaveState.Application/Common/DependencyInjection/ServiceCollectionExtensions.cs
   public static class ServiceCollectionExtensions
   {
       public static IServiceCollection AddApplicationServices(this IServiceCollection services)
       {
           // Register MediatR
           services.AddMediatR(cfg => {
               cfg.RegisterServicesFromAssemblies(
                   typeof(ServiceCollectionExtensions).Assembly,
                   typeof(GameLibraryContext).Assembly);
               cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
               cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
           });

           // Register event publisher
           services.AddScoped<IEventPublisher, EventPublisher>();

           // Register application services
           services.AddScoped<GameImportService>();
           services.AddScoped<RomScannerService>();
           services.AddScoped<AiOrchestrator>();

           return services;
       }

       public static IServiceCollection AddInfrastructureServices(
           this IServiceCollection services,
           IConfiguration configuration)
       {
           // Database
           services.AddDbContext<SaveStateDbContext>((sp, options) => {
               var dbOptions = sp.GetRequiredService<IOptions<DatabaseOptions>>();
               options.UseSqlite(dbOptions.Value.ConnectionString)
                      .EnableSensitiveDataLogging(dbOptions.Value.EnableSensitiveDataLogging)
                      .EnableDetailedErrors(dbOptions.Value.EnableDetailedErrors)
                      .CommandTimeout(dbOptions.Value.CommandTimeoutSeconds);
           });

           // HTTP clients with resilience
           services.AddHttpClient("OpenAI", (sp, client) => {
               var aiOptions = sp.GetRequiredService<IOptions<AiOptions>>();
               var provider = aiOptions.Value.Providers["OpenAI"];
               client.BaseAddress = new Uri(provider.BaseUrl);
               client.DefaultRequestHeaders.Add("Authorization", $"Bearer {provider.ApiKey}");
           }).AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

           // Caching
           services.AddMemoryCache();
           services.AddScoped<ICacheManager, CacheManager>();

           // External API clients
           services.AddScoped<ISteamApiClient, SteamApiClient>();
           services.AddScoped<IGogApiClient, GogApiClient>();
           services.AddScoped<IgdbApiClient>();

           return services;
       }

       private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
       {
           return HttpPolicyExtensions
               .HandleTransientHttpError()
               .WaitAndRetryAsync(3, retryAttempt =>
                   TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
       }

       private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
       {
           return HttpPolicyExtensions
               .HandleTransientHttpError()
               .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
       }
   }
   ```

1. **Application Bootstrap**

   ```csharp
   // SaveState.Presentation/Program.cs

public static class Program
{
       [STAThread]
    public static async Task Main(string[] args)
    {
           var builder = Host.CreateApplicationBuilder(args);

           // Configure configuration
           builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
           builder.Configuration.AddJsonFile(
               $"appsettings.{builder.Environment.EnvironmentName}.json",
               optional: true,
               reloadOnChange: true);
           builder.Configuration.AddEnvironmentVariables();
           builder.Configuration.AddCommandLine(args);

           // Configure logging
           builder.Logging.ClearProviders();
           builder.Logging.AddSerilog(disposableLogger => {
               disposableLogger.ReadFrom.Configuration(builder.Configuration);
           });

           // Configure services
           builder.Services.ConfigureOptions<ApplicationOptions>();
           builder.Services.ConfigureOptions<DatabaseOptions>();
           builder.Services.ConfigureOptions<AiOptions>();
           builder.Services.ConfigureOptions<MemoryOptions>();

           // Add application services
           builder.Services.AddApplicationServices();
           builder.Services.AddInfrastructureServices(builder.Configuration);

           // Add presentation services
           builder.Services.AddAvaloniaServices();
           builder.Services.AddReactiveUI();

           var host = builder.Build();

           // Initialize database
           using (var scope = host.Services.CreateScope())
           {
               var dbContext = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();
               await dbContext.Database.MigrateAsync();
           }

           // Run the application
           await host.RunAvaloniaAppAsync<App>(args);
       }
   }

   ```

#### **Task T-0.3.3: Logging and Monitoring Setup**

| Attribute | Value |
|:---|:---|
| **Estimated Time** | 6 hours |
| **Dependencies** | T-0.3.2 |
| **AI Turns** | 2 |
| **Files Created** | 3 |

**Assumes Exists:**
- DI container from T-0.3.2

**Steps:**

1. **Structured Logging Configuration**

📁 Create: `src/SaveState.Core/Common/Logging/LoggingExtensions.cs`
```csharp
   // SaveState.Core/Common/Logging/LoggingExtensions.cs
   public static class LoggingExtensions
   {
       public static IHostBuilder ConfigureSerilog(this IHostBuilder builder)
       {
           return builder.UseSerilog((context, services, configuration) => {
               configuration
                   .ReadFrom.Configuration(context.Configuration)
                   .ReadFrom.Services(services)
                   .Enrich.FromLogContext()
                   .Enrich.WithMachineName()
                   .Enrich.WithThreadId()
                   .Enrich.WithCorrelationId()
                   .WriteTo.Console(
                       outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                   .WriteTo.File(
                       path: "logs/savestate-.log",
                       rollingInterval: RollingInterval.Day,
                       outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                   .WriteTo.Seq(serverUrl: context.Configuration["Seq:Url"])
                   .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning);
           });
       }
   }
   ```

1. **Health Checks**

   ```csharp
   // SaveState.Application/Common/Health/DatabaseHealthCheck.cs
   public class DatabaseHealthCheck : IHealthCheck
   {
       private readonly SaveStateDbContext _dbContext;

       public DatabaseHealthCheck(SaveStateDbContext dbContext)
       {
           _dbContext = dbContext;
       }

       public async Task<HealthCheckResult> CheckHealthAsync(
           HealthCheckContext context,
           CancellationToken cancellationToken = default)
       {
           try
           {
               // Simple query to check database connectivity
               var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);

               if (!canConnect)
               {
                   return HealthCheckResult.Unhealthy("Cannot connect to database");
               }

               // Check if migrations are applied
               var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
               if (pendingMigrations.Any())
               {
                   return HealthCheckResult.Degraded(
                       $"Database has {pendingMigrations.Count()} pending migrations",
                       data: new Dictionary<string, object>
                       {
                           ["PendingMigrations"] = pendingMigrations
                       });
               }

               return HealthCheckResult.Healthy("Database is healthy");
           }
           catch (Exception ex)
           {
               return HealthCheckResult.Unhealthy("Database check failed", ex);
           }
       }
   }

   // SaveState.Application/Common/Health/AiServiceHealthCheck.cs
   public class AiServiceHealthCheck : IHealthCheck
   {
       private readonly IAiOrchestrator _aiOrchestrator;

       public AiServiceHealthCheck(IAiOrchestrator aiOrchestrator)
       {
           _aiOrchestrator = aiOrchestrator;
       }

       public async Task<HealthCheckResult> CheckHealthAsync(
           HealthCheckContext context,
           CancellationToken cancellationToken = default)
       {
           try
           {
               // Simple AI health check - could be a ping or simple completion
               var healthRequest = new AiRequest
               {
                   Type = AiRequestType.Completion,
                   Prompt = "Hello",
                   MaxTokens = 1
               };

               var result = await _aiOrchestrator.ProcessRequestAsync(healthRequest, cancellationToken);

               return result.IsSuccessful
                   ? HealthCheckResult.Healthy("AI service is responding")
                   : HealthCheckResult.Unhealthy("AI service returned error");
           }
           catch (Exception ex)
           {
               return HealthCheckResult.Unhealthy("AI service check failed", ex);
           }
    }

}

```



---

## ✅ Phase 0 Completion Checklist

- [ ] T-0.1.1 Repository Initialization
- [ ] T-0.1.2 Development Environment Setup
- [ ] T-0.1.3 CI/CD Pipeline Implementation
- [ ] T-0.1.4 Architecture Decision Records Setup
- [ ] T-0.1.5 Walking Skeleton Milestone
- [ ] T-0.2.1 Clean Architecture Implementation
- [ ] T-0.2.2 Bounded Contexts Definition
- [ ] T-0.3.1 Configuration System
- [ ] T-0.3.2 Dependency Injection Setup
- [ ] T-0.3.3 Logging & Monitoring

**Phase 0 Complete When:**
- `dotnet build SaveStateReborn.sln` → 0 errors, 0 warnings
- `dotnet test` → All tests pass
- Walking Skeleton displays "Test Game" from SQLite
- CI/CD pipeline runs on push to `develop`
- All ADR documents committed

**Rollback Checkpoint:**
```bash
git tag rebuild-phase0-complete
git push origin rebuild-phase0-complete
```

---

**📍 Next:** [Phase 1: Core Infrastructure](./phase-1-core-infrastructure.md)
