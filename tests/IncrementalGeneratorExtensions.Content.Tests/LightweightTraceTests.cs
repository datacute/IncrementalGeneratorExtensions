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
    }
}
