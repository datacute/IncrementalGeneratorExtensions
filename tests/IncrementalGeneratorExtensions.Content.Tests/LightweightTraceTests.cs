using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    public class LightweightTraceTests
    {
        // The composite-key bit-packing is the only piece of LightweightTrace with real
        // bug surface: id in bits 0..9, value in bits 10..27, map flag at bit 28. The
        // counters / ring buffer touch global mutable state and are tested elsewhere.

        [Theory]
        [InlineData(0, 0, false)]
        [InlineData(1023, 0, false)]       // max id, min value
        [InlineData(0, 262143, false)]     // min id, max value (2^18 - 1)
        [InlineData(1023, 262143, false)]  // max id, max value — no overflow into map flag
        [InlineData(42, 7, true)]
        [InlineData(0, 0, true)]
        public void EncodeKey_RoundTripsThroughDecodeKey(int id, int value, bool mapValue)
        {
            // Act
            var encoded = LightweightTrace.EncodeKey(id, value, mapValue);
            LightweightTrace.DecodeKey(encoded, out var decodedId, out var decodedValue, out var decodedMapped);

            // Assert
            Assert.Equal(id, decodedId);
            Assert.Equal(value, decodedValue);
            Assert.Equal(mapValue, decodedMapped);
        }

        [Fact]
        public void EncodeKey_MapValueFlag_OccupiesBit28()
        {
            // Act
            var withFlag = LightweightTrace.EncodeKey(0, 0, mapValue: true);
            var withoutFlag = LightweightTrace.EncodeKey(0, 0, mapValue: false);

            // Assert
            Assert.Equal(1 << 28, withFlag);
            Assert.Equal(0, withoutFlag);
        }

        [Fact]
        public void EncodeKey_DistinctInputs_ProduceDistinctKeys()
        {
            // Act — verify id and value don't collide in the same key bits.
            var idOnly = LightweightTrace.EncodeKey(5, 10, false);
            var differentValue = LightweightTrace.EncodeKey(5, 11, false);
            var differentId = LightweightTrace.EncodeKey(6, 10, false);

            // Assert
            Assert.NotEqual(idOnly, differentValue);
            Assert.NotEqual(idOnly, differentId);
        }

        private enum TestTraceStage
        {
            StageA = 900,
            StageB = 901
        }

        private sealed class CapturingEventListener : EventListener
        {
            private readonly string _eventSourceName;
            private readonly EventLevel _level;
            private readonly ConcurrentQueue<RecordedEvent> _events = new ConcurrentQueue<RecordedEvent>();
            private readonly ManualResetEventSlim _enabled = new ManualResetEventSlim(false);

            public CapturingEventListener(string eventSourceName, EventLevel level)
            {
                _eventSourceName = eventSourceName;
                _level = level;
            }

            public RecordedEvent[] Events => _events.ToArray();

            public bool WaitUntilEnabled(TimeSpan timeout) => _enabled.Wait(timeout);

            public bool WaitForEventCount(int count, TimeSpan timeout)
            {
                return SpinWait.SpinUntil(() => _events.Count >= count, timeout);
            }

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == _eventSourceName)
                {
                    EnableEvents(eventSource, _level);
                    _enabled.Set();
                }
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                var payload = eventData.Payload == null
                    ? new object[0]
                    : eventData.Payload.ToArray();
                _events.Enqueue(new RecordedEvent(eventData.EventId, payload));
            }
        }

        private sealed class RecordedEvent
        {
            public RecordedEvent(int eventId, object[] payload)
            {
                EventId = eventId;
                Payload = payload;
            }

            public int EventId { get; }

            public object[] Payload { get; }
        }

        [Fact]
        public void InitializeEtw_CanBeCalledRepeatedly_WithDifferentEventSourceNames()
        {
            LightweightTrace.InitializeEtw(
                eventSourceName: "Datacute-LWT-EventSource-Test-A-" + Guid.NewGuid().ToString("N"),
                eventLevel: EventLevel.Verbose);
            LightweightTrace.InitializeEtw(
                eventSourceName: "Datacute-LWT-EventSource-Test-B-" + Guid.NewGuid().ToString("N"),
                eventLevel: EventLevel.Informational);
        }

        [Fact]
        public void EventSourceMode_EmitsExpectedTraceAndCountEvents()
        {
            var eventSourceName = "Datacute-LWT-EventSource-Test-" + Guid.NewGuid().ToString("N");
            var eventNameMap = new Dictionary<int, string>
            {
                { (int)TestTraceStage.StageA, "Stage A" },
                { (int)TestTraceStage.StageB, "Stage B" },
                { 2, "Bucket Two" },
            };

            using (var listener = new CapturingEventListener(eventSourceName, EventLevel.Verbose))
            {
                LightweightTrace.InitializeEtw(
                    eventSourceName: eventSourceName,
                    eventLevel: EventLevel.Informational,
                    eventNameMap: eventNameMap);

                Assert.True(listener.WaitUntilEnabled(TimeSpan.FromSeconds(2)));

                LightweightTrace.Add(TestTraceStage.StageA);
                LightweightTrace.IncrementCount(TestTraceStage.StageB, 2, mapValue: true);

                Assert.True(listener.WaitForEventCount(2, TimeSpan.FromSeconds(2)));

                var events = listener.Events;
                var trace = events.SingleOrDefault(e => e.EventId == 4 && (int)e.Payload[0] == (int)TestTraceStage.StageA);
                var count = events.SingleOrDefault(e => e.EventId == 9 && (int)e.Payload[0] == (int)TestTraceStage.StageB);

                Assert.NotNull(trace);
                Assert.NotNull(count);

                var traceId = (int)trace.Payload[0];
                var traceValue = (int)trace.Payload[1];
                var traceIsMappedValue = (bool)trace.Payload[2];
                var traceMessage = (string)trace.Payload[3];

                var countId = (int)count.Payload[0];
                var countValue = (int)count.Payload[1];
                var countIsMappedValue = (bool)count.Payload[2];
                var countDelta = (long)count.Payload[3];
                var countMessage = (string)count.Payload[4];

                Assert.Equal((int)TestTraceStage.StageA, traceId);
                Assert.Equal(0, traceValue);
                Assert.False(traceIsMappedValue);
                Assert.Equal("Stage A", traceMessage);

                Assert.Equal((int)TestTraceStage.StageB, countId);
                Assert.Equal(2, countValue);
                Assert.True(countIsMappedValue);
                Assert.Equal(1L, countDelta);
                Assert.Equal("Stage B (Bucket Two)", countMessage);
            }
        }

        [Fact]
        public void EventSourceMode_AddAndCounts_StillProduceDiagnosticsComment()
        {
            var eventNameMap = new Dictionary<int, string>
            {
                { (int)TestTraceStage.StageA, "Stage A" },
                { (int)TestTraceStage.StageB, "Stage B" },
            };

            LightweightTrace.InitializeEtw(
                eventSourceName: "Datacute-LWT-EventSource-Test-C-" + Guid.NewGuid().ToString("N"),
                eventLevel: EventLevel.Verbose,
                eventNameMap: eventNameMap);

            LightweightTrace.Add(TestTraceStage.StageA);
            LightweightTrace.Add(TestTraceStage.StageB, 3);
            LightweightTrace.IncrementCount(TestTraceStage.StageA, 3);
            LightweightTrace.DecrementCount(TestTraceStage.StageA, 1);
            LightweightTrace.SetCount(TestTraceStage.StageB, 2, mapValue: false, count: 5);

            var builder = new StringBuilder();
            builder.AppendDiagnosticsComment(eventNameMap);
            var diagnostics = builder.ToString();

            Assert.Contains("/* Diagnostics", diagnostics);
            Assert.Contains("Counters:", diagnostics);
            Assert.Contains("Trace Log:", diagnostics);
        }
    }
}
