using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Datacute.IncrementalGeneratorExtensions.Benchmarks
{
    /// <summary>
    /// Benchmarks for <see cref="EquatableImmutableArrayInstanceCache{T}.GetOrCreate"/>.
    /// </summary>
    /// <remarks>
    /// Three distinct scenarios are measured:
    /// <list type="bullet">
    ///   <item>Empty array — returns the shared singleton immediately.</item>
    ///   <item>Cache hit — every call returns an already-cached instance; exercises the
    ///       lock-free lookup through length → first-element-hash → element comparison.</item>
    ///   <item>Cache miss — a monotonically-increasing first element guarantees a new bucket
    ///       on every call; measures allocation + insertion cost.  The cache grows continuously
    ///       during this benchmark; GC pressure is visible in the allocation column.</item>
    /// </list>
    /// </remarks>
    [MemoryDiagnoser]
    [HideColumns("Runtime", "IterationCount", "WarmupCount")]
    [BenchmarkCategory("EquatableImmutableArrayInstanceCache")]
    [SimpleJob(RuntimeMoniker.Net481, id: "net481")]
    [SimpleJob(RuntimeMoniker.Net10_0, id: "net10")]
    public class EquatableImmutableArrayInstanceCacheBenchmarks
    {
        private static readonly ImmutableArray<TypeContext> EmptyArray = ImmutableArray<TypeContext>.Empty;

        // Pre-built warm arrays — one per [Params] length.
        private ImmutableArray<TypeContext> _warmShort;
        private ImmutableArray<TypeContext> _warmLong;

        // Counter used to produce a brand-new first element on every cache-miss call.
        private int _missCounter;

        [GlobalSetup]
        public void Setup()
        {
            _warmShort = ImmutableArrayFactory.BuildAscending(3);
            _warmLong = ImmutableArrayFactory.BuildAscending(20);

            // Pre-populate the cache so subsequent hits are truly warm.
            EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_warmShort);
            EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_warmLong);

            _missCounter = 1_000_000; // start well away from the warm arrays
        }

        // ------------------------------------------------------------------ //
        //  Empty                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>Returns <see cref="EquatableImmutableArray{T}.Empty"/> via the fast-path guard.</summary>
        [Benchmark]
        public EquatableImmutableArray<TypeContext> GetOrCreate_Empty()
            => EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(EmptyArray);

        // ------------------------------------------------------------------ //
        //  Cache hit                                                           //
        // ------------------------------------------------------------------ //

        /// <summary>Cache hit for a 3-element array.</summary>
        [Benchmark]
        public EquatableImmutableArray<TypeContext> GetOrCreate_CacheHit_Short()
            => EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_warmShort);

        /// <summary>Cache hit for a 20-element array.</summary>
        [Benchmark]
        public EquatableImmutableArray<TypeContext> GetOrCreate_CacheHit_Long()
            => EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_warmLong);

        // ------------------------------------------------------------------ //
        //  Cache miss                                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Cache miss: a unique first element on every call means the cache will never find a
        /// match, so a new <see cref="EquatableImmutableArray{T}"/> is allocated and inserted each time.
        /// </summary>
        /// <remarks>Bounded to 15 iterations to keep total run time manageable; the sweep cost
        /// makes individual iterations expensive and highly variable.</remarks>
        [Benchmark]
        [WarmupCount(3), IterationCount(15)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_CacheMiss_Short()
        {
            var arr = ImmutableArrayFactory.BuildShort(_missCounter++, 42, 99);
            return EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(arr);
        }

        /// <summary>Cache miss for a 20-element array.</summary>
        /// <remarks>Bounded to 15 iterations to keep total run time manageable.</remarks>
        [Benchmark]
        [WarmupCount(3), IterationCount(15)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_CacheMiss_Long()
        {
            return EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(ImmutableArrayFactory.BuildWithUniqueFirstElement(20, _missCounter++));
        }

        // ------------------------------------------------------------------ //
        //  Cache miss — tail varies (same first element, unique last element) //
        //  All entries collide into the same candidateList bucket, so the     //
        //  linear scan grows O(n) with each miss: demonstrates the O(n²)     //
        //  worst case noted in the TODO comment in the implementation.        //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Cache miss for a 3-element array where only the <em>last</em> element varies.
        /// Every array shares the same first element hash, so all entries accumulate in a
        /// single <c>candidateList</c> bucket.  The linear scan grows O(n) with each miss,
        /// producing O(n²) total work across the benchmark run.
        /// </summary>
        /// <remarks>Bounded to 15 iterations to keep total run time manageable.</remarks>
        [Benchmark]
        [WarmupCount(3), IterationCount(15)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_CacheMiss_Short_TailVaries()
        {
            var arr = ImmutableArrayFactory.BuildShort(42, 99, _missCounter++);
            return EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(arr);
        }

        /// <summary>
        /// Cache miss for a 20-element array where only the <em>last</em> element varies.
        /// Every array shares the same first element hash, so all entries accumulate in a
        /// single <c>candidateList</c> bucket.  The linear scan grows O(n) with each miss,
        /// producing O(n²) total work across the benchmark run.
        /// </summary>
        /// <remarks>Bounded to 15 iterations to keep total run time manageable.</remarks>
        [Benchmark]
        [WarmupCount(3), IterationCount(15)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_CacheMiss_Long_TailVaries()
        {
            return EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(ImmutableArrayFactory.BuildWithUniqueLastElement(20, _missCounter++));
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

| Method                                 | Job    | Mean         | Error         | StdDev      | Gen0   | Gen1   | Allocated |
|--------------------------------------- |------- |-------------:|--------------:|------------:|-------:|-------:|----------:|
| GetOrCreate_CacheHit_Long              | net10  |    92.454 ns |     1.8100 ns |   1.7777 ns |      - |      - |         - |
| GetOrCreate_CacheHit_Long              | net481 |   189.911 ns |     3.2094 ns |   2.8450 ns |      - |      - |         - |
| GetOrCreate_CacheHit_Short             | net10  |    30.901 ns |     0.4236 ns |   0.3962 ns |      - |      - |         - |
| GetOrCreate_CacheHit_Short             | net481 |    64.527 ns |     1.0955 ns |   1.0248 ns |      - |      - |         - |
| GetOrCreate_CacheMiss_Long             | net10  | 3,191.616 ns |   371.6088 ns | 329.4216 ns | 0.1488 | 0.0381 |    1888 B |
| GetOrCreate_CacheMiss_Long             | net481 | 4,554.047 ns |   215.4445 ns | 190.9859 ns | 0.3967 | 0.0839 |    2519 B |
| GetOrCreate_CacheMiss_Long_TailVaries  | net10  | 3,185.128 ns |   174.1148 ns | 154.3482 ns | 0.1488 | 0.0381 |    1888 B |
| GetOrCreate_CacheMiss_Long_TailVaries  | net481 | 4,193.878 ns |   195.8028 ns | 173.5741 ns | 0.5035 | 0.1373 |    3452 B |
| GetOrCreate_CacheMiss_Short            | net10  | 2,362.822 ns |   255.4965 ns | 226.4910 ns | 0.0420 | 0.0210 |     527 B |
| GetOrCreate_CacheMiss_Short            | net481 | 3,178.519 ns | 1,010.2600 ns | 944.9979 ns | 0.1030 | 0.0267 |     650 B |
| GetOrCreate_CacheMiss_Short_TailVaries | net10  | 2,342.099 ns |   305.2144 ns | 270.5646 ns | 0.0420 | 0.0210 |     528 B |
| GetOrCreate_CacheMiss_Short_TailVaries | net481 | 3,298.285 ns |   386.7766 ns | 301.9697 ns | 0.1030 | 0.0324 |     658 B |
| GetOrCreate_Empty                      | net10  |     4.594 ns |     0.1335 ns |   0.1428 ns |      - |      - |         - |
| GetOrCreate_Empty                      | net481 |    12.106 ns |     0.1091 ns |   0.0967 ns |      - |      - |         - |

// * Warnings *
MultimodalDistribution
  EquatableImmutableArrayInstanceCacheBenchmarks.GetOrCreate_CacheMiss_Long: net10  -> It seems that the distribution is bimodal (mValue = 3.25)
  EquatableImmutableArrayInstanceCacheBenchmarks.GetOrCreate_CacheMiss_Short: net10 -> It seems that the distribution is bimodal (mValue = 3.33)


*/
