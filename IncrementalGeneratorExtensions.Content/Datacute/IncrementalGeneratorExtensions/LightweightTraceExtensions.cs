#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACEEXTENSIONS // Feature: LightweightTraceExtensions
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE // Dependency: LightweightTrace
using System;
#if DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE
using System.Diagnostics.Tracing;
#endif
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Datacute.IncrementalGeneratorExtensions
{
    /// <summary>
    /// Integration layer: injects LightweightTrace instrumentation (event logging, counters, value buckets, enum mapping) into <see cref="IncrementalValuesProvider{T}"/> / <see cref="IncrementalValueProvider{T}"/> pipelines and adds cancellation helpers that both log and throw.
    /// </summary>
    /// <remarks>
    /// Encourages regular cancellation checks (logged with a tagged event) so long-running pipelines restart quickly on source edits while still producing a coherent diagnostics block.
    /// </remarks>
    public static class LightweightTraceExtensions
    {
        /// <summary>
        /// Extension of <see cref="IncrementalValueProviderExtensions.WithTrackingName{TSource}(Microsoft.CodeAnalysis.IncrementalValuesProvider{TSource},string)"/>
        /// to trace the pipeline throughput, and to use an <see cref="Enum"/> as the event ID.
        /// </summary>
        /// <param name="source">The source <see cref="IncrementalValuesProvider{T}"/>.</param>
        /// <param name="eventId">The event ID as an <see cref="Enum"/>.</param>
        /// <typeparam name="T">Type of the values in the provider.</typeparam>
        /// <typeparam name="TEnum">Type of the event ID enum.</typeparam>
        /// <returns>The source <see cref="IncrementalValuesProvider{T}"/> to allow method chaining.</returns>
        /// <example>
        /// <code>
        /// var additionalTexts =
        ///     context.AdditionalTextsProvider
        ///            .Select(SelectFileInfo)
        ///            .WithTrackingName(GeneratorStage.AdditionalTextsProviderSelect)
        /// </code>
        /// </example>
        public static IncrementalValuesProvider<T> WithTrackingName<T, TEnum>(
            this IncrementalValuesProvider<T> source,
            TEnum eventId)
            where TEnum : Enum
            => source.Select((input, _) =>
            {
                LightweightTrace.Add(eventId);

                return input;
            }).WithTrackingName(Enum.GetName(typeof(TEnum), eventId) ?? $"({eventId})");

        /// <summary>
        /// Extension of <see cref="IncrementalValueProviderExtensions.WithTrackingName{TSource}(Microsoft.CodeAnalysis.IncrementalValuesProvider{TSource},string)"/>
        /// to trace the pipeline throughput, and to use an <see cref="Enum"/> as the event ID.
        /// </summary>
        /// <param name="source">The source <see cref="IncrementalValuesProvider{T}"/>.</param>
        /// <param name="eventId">The event ID as an <see cref="Enum"/>.</param>
        /// <param name="value">An additional value to log with the event ID.</param>
        /// <typeparam name="T">Type of the values in the provider.</typeparam>
        /// <typeparam name="TEnum">Type of the event ID enum.</typeparam>
        /// <returns>The source <see cref="IncrementalValuesProvider{T}"/> to allow method chaining.</returns>
        /// <example>
        /// <code>
        /// var slowSelection =
        ///     someIncrementalValuesProvider
        ///        .Select(Part1)
        ///        .WithTrackingName(YourCustomEnum.SlowPipeline, 1)
        ///        .CombineEquatable(dataToCombine)
        ///        .WithTrackingName(YourCustomEnum.SlowPipeline, 2)
        ///        .SelectMany(Part3)
        ///        .WithTrackingName(YourCustomEnum.SlowPipeline, 3)
        ///        .CollectEquatable()
        ///        .WithTrackingName(YourCustomEnum.SlowPipeline, 4)
        ///        .Select(Part5)
        ///        .WithTrackingName(YourCustomEnum.SlowPipeline, 5);
        /// </code>
        /// </example>
        public static IncrementalValuesProvider<T> WithTrackingName<T, TEnum>(
            this IncrementalValuesProvider<T> source,
            TEnum eventId,
            int value)
            where TEnum : Enum
            => source.Select((input, _) =>
            {
                LightweightTrace.Add(eventId, value);

                return input;
            }).WithTrackingName(Enum.GetName(typeof(TEnum), eventId) ?? $"({eventId})");

        /// <summary>
        /// Extension of <see cref="IncrementalValueProviderExtensions.WithTrackingName{TSource}(Microsoft.CodeAnalysis.IncrementalValuesProvider{TSource},string)"/>
        /// to trace the pipeline throughput, and to use an <see cref="Enum"/> as the event ID.
        /// </summary>
        /// <param name="source">The source <see cref="IncrementalValuesProvider{T}"/>.</param>
        /// <param name="eventId">The event ID as an <see cref="Enum"/>.</param>
        /// <param name="value">An additional value to log with the event ID.</param>
        /// <typeparam name="T">Type of the values in the provider.</typeparam>
        /// <typeparam name="TEnumKey">Type of the event ID enum.</typeparam>
        /// <typeparam name="TEnumValue">Type of the additional value enum.</typeparam>
        /// <returns>The source <see cref="IncrementalValuesProvider{T}"/> to allow method chaining.</returns>
        /// <example>
        /// <code>
        /// var slowSelection =
        ///     someIncrementalValuesProvider
        ///        .Select(Part1)
        ///        .CombineEquatable(dataToCombine)
        ///        .SelectMany(Part3)
        ///        .CollectEquatable()
        ///        .WithTrackingName(YourCustomEnum.SlowPipeline, GeneratorStage.MethodEntry)
        ///        .Select(ComplicatedPart)
        ///        .WithTrackingName(YourCustomEnum.SlowPipeline, GeneratorStage.MethodExit);
        /// </code>
        /// </example>
        public static IncrementalValuesProvider<T> WithTrackingName<T, TEnumKey, TEnumValue>(
            this IncrementalValuesProvider<T> source,
            TEnumKey eventId,
            TEnumValue value)
            where TEnumKey : Enum
            where TEnumValue : Enum
            => source.Select((input, _) =>
            {
                LightweightTrace.Add(eventId, value);

                return input;
            }).WithTrackingName(Enum.GetName(typeof(TEnumKey), eventId) ?? $"({eventId})");

        /// <summary>
        /// Extension of <see cref="IncrementalValueProviderExtensions.WithTrackingName{TSource}(Microsoft.CodeAnalysis.IncrementalValueProvider{TSource},string)"/>
        /// to trace the pipeline throughput, and to use an <see cref="Enum"/> as the event ID.
        /// </summary>
        /// <param name="source">The source <see cref="IncrementalValueProvider{T}"/>.</param>
        /// <param name="eventId">The event ID as an <see cref="Enum"/>.</param>
        /// <typeparam name="T">Type of the values in the provider.</typeparam>
        /// <typeparam name="TEnum">Type of the event ID enum.</typeparam>
        /// <returns>The source <see cref="IncrementalValueProvider{T}"/> to allow method chaining.</returns>
        public static IncrementalValueProvider<T> WithTrackingName<T, TEnum>(
            this IncrementalValueProvider<T> source,
            TEnum eventId)
            where TEnum : Enum
            => source.Select((input, _) =>
            {
                LightweightTrace.Add(eventId);

                return input;
            }).WithTrackingName(Enum.GetName(typeof(TEnum), eventId) ?? $"({eventId})");

        /// <summary>
        /// Extension of <see cref="IncrementalValueProviderExtensions.WithTrackingName{TSource}(Microsoft.CodeAnalysis.IncrementalValueProvider{TSource},string)"/>
        /// to trace the pipeline throughput, and to use an <see cref="Enum"/> as the event ID.
        /// </summary>
        /// <param name="source">The source <see cref="IncrementalValueProvider{T}"/>.</param>
        /// <param name="eventId">The event ID as an <see cref="Enum"/>.</param>
        /// <param name="value">An additional value to log with the event ID.</param>
        /// <typeparam name="T">Type of the values in the provider.</typeparam>
        /// <typeparam name="TEnum">Type of the event ID enum.</typeparam>
        /// <returns>The source <see cref="IncrementalValueProvider{T}"/> to allow method chaining.</returns>
        public static IncrementalValueProvider<T> WithTrackingName<T, TEnum>(
            this IncrementalValueProvider<T> source,
            TEnum eventId,
            int value)
            where TEnum : Enum
            => source.Select((input, _) =>
            {
                LightweightTrace.Add(eventId, value);

                return input;
            }).WithTrackingName(Enum.GetName(typeof(TEnum), eventId) ?? $"({eventId})");

        /// <summary>
        /// Extension of <see cref="IncrementalValueProviderExtensions.WithTrackingName{TSource}(Microsoft.CodeAnalysis.IncrementalValueProvider{TSource},string)"/>
        /// to trace the pipeline throughput, and to use an <see cref="Enum"/> as the event ID.
        /// </summary>
        /// <param name="source">The source <see cref="IncrementalValueProvider{T}"/>.</param>
        /// <param name="eventId">The event ID as an <see cref="Enum"/>.</param>
        /// <param name="value">An additional value to log with the event ID.</param>
        /// <typeparam name="T">Type of the values in the provider.</typeparam>
        /// <typeparam name="TEnumKey">Type of the event ID enum.</typeparam>
        /// <typeparam name="TEnumValue">Type of the additional value enum.</typeparam>
        /// <returns>The source <see cref="IncrementalValueProvider{T}"/> to allow method chaining.</returns>
        public static IncrementalValueProvider<T> WithTrackingName<T, TEnumKey, TEnumValue>(
            this IncrementalValueProvider<T> source,
            TEnumKey eventId,
            TEnumValue value)
            where TEnumKey : Enum
            where TEnumValue : Enum
            => source.Select((input, _) =>
            {
                LightweightTrace.Add(eventId, value);

                return input;
            }).WithTrackingName(Enum.GetName(typeof(TEnumKey), eventId) ?? $"({eventId})");

#if DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE
        /// <summary>
        /// Captures EventSource configuration and registers conditional EventSource publication.
        /// </summary>
        /// <param name="context">The <see cref="IncrementalGeneratorInitializationContext"/>.</param>
        /// <param name="buildPropertyName">The MSBuild property name (e.g., "DatacuteGeneratorUseEventSource"). The property is read from build_property.{buildPropertyName}.</param>
        /// <param name="eventSourceName">Optional EventSource name. Defaults to "Datacute-IncrementalGenerator-Trace".</param>
        /// <param name="eventLevel">Optional event level. Defaults to <see cref="EventLevel.Informational"/>.</param>
        /// <param name="eventNameMap">Optional mapping of event IDs to names for diagnostic output.</param>
        /// <returns>The context to allow method chaining.</returns>
        /// <remarks>
        /// Captures configuration immediately. When the MSBuild property is "true", publication is enabled;
        /// otherwise publication is disabled. The conditional action runs during source generation.
        /// </remarks>
        /// <example>
        /// In your generator's Initialize method:
        /// <code>
        /// context.InitializeEventSourceIfEnabled("DatacuteGeneratorUseEventSource");
        /// // or with custom parameters:
        /// context.InitializeEventSourceIfEnabled(
        ///     buildPropertyName: "DatacuteGeneratorUseEventSource",
        ///     eventSourceName: "MyGenerator-Trace",
        ///     eventNameMap: eventNameMap);
        /// </code>
        /// 
        /// In the consuming project's .csproj file to enable tracing:
        /// <code>
        /// &lt;PropertyGroup&gt;
        ///   &lt;DatacuteGeneratorUseEventSource&gt;true&lt;/DatacuteGeneratorUseEventSource&gt;
        /// &lt;/PropertyGroup&gt;
        /// </code>
        /// </example>
        public static IncrementalGeneratorInitializationContext InitializeEventSourceIfEnabled(
            this IncrementalGeneratorInitializationContext context,
            string buildPropertyName,
            string eventSourceName = LightweightTrace.DefaultEventSourceName,
            EventLevel eventLevel = LightweightTrace.DefaultEventSourceLevel,
            System.Collections.Generic.Dictionary<int, string> eventNameMap = null)
        {
            LightweightTrace.InitializeEventSource(eventSourceName, eventLevel, eventNameMap);

            var eventSourceEnabledValueProvider = context.AnalyzerConfigOptionsProvider
                .Select((provider, _) => provider.GlobalOptions)
                .Select((globalOptions, _) =>
                {
                    globalOptions.TryGetValue($"build_property.{buildPropertyName}", out var value);
                    return bool.TryParse(value, out var result) && result;
                });

            context.RegisterSourceOutput(eventSourceEnabledValueProvider, (_, isEnabled) =>
            {
                if (isEnabled)
                {
                    TryEnableEventSource();
                }
                else
                {
                    LightweightTrace.DisableEventSource();
                }
            });

            return context;
        }

        /// <summary>
        /// Captures unconditional EventSource configuration.
        /// </summary>
        /// <param name="context">The <see cref="IncrementalGeneratorInitializationContext"/>.</param>
        /// <param name="eventSourceName">Optional EventSource name. Defaults to "Datacute-IncrementalGenerator-Trace".</param>
        /// <param name="eventLevel">Optional event level. Defaults to <see cref="EventLevel.Informational"/>.</param>
        /// <param name="eventNameMap">Optional mapping of event IDs to names for diagnostic output.</param>
        /// <param name="enableEventSource">Whether to enable EventSource publication after capturing configuration. Defaults to true.</param>
        /// <returns>The context to allow method chaining.</returns>
        /// <remarks>
        /// Calls <see cref="LightweightTrace.InitializeEventSource(string, EventLevel, System.Collections.Generic.Dictionary{int, string})"/>
        /// with the specified parameters immediately, then enables EventSource publication unless <paramref name="enableEventSource"/> is false.
        /// </remarks>
        /// <example>
        /// <code>
        /// context.InitializeEventSource();
        /// // or with custom parameters:
        /// context.InitializeEventSource(
        ///     eventSourceName: "MyGenerator-Trace",
        ///     eventNameMap: eventNameMap);
        /// </code>
        /// </example>
        public static IncrementalGeneratorInitializationContext InitializeEventSource(
            this IncrementalGeneratorInitializationContext context,
            string eventSourceName = LightweightTrace.DefaultEventSourceName,
            EventLevel eventLevel = LightweightTrace.DefaultEventSourceLevel,
            System.Collections.Generic.Dictionary<int, string> eventNameMap = null,
            bool enableEventSource = true)
        {
            LightweightTrace.InitializeEventSource(eventSourceName, eventLevel, eventNameMap);
            if (enableEventSource)
            {
                TryEnableEventSource();
            }

            return context;
        }

        private static void TryEnableEventSource()
        {
            try
            {
                LightweightTrace.EnableEventSource();
            }
            catch
            {
                // EventSource creation may fail or be unavailable; silently ignore.
#if !DATACUTE_EXCLUDE_GENERATORSTAGE
                LightweightTrace.Add(GeneratorStage.MethodException);
#endif
            }
        }
#endif

#if !DATACUTE_EXCLUDE_GENERATORSTAGE
        /// <summary>
        /// Throws an <see cref="OperationCanceledException"/> if the <see cref="CancellationToken"/> is cancelled,
        /// but first adds a trace entry for the cancellation with the specified <see cref="Enum"/>
        /// </summary>
        /// <param name="token">The <see cref="CancellationToken"/> to check for cancellation.</param>
        /// <param name="tracingInstanceEnum">The tracing instance enum to log with the cancellation event.</param>
        /// <typeparam name="TEnum">Type of the tracing instance enum.</typeparam>
        public static void ThrowIfCancellationRequested<TEnum>(
            this CancellationToken token,
            TEnum tracingInstanceEnum)
            where TEnum : Enum
        {
            if (token.IsCancellationRequested)
            {
                LightweightTrace.Add(GeneratorStage.Cancellation, tracingInstanceEnum);
            }
            token.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Throws an <see cref="OperationCanceledException"/> if the <see cref="CancellationToken"/> is cancelled,
        /// but first adds a trace entry for the cancellation with the specified id
        /// </summary>
        /// <param name="token">The <see cref="CancellationToken"/> to check for cancellation.</param>
        /// <param name="tracingInstanceId">The tracing instance id to log with the cancellation event.</param>
        /// <remarks>
        /// This method is intended for quick debugging using simple numeric identifiers to see which cancellation check was hit,
        /// </remarks>
        public static void ThrowIfCancellationRequested(
            this CancellationToken token,
            int tracingInstanceId)
        {
            if (token.IsCancellationRequested)
            {
                LightweightTrace.Add(GeneratorStage.Cancellation, tracingInstanceId);
            }
            token.ThrowIfCancellationRequested();
        }
#endif
    }
}
#endif // !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE
#endif // !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACEEXTENSIONS
