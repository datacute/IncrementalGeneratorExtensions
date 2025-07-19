#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACEEXTENSIONS
using System;
using Microsoft.CodeAnalysis;

namespace Datacute.IncrementalGeneratorExtensions
{
    public static class LightweightTraceExtensions
    {
        public static IncrementalValuesProvider<T> Trace<T, TEnum>(this IncrementalValuesProvider<T> source, TEnum eventId) where TEnum : Enum =>
            source.Select((input, _) =>
            {
                LightweightTrace.Add(eventId);

                return input;
            }).WithTrackingName(Enum.GetName(typeof(TEnum), eventId) ?? $"({eventId})");

        public static IncrementalValueProvider<T> Trace<T, TEnum>(this IncrementalValueProvider<T> source, TEnum eventId) where TEnum : Enum =>
            source.Select((input, _) =>
            {
                LightweightTrace.Add(eventId);

                return input;
            }).WithTrackingName(Enum.GetName(typeof(TEnum), eventId) ?? $"({eventId})");
    }
}
#endif