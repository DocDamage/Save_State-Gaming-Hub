# Quick Start Guide

Get up and running with the SaveState rebuild in 15 minutes.

---

[← Back to README](./README.md)

---

## **⚡ Prerequisites Checklist**

Before starting, ensure you have:

- [ ] **.NET 9 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/9.0)
- [ ] **Git** - [Download](https://git-scm.com/downloads)
- [ ] **Visual Studio 2022** or **VS Code** with C# extension
- [ ] **Windows 10/11** (for Avalonia UI testing)

Verify installation:

```bash
dotnet --version    # Should show 9.0.x
git --version       # Should show 2.x.x
```

---

## **🚀 Step 1: Clone & Setup (5 minutes)**

```bash
# Clone the repository
git clone https://github.com/DocDamage/SaveState-Gaming-Hub.git SaveStateReborn
cd SaveStateReborn

# Checkout the rebuild branch
git checkout SaveState2

# Create working branch
git checkout -b rebuild-phase0

# Restore packages
dotnet restore
```

---

## **🔧 Step 2: Configure User Secrets (2 minutes)**

API keys should NEVER be in source code. Use user secrets:

```bash
cd src/SaveState.App

# Initialize user secrets
dotnet user-secrets init

# Add API keys (replace with your actual keys)
dotnet user-secrets set "OpenAi:ApiKey" "sk-your-openai-key"
dotnet user-secrets set "Groq:ApiKey" "gsk_your-groq-key"
dotnet user-secrets set "Steam:ApiKey" "your-steam-api-key"
dotnet user-secrets set "Igdb:ClientId" "your-igdb-client-id"
dotnet user-secrets set "Igdb:ClientSecret" "your-igdb-client-secret"

# Verify secrets are stored
dotnet user-secrets list
```

---

## **🗄️ Step 3: Setup Database (2 minutes)**

```bash
# Install EF Core tools (if not installed)
dotnet tool install --global dotnet-ef

# Navigate to Infrastructure project
cd src/SaveState.Infrastructure

# Create initial migration
dotnet ef migrations add InitialCreate --startup-project ../SaveState.App

# Apply migration
dotnet ef database update --startup-project ../SaveState.App

# Verify database exists
ls ../SaveState.App/SaveState.db
```

---

## **✅ Step 4: Verify Build (2 minutes)**

```bash
# Build entire solution
cd ../..
dotnet build SaveStateReborn.sln

# Expected output:
# Build succeeded.
#     0 Warning(s)
#     0 Error(s)
```

If build fails, see [Debugging Guide](./architecture-reference.md#-debugging-guide).

---

## **🧪 Step 5: Run Tests (2 minutes)**

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter "Category=Unit"
```

---

## **🎮 Step 6: Run Application (2 minutes)**

```bash
# Run the application
dotnet run --project src/SaveState.App

# Or with hot reload
dotnet watch run --project src/SaveState.App
```

---

## **📋 First Task: T-0.1.1**

You're ready to start! Open the first task:

1. Open [Phase 0: Foundation](./phase-0-foundation.md)
2. Find **Task T-0.1.1: Repository Initialization**
3. Follow the steps exactly
4. Run the ✅ Verify commands
5. If errors occur, follow 🔧 If Fails instructions

---

## **🔄 Development Workflow**

### **Per-Task Workflow**

```bash
# 1. Create branch for task
git checkout -b task/T-0.1.1-repo-init

# 2. Implement task following phase document

# 3. Verify build
dotnet build

# 4. Run tests
dotnet test

# 5. Commit changes
git add .
git commit -m "feat(T-0.1.1): Repository initialization complete"

# 6. Push branch
git push -u origin task/T-0.1.1-repo-init

# 7. Merge to main branch
git checkout rebuild-phase0
git merge task/T-0.1.1-repo-init
```

### **Per-Phase Workflow**

```bash
# After completing all tasks in a phase:

# 1. Run full test suite
dotnet test

# 2. Check code coverage
dotnet test --collect:"XPlat Code Coverage"

# 3. Create phase tag
git tag rebuild-phase0-complete

# 4. Push tag
git push origin rebuild-phase0-complete

# 5. Start next phase
git checkout -b rebuild-phase1
```

---

## **📁 Project Structure**

```
SaveStateReborn/
├── src/
│   ├── SaveState.Core/           # Domain entities, interfaces, events
│   ├── SaveState.Application/    # Commands, queries, handlers, DTOs
│   ├── SaveState.Infrastructure/ # EF Core, repositories, external APIs
│   ├── SaveState.Presentation/   # Avalonia views, view models
│   └── SaveState.App/            # Entry point, DI configuration
├── tests/
│   ├── SaveState.Core.Tests/     # Unit tests for domain
│   ├── SaveState.Application.Tests/ # Unit tests for handlers
│   └── SaveState.IntegrationTests/  # Database integration tests
├── docs/
│   └── rebuild/                  # This documentation
├── tools/
│   └── SaveState.Benchmarks/     # Performance benchmarks
└── SaveStateReborn.sln
```

---

## **🆘 Common Issues**

### **Build Errors**

| Error | Solution |
|:---|:---|
| `CS0246: Type not found` | Add missing `using` statement or NuGet package |
| `NETSDK1004: Assets file not found` | Run `dotnet restore` |
| `error MSB4025: Project file not found` | Check `.csproj` paths in solution |

### **Database Errors**

| Error | Solution |
|:---|:---|
| `No DbContext was found` | Register `SaveStateDbContext` in DI |
| `ef: command not found` | Run `dotnet tool install --global dotnet-ef` |
| `SQLite Error: no such table` | Run `dotnet ef database update` |

### **Runtime Errors**

| Error | Solution |
|:---|:---|
| `InvalidOperationException: Unable to resolve service` | Check DI registration in `DependencyInjection.cs` |
| `HttpRequestException: 401 Unauthorized` | Check API key in user secrets |
| `FileNotFoundException` | Check file paths and working directory |

---

## **📚 Documentation Index**

| Document | Description |
|:---|:---|
| [README.md](./README.md) | Master index, task overview, conventions |
| [Common Infrastructure](./common-infrastructure.md) | Exceptions, DTOs, DI, test patterns |
| [Architecture Reference](./architecture-reference.md) | Diagrams, ERD, event catalog, debugging |
| [Phase 0](./phase-0-foundation.md) | Foundation & governance tasks |
| [Phase 1](./phase-1-core-infrastructure.md) | Core domain & infrastructure tasks |
| [Phase 2](./phase-2-game-library.md) | Game discovery & ROM management |
| [Phase 3](./phase-3-ai-integration.md) | AI pipeline & memory systems |
| [Phase 4/5](./phase-4-5-polish.md) | Advanced features & polish |

---

## **💬 AI Assistant Prompt**

When working with an AI coding assistant, use this prompt template:

```
I'm working on the SaveState Reborn rebuild project.

Current task: T-0.1.1 from phase-0-foundation.md
Current branch: task/T-0.1.1-repo-init

Please implement this task following these rules:
1. Create all files listed under "📁 Create:"
2. Add DI registration to DependencyInjection.cs
3. Create unit test stubs for new services
4. Run the "✅ Verify" commands
5. If errors occur, follow "🔧 If Fails" instructions

Reference documents:
- docs/rebuild/phase-0-foundation.md
- docs/rebuild/common-infrastructure.md
- docs/rebuild/architecture-reference.md
```

---

## **🎯 Success Criteria**

You've completed the setup successfully when:

- [ ] `dotnet build` succeeds with 0 errors
- [ ] `dotnet test` passes all tests
- [ ] `dotnet run --project src/SaveState.App` starts without errors
- [ ] User secrets are configured
- [ ] Database file exists and is accessible

---

**🚀 You're ready to start the rebuild! Begin with [Phase 0](./phase-0-foundation.md).**
