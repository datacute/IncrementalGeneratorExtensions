using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.CodeAnalysis;

namespace Datacute.IncrementalGeneratorExtensions.Benchmarks
{
    /// <summary>
    /// Realistic benchmarks that measure the common incremental-generator pattern:
    /// create a wrapper from a freshly-built ImmutableArray, then compare it to a
    /// previously-produced instance. The instance cache should return a shared
    /// instance so the equality check is a cheap reference equality.
    /// </summary>
    /// <remarks>
    /// Compares the two <see cref="EquatableImmutableArray{T}.Create"/> code paths side by side:
    /// the instance-cache path (default) and the no-cache path
    /// (<c>DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE</c>).
    ///
    /// Rather than requiring a separate build with a different define, the no-cache benchmarks
    /// call <see cref="EquatableImmutableArray{T}.CalculateHashCode"/> and the
    /// <c>internal</c> constructor directly — the exact code that <c>Create</c> compiles to
    /// when the instance cache is excluded.
    /// </remarks>
    [MemoryDiagnoser]
    [BenchmarkCategory("EquatableImmutableArrayCreate")]
    [HideColumns("Runtime")]
    [SimpleJob(RuntimeMoniker.Net481, id: "net481")]
    [SimpleJob(RuntimeMoniker.Net10_0, id: "net10")]
    public class EquatableImmutableArrayCreateBenchmarks
    {
        private ImmutableArray<TypeContext> _rawShort;
        private ImmutableArray<TypeContext> _rawLong;

        private EquatableImmutableArray<TypeContext> _prevShort;
        private EquatableImmutableArray<TypeContext> _prevLong;

        private const int OpsCachedShort = 5_000_000;
        private const int OpsCachedLong = 2_000_000;
        private const int OpsNoCacheShort = 2_500_000;
        private const int OpsNoCacheLong = 300_000;

        [GlobalSetup]
        public void Setup()
        {
            _rawShort = ImmutableArrayFactory.BuildShort(1, 2, 3);
            _rawLong = ImmutableArrayFactory.BuildAscending(20);

            // Pre-populate the instance cache so cached Create paths will return the shared instance.
            _prevShort = EquatableImmutableArray<TypeContext>.Create(_rawShort);
            _prevLong = EquatableImmutableArray<TypeContext>.Create(_rawLong);
        }

        [IterationSetup]
        public void IterationSetup()
        {
            EquatableImmutableArrayInstanceCache<TypeContext>.Clear();
            
            // Re-populate the cache
            _prevShort = EquatableImmutableArray<TypeContext>.Create(_rawShort);
            _prevLong = EquatableImmutableArray<TypeContext>.Create(_rawLong);
        }

        // ------------------------------------------------------------------ //
        //  Cached: Create via instance cache, then Equals against previous      //
        // ------------------------------------------------------------------ //

        [Benchmark(Baseline = true, OperationsPerInvoke = OpsCachedShort)]
        public bool Cached_CreateThenEquals_Short()
        {
            bool result = false;
            for (int i = 0; i < OpsCachedShort; i++)
            {
                var created = EquatableImmutableArray<TypeContext>.Create(_rawShort);
                result = created.Equals(_prevShort);
            }
            return result;
        }

        [Benchmark(OperationsPerInvoke = OpsCachedLong)]
        public bool Cached_CreateThenEquals_Long()
        {
            bool result = false;
            for (int i = 0; i < OpsCachedLong; i++)
            {
                var created = EquatableImmutableArray<TypeContext>.Create(_rawLong);
                result = created.Equals(_prevLong);
            }
            return result;
        }

        // ------------------------------------------------------------------ //
        //  No-cache path (mirrors DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a new instance without the cache, as <see cref="EquatableImmutableArray{T}.Create"/>
        /// does when <c>DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE</c> is defined.
        /// </summary>
        [Benchmark(OperationsPerInvoke = OpsNoCacheShort)]
        public bool NoCache_CreateThenEquals_Short()
        {
            bool result = false;
            for (int i = 0; i < OpsNoCacheShort; i++)
            {
                var comparer = EqualityComparer<TypeContext>.Default;
                int hash = EquatableImmutableArray<TypeContext>.CalculateHashCode(_rawShort, comparer, 0, 0);
                var created = new EquatableImmutableArray<TypeContext>(_rawShort, hash);
                result = created.Equals(_prevShort);
            }
            return result;
        }

        [Benchmark(OperationsPerInvoke = OpsNoCacheLong)]
        public bool NoCache_CreateThenEquals_Long()
        {
            bool result = false;
            for (int i = 0; i < OpsNoCacheLong; i++)
            {
                var comparer = EqualityComparer<TypeContext>.Default;
                int hash = EquatableImmutableArray<TypeContext>.CalculateHashCode(_rawLong, comparer, 0, 0);
                var created = new EquatableImmutableArray<TypeContext>(_rawLong, hash);
                result = created.Equals(_prevLong);
            }
            return result;
        }
    }
}

/*
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8457/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-12700H 2.30GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.300
  [Host] : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  net10  : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  net481 : .NET Framework 4.8.1 (4.8.9325.0), X64 RyuJIT VectorSize=256

InvocationCount=1  UnrollFactor=1  

| Method                         | Job    | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Cached_CreateThenEquals_Long   | net10  |  89.48 ns | 1.051 ns | 0.932 ns |  2.90 |    0.07 |      - |         - |          NA |
| Cached_CreateThenEquals_Long   | net481 | 202.10 ns | 3.539 ns | 3.310 ns |  2.70 |    0.06 |      - |         - |          NA |
| Cached_CreateThenEquals_Short  | net10  |  30.84 ns | 0.608 ns | 0.700 ns |  1.00 |    0.03 |      - |         - |          NA |
| Cached_CreateThenEquals_Short  | net481 |  74.73 ns | 1.152 ns | 1.078 ns |  1.00 |    0.02 |      - |         - |          NA |
| NoCache_CreateThenEquals_Long  | net10  | 398.01 ns | 2.787 ns | 2.176 ns | 12.91 |    0.29 |      - |      32 B |          NA |
| NoCache_CreateThenEquals_Long  | net481 | 542.56 ns | 6.118 ns | 5.723 ns |  7.26 |    0.13 | 0.0033 |      32 B |          NA |
| NoCache_CreateThenEquals_Short | net10  |  64.72 ns | 1.261 ns | 1.595 ns |  2.10 |    0.07 | 0.0024 |      32 B |          NA |
| NoCache_CreateThenEquals_Short | net481 |  93.22 ns | 0.669 ns | 0.626 ns |  1.25 |    0.02 | 0.0048 |      32 B |          NA |

*/
