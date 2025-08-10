#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Datacute.IncrementalGeneratorExtensions
{
    /// <summary>
    /// Zero-allocation instrumentation core for incremental source generators.
    /// <para>Features: ring-buffer timestamped event trace; composite-key counters (id + value + mapping flag) for histograms & categorical counts; automatic method-call frequency counting; method entry/exit tagging; single-int key encoding to minimize memory & dictionary churn; unified AppendDiagnosticsComment output (counters + trace) embeddable in generated code.</para>
    /// <para>Goal: fast, in-process behavioral visibility without external profilers or large allocations.</para>
    /// </summary>
    public static class LightweightTrace
    {
        private const int Capacity = 1024;

        /// <summary>
        /// The stride used to encode a composite key where <c>key = id + (value * CompositeValueShift)</c>.
        /// Exposed to clarify how counters/events pack both an ID and a value into a single <see cref="int"/>.
        /// </summary>
        public const int CompositeValueShift = 1 << 10; // 1024 distinct IDs per value bucket
        /// <summary>
        /// A flag bit used in composite keys to indicate that the encoded value should be mapped via <c>eventNameMap</c>.
        /// This lets values represent enum-like categories instead of plain numbers when formatting output.
        /// </summary>
        public const int MapValueFlag = 1 << 28;

        private static readonly DateTime StartTime = DateTime.UtcNow;
        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        private static readonly (long, int)[] Events = new (long, int)[Capacity];
        private static int _index;

        /// <summary>
        /// Adds an event to the trace log with the specified event ID.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <typeparam name="TEnum">The type of the event ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void Add<TEnum>(TEnum eventId) where TEnum : Enum => Add(Convert.ToInt32(eventId));
        /// <summary>
        /// Adds an event to the trace log with the specified event ID and numeric value.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <param name="value">The value associated with the event, which can be used for additional context or categorization.</param>
        /// <typeparam name="TEnum">The type of the event ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void Add<TEnum>(TEnum eventId, int value) where TEnum : Enum => Add(Convert.ToInt32(eventId), value);
        /// <summary>
        /// Adds an event to the trace log with the specified event ID and enum value.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <param name="value">The value associated with the event, which can be used for additional context or categorization.</param>
        /// <typeparam name="TEnumKey">The type of the event ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        /// <typeparam name="TEnumValue">The type of the value, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void Add<TEnumKey,TEnumValue>(TEnumKey eventId, TEnumValue value) where TEnumKey : Enum where TEnumValue : Enum => Add(Convert.ToInt32(eventId), Convert.ToInt32(value), true);
        /// <summary>
        /// Adds an event to the trace log with the specified numeric event ID and value.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <param name="value">The value associated with the event, which can be used for additional context or categorization.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        public static void Add(int eventId, int value, bool mapValue = false) => Add(EncodeKey(eventId, value, mapValue));
        /// <summary>
        /// Adds an event to the trace log with the specified numeric event ID.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <param name="mapValue">If true, and the eventId encapsulates a value, the value is treated as a mapped value when generating the diagnostic log.</param>
        public static void Add(int eventId, bool mapValue = false)
        {
            var index = Interlocked.Increment(ref _index) % Capacity;
            Events[index] = (Stopwatch.ElapsedTicks, eventId | (mapValue ? MapValueFlag : 0));
#if !DATACUTE_EXCLUDE_GENERATORSTAGE
            if ((eventId / CompositeValueShift) != Convert.ToInt32(GeneratorStage.MethodExit))
            {
                IncrementCount(GeneratorStage.MethodCall, eventId % CompositeValueShift, true);
            }
#else
            IncrementCount(eventId % CompositeValueShift);
#endif
        }

#if !DATACUTE_EXCLUDE_GENERATORSTAGE
        /// <summary>
        /// Adds a method entry event to the trace log with the specified event ID.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <typeparam name="TEnum">The type of the event ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void MethodEntry<TEnum>(TEnum eventId) where TEnum : Enum => Add(Convert.ToInt32(eventId), Convert.ToInt32(GeneratorStage.MethodEntry), true);
        /// <summary>
        /// Adds a method exit event to the trace log with the specified event ID.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <typeparam name="TEnum">The type of the event ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void MethodExit<TEnum>(TEnum eventId) where TEnum : Enum => Add(Convert.ToInt32(eventId), Convert.ToInt32(GeneratorStage.MethodExit), true);
#endif

        /// <summary>
        /// Appends the trace log to the provided StringBuilder.
        /// </summary>
        /// <param name="stringBuilder">The StringBuilder to append the trace log to.</param>
        /// <param name="eventNameMap">A dictionary mapping event IDs and values to their names, used for more readable output.</param>
        public static void AppendTrace(this StringBuilder stringBuilder, Dictionary<int, string> eventNameMap = null)
        {
            if (stringBuilder is null)
            {
                return;
            }

            var index = _index;
            for (var i = 0; i < Capacity; i++)
            {
                index = (index + 1) % Capacity;
                var (timestamp, eventId) = Events[index];
                if (timestamp > 0)
                {
                    var textAndValue = GetTextAndValue(eventNameMap, eventId);
                    stringBuilder.AppendFormat("{0:o} [{1:000}] {2}",
                            StartTime.AddTicks(timestamp),
                            eventId % CompositeValueShift,
                            textAndValue)
                        .AppendLine();
                }
            }
        }

        private static readonly ConcurrentDictionary<int, long> Counters = new ConcurrentDictionary<int, long>();

        /// <summary>
        /// Increments the value of a given key by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to increment.</param>
        /// <typeparam name="TEnum">The type of the counter ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void IncrementCount<TEnum>(TEnum counterId) where TEnum : Enum => IncrementCount(Convert.ToInt32(counterId));
        /// <summary>
        /// Increments the value of a given key[value] combination by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to increment.</param>
        /// <param name="value">The value associated with the counter, which can be used for additional context or categorization.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        /// <typeparam name="TEnum">The type of the counter ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        /// <example>
        /// <code lang="csharp">
        /// LightweightTrace.IncrementCount(GeneratorStage.EquatableImmutableArrayLength, values.Length);
        /// LightweightTrace.IncrementCount(GeneratorStage.MethodCall, eventId, true);
        /// </code>
        /// </example>
        public static void IncrementCount<TEnum>(TEnum counterId, int value, bool mapValue = false) where TEnum : Enum => IncrementCount(Convert.ToInt32(counterId), value, mapValue);

        /// <summary>
        /// Decrements the value of a given key by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to decrement.</param>
        /// <typeparam name="TEnum">The type of the counter ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void DecrementCount<TEnum>(TEnum counterId) where TEnum : Enum => DecrementCount(Convert.ToInt32(counterId));
        /// <summary>
        /// Decrements the value of a given key[value] combination by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to decrement.</param>
        /// <param name="value">The value associated with the counter, which can be used for additional context or categorization.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        /// <typeparam name="TEnum">The type of the counter ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void DecrementCount<TEnum>(TEnum counterId, int value, bool mapValue = false) where TEnum : Enum => DecrementCount(Convert.ToInt32(counterId), value, mapValue);

        /// <summary>
        /// Increments the value of a given key by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to increment.</param>
        public static void IncrementCount(int counterId) => Counters.AddOrUpdate(counterId, 1, (_, count) => count + 1);
        /// <summary>
        /// Increments the value of a given key[value] combination by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to increment.</param>
        /// <param name="value">The value associated with the counter, which can be used for additional context or categorization.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        public static void IncrementCount(int counterId, int value, bool mapValue = false) => Counters.AddOrUpdate(EncodeKey(counterId, value, mapValue), 1, (_, count) => count + 1);

        /// <summary>
        /// Decrements the value of a given key by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to increment.</param>
        public static void DecrementCount(int counterId) => Counters.AddOrUpdate(counterId, -1, (_, count) => count - 1);
        /// <summary>
        /// Decrements the value of a given key[value] combination by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to decrement.</param>
        /// <param name="value">The value associated with the counter, which can be used for additional context or categorization.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        public static void DecrementCount(int counterId, int value, bool mapValue = false) => Counters.AddOrUpdate(EncodeKey(counterId, value, mapValue), -1, (_, count) => count - 1);

        /// <summary>
        /// Gets a string with the current cache performance metrics.
        /// It intelligently separates simple counters from histogram data based on key prefixes.
        /// </summary>
        /// <param name="stringBuilder">The StringBuilder to append the performance metrics to.</param>
        /// <param name="eventNameMap">A dictionary mapping event IDs to their names, used for more readable output.</param>
        public static void AppendCounts(this StringBuilder stringBuilder, Dictionary<int, string> eventNameMap = null)
        {
            if (stringBuilder is null)
            {
                return;
            }

            // Order by key for a consistent, readable output
            foreach (var kvp in Counters.OrderBy(kvp => kvp.Key % CompositeValueShift).ThenBy(kvp => kvp.Key))
            {
                int counterId = kvp.Key;
                long count = kvp.Value;

                var textAndValue = GetTextAndValue(eventNameMap, counterId);
                stringBuilder.AppendFormat(
                    "[{0:000}] {1}: {2}", 
                    counterId % CompositeValueShift, textAndValue, count)
                    .AppendLine();
            }
        }

        /// <summary>
        /// Encodes a composite key combining an ID and a value into a single int.
        /// Set <paramref name="mapValue"/> when the value represents a categorical/enum mapping rather than a numeric measurement.
        /// </summary>
        /// <param name="id">The base ID (0..CompositeValueShift-1).</param>
        /// <param name="value">The associated value bucket or enum ordinal.</param>
        /// <param name="mapValue">True to mark the value as mapped (name lookup) instead of numeric.</param>
        public static int EncodeKey(int id, int value, bool mapValue = false) => id + (value * CompositeValueShift) + (mapValue ? MapValueFlag : 0);

        /// <summary>
        /// Decodes a composite key into its ID, value, and mapped-value flag.
        /// </summary>
        /// <param name="key">The composite key previously produced by <see cref="EncodeKey"/> or any API that accepts (id,value).</param>
        /// <param name="id">The extracted base ID.</param>
        /// <param name="value">The extracted associated value.</param>
        /// <param name="isMappedValue">True if the value should be mapped by name for display.</param>
        public static void DecodeKey(int key, out int id, out int value, out bool isMappedValue)
        {
            isMappedValue = (key & MapValueFlag) != 0;
            var unflagged = key & ~MapValueFlag;
            id = unflagged % CompositeValueShift;
            value = unflagged / CompositeValueShift;
        }

        private static string GetTextAndValue(Dictionary<int, string> eventNameMap, int key)
        {
            int id = key % CompositeValueShift;
            int value = key / CompositeValueShift;

            if ((key & MapValueFlag) != 0)
            {
                value = (key & ~MapValueFlag) / CompositeValueShift;
            }

            string text  = null;
            if (eventNameMap != null)
            {
                eventNameMap.TryGetValue(id, out text);
            }
            if (text == null)
            {
                text = string.Empty;
            }

            string valueText = null;
            if (key >= CompositeValueShift)
            {
                if (eventNameMap != null && (key & MapValueFlag) != 0)
                {
                    eventNameMap.TryGetValue(value, out valueText);
                }
                if (valueText == null)
                {
                    valueText = $"{value}";
                }
            }

            return (key >= CompositeValueShift) ? $"{text} ({valueText})" : text;
        }

        /// <summary>
        /// Append a comment containing all the diagnostic counters and logs to the StringBuilder.
        /// </summary>
        /// <param name="stringBuilder">The StringBuilder to append the diagnostics comment to.</param>
        /// <param name="eventNameMap">A dictionary mapping event IDs to their names, used for more readable output.</param>
        /// <example>
        /// <code lang="csharp">
        /// _buffer.AppendDiagnosticsComment(GeneratorStageDescriptions.GeneratorStageNameMap);
        /// </code>
        /// </example>
        public static void AppendDiagnosticsComment(this StringBuilder stringBuilder, Dictionary<int, string> eventNameMap = null)
        {
            if (stringBuilder is null)
            {
                return;
            }

            stringBuilder.AppendLine("/* Diagnostics");
            stringBuilder.AppendLine("Counters:");
            stringBuilder.AppendCounts(eventNameMap);
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Trace Log:");
            stringBuilder.AppendTrace(eventNameMap);
            stringBuilder.AppendLine("*/");
        }
    }
}
#endif