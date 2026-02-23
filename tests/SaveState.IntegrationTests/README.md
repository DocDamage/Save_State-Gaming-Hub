# SaveStateReborn Integration Tests

This directory contains comprehensive integration tests for all major features implemented in Phases 1-5 and Tier 4 of SaveStateReborn.

## Test Suite Overview

### 1. Tournament Management Tests (`Esports/TournamentManagementTests.cs`)
- Tournament creation, update, and deletion
- Participant registration and check-in
- Bracket generation (Single/D elimination, 8/16/32 players)
- Match scheduling and result reporting
- Tournament lifecycle (start, pause, resume, complete, cancel)
- Standings and statistics
- Filtering by status, format, and date range

### 2. Mobile Companion Tests (`MobileCompanion/MobileCompanionTests.cs`)
- Pairing code generation and device pairing
- QR code generation
- Connection management (connect/disconnect)
- Session management and control mode switching
- Remote command execution (gamepad, touchpad, keyboard, media)
- Push notifications (single device and broadcast)
- Input handling (gamepad, touchpad, keyboard)
- Library sync and system status

### 3. Cloud Gaming Tests (`CloudGaming/CloudGamingTests.cs`)
- Provider connection management
- Game library synchronization
- Cloud session management (start, stop, resume)
- Stream quality settings (Low, Medium, High, Ultra)
- Connection testing and metrics (latency, packet loss)
- Network quality monitoring
- Data center selection
- Cloud save state integration

### 4. RGB Sync Tests (`RgbSync/RgbSyncTests.cs`)
- Device discovery and connection
- Effect application (Static, Breathing, Rainbow, Wave, etc.)
- Profile CRUD operations
- Import/export functionality
- Sync groups (create, update, add/remove devices)
- Game state triggers (health, level up, boss, victory, etc.)
- Provider management

### 5. Web Browser Tests (`WebBrowser/WebBrowserTests.cs`)
- Tab management (create, close, switch, pin, duplicate, mute)
- Navigation (navigate, back, forward, refresh, stop)
- Zoom level control
- OAuth flow initiation and callback handling
- Download management (list, cancel, pause, resume)
- Bookmark management (add, update, delete)
- History management
- Browser settings
- Find in page
- Cookie management
- Extension management

### 6. Theme System Tests (`Theme/ThemeSystemTests.cs`)
- Theme CRUD operations
- Theme application and preview
- Import/export (JSON, XML)
- Material You generation from seed color/wallpaper
- Color contrast checking (WCAG AA/AAA)
- Color blindness simulation
- Typography and effects customization
- Preset theme management

### 7. Voice Command Tests (`Voice/VoiceCommandTests.cs`)
- State transitions (start/stop listening)
- Listening status events
- Command processing
- Voice command recognized events
- Command registration/unregistration
- Voice model training
- Error handling

### 8. Database Tests (`Database/DatabaseTests.cs`)
- Migration compatibility
- Repository pattern (CRUD operations, filtering, pagination, ordering)
- Transaction handling (commit, rollback, multiple operations)
- Connection resilience
- Change tracking
- Concurrency detection
- Bulk operations

## Test Infrastructure

### Helpers

#### `TestDataSeeder.cs`
Provides factory methods for creating test data:
- Tournament requests and participants
- Mobile devices and pairing requests
- RGB devices, effects, and profiles
- Theme definitions
- Browser tabs and settings

#### `ApiClientExtensions.cs`
Extension methods for HTTP client testing:
- `PostAsJsonAsync<T>` - POST with JSON body
- `PutAsJsonAsync<T>` - PUT with JSON body
- `PatchAsJsonAsync<T>` - PATCH with JSON body
- `GetFromJsonAsync<T>` - GET and deserialize
- Response assertion helpers

#### `SignalRTestClient.cs`
SignalR test client for real-time communication testing:
- Connection management
- Hub method invocation
- Message receiving and waiting
- Event subscription

### Fixtures

#### `IntegrationTestFixture.cs`
Shared test infrastructure:
- Service provider configuration
- In-memory database setup
- Mock service registration
- Test data repositories

## Running the Tests

### Run All Integration Tests
```bash
dotnet test tests/SaveState.IntegrationTests
```

### Run Specific Test Suite
```bash
dotnet test tests/SaveState.IntegrationTests --filter "FullyQualifiedName~TournamentManagementTests"
dotnet test tests/SaveState.IntegrationTests --filter "FullyQualifiedName~MobileCompanionTests"
dotnet test tests/SaveState.IntegrationTests --filter "FullyQualifiedName~CloudGamingTests"
```

### Run with Verbose Output
```bash
dotnet test tests/SaveState.IntegrationTests --verbosity normal
```

### Run with Code Coverage
```bash
dotnet test tests/SaveState.IntegrationTests --collect:"XPlat Code Coverage"
```

## Test Patterns

### Async Test Lifecycle
```csharp
public class MyTests : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        // Setup before each test
    }

    public async Task DisposeAsync()
    {
        // Cleanup after each test
    }
}
```

### Using Fixtures
```csharp
public class MyTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public MyTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }
}
```

### Result Pattern Assertions
```csharp
var result = await _service.DoSomethingAsync();

// Success case
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();

// Failure case
result.IsFailure.Should().BeTrue();
result.ErrorType.Should().Be(ErrorType.NotFound);
```

## Adding New Tests

1. Create test class in appropriate subdirectory
2. Inherit from `IClassFixture<IntegrationTestFixture>`
3. Inject required services from fixture
4. Use `TestDataSeeder` for test data
5. Follow naming convention: `MethodName_Scenario_ExpectedResult`
6. Use FluentAssertions for readable assertions

## Notes

- Tests use in-memory database for isolation
- Some tests may require actual service implementations
- SignalR tests require running SignalR hub
- OAuth tests require configured OAuth providers
- Voice command tests may require audio processing capabilities
