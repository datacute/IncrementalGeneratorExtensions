using System.Collections.Immutable;
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

        [GlobalSetup]
        public void Setup()
        {
            _shortA = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildShort(1, 2, 3));
            _shortB = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildShort(10, 20, 30));

            _longA = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildAscending(20));
            _longB = EquatableImmutableArray<TypeContext>.Create(ImmutableArrayFactory.BuildDescending(20));
        }

        // ------------------------------------------------------------------ //
        //  Equals — reference equality (same instance from cache)             //
        // ------------------------------------------------------------------ //

        /// <summary>Reference-equality short-circuit on a 3-element array.</summary>
        [Benchmark]
        public bool Equals_SameInstance_Short() => _shortA.Equals(_shortA);

        /// <summary>Reference-equality short-circuit on a 20-element array.</summary>
        [Benchmark]
        public bool Equals_SameInstance_Long() => _longA.Equals(_longA);

        // ------------------------------------------------------------------ //
        //  Equals — hash-code mismatch (fast rejection, no element scan)      //
        // ------------------------------------------------------------------ //

        /// <summary>Hash-mismatch early exit on a 3-element array.</summary>
        [Benchmark]
        public bool Equals_DifferentHashCode_Short() => _shortA.Equals(_shortB);

        /// <summary>Hash-mismatch early exit on a 20-element array.</summary>
        [Benchmark]
        public bool Equals_DifferentHashCode_Long() => _longA.Equals(_longB);

        // ------------------------------------------------------------------ //
        //  GetHashCode — returns cached field                                 //
        // ------------------------------------------------------------------ //

        /// <summary>Returns the pre-computed hash stored in the instance (3-element array).</summary>
        [Benchmark]
        public int GetHashCode_Short() => _shortA.GetHashCode();

        /// <summary>Returns the pre-computed hash stored in the instance (20-element array).</summary>
        [Benchmark]
        public int GetHashCode_Long() => _longA.GetHashCode();
    }
}

/*
BenchmarkDotNet v0.15.1, Windows 11 (10.0.26200.8457)
12th Gen Intel Core i7-12700H 2.30GHz, 1 CPU, 20 logical and 14 physical cores                                                                                                                              
.NET SDK 10.0.300                                                                                                                                                                                           
  [Host] : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2                                                                                                                                                    
  net10  : .NET 10.0.8 (10.0.826.23019), X64 RyuJIT AVX2                                                                                                                                                    
  net481 : .NET Framework 4.8.1 (4.8.9325.0), X64 RyuJIT VectorSize=256                                                                                                                                     
                                                                                                                                                                                                            

| Method                         | Job    | Mean       | Error     | StdDev    | Median     | Allocated |
|------------------------------- |------- |-----------:|----------:|----------:|-----------:|----------:|
| Equals_DifferentHashCode_Long  | net10  |  0.3700 ns | 0.0341 ns | 0.0444 ns |  0.3713 ns |         - |
| Equals_DifferentHashCode_Long  | net481 |  9.4795 ns | 0.1355 ns | 0.1201 ns |  9.4434 ns |         - |
| Equals_DifferentHashCode_Short | net10  |  0.2715 ns | 0.0305 ns | 0.0299 ns |  0.2731 ns |         - |
| Equals_DifferentHashCode_Short | net481 |  9.4793 ns | 0.0786 ns | 0.0735 ns |  9.4524 ns |         - |
| Equals_SameInstance_Long       | net10  |  0.0022 ns | 0.0056 ns | 0.0052 ns |  0.0000 ns |         - |
| Equals_SameInstance_Long       | net481 | 13.6385 ns | 0.1473 ns | 0.1306 ns | 13.6192 ns |         - |
| Equals_SameInstance_Short      | net10  |  0.0146 ns | 0.0194 ns | 0.0181 ns |  0.0042 ns |         - |
| Equals_SameInstance_Short      | net481 |  9.5169 ns | 0.0820 ns | 0.0767 ns |  9.5437 ns |         - |
| GetHashCode_Long               | net10  |  0.0054 ns | 0.0085 ns | 0.0071 ns |  0.0020 ns |         - |
| GetHashCode_Long               | net481 |  0.0002 ns | 0.0006 ns | 0.0005 ns |  0.0000 ns |         - |
| GetHashCode_Short              | net10  |  0.0204 ns | 0.0239 ns | 0.0223 ns |  0.0192 ns |         - |
| GetHashCode_Short              | net481 |  0.0000 ns | 0.0000 ns | 0.0000 ns |  0.0000 ns |         - |

// * Warnings *
ZeroMeasurement
  EquatableImmutableArrayBenchmarks.Equals_SameInstance_Long: net10  -> The method duration is indistinguishable from the empty method duration
  EquatableImmutableArrayBenchmarks.Equals_SameInstance_Short: net10 -> The method duration is indistinguishable from the empty method duration
  EquatableImmutableArrayBenchmarks.GetHashCode_Long: net10          -> The method duration is indistinguishable from the empty method duration
  EquatableImmutableArrayBenchmarks.GetHashCode_Long: net481         -> The method duration is indistinguishable from the empty method duration
  EquatableImmutableArrayBenchmarks.GetHashCode_Short: net10         -> The method duration is indistinguishable from the empty method duration
  EquatableImmutableArrayBenchmarks.GetHashCode_Short: net481        -> The method duration is indistinguishable from the empty method duration
MultimodalDistribution
  EquatableImmutableArrayBenchmarks.Equals_DifferentHashCode_Long: net10  -> It seems that the distribution can have several modes (mValue = 3.11)
  EquatableImmutableArrayBenchmarks.Equals_DifferentHashCode_Short: net10 -> It seems that the distribution can have several modes (mValue = 2.89)

*/
