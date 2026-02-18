# Rider Live Templates

To import these templates into JetBrains Rider:

1. Go to **File** → **Settings** → **Editor** → **Live Templates**
2. Click **+** to add a new template
3. Copy the template content from below

## Template 1: Result Method

**Abbreviation:** `result-method`  
**Description:** Create service method returning Result<T>  
**Applicable in:** C#  

```csharp
/// <summary>
/// $DESCRIPTION$</summary>
public async Task<Result<$RETURN_TYPE$>> $METHOD_NAME$Async($PARAMETERS$, CancellationToken ct = default)
{
    try
    {
        $VALIDATION$
        
        $IMPLEMENTATION$
        
        return Result<$RETURN_TYPE$>.Success(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "$METHOD_NAME$ failed");
        return Result<$RETURN_TYPE$>.Failure($"Error: {ex.Message}", ErrorType.Internal);
    }
}
```

**Variables:**
- `$RETURN_TYPE$` - Return type (suggest: string, int, Entity)
- `$METHOD_NAME$` - Method name
- `$PARAMETERS$` - Parameters
- `$VALIDATION$` - Validation code
- `$DESCRIPTION$` - XML documentation
- `$IMPLEMENTATION$` - Method implementation

---

## Template 2: CQRS Command Handler

**Abbreviation:** `cmd-handler`  
**Description:** Create CQRS Command Handler  

```csharp
using MediatR;
using SaveState.Core.Common;

namespace $NAMESPACE$.Commands;

/// <summary>
/// Command to $DESCRIPTION$</summary>
public sealed record $COMMAND_NAME$Command($PARAMETERS$) : IRequest<Result<$RETURN_TYPE$>>;

/// <summary>
/// Handler for $COMMAND_NAME$Command</summary>
public sealed class $COMMAND_NAME$CommandHandler : IRequestHandler<$COMMAND_NAME$Command, Result<$RETURN_TYPE$>>
{
    private readonly I$REPOSITORY$ _repository;
    private readonly ILogger<$COMMAND_NAME$CommandHandler> _logger;

    public $COMMAND_NAME$CommandHandler(I$REPOSITORY$ repository, ILogger<$COMMAND_NAME$CommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<$RETURN_TYPE$>> Handle($COMMAND_NAME$Command request, CancellationToken ct)
    {
        try
        {
            $IMPLEMENTATION$
            
            _logger.LogInformation("$COMMAND_NAME$ executed successfully");
            return Result<$RETURN_TYPE$>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "$COMMAND_NAME$ failed");
            return Result<$RETURN_TYPE$>.Failure($"Error: {ex.Message}", ErrorType.Internal);
        }
    }
}
```

---

## Template 3: CQRS Query Handler

**Abbreviation:** `query-handler`  
**Description:** Create CQRS Query Handler  

```csharp
using MediatR;
using SaveState.Core.Common;

namespace $NAMESPACE$.Queries;

/// <summary>
/// Query to $DESCRIPTION$</summary>
public sealed record Get$ENTITY$Query($PARAMETERS$) : IRequest<Result<$DTO$>>;

public sealed class Get$ENTITY$QueryHandler : IRequestHandler<Get$ENTITY$Query, Result<$DTO$>>
{
    private readonly I$REPOSITORY$ _repository;
    private readonly ILogger<Get$ENTITY$QueryHandler> _logger;

    public Get$ENTITY$QueryHandler(I$REPOSITORY$ repository, ILogger<Get$ENTITY$QueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<$DTO$>> Handle(Get$ENTITY$Query request, CancellationToken ct)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(request.Id, ct);
            
            if (entity == null)
            {
                _logger.LogWarning("$ENTITY$ {Id} not found", request.Id);
                return Result<$DTO$>.Failure($"$ENTITY$ {request.Id} not found", ErrorType.NotFound);
            }
            
            var dto = MapToDto(entity);
            return Result<$DTO$>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get $ENTITY$ {Id}", request.Id);
            return Result<$DTO$>.Failure($"Error: {ex.Message}", ErrorType.Internal);
        }
    }

    private static $DTO$ MapToDto($ENTITY$ entity)
    {
        return new $DTO$
        {
            $MAPPING$
        };
    }
}
```

---

## Template 4: Unit Test

**Abbreviation:** `test-method`  
**Description:** Create xUnit test with AAA pattern  

```csharp
[Fact]
public async Task $METHOD$_$SCENARIO$_$EXPECTED$()
{
    // Arrange
    $ARRANGE$

    // Act
    var result = await _sut.$METHOD_TO_TEST$Async();

    // Assert
    result.IsSuccess.Should().BeTrue();
    $ASSERTIONS$
}
```

---

## Import Instructions

### Option 1: Manual Import
1. Copy each template above
2. Open Rider Settings (Ctrl+Alt+S)
3. Navigate to Editor → Live Templates
4. Click "+" → "Live Template"
5. Paste the template code
6. Set abbreviation and description
7. Define variables
8. Click "Save"

### Option 2: Import from File
1. Save templates to `.DotSettings` file
2. Go to **File** → **Import Settings**
3. Select the `.DotSettings` file
4. Import live templates

---

## Quick Reference

| Abbreviation | Purpose |
|--------------|---------|
| `result-method` | Service method with Result<T> |
| `cmd-handler` | CQRS Command Handler |
| `query-handler` | CQRS Query Handler |
| `entity-factory` | Entity factory method |
| `plugin-class` | New plugin class |
| `repo-method` | Repository method |
| `test-method` | Unit test method |
| `guard-validate` | Guard clauses |
| `log-scope` | Logging with correlation ID |
