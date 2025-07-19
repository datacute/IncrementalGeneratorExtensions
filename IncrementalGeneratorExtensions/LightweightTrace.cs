#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Datacute.IncrementalGeneratorExtensions
{
    public static class LightweightTrace
    {
        private const int Capacity = 1024;

        private static readonly DateTime StartTime = DateTime.UtcNow;
        private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();

        private static readonly (long, int)[] Events = new (long, int)[Capacity];
        private static int _index;

        public static void Add<TEnum>(TEnum eventId) where TEnum : Enum => Add(Convert.ToInt32(eventId));

        public static void Add(int eventId)
        {
            var index = Interlocked.Increment(ref _index) % Capacity;
            Events[index] = (Stopwatch.ElapsedTicks, eventId);
        }

        public static void GetTrace(StringBuilder stringBuilder, Dictionary<int, string> eventNameMap)
        {
            var index = _index;
            for (var i = 0; i < Capacity; i++)
            {
                index = (index + 1) % Capacity;
                var (timestamp, eventId) = Events[index];
                if (timestamp > 0)
                {
                    var item = string.Empty;
                    if (eventId > 1000)
                    {
                        item = $" ({eventId / 1000})";
                    }
                    var text = eventNameMap.TryGetValue(eventId % 1000, out var name) ? name : string.Empty;
                    stringBuilder.AppendFormat("{0:o} [{1:000}] {2} {3}",
                            StartTime.AddTicks(timestamp),
                            eventId % 1000,
                            text,
                            item)
                        .AppendLine();
                }
            }
        }
    }
}
#endif