using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Datacute.IncrementalGeneratorExtensions.Benchmarks
{
    /// <summary>
    /// Orders the summary table by method name first, then by job/runtime, so that
    /// both runtime results for each method appear together rather than all results
    /// for one runtime appearing before all results for another.
    /// </summary>
    internal class MethodFirstOrderer : DefaultOrderer
    {
        /// <summary>The shared singleton instance.</summary>
        public new static readonly IOrderer Instance = new MethodFirstOrderer();

        /// <inheritdoc/>
        public override IEnumerable<BenchmarkCase> GetSummaryOrder(
            ImmutableArray<BenchmarkCase> benchmarksCases, Summary summary)
            => benchmarksCases
                .OrderBy(b => b.Descriptor.WorkloadMethodDisplayInfo)
                .ThenBy(b => b.Job.ResolvedId);
    }
}

