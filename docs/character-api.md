# Character Management API

This document describes the APIs for managing MUGEN/IKEMEN characters in SaveState Reborn V2.0.

**Note**: This is part of the included fighting game platform features in V2.0, providing IKEMEN GO integration alongside the universal game library management.

## Overview

The character management system provides comprehensive support for:

- **Character Discovery**: Automatic scanning of character directories
- **Metadata Extraction**: Parsing character definition files (.def)
- **Database Cataloging**: Persistent storage of character information
- **Launch Integration**: Direct launching with IKEMEN engine

## Architecture

### Domain Layer

```csharp
// Core entities
public class MugenCharacter : EntityBase
{
    public string Name { get; private set; }
    public string DisplayName { get; private set; }
    public string Author { get; private set; }
    public string Version { get; private set; }
    public string DefinitionFilePath { get; private set; }
    public string CharacterDirectory { get; private set; }
    public CharacterStats Stats { get; private set; }
}

// Value objects
public record CharacterStats(
    int Life = 1000,
    int Power = 3000,
    int Attack = 100,
    int Defense = 100
);
```

### Application Layer

#### Commands

```csharp
// Scan characters from bundled directories
public record ScanIkemenCharactersCommand : IRequest<Unit>;

// Launch versus match
public record LaunchIkemenVersusCommand(
    string Player1Character,
    string Player2Character,
    int Rounds = 3
) : IRequest<ProcessInfo>;
```

#### Queries

```csharp
// Get all characters
public record GetMugenCharactersQuery(
    string? AuthorFilter = null,
    string? NameFilter = null,
    bool IncludeInvalid = false
) : IRequest<IReadOnlyList<MugenCharacterSummaryDto>>;
```

### Infrastructure Layer

```csharp
// Character loader service
public interface IMugenCharacterLoader
{
    Task<IReadOnlyList<MugenCharacter>> ScanIkemenCharactersAsync(CancellationToken ct = default);
    Task<Result<MugenCharacter>> LoadCharacterFromDefAsync(string definitionFilePath, CancellationToken ct = default);
}

// Launch service
public interface IMugenLauncher
{
    Task<Process> LaunchVersusAsync(string player1, string player2, int rounds = 3);
    Task<Process> LaunchTrainingAsync(string character, string dummy = "KFM");
    bool IsIkemenAvailable();
}
```

## API Usage

### Scanning Characters

```csharp
// Scan all bundled IKEMEN characters
var scanCommand = new ScanIkemenCharactersCommand();
await mediator.Send(scanCommand);

// Get character list
var query = new GetMugenCharactersQuery();
var characters = await mediator.Send(query);
```

### Launching Games

```csharp
// Launch versus match
var launchCommand = new LaunchIkemenVersusCommand(
    Player1Character: "Ryu",
    Player2Character: "Ken",
    Rounds: 3
);
var processInfo = await mediator.Send(launchCommand);

// Launch training
var trainingProcess = await launcher.LaunchTrainingAsync("Ryu");
```

## Directory Structure

```
data/characters/
├── streetfighter/     # SF characters (Ryu, Ken, Chun-Li, etc.)
├── mvc2/             # MVC2 characters (Ryu, Megaman, etc.)
└── builtin/          # Custom and additional characters
```

## File Format Support

### Character Definition Files (.def)

The system parses standard MUGEN character definition files:

```ini
[Info]
name = "Ryu"
displayname = "Ryu"
version = "1.0"
author = "Capcom"

[Files]
cmd = ryu.cmd
cns = ryu.cns
st = ryu.st
sprite = ryu.sff
sound = ryu.snd

[Data]
life = 1000
power = 3000
attack = 100
defence = 100
```

### Supported Assets

- **.def**: Character definition files
- **.sff**: Sprite files
- **.snd**: Sound files
- **.cmd**: Command/input files
- **.cns**: Constants/state files
- **.st**: State files
- **.air**: Animation files

## IKEMEN Integration

### Engine Configuration

```json
{
  "executable": "Ikemen_GO.exe",
  "arguments": {
    "versus": "-p1 {player1} -p2 {player2} -rounds 3",
    "training": "-p1 {player1} -p2 {dummy} -training"
  },
  "characterDirectories": [
    "../../../data/characters/streetfighter",
    "../../../data/characters/mvc2",
    "../../../data/characters/builtin"
  ]
}
```

### Launch Arguments

- `-p1 {character}`: Player 1 character
- `-p2 {character}`: Player 2 character
- `-rounds {n}`: Number of rounds
- `-training`: Training mode
- `-watch`: Watch mode (AI vs AI)

## Error Handling

### Character Loading Errors

```csharp
try
{
    var characters = await loader.ScanIkemenCharactersAsync(cancellationToken);
}
catch (DirectoryNotFoundException)
{
    // Character directory missing
}
catch (FileNotFoundException)
{
    // Required character files missing
}
catch (FormatException)
{
    // Invalid .def file format
}
```

### Launch Errors

```csharp
try
{
    var process = await launcher.LaunchVersusAsync("Ryu", "Ken");
}
catch (FileNotFoundException)
{
    // IKEMEN executable not found
}
catch (InvalidOperationException)
{
    // Character not found or invalid
}
```

## Performance Considerations

- **Lazy Loading**: Character metadata loaded on-demand
- **Caching**: Parsed .def files cached in memory
- **Async Operations**: All file I/O is asynchronous
- **Cancellation**: All operations support cancellation tokens

## Testing

### Unit Tests

```csharp
[Fact]
public async Task Should_Parse_Character_Definition()
{
    // Arrange
    var loader = new MugenCharacterLoader(parser);

    // Act
    var result = await loader.LoadCharacterFromDefAsync("ryu.def");

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.Value.Should().NotBeNull();
    result.Value.Name.Should().Be("Ryu");
}
```

### Integration Tests

```csharp
[Fact]
public async Task Should_Scan_All_Ikemen_Characters()
{
    // Arrange
    var loader = new MugenCharacterLoader(parser);

    // Act
    var characters = await loader.ScanIkemenCharactersAsync();

    // Assert
    characters.Should().NotBeEmpty();
    characters.Should().Contain(c => c.Name == "Ryu");
}
```

## Troubleshooting

### Character Not Loading

1. Verify `.def` file exists and is valid
2. Check required asset files (`.sff`, `.snd`) exist
3. Ensure character directory structure is correct
4. Check file permissions

### Launch Failures

1. Verify IKEMEN executable exists and is executable
2. Check character names match exactly
3. Ensure working directory is set correctly
4. Verify no antivirus blocking the executable

### Performance Issues

1. Limit concurrent character scanning
2. Use cancellation tokens for long operations
3. Cache frequently accessed character data
4. Monitor memory usage with large character collections
