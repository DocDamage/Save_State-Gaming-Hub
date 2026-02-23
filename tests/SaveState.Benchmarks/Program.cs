using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using SaveState.Benchmarks;

Console.WriteLine("SaveStateReborn Performance Benchmarks");
Console.WriteLine("======================================");
Console.WriteLine();

// Run all benchmarks
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

// Or run specific benchmarks:
// BenchmarkRunner.Run<GameLibraryBenchmarks>();
// BenchmarkRunner.Run<GameSearchBenchmarks>();
// BenchmarkRunner.Run<SaveStateBenchmarks>();
// BenchmarkRunner.Run<SaveStateBranchingBenchmarks>();
// BenchmarkRunner.Run<CloudSyncBenchmarks>();
// BenchmarkRunner.Run<SearchBenchmarks>();
// BenchmarkRunner.Run<ResultPatternBenchmarks>();
