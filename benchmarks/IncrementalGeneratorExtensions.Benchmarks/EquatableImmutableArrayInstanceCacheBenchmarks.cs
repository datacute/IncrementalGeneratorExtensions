using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Datacute.IncrementalGeneratorExtensions.Benchmarks
{
    /// <summary>
    /// Benchmarks for <see cref="EquatableImmutableArrayInstanceCache{T}.GetOrCreate"/>.
    /// </summary>
    /// <remarks>
    /// Scenarios are closely aligned to the two-level cache structure:
    /// <list type="bullet">
    ///   <item>Empty array — returns the shared singleton immediately.</item>
    ///   <item>MRU Hit — immediately returns from the lock-free strong reference hot path.</item>
    ///   <item>Dictionary Hit — misses the MRU (using a pool larger than MRU size) but finds the instance via dictionary lookup.</item>
    ///   <item>Miss (Unique) — unique first element produces a new MRU slot and new dictionary bucket every call.</item>
    ///   <item>Miss (Tail Varies) — a typical miss scenario. Same first element forces an MRU element check (fail), but a unique last element creates a new dictionary bucket.</item>
    ///   <item>Miss (Middle Varies) — forces dictionary bucket collisions since the first/last elements are identical, exercising inline weak reference list scanning.</item>
    /// </list>
    /// </remarks>
    [MemoryDiagnoser]
    [HideColumns("Runtime")]
    [BenchmarkCategory("EquatableImmutableArrayInstanceCache")]
    [SimpleJob(RuntimeMoniker.Net481, id: "net481")]
    [SimpleJob(RuntimeMoniker.Net10_0, id: "net10")]
    public class EquatableImmutableArrayInstanceCacheBenchmarks
    {
        private static readonly ImmutableArray<TypeContext> EmptyArray = ImmutableArray<TypeContext>.Empty;

        // Pre-built warm arrays — one per [Params] length.
        private ImmutableArray<TypeContext> _warmShort;
        private ImmutableArray<TypeContext> _warmLong;

        // Pools to guarantee MRU eviction but Dictionary hits
        private ImmutableArray<ImmutableArray<TypeContext>> _dictPoolShort;
        private ImmutableArray<ImmutableArray<TypeContext>> _dictPoolLong;
        private const int PoolSize = 256;

        // Counter used to produce a brand-new first element on every cache-miss call.
        private int _missCounter;

        private const int OpsEmpty = 25_000_000;
        private const int OpsMruHitShort = 4_000_000;
        private const int OpsMruHitLong = 1_500_000;
        private const int OpsDictHitShort = 2_000_000;
        private const int OpsDictHitLong = 1_000_000;
        private const int OpsMissUniqueShort = 200_000;
        private const int OpsMissUniqueLong = 65_000;
        private const int OpsMissTailVariesShort = 100_000;
        private const int OpsMissTailVariesLong = 55_000;
        private const int OpsMissMiddleVariesShort = 20_000;
        private const int OpsMissMiddleVariesLong = 2_000;

        [GlobalSetup]
        public void Setup()
        {
            _warmShort = ImmutableArrayFactory.BuildAscending(3);
            _warmLong = ImmutableArrayFactory.BuildAscending(20);

            var shortPool = ImmutableArray.CreateBuilder<ImmutableArray<TypeContext>>(PoolSize);
            var longPool = ImmutableArray.CreateBuilder<ImmutableArray<TypeContext>>(PoolSize);
            for (int i = 0; i < PoolSize; i++)
            {
                shortPool.Add(ImmutableArrayFactory.BuildShort(1000 + i, 42, 99));
                longPool.Add(ImmutableArrayFactory.BuildWithUniqueFirstElement(20, 1000 + i));
            }
            _dictPoolShort = shortPool.ToImmutable();
            _dictPoolLong = longPool.ToImmutable();

            // Pre-populate the cache so subsequent hits are truly warm.
            EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_warmShort);
            EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_warmLong);
            foreach (var arr in _dictPoolShort) EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(arr);
            foreach (var arr in _dictPoolLong) EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(arr);

            _missCounter = 1_000_000; // start well away from the warm arrays
        }

        [IterationSetup]
        public void IterationSetup()
        {
            EquatableImmutableArrayInstanceCache<TypeContext>.Clear();
            
            // Re-populate the cache
            EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_warmShort);
            EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_warmLong);
            foreach (var arr in _dictPoolShort) EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(arr);
            foreach (var arr in _dictPoolLong) EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(arr);
            
            _missCounter = 1_000_000;
        }

        // ------------------------------------------------------------------ //
        //  Empty                                                               //
        // ------------------------------------------------------------------ //

        /// <summary>Returns <see cref="EquatableImmutableArray{T}.Empty"/> via the fast-path guard.</summary>
        [Benchmark(OperationsPerInvoke = OpsEmpty)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_Empty()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsEmpty; i++)
            {
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(EmptyArray);
            }
            return result;
        }

        // ------------------------------------------------------------------ //
        //  MRU Hit                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>MRU cache hit for a 3-element array.</summary>
        [Benchmark(OperationsPerInvoke = OpsMruHitShort)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_MruHit_Short()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsMruHitShort; i++)
            {
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_warmShort);
            }
            return result;
        }

        /// <summary>MRU cache hit for a 20-element array.</summary>
        [Benchmark(OperationsPerInvoke = OpsMruHitLong)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_MruHit_Long()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsMruHitLong; i++)
            {
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_warmLong);
            }
            return result;
        }

        // ------------------------------------------------------------------ //
        //  Dictionary Hit                                                      //
        // ------------------------------------------------------------------ //

        /// <summary>Dictionary hit (MRU miss) for 3-element arrays.</summary>
        [Benchmark(OperationsPerInvoke = OpsDictHitShort)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_DictHit_Short()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsDictHitShort; i++)
            {
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_dictPoolShort[i % PoolSize]);
            }
            return result;
        }

        /// <summary>Dictionary hit (MRU miss) for 20-element arrays.</summary>
        [Benchmark(OperationsPerInvoke = OpsDictHitLong)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_DictHit_Long()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsDictHitLong; i++)
            {
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(_dictPoolLong[i % PoolSize]);
            }
            return result;
        }

        // ------------------------------------------------------------------ //
        //  Cache Miss - Unique                                               //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Cache miss (Unique): unique first element produces a new MRU entry and new dictionary bucket every call.
        /// </summary>
        [Benchmark(OperationsPerInvoke = OpsMissUniqueShort)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_Miss_Unique_Short()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsMissUniqueShort; i++)
            {
                var arr = ImmutableArrayFactory.BuildShort(_missCounter++, 42, 99);
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(arr);
            }
            return result;
        }

        /// <summary>Cache miss (Unique) for a 20-element array.</summary>
        [Benchmark(OperationsPerInvoke = OpsMissUniqueLong)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_Miss_Unique_Long()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsMissUniqueLong; i++)
            {
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(ImmutableArrayFactory.BuildWithUniqueFirstElement(20, _missCounter++));
            }
            return result;
        }

        // ------------------------------------------------------------------ //
        //  Cache Miss - Tail Varies                                          //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Cache miss (Tail Varies): typical scenario where arrays share a prefix. MRU element
        /// check fails, and the differing last element produces a new dictionary bucket.
        /// </summary>
        [Benchmark(OperationsPerInvoke = OpsMissTailVariesShort)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_Miss_TailVaries_Short()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsMissTailVariesShort; i++)
            {
                var arr = ImmutableArrayFactory.BuildShort(42, 99, _missCounter++);
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(arr);
            }
            return result;
        }

        /// <summary>Cache miss (Tail Varies) for a 20-element array.</summary>
        [Benchmark(OperationsPerInvoke = OpsMissTailVariesLong)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_Miss_TailVaries_Long()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsMissTailVariesLong; i++)
            {
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(ImmutableArrayFactory.BuildWithUniqueLastElement(20, _missCounter++));
            }
            return result;
        }

        // ------------------------------------------------------------------ //
        //  Cache Miss - Middle Varies (Collisions)                           //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Cache miss (Middle Varies): only the middle element varies, forcing entries into
        /// the same MRU slot and the exact same Dictionary bucket (since BucketHash relies on first/last).
        /// </summary>
        [Benchmark(OperationsPerInvoke = OpsMissMiddleVariesShort)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_Miss_MiddleVaries_Short()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsMissMiddleVariesShort; i++)
            {
                var arr = ImmutableArrayFactory.BuildShort(42, _missCounter++, 99);
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(arr);
            }
            return result;
        }

        /// <summary>Cache miss (Middle Varies) for a 20-element array.</summary>
        [Benchmark(OperationsPerInvoke = OpsMissMiddleVariesLong)]
        public EquatableImmutableArray<TypeContext> GetOrCreate_Miss_MiddleVaries_Long()
        {
            EquatableImmutableArray<TypeContext> result = null;
            for (int i = 0; i < OpsMissMiddleVariesLong; i++)
            {
                result = EquatableImmutableArrayInstanceCache<TypeContext>.GetOrCreate(ImmutableArrayFactory.BuildWithUniqueMiddleElement(20, _missCounter++));
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

| Method                              | Job    | Mean           | Error         | StdDev        | Median         | Gen0   | Gen1   | Gen2   | Allocated |
|------------------------------------ |------- |---------------:|--------------:|--------------:|---------------:|-------:|-------:|-------:|----------:|
| GetOrCreate_DictHit_Long            | net10  |     122.453 ns |     2.2861 ns |     4.2939 ns |     121.798 ns |      - |      - |      - |         - |
| GetOrCreate_DictHit_Long            | net481 |     231.800 ns |     4.1890 ns |     3.9183 ns |     231.807 ns |      - |      - |      - |         - |
| GetOrCreate_DictHit_Short           | net10  |      63.520 ns |     1.2611 ns |     2.7415 ns |      63.050 ns |      - |      - |      - |         - |
| GetOrCreate_DictHit_Short           | net481 |     102.215 ns |     2.0068 ns |     2.2306 ns |     102.851 ns |      - |      - |      - |         - |
| GetOrCreate_Empty                   | net10  |       3.977 ns |     0.0466 ns |     0.0436 ns |       3.977 ns |      - |      - |      - |         - |
| GetOrCreate_Empty                   | net481 |      12.998 ns |     0.0794 ns |     0.0704 ns |      12.974 ns |      - |      - |      - |         - |
| GetOrCreate_Miss_MiddleVaries_Long  | net10  |  64,299.202 ns | 1,456.5561 ns | 4,294.6888 ns |  63,710.525 ns |      - |      - |      - |    1776 B |
| GetOrCreate_Miss_MiddleVaries_Long  | net481 | 122,452.693 ns | 1,883.7521 ns | 1,762.0629 ns | 121,959.350 ns |      - |      - |      - |    2396 B |
| GetOrCreate_Miss_MiddleVaries_Short | net10  | 120,740.832 ns | 2,365.9418 ns | 2,992.1591 ns | 120,325.005 ns |      - |      - |      - |     466 B |
| GetOrCreate_Miss_MiddleVaries_Short | net481 | 111,712.478 ns | 2,134.4820 ns | 2,283.8718 ns | 111,125.402 ns | 0.0500 |      - |      - |     526 B |
| GetOrCreate_Miss_TailVaries_Long    | net10  |   1,783.799 ns |    35.2943 ns |    93.5955 ns |   1,770.565 ns | 0.1455 | 0.0364 |      - |    1993 B |
| GetOrCreate_Miss_TailVaries_Long    | net481 |   3,254.096 ns |    64.0123 ns |   137.7932 ns |   3,195.556 ns | 0.4182 | 0.1091 | 0.0364 |    2613 B |
| GetOrCreate_Miss_TailVaries_Short   | net10  |     892.240 ns |    17.0059 ns |    33.9626 ns |     883.653 ns | 0.0400 | 0.0200 |      - |     623 B |
| GetOrCreate_Miss_TailVaries_Short   | net481 |   1,437.001 ns |    32.7436 ns |    96.0314 ns |   1,410.052 ns | 0.1100 | 0.0400 | 0.0200 |     704 B |
| GetOrCreate_Miss_Unique_Long        | net10  |   1,779.124 ns |    34.7808 ns |    65.3267 ns |   1,762.583 ns | 0.1538 | 0.0462 |      - |    1974 B |
| GetOrCreate_Miss_Unique_Long        | net481 |   3,085.658 ns |    58.6840 ns |    65.2271 ns |   3,071.978 ns | 0.4308 | 0.1077 | 0.0462 |    2604 B |
| GetOrCreate_Miss_Unique_Short       | net10  |   1,291.066 ns |    25.5915 ns |    58.8006 ns |   1,290.112 ns | 0.0500 | 0.0250 | 0.0050 |     633 B |
| GetOrCreate_Miss_Unique_Short       | net481 |   1,769.416 ns |    34.1502 ns |    45.5896 ns |   1,769.187 ns | 0.1150 | 0.0400 | 0.0150 |     711 B |
| GetOrCreate_MruHit_Long             | net10  |      90.103 ns |     1.7478 ns |     2.2727 ns |      89.641 ns |      - |      - |      - |         - |
| GetOrCreate_MruHit_Long             | net481 |     194.187 ns |     3.6635 ns |     3.5981 ns |     193.643 ns |      - |      - |      - |         - |
| GetOrCreate_MruHit_Short            | net10  |      30.476 ns |     0.5442 ns |     0.5091 ns |      30.232 ns |      - |      - |      - |         - |
| GetOrCreate_MruHit_Short            | net481 |      63.255 ns |     1.2377 ns |     2.0679 ns |      63.529 ns |      - |      - |      - |         - |

// * Warnings *
MultimodalDistribution
  EquatableImmutableArrayInstanceCacheBenchmarks.GetOrCreate_Miss_TailVaries_Long: net10 -> It seems that the distribution is bimodal (mValue = 3.67)
  EquatableImmutableArrayInstanceCacheBenchmarks.GetOrCreate_Miss_Unique_Short: net10    -> It seems that the distribution is bimodal (mValue = 3.37)
MinIterationTime
  EquatableImmutableArrayInstanceCacheBenchmarks.GetOrCreate_Empty: net10                 -> The minimum observed iteration time is 97.671ms which is very small. It's recommended to increase it to at least 100ms using more operations.
  EquatableImmutableArrayInstanceCacheBenchmarks.GetOrCreate_Miss_TailVaries_Long: net10  -> The minimum observed iteration time is 90.708ms which is very small. It's recommended to increase it to at least 100ms using more operations.
  EquatableImmutableArrayInstanceCacheBenchmarks.GetOrCreate_Miss_TailVaries_Short: net10 -> The minimum observed iteration time is 83.901ms which is very small. It's recommended to increase it to at least 100ms using more operations.

*/
