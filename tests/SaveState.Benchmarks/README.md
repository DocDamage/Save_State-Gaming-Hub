# SaveStateReborn Performance Benchmarks

This project contains performance benchmarks for critical operations in SaveStateReborn using BenchmarkDotNet.

## Running Benchmarks

### Run All Benchmarks
```bash
dotnet run --project tests/SaveState.Benchmarks --configuration Release
```

### Run Specific Benchmark
```bash
dotnet run --project tests/SaveState.Benchmarks --configuration Release -- --filter "GameSearchBenchmarks"
```

### Run with Exporters
```bash
dotnet run --project tests/SaveState.Benchmarks --configuration Release -- --exporters json html md
```

## Available Benchmarks

### GameSearchBenchmarks
Tests game search operations with different strategies:
- `SearchByTitle_Linq` - Standard LINQ search
- `SearchByTitle_Parallel` - Parallel LINQ search
- `SearchByTitle_Span` - Span-based search
- `GroupByPlatform` - Platform grouping
- `OrderByTitle` - Sorting operations
- `FilterAndSort` - Combined operations

### SaveStateBenchmarks
Tests save state file operations:
- `SaveToDiskAsync` - File write performance
- `LoadFromDiskAsync` - File read performance
- `CompressData` - GZip compression
- `DecompressData` - GZip decompression
- `CalculateHash_MD5/SHA256/SHA512` - Hash algorithms

Parameters: 1MB, 10MB, 50MB file sizes

### CloudSyncBenchmarks
Tests cloud synchronization operations:
- `FilterFiles_Sequential/Parallel` - File filtering
- `CalculateTotalSize_Sequential/Parallel` - Size calculation
- `GroupByFolder` - Directory grouping
- `SortFiles_ByName/ByFolderThenName` - Sorting strategies

Parameters: 100, 1000, 5000 file counts

### ResultPatternBenchmarks
Tests Result pattern vs alternatives:
- `Result_Success/Failure` - Result<T> pattern
- `Exception_ThrowCatch` - Exception-based error handling
- `Nullable_ReturnNull` - Nullable return values
- `Result_Async_Success/Failure` - Async operations

### StringOperationBenchmarks
Tests string operations:
- `Contains_OrdinalIgnoreCase` - Case-insensitive search
- `Contains_ToLower` - Lowercase comparison
- `Span_Contains` - Span-based search
- `Regex_Match` - Regular expression matching

## Interpreting Results

### Key Metrics
- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation
- **Gen0/Gen1/Gen2**: Garbage collections per 1000 operations
- **Allocated**: Memory allocated per operation

### Rank Column
- **1**: Fastest
- **2**: Second fastest
- etc.

## CI Integration

Benchmarks run automatically on every push to `main` branch. Results are stored as artifacts.

### Compare Benchmarks
```bash
# Run baseline
dotnet run --project tests/SaveState.Benchmarks --configuration Release -- --exporters json --artifacts ./benchmarks/baseline

# Run after changes
dotnet run --project tests/SaveState.Benchmarks --configuration Release -- --exporters json --artifacts ./benchmarks/current

# Compare (requires BenchView or custom tool)
```

## Adding New Benchmarks

1. Create a new class with `[MemoryDiagnoser]` attribute
2. Add `[GlobalSetup]` method for test data
3. Add `[Benchmark]` methods for operations to test
4. Run and verify benchmarks work
5. Add documentation to this README

## Performance Guidelines

### Good Benchmark Candidates
- Hot paths (frequently executed code)
- Operations with large data sets
- Alternative implementations
- Memory-intensive operations

### Benchmark Best Practices
- Use realistic data sizes
- Test multiple scenarios
- Include memory diagnostics
- Document what you're measuring
