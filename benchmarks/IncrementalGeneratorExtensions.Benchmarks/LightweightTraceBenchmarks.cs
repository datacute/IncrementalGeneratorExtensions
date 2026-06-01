using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Datacute.IncrementalGeneratorExtensions.Benchmarks
{
    /// <summary>
    /// Benchmarks for <see cref="LightweightTrace"/> — the zero-allocation instrumentation core.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>
    ///     <term>IncrementCount</term>
    ///     <description>Atomic <see cref="System.Collections.Concurrent.ConcurrentDictionary{TKey,TValue}.AddOrUpdate"/>
    ///     used for simple counters and histogram buckets.  Measured both as a plain counter and as a
    ///     composite (id + value) key to isolate the <see cref="LightweightTrace.EncodeKey"/> cost.</description>
    ///   </item>
    ///   <item>
    ///     <term>Add</term>
    ///     <description>Appends a timestamped event to the fixed-size ring buffer;
    ///     includes an <see cref="System.Threading.Interlocked.Increment(ref int)"/> and a
    ///     <see cref="System.Diagnostics.Stopwatch"/> read.</description>
    ///   </item>
    ///   <item>
    ///     <term>EncodeKey / DecodeKey</term>
    ///     <description>Pure arithmetic — no allocation; used to verify the bit-manipulation overhead is negligible.</description>
    ///   </item>
    /// </list>
    /// </remarks>
    [MemoryDiagnoser]
    [BenchmarkCategory("LightweightTrace")]
    [HideColumns("Runtime")]
    [SimpleJob(RuntimeMoniker.Net481, id: "net481")]
    [SimpleJob(RuntimeMoniker.Net10_0, id: "net10")]
    public class LightweightTraceBenchmarks
    {
        private const int SampleId = 42;
        private const int SampleValue = 7;

        // ------------------------------------------------------------------ //
        //  IncrementCount                                                     //
        // ------------------------------------------------------------------ //

        /// <summary>Increment a plain counter by its raw integer ID.</summary>
        [Benchmark]
        public void IncrementCount_Simple()
            => LightweightTrace.IncrementCount(SampleId);

        /// <summary>Increment a composite-key histogram bucket (id + numeric value).</summary>
        [Benchmark]
        public void IncrementCount_Composite()
            => LightweightTrace.IncrementCount(SampleId, SampleValue);

        /// <summary>Increment via the generic enum overload; exercises <see cref="System.Convert.ToInt32(object)"/>.</summary>
        [Benchmark]
        public void IncrementCount_EnumOverload()
            => LightweightTrace.IncrementCount(GeneratorStage.EquatableImmutableArrayCacheHit);

        // ------------------------------------------------------------------ //
        //  Add (ring-buffer write)                                            //
        // ------------------------------------------------------------------ //

        /// <summary>Write a plain event ID to the ring buffer.</summary>
        [Benchmark]
        public void Add_Simple()
            => LightweightTrace.Add(SampleId);

        /// <summary>Write a composite (id + numeric value) event to the ring buffer.</summary>
        [Benchmark]
        public void Add_Composite()
            => LightweightTrace.Add(SampleId, SampleValue);

        // ------------------------------------------------------------------ //
        //  EncodeKey / DecodeKey (pure bit-arithmetic)                        //
        // ------------------------------------------------------------------ //

        /// <summary>Encodes an id and value into a composite key integer.</summary>
        [Benchmark]
        public int EncodeKey()
            => LightweightTrace.EncodeKey(SampleId, SampleValue);

        /// <summary>Decodes a composite key back into its id, value, and mapped-value flag.</summary>
        [Benchmark]
        public int DecodeKey()
        {
            LightweightTrace.DecodeKey(
                LightweightTrace.EncodeKey(SampleId, SampleValue),
                out int id, out _, out _);
            return id;
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
                                                                                                                                                                                                            

| Method                      | Job    | Mean       | Error     | StdDev    | Median     | Allocated |
|---------------------------- |------- |-----------:|----------:|----------:|-----------:|----------:|
| Add_Composite               | net10  | 27.5475 ns | 0.2628 ns | 0.2330 ns | 27.5307 ns |         - |
| Add_Composite               | net481 | 38.5967 ns | 0.2179 ns | 0.2141 ns | 38.5779 ns |         - |
| Add_Simple                  | net10  | 27.3032 ns | 0.2102 ns | 0.1641 ns | 27.2731 ns |         - |
| Add_Simple                  | net481 | 38.3498 ns | 0.4221 ns | 0.3742 ns | 38.2586 ns |         - |
| DecodeKey                   | net10  |  0.0011 ns | 0.0032 ns | 0.0028 ns |  0.0000 ns |         - |
| DecodeKey                   | net481 |  7.6176 ns | 0.0723 ns | 0.0676 ns |  7.5977 ns |         - |
| EncodeKey                   | net10  |  0.0000 ns | 0.0000 ns | 0.0000 ns |  0.0000 ns |         - |
| EncodeKey                   | net481 |  0.0055 ns | 0.0098 ns | 0.0096 ns |  0.0001 ns |         - |
| IncrementCount_Composite    | net10  |  4.2282 ns | 0.0537 ns | 0.0419 ns |  4.2191 ns |         - |
| IncrementCount_Composite    | net481 |  9.2481 ns | 0.2052 ns | 0.2281 ns |  9.1806 ns |         - |
| IncrementCount_EnumOverload | net10  |  4.1845 ns | 0.0383 ns | 0.0340 ns |  4.1844 ns |         - |
| IncrementCount_EnumOverload | net481 |  7.6360 ns | 0.1747 ns | 0.1794 ns |  7.6799 ns |         - |
| IncrementCount_Simple       | net10  |  4.2527 ns | 0.1049 ns | 0.1248 ns |  4.2063 ns |         - |
| IncrementCount_Simple       | net481 |  7.8308 ns | 0.1236 ns | 0.1032 ns |  7.8495 ns |         - |

// * Warnings *
ZeroMeasurement
  LightweightTraceBenchmarks.DecodeKey: net10  -> The method duration is indistinguishable from the empty method duration
  LightweightTraceBenchmarks.EncodeKey: net10  -> The method duration is indistinguishable from the empty method duration
  LightweightTraceBenchmarks.EncodeKey: net481 -> The method duration is indistinguishable from the empty method duration
*/