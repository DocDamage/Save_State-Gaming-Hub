# Troubleshooting Encyclopedia

Comprehensive solutions for every error you might encounter.

---

[← Back to README](./README.md) | [Architecture Reference](./architecture-reference.md)

---

## **🔴 Build Errors (CS0XXX)**

### **CS0246: The type or namespace name could not be found**

**Symptom:**

```
error CS0246: The type or namespace name 'IGameRepository' could not be found
```

**Causes & Solutions:**

| Cause | Solution |
|:---|:---|
| Missing `using` statement | Add `using SaveState.Core.Interfaces;` |
| Missing project reference | Add `<ProjectReference Include="../SaveState.Core/SaveState.Core.csproj"/>` |
| Missing NuGet package | Run `dotnet add package PackageName` |
| Typo in type name | Check spelling matches interface/class definition |
| File not saved | Save all files and rebuild |

**Quick Fix Script:**

```bash
# Find where the type is defined
grep -rn "interface IGameRepository" src/
grep -rn "class GameRepository" src/

# Add missing reference
dotnet add src/SaveState.Application reference src/SaveState.Core
```

---

### **CS0535: Does not implement interface member**

**Symptom:**

```
error CS0535: 'GameRepository' does not implement interface member 'IGameRepository.GetByIdAsync'
```

**Causes & Solutions:**

| Cause | Solution |
|:---|:---|
| Missing method | Add the missing method to the class |
| Wrong signature | Match exact parameter types and return type |
| Async mismatch | Ensure Task<T> return type for async methods |
| Generic mismatch | Match generic type parameters exactly |

**Example Fix:**

```csharp
// Interface defines:
Task<Game?> GetByIdAsync(GameId id, CancellationToken ct);

// Implementation must match EXACTLY:
public async Task<Game?> GetByIdAsync(GameId id, CancellationToken ct)
{
    return await _context.Games.FindAsync(new object[] { id }, ct);
}
```

---

### **CS1061: Does not contain a definition for**

**Symptom:**

```
error CS1061: 'Game' does not contain a definition for 'UpdateTitle'
```

**Causes & Solutions:**

| Cause | Solution |
|:---|:---|
| Method doesn't exist | Add the method to the class |
| Wrong class referenced | Check you have the correct type |
| Extension method missing | Add `using` for extension method namespace |
| Property vs method | Check if it's `game.Title` vs `game.GetTitle()` |

---

### **CS0103: The name does not exist in the current context**

**Symptom:**

```
error CS0103: The name '_logger' does not exist in the current context
```

**Causes & Solutions:**

| Cause | Solution |
|:---|:---|
| Field not declared | Add `private readonly ILogger<T> _logger;` |
| Not injected | Add parameter to constructor |
| Scope issue | Check variable is declared in accessible scope |
| Typo | Check spelling matches declaration |

---

### **CS7036: Required formal parameter not provided**

**Symptom:**

```
error CS7036: There is no argument given that corresponds to the required formal parameter 'logger'
```

**Solutions:**

```csharp
// Wrong:
var service = new GameService(repository);

// Correct:
var service = new GameService(repository, logger);

// Or with DI:
services.AddScoped<IGameService, GameService>(); // DI resolves all parameters
```

---

## **🟡 Runtime Errors**

### **NullReferenceException**

**Symptom:**

```
System.NullReferenceException: Object reference not set to an instance of an object.
```

**Debugging Steps:**

1. **Find the null object:**

```csharp
// Add null checks
if (game is null)
    throw new ArgumentNullException(nameof(game), "Game was null");

// Or use null-conditional
var title = game?.Title ?? "Unknown";
```

1. **Check DI registration:**

```csharp
// Ensure service is registered
services.AddScoped<IGameService, GameService>();

// Check injection
public class Handler
{
    private readonly IGameService _gameService; // Could be null if not registered
}
```

1. **Check async operations:**

```csharp
// Wrong - may return null
var game = _context.Games.FirstOrDefault(g => g.Id == id);
game.Title = newTitle; // NullReferenceException if not found!

// Correct
var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == id, ct);
if (game is null)
    throw new EntityNotFoundException(nameof(Game), id);
```

---

### **InvalidOperationException: Unable to resolve service**

**Symptom:**

```
InvalidOperationException: Unable to resolve service for type 'IGameRepository'
while attempting to activate 'CreateGameCommandHandler'.
```

**Solutions:**

1. **Register the service:**

```csharp
// In DependencyInjection.cs
services.AddScoped<IGameRepository, GameRepository>();
```

1. **Check service lifetime:**

```csharp
// Singleton can't depend on Scoped
services.AddSingleton<ICache, MemoryCache>();      // OK
services.AddScoped<IRepository, Repository>();      // OK
services.AddSingleton<IService, Service>();         // ERROR if Service depends on Scoped
```

1. **Check circular dependencies:**

```csharp
// A depends on B, B depends on A = Error
// Solution: Use Lazy<T> or refactor
```

---

### **HttpRequestException: 401 Unauthorized**

**Symptom:**

```
HttpRequestException: Response status code does not indicate success: 401 (Unauthorized)
```

**Solutions:**

1. **Check API key:**

```bash
dotnet user-secrets list
# Verify OpenAi:ApiKey is set correctly
```

1. **Check header format:**

```csharp
// Correct format for OpenAI
client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

// NOT this:
client.DefaultRequestHeaders.Add("Authorization", apiKey); // Wrong!
```

1. **Check API key validity:**

- Verify key hasn't expired
- Verify key has correct permissions
- Check rate limits haven't been exceeded

---

### **SqliteException: no such table**

**Symptom:**

```
Microsoft.Data.Sqlite.SqliteException: SQLite Error 1: 'no such table: Games'
```

**Solutions:**

```bash
# Apply pending migrations
cd src/SaveState.Infrastructure
dotnet ef database update --startup-project ../SaveState.App

# If migrations are corrupted, reset
rm ../SaveState.App/SaveState.db
dotnet ef database update --startup-project ../SaveState.App

# Verify tables exist
sqlite3 ../SaveState.App/SaveState.db ".tables"
```

---

### **TaskCanceledException / OperationCanceledException**

**Symptom:**

```
TaskCanceledException: A task was canceled.
```

**Causes & Solutions:**

| Cause | Solution |
|:---|:---|
| HTTP timeout | Increase timeout in HttpClient configuration |
| Cancellation token triggered | Expected behavior - handle gracefully |
| Circuit breaker open | Wait for circuit to reset, use fallback |
| Deadlock | Use `ConfigureAwait(false)` or async all the way |

---

## **🟢 Test Failures**

### **Mock Not Returning Expected Value**

**Symptom:**

```
Expected result to be "Half-Life 2", but got null.
```

**Solutions:**

```csharp
// Wrong - Setup doesn't match actual call
_mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<GameId>(), CancellationToken.None))
    .ReturnsAsync(testGame);

// Actual call uses different CancellationToken
var result = await _sut.GetGameAsync(gameId, cancellationToken); // Different token!

// Correct - Use It.IsAny for all parameters
_mockRepo.Setup(x => x.GetByIdAsync(It.IsAny<GameId>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(testGame);
```

---

### **DbContext Already Disposed**

**Symptom:**

```
ObjectDisposedException: Cannot access a disposed context instance
```

**Solutions:**

```csharp
// Wrong - Context disposed before async completes
using (var context = new SaveStateDbContext(options))
{
    return context.Games.ToListAsync(); // Context disposed before await!
}

// Correct - Await inside using
using (var context = new SaveStateDbContext(options))
{
    return await context.Games.ToListAsync();
}

// Or for tests - Don't share context between tests
public class Tests : IAsyncLifetime
{
    private SaveStateDbContext _context = null!;

    public async Task InitializeAsync()
    {
        _context = new SaveStateDbContext(CreateOptions());
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }
}
```

---

### **Async Test Timeout/Deadlock**

**Symptom:**

```
Test timed out after 30000ms
```

**Solutions:**

```csharp
// Wrong - Blocking on async
var result = _sut.GetGamesAsync().Result; // DEADLOCK!

// Correct - Async all the way
var result = await _sut.GetGamesAsync();

// Wrong - Missing async/await
public void Test()
{
    _sut.DoSomethingAsync(); // Fire and forget!
}

// Correct
public async Task Test()
{
    await _sut.DoSomethingAsync();
}
```

---

## **🔵 EF Core / Database Errors**

### **Migration Conflicts**

**Symptom:**

```
The current database model matches the most recent migration but it has not been applied
```

**Solutions:**

```bash
# Remove last migration (if not applied)
dotnet ef migrations remove --startup-project ../SaveState.App

# Force remove all migrations and start fresh
rm -rf Migrations/
dotnet ef migrations add InitialCreate --startup-project ../SaveState.App
dotnet ef database update --startup-project ../SaveState.App
```

---

### **Concurrency Conflict**

**Symptom:**

```
DbUpdateConcurrencyException: The database operation was expected to affect 1 row(s), but actually affected 0 row(s)
```

**Solutions:**

```csharp
// Add concurrency token to entity
public class Game
{
    public Guid Id { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}

// Handle in code
try
{
    await _context.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    // Reload and retry, or notify user
    await entry.ReloadAsync(ct);
    throw new ConcurrencyException("Data was modified by another user");
}
```

---

### **N+1 Query Problem**

**Symptom:**

```
Slow performance, many small queries in logs
```

**Solutions:**

```csharp
// Wrong - N+1 queries
var games = await _context.Games.ToListAsync();
foreach (var game in games)
{
    var platform = game.Platform; // Lazy load = 1 query per game!
}

// Correct - Eager loading
var games = await _context.Games
    .Include(g => g.Platform)
    .Include(g => g.Tags)
    .ToListAsync();

// Or - Explicit loading when needed
var games = await _context.Games.ToListAsync();
await _context.Entry(games[0]).Reference(g => g.Platform).LoadAsync();
```

---

## **🟣 AI / HTTP Errors**

### **Circuit Breaker Open**

**Symptom:**

```
BrokenCircuitException: The circuit is now open and is not allowing calls
```

**Solutions:**

1. **Wait for circuit to reset** (default 60 seconds)
2. **Use fallback provider:**

```csharp
var response = await _orchestrator.ProcessRequestAsync(
    request with { PreferredProvider = "Groq" }, // Try different provider
    ct);
```

3. **Check underlying issue** (rate limits, API down, etc.)

---

### **Rate Limited (429)**

**Symptom:**

```
HttpRequestException: Response status code: 429 (Too Many Requests)
```

**Solutions:**

1. **Implement rate limiting:**

```csharp
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("ai-requests", limiter =>
    {
        limiter.PermitLimit = 60;
        limiter.Window = TimeSpan.FromMinutes(1);
    });
});
```

1. **Add retry with backoff:**

```csharp
Policy
    .Handle<HttpRequestException>(ex =>
        ex.StatusCode == HttpStatusCode.TooManyRequests)
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
```

---

### **Token Limit Exceeded**

**Symptom:**

```
BadRequestException: This model's maximum context length is 4096 tokens
```

**Solutions:**

1. **Truncate input:**

```csharp
public string TruncateToTokenLimit(string text, int maxTokens)
{
    var estimatedTokens = text.Length / 4;
    if (estimatedTokens <= maxTokens)
        return text;

    var targetLength = maxTokens * 4;
    return text[..targetLength] + "...";
}
```

1. **Use a model with higher limits:**

```csharp
var request = new ChatRequest(messages, "gpt-4-32k", maxTokens: 4000);
```

---

## **📋 Error Code Quick Reference**

| Code | Type | Quick Fix |
|:---|:---|:---|
| CS0246 | Missing type | Add `using` or NuGet package |
| CS0535 | Interface not implemented | Add missing method |
| CS1061 | Method not found | Check method exists on type |
| CS0103 | Variable not found | Check declaration and scope |
| CS7036 | Missing parameter | Add all required parameters |
| CS0029 | Cannot convert | Check type compatibility |
| CS0266 | Cannot convert implicitly | Add explicit cast |
| CS0121 | Ambiguous call | Specify which overload |
| CS0019 | Operator cannot be applied | Check operand types |
| CS0117 | Does not contain definition | Check static vs instance |

---

## **🛠️ Diagnostic Commands**

```bash
# Check .NET version
dotnet --info

# List installed tools
dotnet tool list -g

# Clean and rebuild
dotnet clean && dotnet build

# Restore packages
dotnet restore --force

# Check for outdated packages
dotnet list package --outdated

# View EF migrations
dotnet ef migrations list --startup-project ../SaveState.App

# Check database
sqlite3 SaveState.db ".schema"

# View recent logs
Get-Content logs/savestate-*.log -Tail 50

# Find where type is defined
grep -rn "class GameRepository" src/
```

---

**Still stuck? Check the [Architecture Reference](./architecture-reference.md) or ask in the issues.**
