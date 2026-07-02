#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
#if DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE
using System.Diagnostics.Tracing;
#endif
using System.Linq;
using System.Text;
using System.Threading;

namespace Datacute.IncrementalGeneratorExtensions
{
    /// <summary>
    /// Zero-allocation instrumentation core for incremental source generators.
    /// <para>Features: ring-buffer timestamped event trace; composite-key counters (id + value + mapping flag) for histograms & categorical counts; automatic method-call frequency counting; method entry/exit tagging; single-int key encoding to minimize memory & dictionary churn; unified AppendDiagnosticsComment output (counters + trace) embeddable in generated code.</para>
    /// <para>Goal: fast, in-process behavioural visibility without external profilers or large allocations.</para>
    /// </summary>
    public static class LightweightTrace
    {
        private const int Capacity = 1024;

        /// <summary>
        /// Size of the contiguous ID range (also the multiplier for packing values):
        /// compositeKey = id + (value * CompositeValueShift) (+ MapValueFlag).
        /// id must be &lt; CompositeValueShift; value is shifted by this amount when encoded.
        /// </summary>
        public const int CompositeValueShift = 1 << ValueShift; // 1024 id range (value bucket multiplier)

        /// <summary>
        /// A flag bit used in composite keys to indicate that the encoded value should be mapped via <c>eventNameMap</c>.
        /// This lets values represent enum-like categories instead of plain numbers when formatting output.
        /// </summary>
        public const int MapValueFlag = 1 << 28;

        // Bit layout (little endian within int):
        // bits 0..9   : id        (0..1023)
        // bits 10..27 : value     (0..(2^18 - 1))
        // bit  28     : map flag  (categorical value lookup)
        // bits 29..31 : currently unused (reserved)
        private const int IdMask = CompositeValueShift - 1; // 0x3FF mask for id bits (bits 0..ValueShift-1)
        private const int ValueShift = 10;                  // Number of bits reserved for the id (0..(2^ValueShift - 1))
        private const int ValueBits = 18;                   // Number of bits reserved for the value component (bits ValueShift .. ValueShift+ValueBits-1)
        private const int ValueMask = ((1 << ValueBits) - 1) << ValueShift; // covers bits 10..27

        private static readonly DateTime StartTime = DateTime.UtcNow;
        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        private static readonly (long, int)[] Events = new (long, int)[Capacity];
        private static int _index;

#if DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE
        private static LightweightTraceEventSource _etwLog;
        private static EventLevel _etwLevel = EventLevel.Informational;
#endif
        /// <summary>
        /// Custom names supplied by the caller for event IDs and mapped values.
        /// </summary>
        /// <remarks>
        /// Used for known names wired by the generator, such as stage descriptions.
        /// Lookup precedence is: the map passed to the formatting call, then this map (set via <see cref="SetCustomEventNames"/>), then dynamically registered names.
        /// </remarks>
        private static Dictionary<int, string> _customEventNames;

        /// <summary>
        /// Runtime-registered names keyed by allocated IDs.
        /// </summary>
        private static ConcurrentDictionary<int, string> _dynamicValueNames;

        /// <summary>
        /// Backing counter for dynamically registered names.
        /// </summary>
        /// <remarks>
        /// Starts one below <see cref="CompositeValueShift"/> so the first allocated ID enters the dynamic ID range.
        /// </remarks>
        private static int _nextId = CompositeValueShift - 1;

        /// <summary>
        /// Supplies custom names for known event IDs and mapped values.
        /// </summary>
        /// <param name="customEventNames">The custom name map.</param>
        /// <remarks>
        /// Use this for known names that are available at wiring time.
        /// </remarks>
        public static void SetCustomEventNames(Dictionary<int, string> customEventNames)
        {
            _customEventNames = customEventNames;
        }

        /// <summary>
        /// Dynamically registers a mapped-value name and returns a unique ID for it.
        /// </summary>
        /// <param name="name">The name to register.</param>
        /// <returns>A unique ID representing the registered mapped value name.</returns>
        /// <remarks>
        /// IDs are allocated from the contiguous range above <see cref="CompositeValueShift"/> and are thread-safe.
        /// This is typically used for runtime-discovered names such as generic type names.
        /// </remarks>
        public static int RegisterName(string name)
        {
            var id = Interlocked.Increment(ref _nextId);
            var dynamicValueNames = _dynamicValueNames;
            if (dynamicValueNames == null)
            {
                dynamicValueNames = new ConcurrentDictionary<int, string>();
                Interlocked.CompareExchange(ref _dynamicValueNames, dynamicValueNames, null);
                dynamicValueNames = _dynamicValueNames;
            }

            dynamicValueNames[id] = name;
            return id;
        }

#if DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE
        /// <summary>
        /// Initializes ETW logging with a custom event source name and event level.
        /// </summary>
        /// <param name="eventSourceName">The name of the ETW event source.</param>
        /// <param name="eventLevel">The event level for the traces.</param>
        /// <param name="eventNameMap">A dictionary mapping event IDs and values to their names, used to turn raw numeric event IDs and values into meaningful ETW output.</param>
        public static void InitializeEtw(string eventSourceName = "Datacute-IncrementalGenerator-Trace", EventLevel eventLevel = EventLevel.Informational, Dictionary<int, string> eventNameMap = null)
        {
            if (_etwLog == null || _etwLog.Name != eventSourceName)
            {
                try
                {
                    _etwLog?.Dispose();
                    _etwLog = new LightweightTraceEventSource(eventSourceName);
                }
                catch (ArgumentException)
                {
                    _etwLog = null;
                }
            }

            _etwLevel = eventLevel;
            if (eventNameMap != null)
            {
                SetCustomEventNames(eventNameMap);
            }
        }

        private sealed class LightweightTraceEventSource : EventSource
        {
            public LightweightTraceEventSource(string eventSourceName) : base(eventSourceName)
            {
            }

            [Event(1, Level = EventLevel.Critical)]
            private void TraceCritical(int id, int value, bool isMappedValue, string message) => WriteEvent(1, id, value, isMappedValue, message);

            [Event(2, Level = EventLevel.Error)]
            private void TraceError(int id, int value, bool isMappedValue, string message) => WriteEvent(2, id, value, isMappedValue, message);

            [Event(3, Level = EventLevel.Warning)]
            private void TraceWarning(int id, int value, bool isMappedValue, string message) => WriteEvent(3, id, value, isMappedValue, message);

            [Event(4, Level = EventLevel.Informational)]
            private void TraceInformational(int id, int value, bool isMappedValue, string message) => WriteEvent(4, id, value, isMappedValue, message);

            [Event(5, Level = EventLevel.Verbose)]
            private void TraceVerbose(int id, int value, bool isMappedValue, string message) => WriteEvent(5, id, value, isMappedValue, message);

            [Event(6, Level = EventLevel.Critical)]
            private void CountCritical(int id, int value, bool isMappedValue, long count, string message) => WriteEvent(6, id, value, isMappedValue, count, message);

            [Event(7, Level = EventLevel.Error)]
            private void CountError(int id, int value, bool isMappedValue, long count, string message) => WriteEvent(7, id, value, isMappedValue, count, message);

            [Event(8, Level = EventLevel.Warning)]
            private void CountWarning(int id, int value, bool isMappedValue, long count, string message) => WriteEvent(8, id, value, isMappedValue, count, message);

            [Event(9, Level = EventLevel.Informational)]
            private void CountInformational(int id, int value, bool isMappedValue, long count, string message) => WriteEvent(9, id, value, isMappedValue, count, message);

            [Event(10, Level = EventLevel.Verbose)]
            private void CountVerbose(int id, int value, bool isMappedValue, long count, string message) => WriteEvent(10, id, value, isMappedValue, count, message);

            [NonEvent]
            public void Trace(EventLevel level, int id, int value, bool isMappedValue, string message)
            {
                if (IsEnabled(level, EventKeywords.None))
                {
                    switch (level)
                    {
                        case EventLevel.Critical: TraceCritical(id, value, isMappedValue, message); break;
                        case EventLevel.Error: TraceError(id, value, isMappedValue, message); break;
                        case EventLevel.Warning: TraceWarning(id, value, isMappedValue, message); break;
                        case EventLevel.Informational: TraceInformational(id, value, isMappedValue, message); break;
                        case EventLevel.Verbose: TraceVerbose(id, value, isMappedValue, message); break;
                        default: TraceInformational(id, value, isMappedValue, message); break;
                    }
                }
            }

            [NonEvent]
            public void Count(EventLevel level, int id, int value, bool isMappedValue, long count, string message)
            {
                if (IsEnabled(level, EventKeywords.None))
                {
                    switch (level)
                    {
                        case EventLevel.Critical: CountCritical(id, value, isMappedValue, count, message); break;
                        case EventLevel.Error: CountError(id, value, isMappedValue, count, message); break;
                        case EventLevel.Warning: CountWarning(id, value, isMappedValue, count, message); break;
                        case EventLevel.Informational: CountInformational(id, value, isMappedValue, count, message); break;
                        case EventLevel.Verbose: CountVerbose(id, value, isMappedValue, count, message); break;
                        default: CountInformational(id, value, isMappedValue, count, message); break;
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Adds an event to the trace log with the specified event ID.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <typeparam name="TEnum">The type of the event ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void Add<TEnum>(TEnum eventId) where TEnum : Enum
            => AddInternal(
                eventId: System.Runtime.CompilerServices.Unsafe.As<TEnum, int>(ref eventId), 
                value: 0, 
                mapValue: false);

        /// <summary>
        /// Adds an event to the trace log with the specified event ID and numeric value.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <param name="value">The value associated with the event, which can be used for additional context or categorisation.</param>
        /// <typeparam name="TEnum">The type of the event ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void Add<TEnum>(TEnum eventId, int value) where TEnum : Enum
            => AddInternal(
                eventId: System.Runtime.CompilerServices.Unsafe.As<TEnum, int>(ref eventId), 
                value: value, 
                mapValue: false);

        /// <summary>
        /// Adds an event to the trace log with the specified event ID and enum value.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <param name="value">The value associated with the event, which can be used for additional context or categorisation.</param>
        /// <typeparam name="TEnumKey">The type of the event ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        /// <typeparam name="TEnumValue">The type of the value, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void Add<TEnumKey, TEnumValue>(TEnumKey eventId, TEnumValue value)
            where TEnumKey : Enum
            where TEnumValue : Enum
            => AddInternal(
                eventId: System.Runtime.CompilerServices.Unsafe.As<TEnumKey, int>(ref eventId), 
                value: System.Runtime.CompilerServices.Unsafe.As<TEnumValue, int>(ref value), 
                mapValue: true);

        /// <summary>
        /// Adds an event to the trace log with the specified numeric event ID and value.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <param name="value">The value associated with the event, which can be used for additional context or categorisation.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        public static void Add(int eventId, int value, bool mapValue = false)
            => AddInternal(eventId, value, mapValue);

        /// <summary>
        /// Adds an event to the trace log with the specified numeric event ID.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <param name="mapValue">If true, and the eventId encapsulates a value, the value is treated as a mapped value when generating the diagnostic log.</param>
        public static void Add(int eventId, bool mapValue = false)
            => AddInternal(eventId, 0, mapValue);

#if !DATACUTE_EXCLUDE_GENERATORSTAGE
        /// <summary>
        /// Adds a method entry event to the trace log with the specified event ID.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <typeparam name="TEnum">The type of the event ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void MethodEntry<TEnum>(TEnum eventId) where TEnum : Enum
            => AddInternal(
                eventId: System.Runtime.CompilerServices.Unsafe.As<TEnum, int>(ref eventId), 
                value: (int)GeneratorStage.MethodEntry, 
                mapValue: true);

        /// <summary>
        /// Adds a method exit event to the trace log with the specified event ID.
        /// </summary>
        /// <param name="eventId">The ID of the event to log.</param>
        /// <typeparam name="TEnum">The type of the event ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void MethodExit<TEnum>(TEnum eventId) where TEnum : Enum
            => AddInternal(
                eventId: System.Runtime.CompilerServices.Unsafe.As<TEnum, int>(ref eventId), 
                value: (int)GeneratorStage.MethodExit, 
                mapValue: true);
#endif

        /// <summary>
        /// Appends the trace log to the provided StringBuilder.
        /// </summary>
        /// <param name="stringBuilder">The StringBuilder to append the trace log to.</param>
        /// <param name="eventNameMap">An optional per-call name map for event IDs and mapped values.</param>
        /// <remarks>
        /// Name lookup precedence is: the per-call map, then <see cref="SetCustomEventNames"/>, then <see cref="RegisterName"/> registrations.
        /// </remarks>
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
                    var textAndValue = FormatEventKey(eventNameMap, eventId);
                    stringBuilder.AppendFormat("{0:o} [{1:000}] {2}",
                            StartTime.AddTicks(timestamp),
                            eventId % CompositeValueShift,
                            textAndValue)
                        .AppendLine();
                }
            }
        }

        private class Counter
        {
            public long Value;
        }

        private static readonly ConcurrentDictionary<int, Counter> Counters = new ConcurrentDictionary<int, Counter>();

        private static Counter GetOrAddCounter(int key)
        {
            if (!Counters.TryGetValue(key, out var counter))
            {
                counter = Counters.GetOrAdd(key, _ => new Counter());
            }

            return counter;
        }

        /// <summary>
        /// Increments the value of a given key by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to increment.</param>
        /// <typeparam name="TEnum">The type of the counter ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void IncrementCount<TEnum>(TEnum counterId) where TEnum : Enum
            => IncrementCount(System.Runtime.CompilerServices.Unsafe.As<TEnum, int>(ref counterId));

        /// <summary>
        /// Increments the value of a given key[value] combination by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to increment.</param>
        /// <param name="value">The value associated with the counter, which can be used for additional context or categorisation.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        /// <typeparam name="TEnum">The type of the counter ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        /// <example>
        /// <code lang="csharp">
        /// LightweightTrace.IncrementCount(GeneratorStage.EquatableImmutableArrayInstanceCacheSize, TypeMapId, true);
        /// LightweightTrace.IncrementCount(GeneratorStage.MethodCall, eventId, true);
        /// </code>
        /// </example>
        public static void IncrementCount<TEnum>(TEnum counterId, int value, bool mapValue = false) where TEnum : Enum
            => IncrementCount(System.Runtime.CompilerServices.Unsafe.As<TEnum, int>(ref counterId), value, mapValue);

        /// <summary>
        /// Decrements the value of a given key by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to decrement.</param>
        /// <typeparam name="TEnum">The type of the counter ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void DecrementCount<TEnum>(TEnum counterId) where TEnum : Enum
            => DecrementCount(System.Runtime.CompilerServices.Unsafe.As<TEnum, int>(ref counterId));

        /// <summary>
        /// Decrements the value of a given key[value] combination by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to decrement.</param>
        /// <param name="value">The value associated with the counter, which can be used for additional context or categorisation.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        /// <typeparam name="TEnum">The type of the counter ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void DecrementCount<TEnum>(TEnum counterId, int value, bool mapValue = false) where TEnum : Enum
            => DecrementCount(System.Runtime.CompilerServices.Unsafe.As<TEnum, int>(ref counterId), value, mapValue);

        /// <summary>
        /// Sets the value of a given key.
        /// </summary>
        /// <param name="counterId">The ID of the counter to set.</param>
        /// <param name="count">The new value for the counter.</param>
        /// <typeparam name="TEnum">The type of the counter ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void SetCount<TEnum>(TEnum counterId, long count) where TEnum : Enum
            => SetCount(System.Runtime.CompilerServices.Unsafe.As<TEnum, int>(ref counterId), count);

        /// <summary>
        /// Sets the value of a given key.
        /// </summary>
        /// <param name="counterId">The ID of the counter to set.</param>
        /// <param name="value">The value associated with the counter, which can be used for additional context or categorisation.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        /// <param name="count">The new value for the counter.</param>
        /// <typeparam name="TEnum">The type of the counter ID, which must be an enum, either <see cref="GeneratorStage"/>, or your own.</typeparam>
        public static void SetCount<TEnum>(TEnum counterId, int value, bool mapValue, long count) where TEnum : Enum
            => SetCount(System.Runtime.CompilerServices.Unsafe.As<TEnum, int>(ref counterId), value, mapValue, count);

        /// <summary>
        /// Increments the value of a given key by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to increment.</param>
        public static void IncrementCount(int counterId) => Interlocked.Increment(ref GetOrAddCounter(counterId).Value);

        /// <summary>
        /// Increments the value of a given key[value] combination by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to increment.</param>
        /// <param name="value">The value associated with the counter, which can be used for additional context or categorisation.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        public static void IncrementCount(int counterId, int value, bool mapValue = false)
        {
            var key = EncodeKey(counterId, value, mapValue);
            var index = Interlocked.Increment(ref _index) % Capacity;
            Events[index] = (Stopwatch.ElapsedTicks, key);
#if DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE
            TraceEtwCount(counterId, value, mapValue, 1L);
#endif
            Interlocked.Increment(ref GetOrAddCounter(key).Value);
        }

        /// <summary>
        /// Decrements the value of a given key by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to decrement.</param>
        public static void DecrementCount(int counterId) => Interlocked.Decrement(ref GetOrAddCounter(counterId).Value);

        /// <summary>
        /// Decrements the value of a given key[value] combination by 1.
        /// </summary>
        /// <param name="counterId">The ID of the counter to decrement.</param>
        /// <param name="value">The value associated with the counter, which can be used for additional context or categorisation.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        public static void DecrementCount(int counterId, int value, bool mapValue = false)
        {
            var key = EncodeKey(counterId, value, mapValue);
            var index = Interlocked.Increment(ref _index) % Capacity;
            Events[index] = (Stopwatch.ElapsedTicks, key);
#if DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE
            TraceEtwCount(counterId, value, mapValue, -1L);
#endif
            Interlocked.Decrement(ref GetOrAddCounter(key).Value);
        }

        /// <summary>
        /// Sets the value of a given key.
        /// </summary>
        /// <param name="counterId">The ID of the counter to set.</param>
        /// <param name="count">The new value for the counter.</param>
        public static void SetCount(int counterId, long count) => Interlocked.Exchange(ref GetOrAddCounter(counterId).Value, count);

        /// <summary>
        /// Sets the value of a given key.
        /// </summary>
        /// <param name="counterId">The ID of the counter to set.</param>
        /// <param name="value">The value associated with the counter.</param>
        /// <param name="mapValue">If true, the value is treated as a mapped value when generating the diagnostic log.</param>
        /// <param name="count">The new value for the counter.</param>
        public static void SetCount(int counterId, int value, bool mapValue, long count)
        {
            var key = EncodeKey(counterId, value, mapValue);
            var index = Interlocked.Increment(ref _index) % Capacity;
            Events[index] = (Stopwatch.ElapsedTicks, key);
#if DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE
            TraceEtwCount(counterId, value, mapValue, count);
#endif
            Interlocked.Exchange(ref GetOrAddCounter(key).Value, count);
        }
        
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
                long count = Interlocked.Read(ref kvp.Value.Value);

                var textAndValue = FormatEventKey(eventNameMap, counterId);
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
        public static int EncodeKey(int id, int value, bool mapValue = false) => 
            id + (value * CompositeValueShift) + (mapValue ? MapValueFlag : 0);

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
            id = key & IdMask;
            value = (key & ValueMask) >> ValueShift;
        }

        private static void AddInternal(int eventId, int value, bool mapValue)
        {
            var key = EncodeKey(eventId, value, mapValue);
            var index = Interlocked.Increment(ref _index) % Capacity;
            Events[index] = (Stopwatch.ElapsedTicks, key);
#if DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE
            TraceEtw(eventId, value, mapValue);
#endif
            AddInternal(key);
        }

        private static void AddInternal(int key)
        {
            Interlocked.Increment(ref GetOrAddCounter(key).Value);
        }

#if DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE
        private static void TraceEtw(int eventId, int value, bool mapValue)
        {
            if (_etwLog != null && _etwLog.IsEnabled())
            {
                var message = FormatEventKey(eventId, value, mapValue);
                _etwLog.Trace(_etwLevel, eventId, value, mapValue, message);
            }
        }

        private static void TraceEtwCount(int counterId, int value, bool mapValue, long count)
        {
            if (_etwLog != null && _etwLog.IsEnabled())
            {
                var message = FormatEventKey(counterId, value, mapValue);
                _etwLog.Count(_etwLevel, counterId, value, mapValue, count, message);
            }
        }
#endif

        private static string FormatEventKey(Dictionary<int, string> eventNameMap, int key)
        {
            DecodeKey(key, out var id, out var value, out var mapped);
            return FormatEventKey(id, value, mapped, eventNameMap);
        }

        private static string FormatEventKey(int id, int value, bool mapped, Dictionary<int, string> eventNameMap = null)
        {
            string idText = GetMappedName(eventNameMap, id);
            if (idText == null)
            {
                idText = string.Empty;
            }

            if (!mapped && value == 0)
            {
                return idText;
            }

            string valueText = null;
            if (mapped)
            {
                valueText = GetMappedName(eventNameMap, value);
            }

            if (valueText == null)
            {
                valueText = value.ToString();
            }

            return idText.Length == 0 ? valueText : $"{idText} ({valueText})";
        }

        private static string GetMappedName(Dictionary<int, string> eventNameMap, int id)
        {
            string idText = null;
            if (eventNameMap != null)
            {
                eventNameMap.TryGetValue(id, out idText);
            }
            if (idText == null && _customEventNames != null)
            {
                _customEventNames.TryGetValue(id, out idText);
            }
            if (idText == null && _dynamicValueNames != null)
            {
                _dynamicValueNames.TryGetValue(id, out idText);
            }

            return idText;
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