```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.100-rc.1.25451.107
  [Host]   : .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v4
  .NET 9.0 : .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v4

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method                       | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|----------------------------- |-----:|------:|------:|--------:|------------:|
| ServiceProviderBuild         |   NA |    NA |     ? |       ? |           ? |
| DatabaseQuery                |   NA |    NA |     ? |       ? |           ? |
| DatabaseInitialization       |   NA |    NA |     ? |       ? |           ? |
| MemoryAllocation             |   NA |    NA |     ? |       ? |           ? |
| AiMemoryOperations           |   NA |    NA |     ? |       ? |           ? |
| FileSystemOperations         |   NA |    NA |     ? |       ? |           ? |
| ConcurrentDatabaseOperations |   NA |    NA |     ? |       ? |           ? |
| AiOrchestratorInitialization |   NA |    NA |     ? |       ? |           ? |
| BulkGameCreation             |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  PerformanceBenchmarks.ServiceProviderBuild: .NET 9.0(Runtime=.NET 9.0)
  PerformanceBenchmarks.DatabaseQuery: .NET 9.0(Runtime=.NET 9.0)
  PerformanceBenchmarks.DatabaseInitialization: .NET 9.0(Runtime=.NET 9.0)
  PerformanceBenchmarks.MemoryAllocation: .NET 9.0(Runtime=.NET 9.0)
  PerformanceBenchmarks.AiMemoryOperations: .NET 9.0(Runtime=.NET 9.0)
  PerformanceBenchmarks.FileSystemOperations: .NET 9.0(Runtime=.NET 9.0)
  PerformanceBenchmarks.ConcurrentDatabaseOperations: .NET 9.0(Runtime=.NET 9.0)
  PerformanceBenchmarks.AiOrchestratorInitialization: .NET 9.0(Runtime=.NET 9.0)
  PerformanceBenchmarks.BulkGameCreation: .NET 9.0(Runtime=.NET 9.0)
