using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    public class EquatableImmutableArrayTests
    {
        // Construct via the internal ctor to bypass the instance cache and force the
        // element-by-element comparison branch in Equals to actually execute.

        [Fact]
        public void Equals_DifferentInstancesWithSameValues_ReturnsTrue()
        {
            // Arrange
            var values1 = ImmutableArray.Create(10, 20, 30);
            var values2 = ImmutableArray.Create(10, 20, 30);
            var hash = EquatableImmutableArray<int>.CalculateHashCode(values1, EqualityComparer<int>.Default, 0, 0);
            var a = new EquatableImmutableArray<int>(values1, hash);
            var b = new EquatableImmutableArray<int>(values2, hash);

            // Act
            var result = a.Equals(b);

            // Assert
            Assert.False(ReferenceEquals(a, b));
            Assert.True(result);
        }

        [Fact]
        public void Equals_DifferentValuesSameLength_ReturnsFalse()
        {
            // Arrange
            var comparer = EqualityComparer<int>.Default;
            var values1 = ImmutableArray.Create(1, 2, 3);
            var values2 = ImmutableArray.Create(1, 2, 4);
            var a = new EquatableImmutableArray<int>(values1, EquatableImmutableArray<int>.CalculateHashCode(values1, comparer, 0, 0));
            var b = new EquatableImmutableArray<int>(values2, EquatableImmutableArray<int>.CalculateHashCode(values2, comparer, 0, 0));

            // Act
            var result = a.Equals(b);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Equals_DifferentLengthsSameHash_ReturnsFalse()
        {
            // Arrange — force matching hashes so the length-mismatch branch is reached
            // rather than the hash-mismatch short-circuit.
            var a = new EquatableImmutableArray<int>(ImmutableArray.Create(1, 2), 0);
            var b = new EquatableImmutableArray<int>(ImmutableArray.Create(1, 2, 3), 0);

            // Act
            var result = a.Equals(b);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetHashCode_OrderSensitive_DifferentOrdersProduceDifferentHashes()
        {
            // Arrange
            var ascending = EquatableImmutableArray<int>.Create(ImmutableArray.Create(1, 2, 3));
            var descending = EquatableImmutableArray<int>.Create(ImmutableArray.Create(3, 2, 1));

            // Act
            var ascHash = ascending.GetHashCode();
            var descHash = descending.GetHashCode();

            // Assert
            Assert.NotEqual(ascHash, descHash);
        }

        [Fact]
        public void Create_EmptyArray_ReturnsEmptySingleton()
        {
            // Arrange
            var values = ImmutableArray<int>.Empty;

            // Act
            var result = EquatableImmutableArray<int>.Create(values);

            // Assert
            Assert.Same(EquatableImmutableArray<int>.Empty, result);
        }
    }
}
