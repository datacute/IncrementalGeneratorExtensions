using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Datacute.IncrementalGeneratorExtensions.Benchmarks;

// Two runtimes are benchmarked per class (via [SimpleJob] attributes on each class):
//   net481  — Visual Studio 2022 (devenv.exe) hosts source generators in-process on .NET Framework
//   net10.0 — dotnet CLI build server and JetBrains Rider both use the current .NET SDK runtime
//
// Results are ordered method-first (both runtimes together per method) via MethodFirstOrderer.
var config = ManualConfig.Create(DefaultConfig.Instance)
    .WithOrderer(MethodFirstOrderer.Instance);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

/*
 * To run these benchmarks:
 *   dotnet run -c Release -f net10.0 --project benchmarks\IncrementalGeneratorExtensions.Benchmarks
 *
 * To filter to a subset:
 *   dotnet run -c Release -f net10.0 --project benchmarks\IncrementalGeneratorExtensions.Benchmarks -- --filter *EquatableImmutableArrayBenchmarks*
 *   dotnet run -c Release -f net10.0 --project benchmarks\IncrementalGeneratorExtensions.Benchmarks -- --filter *EquatableImmutableArrayCreateBenchmarks*
 *   dotnet run -c Release -f net10.0 --project benchmarks\IncrementalGeneratorExtensions.Benchmarks -- --filter *EquatableImmutableArrayInstanceCacheBenchmarks*
 *   dotnet run -c Release -f net10.0 --project benchmarks\IncrementalGeneratorExtensions.Benchmarks -- --filter *IndentingLineAppenderBenchmarks*
 *   dotnet run -c Release -f net10.0 --project benchmarks\IncrementalGeneratorExtensions.Benchmarks -- --filter *LightweightTraceBenchmarks*
 */
