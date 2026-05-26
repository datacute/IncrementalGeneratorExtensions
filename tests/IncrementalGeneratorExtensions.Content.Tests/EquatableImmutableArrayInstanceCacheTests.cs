using System.Collections.Immutable;
using Xunit;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    public class EquatableImmutableArrayInstanceCacheTests
    {
        [Fact]
        public void GetOrCreate_TwoDistinctArraysWithSameValues_ReturnsSameInstance()
        {
            // Arrange — two distinct ImmutableArray<int> instances with equal contents.
            var values1 = ImmutableArray.Create(7, 8, 9);
            var values2 = ImmutableArray.Create(7, 8, 9);

            // Act
            var a = EquatableImmutableArrayInstanceCache<int>.GetOrCreate(values1);
            var b = EquatableImmutableArrayInstanceCache<int>.GetOrCreate(values2);

            // Assert — the cache reuses the existing instance (reference equality implies value equality).
            Assert.Same(a, b);
            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void GetOrCreate_SameFirstAndLastElementDifferentMiddle_ReturnsDifferentInstances()
        {
            // Arrange — both arrays bucket to (length=3, compositeHash(first, last)), so the
            // cache must compare element-by-element to avoid a false reuse.
            var values1 = ImmutableArray.Create(100, 1, 999);
            var values2 = ImmutableArray.Create(100, 2, 999);

            // Act
            var a = EquatableImmutableArrayInstanceCache<int>.GetOrCreate(values1);
            var b = EquatableImmutableArrayInstanceCache<int>.GetOrCreate(values2);

            // Assert
            Assert.NotSame(a, b);
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void GetOrCreate_EmptyArray_ReturnsEmptySingleton()
        {
            // Arrange
            var empty = ImmutableArray<int>.Empty;

            // Act
            var result = EquatableImmutableArrayInstanceCache<int>.GetOrCreate(empty);

            // Assert
            Assert.Same(EquatableImmutableArray<int>.Empty, result);
        }

        [Fact]
        public void GetOrCreate_MruCollision_FunctionsCorrectly()
        {
            // Arrange
            // Create two completely different arrays that happen to hash to the exact same MRU slot.
            // MRU mask is 63. slot = (firstElementHash ^ length) & 63.
            // Array A: length 1, firstElementHash = 0. => slot 1
            // Array B: length 2, firstElementHash = 3. => (3 ^ 2) = 1 & 63 = 1 => slot 1
            var valuesA = ImmutableArray.Create(0);
            var valuesB = ImmutableArray.Create(3, 4);

            // Act
            var a1 = EquatableImmutableArrayInstanceCache<int>.GetOrCreate(valuesA); // Sets slot 1 to A
            var b1 = EquatableImmutableArrayInstanceCache<int>.GetOrCreate(valuesB); // Overwrites slot 1 with B (Mru logic kicks A out of MRU, leaves it in Dictionary)
            
            var a2 = EquatableImmutableArrayInstanceCache<int>.GetOrCreate(valuesA); // Misses MRU (it holds B), but hits Dictionary.

            // Assert
            Assert.Same(a1, a2); // Ensure we still successfully got the same instance using the Dictionary hit path.
            Assert.NotSame(a1, b1);
        }
    }
}
