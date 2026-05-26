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
| Add_Composite               | net10  | 27.9431 ns | 0.4604 ns | 0.4081 ns | 27.8671 ns |         - |
| Add_Composite               | net481 | 39.4892 ns | 0.7954 ns | 1.1151 ns | 38.9476 ns |         - |
| Add_Simple                  | net10  | 27.7514 ns | 0.2972 ns | 0.2634 ns | 27.7237 ns |         - |
| Add_Simple                  | net481 | 38.8105 ns | 0.3512 ns | 0.3113 ns | 38.7969 ns |         - |
| DecodeKey                   | net10  |  0.0001 ns | 0.0004 ns | 0.0004 ns |  0.0000 ns |         - |
| DecodeKey                   | net481 |  7.8579 ns | 0.0680 ns | 0.0603 ns |  7.8615 ns |         - |
| EncodeKey                   | net10  |  0.0069 ns | 0.0080 ns | 0.0071 ns |  0.0057 ns |         - |
| EncodeKey                   | net481 |  0.0164 ns | 0.0101 ns | 0.0094 ns |  0.0181 ns |         - |
| IncrementCount_Composite    | net10  |  4.2033 ns | 0.0443 ns | 0.0370 ns |  4.2069 ns |         - |
| IncrementCount_Composite    | net481 |  8.9732 ns | 0.1968 ns | 0.2343 ns |  9.0402 ns |         - |
| IncrementCount_EnumOverload | net10  |  4.3778 ns | 0.1078 ns | 0.1283 ns |  4.3421 ns |         - |
| IncrementCount_EnumOverload | net481 |  8.1454 ns | 0.1772 ns | 0.2177 ns |  8.1805 ns |         - |
| IncrementCount_Simple       | net10  |  4.2445 ns | 0.0648 ns | 0.0574 ns |  4.2432 ns |         - |
| IncrementCount_Simple       | net481 |  8.2045 ns | 0.1839 ns | 0.2044 ns |  8.1801 ns |         - |

// * Warnings *
ZeroMeasurement
  LightweightTraceBenchmarks.DecodeKey: net10  -> The method duration is indistinguishable from the empty method duration
  LightweightTraceBenchmarks.EncodeKey: net10  -> The method duration is indistinguishable from the empty method duration
  LightweightTraceBenchmarks.EncodeKey: net481 -> The method duration is indistinguishable from the empty method duration
*/