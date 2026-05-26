using System.Collections.Immutable;
using Xunit;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    /// <summary>
    /// Verifies the behavioural contract of <see cref="EquatableImmutableArray{T}"/> when
    /// <c>DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE</c> is defined, so
    /// <see cref="EquatableImmutableArray{T}.Create"/> always allocates a new instance.
    /// </summary>
    public class EquatableImmutableArrayWithoutCacheTests
    {
        [Fact]
        public void Create_TwoEqualArrays_AreValueEqualButNotSameReference()
        {
            // Arrange
            var a = EquatableImmutableArray<int>.Create(ImmutableArray.Create(1, 2, 3));
            var b = EquatableImmutableArray<int>.Create(ImmutableArray.Create(1, 2, 3));

            // Assert — no instance reuse without the cache, but value equality must still hold
            Assert.False(ReferenceEquals(a, b));
            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Create_TwoUnequalArrays_AreNotValueEqual()
        {
            // Arrange
            var a = EquatableImmutableArray<int>.Create(ImmutableArray.Create(1, 2, 3));
            var b = EquatableImmutableArray<int>.Create(ImmutableArray.Create(3, 2, 1));

            // Assert
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Create_EmptyArray_ReturnsEmptySingleton()
        {
            // The empty singleton path is unconditional and must still hold.
            var result = EquatableImmutableArray<int>.Create(ImmutableArray<int>.Empty);

            Assert.Same(EquatableImmutableArray<int>.Empty, result);
        }
    }
}

