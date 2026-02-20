# API Documentation Guide

## Overview
SaveStateReborn provides a comprehensive API for gaming management, memory intelligence, and MUGEN integration.

## OpenAPI Specification

The API is documented using OpenAPI 3.0 (Swagger) specification via NSwag.

### Accessing Documentation

- **JSON Specification**: `GET /openapi/v2.5.json`
- **Swagger UI**: `GET /swagger`

## Authentication

All API endpoints require JWT Bearer authentication:

```http
Authorization: Bearer <jwt_token>
```

## API Categories

### Games
- `GET /api/games` - List all games
- `GET /api/games/{id}` - Get game details
- `POST /api/games` - Add new game
- `PUT /api/games/{id}` - Update game
- `DELETE /api/games/{id}` - Delete game

### Memory Intelligence
- `POST /api/memory/attach` - Attach to game process
- `POST /api/memory/detach` - Detach from process
- `GET /api/memory/patterns` - Detect memory patterns
- `POST /api/memory/scan` - Scan for values

### Save States
- `GET /api/games/{gameId}/saves` - List save states
- `POST /api/games/{gameId}/saves` - Create save state
- `GET /api/saves/{id}` - Load save state
- `DELETE /api/saves/{id}` - Delete save state

### MUGEN
- `GET /api/mugen/characters` - List characters
- `POST /api/mugen/fights` - Start fight
- `GET /api/mugen/tournaments` - List tournaments

## CQRS Commands

Commands are executed via `POST /api/commands`:

```json
{
  "commandType": "CreateGameCommand",
  "payload": {
    "title": "My Game",
    "platform": "Steam"
  }
}
```

## CQRS Queries

Queries are executed via `GET /api/queries`:

```json
{
  "queryType": "GetGameQuery",
  "parameters": {
    "id": 123
  }
}
```

## Rate Limiting

- 100 requests per minute for authenticated users
- 10 requests per minute for anonymous users

## Error Handling

All errors return standard ProblemDetails format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Game title is required",
  "instance": "/api/games"
}
```

## Custom Attributes

### OpenApiExampleAttribute

Specify example values for schema properties:

```csharp
public class CreateGameCommand
{
    [OpenApiExample("Super Mario Bros", Description = "The title of the game")]
    public string Title { get; set; } = string.Empty;
    
    [OpenApiExample("Steam", Description = "Platform where the game is installed")]
    public string Platform { get; set; } = string.Empty;
}
```

### OpenApiTagAttribute

Organize endpoints by tags:

```csharp
[OpenApiTag("Games", Description = "Game library management endpoints")]
public class GameController : ControllerBase
{
    // ...
}
```

### OpenApiExcludeAttribute

Exclude classes from documentation:

```csharp
[OpenApiExclude]
public class InternalService
{
    // This class won't appear in the OpenAPI documentation
}
```

## SDK Generation

Generate client SDKs using OpenAPI Generator:

```bash
# TypeScript
openapi-generator-cli generate -i openapi.json -g typescript-fetch -o sdk/ts

# C#
openapi-generator-cli generate -i openapi.json -g csharp -o sdk/csharp

# Python
openapi-generator-cli generate -i openapi.json -g python -o sdk/python
```

## Configuration

The OpenAPI documentation is configured in `OpenApiConfiguration.cs`:

```csharp
services.AddOpenApiDocument(config =>
{
    config.DocumentName = "v2.5";
    config.Title = "SaveStateReborn API";
    config.Description = "Gaming management platform API";
    config.Version = "2.5.1";
    
    // Add security
    config.AddSecurity("Bearer", new NSwag.OpenApiSecurityScheme
    {
        Type = NSwag.OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});
```

## CQRS Document Processor

The `CqrsDocumentProcessor` automatically discovers and documents:

- All commands (classes ending with "Command")
- All queries (classes ending with "Query")
- Command and query handlers

This ensures your CQRS operations are always documented without manual annotation.
