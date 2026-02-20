# Contributing to SaveStateReborn

Thank you for your interest in contributing! This guide will help you get started.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Git](https://git-scm.com/)
- (Optional) [pre-commit](https://pre-commit.com/) for automated checks

## Setting Up Development Environment

### 1. Clone the Repository

```bash
git clone https://github.com/DocDamage/Save_State-Gaming-Hub.git
cd Save_State-Gaming-Hub
```

### 2. Install Pre-Commit Hooks

We use pre-commit hooks to ensure code quality. Install them with:

```powershell
# Windows
.\scripts\install-hooks.ps1

# Or manually with pre-commit
pre-commit install
```

The hooks will check:
- ✅ No `return null` in public APIs (use Result pattern)
- ✅ No `DateTime.Now` usage (use ITimeProvider)
- ✅ Async methods end with `Async` suffix
- ✅ No unnecessary null-forgiving operators (`!`)
- ✅ Build with 0 warnings
- ✅ Architecture tests pass

### 3. Build the Solution

```bash
dotnet build SaveStateReborn.Core.sln
```

### 4. Run Tests

```bash
dotnet test SaveStateReborn.Core.sln
```

## Code Standards

### Required Patterns

1. **Result Pattern**: All public methods that can fail must return `Result<T>`
   ```csharp
   public async Task<Result<Game>> GetGameAsync(int id)
   {
       var game = await _repository.GetByIdAsync(id);
       if (game is null)
           return Result<Game>.Failure($"Game {id} not found", ErrorType.NotFound);
       return Result<Game>.Success(game);
   }
   ```

2. **ITimeProvider**: Never use `DateTime.Now`, always inject `ITimeProvider`
   ```csharp
   public class MyService
   {
       private readonly ITimeProvider _timeProvider;
       
       public MyService(ITimeProvider timeProvider)
       {
           _timeProvider = timeProvider;
       }
       
       public void DoWork()
       {
           var now = _timeProvider.Now; // ✅ Correct
           // var now = DateTime.Now; // ❌ Wrong
       }
   }
   ```

3. **Async Naming**: All async methods must end with `Async` suffix
   ```csharp
   public async Task<Result<Game>> GetGameAsync(int id) { } // ✅ Correct
   public async Task<Result<Game>> GetGame(int id) { } // ❌ Wrong
   ```

### Pull Request Process

1. **Create a Branch**
   ```bash
   git checkout -b feature/my-feature-name
   ```

2. **Make Changes**
   - Follow code standards above
   - Add tests for new functionality
   - Update documentation if needed

3. **Run Pre-Commit Checks**
   ```bash
   # Hooks run automatically on commit
   git commit -m "feat: my feature"
   
   # Or run manually
   .\scripts\pre-commit.ps1
   ```

4. **Push and Create PR**
   ```bash
   git push origin feature/my-feature-name
   ```
   
   Then create a Pull Request on GitHub. The PR will be checked by:
   - Build verification (0 warnings)
   - Architecture tests
   - Code quality tests
   - Unit tests
   - Code coverage (80% threshold)
   - Vulnerability scanning

5. **Code Review**
   - Address review comments
   - Ensure CI checks pass
   - Get approval from maintainers

## Commit Message Guidelines

We follow [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `style:` Code style (formatting, no logic change)
- `refactor:` Code refactoring
- `test:` Test changes
- `chore:` Build, tooling, dependencies

Examples:
```
feat(memory): Add freeze functionality for game values
fix(ui): Resolve crash in GameMemoryView
refactor(services): Split GameMemoryReader into managers
docs: Update Memory Intelligence guide
```

## Getting Help

- Check [AGENTS.md](../../AGENTS.md) for architecture patterns
- Read [MEMORY_INTELLIGENCE.md](MEMORY_INTELLIGENCE.md) for memory system details
- Open an issue for bugs or feature requests
