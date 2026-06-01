using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Datacute.IncrementalGeneratorExtensions.Benchmarks
{
    /// <summary>
    /// Benchmarks for <see cref="EquatableImmutableArray{T}"/> equality and hash-code paths.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>
    ///     <term>SameInstance</term>
    ///     <description>
    ///       The common case when the instance cache is active: <see cref="object.ReferenceEquals"/>
    ///       short-circuits the comparison immediately.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>DifferentHashCode</term>
    ///     <description>
    ///       Two instances with differing hash codes; the comparison returns <see langword="false"/>
    ///       after a single integer comparison — no element traversal.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>GetHashCode</term>
    ///     <description>
    ///       Reads the pre-computed hash stored in the field — effectively free.
    ///     </description>
    ///   </item>
    /// </list>
    /// Creation benchmarks (including cached vs no-cache comparison) are in
    /// <see cref="EquatableImmutableArrayCreateBenchmarks"/>.
    /// </remarks>
    [MemoryDiagnoser]
    [BenchmarkCategory("EquatableImmutableArray")]
    [HideColumns("Runtime")]
    [SimpleJob(RuntimeMoniker.Net481, id: "net481")]
    [SimpleJob(RuntimeMoniker.Net10_0, id: "net10")]
    public class EquatableImmutableArrayBenchmarks
    {
        private EquatableImmutableArray<TypeContext> _shortA = null!;
        private EquatableImmutableArray<TypeContext> _shortB = null!;   // different values → different hash
        private EquatableImmutableArray<TypeContext> _longA = null!;
        private EquatableImmutableArray<TypeContext> _longB = null!;    // different values → different hash

        private const int OpsEqualsSame = 400_000_000;
        private const int OpsEqualsDiff = 200_000_000;
        private const int OpsHashCode = 500_000_000;

        [GlobalSetup]
        public void Setup()
        {
            _shortA = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildShort(1, 2, 3));
            _shortB = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildShort(10, 20, 30));

            _longA = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildAscending(20));
            _longB = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildDescending(20));
        }

        [IterationSetup]
        public void IterationSetup()
        {
            EquatableImmutableArrayInstanceCache<TypeContext>.Clear();
            
            // Re-populate the cache
            _shortA = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildShort(1, 2, 3));
            _shortB = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildShort(10, 20, 30));

            _longA = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildAscending(20));
            _longB = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildDescending(20));
        }

        // ------------------------------------------------------------------ //
        //  Equals — reference equality (same instance from cache)             //
        // ------------------------------------------------------------------ //

        /// <summary>Reference-equality short-circuit on a 3-element array.</summary>
        [Benchmark(OperationsPerInvoke = OpsEqualsSame)]
        public bool Equals_SameInstance_Short()
        {
            bool result = false;
            for (int i = 0; i < OpsEqualsSame; i++)
            {
                result = _shortA.Equals(_shortA);
            }
            return result;
        }

        /// <summary>Reference-equality short-circuit on a 20-element array.</summary>
        [Benchmark(OperationsPerInvoke = OpsEqualsSame)]
        public bool Equals_SameInstance_Long()
        {
            bool result = false;
            for (int i = 0; i < OpsEqualsSame; i++)
            {
                result = _longA.Equals(_longA);
            }
            return result;
        }

        // ------------------------------------------------------------------ //
        //  Equals — hash-code mismatch (fast rejection, no element scan)      //
        // ------------------------------------------------------------------ //

        /// <summary>Hash-mismatch early exit on a 3-element array.</summary>
        [Benchmark(OperationsPerInvoke = OpsEqualsDiff)]
        public bool Equals_DifferentHashCode_Short()
        {
            bool result = false;
            for (int i = 0; i < OpsEqualsDiff; i++)
            {
                result = _shortA.Equals(_shortB);
            }
            return result;
        }

        /// <summary>Hash-mismatch early exit on a 20-element array.</summary>
        [Benchmark(OperationsPerInvoke = OpsEqualsDiff)]
        public bool Equals_DifferentHashCode_Long()
        {
            bool result = false;
            for (int i = 0; i < OpsEqualsDiff; i++)
            {
                result = _longA.Equals(_longB);
            }
            return result;
        }

        // ------------------------------------------------------------------ //
        //  GetHashCode — returns cached field                                 //
        // ------------------------------------------------------------------ //

        /// <summary>Returns the pre-computed hash stored in the instance (3-element array).</summary>
        [Benchmark(OperationsPerInvoke = OpsHashCode)]
        public int GetHashCode_Short()
        {
            int result = 0;
            for (int i = 0; i < OpsHashCode; i++)
            {
                result = _shortA.GetHashCode();
            }
            return result;
        }

        /// <summary>Returns the pre-computed hash stored in the instance (20-element array).</summary>
        [Benchmark(OperationsPerInvoke = OpsHashCode)]
        public int GetHashCode_Long()
        {
            int result = 0;
            for (int i = 0; i < OpsHashCode; i++)
            {
                result = _longA.GetHashCode();
            }
            return result;
        }
    }
}

/*
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
12th Gen Intel Core i7-12700H 2.30GHz, 1 CPU, 20 logical and 14 physical cores
.NET SDK 10.0.300
  [Host] : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  net10  : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  net481 : .NET Framework 4.8.1 (4.8.9337.0), X64 RyuJIT VectorSize=256

InvocationCount=1  UnrollFactor=1

| Method                         | Job    | Mean       | Error     | StdDev    | Allocated |
|------------------------------- |------- |-----------:|----------:|----------:|----------:|
| Equals_DifferentHashCode_Long  | net10  |  0.4497 ns | 0.0089 ns | 0.0158 ns |         - |
| Equals_DifferentHashCode_Long  | net481 |  8.8106 ns | 0.1403 ns | 0.1244 ns |         - |
| Equals_DifferentHashCode_Short | net10  |  0.4426 ns | 0.0084 ns | 0.0087 ns |         - |
| Equals_DifferentHashCode_Short | net481 |  8.7547 ns | 0.1095 ns | 0.1025 ns |         - |
| Equals_SameInstance_Long       | net10  |  0.2364 ns | 0.0032 ns | 0.0029 ns |         - |
| Equals_SameInstance_Long       | net481 |  8.6131 ns | 0.1268 ns | 0.1059 ns |         - |
| Equals_SameInstance_Short      | net10  |  0.2372 ns | 0.0040 ns | 0.0041 ns |         - |
| Equals_SameInstance_Short      | net481 | 17.5429 ns | 0.1368 ns | 0.1280 ns |         - |
| GetHashCode_Long               | net10  |  0.2192 ns | 0.0032 ns | 0.0028 ns |         - |
| GetHashCode_Long               | net481 |  0.2190 ns | 0.0020 ns | 0.0018 ns |         - |
| GetHashCode_Short              | net10  |  0.2253 ns | 0.0044 ns | 0.0064 ns |         - |
| GetHashCode_Short              | net481 |  0.2214 ns | 0.0033 ns | 0.0043 ns |         - |

// * Warnings *
MinIterationTime
  EquatableImmutableArrayBenchmarks.Equals_DifferentHashCode_Long: net10  -> The minimum observed iteration time is 86.679ms which is very small. It's recommended to increase it to at least 100ms using more operations.
  EquatableImmutableArrayBenchmarks.Equals_DifferentHashCode_Short: net10 -> The minimum observed iteration time is 86.764ms which is very small. It's recommended to increase it to at least 100ms using more operations.
  EquatableImmutableArrayBenchmarks.Equals_SameInstance_Long: net10       -> The minimum observed iteration time is 93.033ms which is very small. It's recommended to increase it to at least 100ms using more operations.
  EquatableImmutableArrayBenchmarks.Equals_SameInstance_Short: net10      -> The minimum observed iteration time is 93.284ms which is very small. It's recommended to increase it to at least 100ms using more operations.


*/
