```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7462/25H2/2025Update/HudsonValley2)
11th Gen Intel Core i7-1185G7 3.00GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.100-rc.1.25451.107
  [Host] : .NET 10.0.0 (10.0.0-rc.1.25451.107, 10.0.25.45207), X64 RyuJIT x86-64-v4

Job=.NET 9.0  Runtime=.NET 9.0  

```
| Method               | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|--------------------- |-----:|------:|------:|--------:|------------:|
| ServiceProviderBuild |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  PerformanceBenchmarks.ServiceProviderBuild: .NET 9.0(Runtime=.NET 9.0)
