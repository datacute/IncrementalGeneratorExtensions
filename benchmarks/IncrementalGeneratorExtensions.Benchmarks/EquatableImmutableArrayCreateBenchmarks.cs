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

        [GlobalSetup]
        public void Setup()
        {
            _rawShort = ImmutableArrayFactory.BuildShort(1, 2, 3);
            _rawLong = ImmutableArrayFactory.BuildAscending(20);

            // Pre-populate the instance cache so cached Create paths will return the shared instance.
            _prevShort = EquatableImmutableArray<TypeContext>.Create(_rawShort);
            _prevLong = EquatableImmutableArray<TypeContext>.Create(_rawLong);
        }

        // ------------------------------------------------------------------ //
        //  Cached: Create via instance cache, then Equals against previous      //
        // ------------------------------------------------------------------ //

        [Benchmark(Baseline = true)]
        public bool Cached_CreateThenEquals_Short()
        {
            var created = EquatableImmutableArray<TypeContext>.Create(_rawShort);
            return created.Equals(_prevShort);
        }

        [Benchmark]
        public bool Cached_CreateThenEquals_Long()
        {
            var created = EquatableImmutableArray<TypeContext>.Create(_rawLong);
            return created.Equals(_prevLong);
        }

        // ------------------------------------------------------------------ //
        //  No-cache path (mirrors DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Creates a new instance without the cache, as <see cref="EquatableImmutableArray{T}.Create"/>
        /// does when <c>DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE</c> is defined.
        /// </summary>
        [Benchmark]
        public bool NoCache_CreateThenEquals_Short()
        {
            var comparer = EqualityComparer<TypeContext>.Default;
            int hash = EquatableImmutableArray<TypeContext>.CalculateHashCode(_rawShort, comparer, 0, 0);
            var created = new EquatableImmutableArray<TypeContext>(_rawShort, hash);
            return created.Equals(_prevShort);
        }

        [Benchmark]
        public bool NoCache_CreateThenEquals_Long()
        {
            var comparer = EqualityComparer<TypeContext>.Default;
            int hash = EquatableImmutableArray<TypeContext>.CalculateHashCode(_rawLong, comparer, 0, 0);
            var created = new EquatableImmutableArray<TypeContext>(_rawLong, hash);
            return created.Equals(_prevLong);
        }
    }
}

/*
BenchmarkDotNet v0.15.1, Windows 11 (10.0.26200.8457)
12th Gen Intel Core i7-12700H 2.30GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.300
  [Host] : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2
  net10  : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2
  net481 : .NET Framework 4.8.1 (4.8.9325.0), X64 RyuJIT VectorSize=256

| Method                         | Job    | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------- |------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Cached_CreateThenEquals_Long   | net10  |  94.09 ns |  1.818 ns |  1.785 ns |  2.98 |    0.06 |      - |         - |          NA |
| Cached_CreateThenEquals_Long   | net481 | 200.99 ns |  2.268 ns |  2.122 ns |  2.64 |    0.04 |      - |         - |          NA |
| Cached_CreateThenEquals_Short  | net10  |  31.62 ns |  0.319 ns |  0.267 ns |  1.00 |    0.01 |      - |         - |          NA |
| Cached_CreateThenEquals_Short  | net481 |  76.01 ns |  0.751 ns |  0.703 ns |  1.00 |    0.01 |      - |         - |          NA |
| NoCache_CreateThenEquals_Long  | net10  | 381.61 ns |  4.086 ns |  3.190 ns | 12.07 |    0.14 |      - |         - |          NA |
| NoCache_CreateThenEquals_Long  | net481 | 561.52 ns | 10.923 ns | 11.217 ns |  7.39 |    0.16 | 0.0048 |      32 B |          NA |
| NoCache_CreateThenEquals_Short | net10  |  58.51 ns |  0.462 ns |  0.409 ns |  1.85 |    0.02 |      - |         - |          NA |
| NoCache_CreateThenEquals_Short | net481 |  97.28 ns |  1.439 ns |  1.346 ns |  1.28 |    0.02 | 0.0050 |      32 B |          NA |

*/


