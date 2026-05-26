using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    public class EquatableImmutableArrayExtensionsTests
    {
        // The selector-based overloads pre-allocate a builder with capacity = input length
        // and then call MoveToImmutable, which requires count == capacity. An off-by-one
        // (selector skipping or doubling an element) would throw or produce wrong output.

        [Fact]
        public void ToEquatableImmutableArray_FromImmutableArrayWithSelector_CallsSelectorOncePerElementInOrder()
        {
            // Arrange
            var input = ImmutableArray.Create(1, 2, 3, 4);
            var callOrder = new List<int>();

            // Act
            var result = input.ToEquatableImmutableArray(value =>
            {
                callOrder.Add(value);
                return value * 10;
            });

            // Assert
            Assert.Equal(new[] { 1, 2, 3, 4 }, callOrder);
            Assert.Equal(4, result.Length);
            Assert.Equal(new[] { 10, 20, 30, 40 }, new[] { result[0], result[1], result[2], result[3] });
        }

        [Fact]
        public void ToEquatableImmutableArray_FromEquatableImmutableArrayWithSelector_PreservesOrder()
        {
            // Arrange
            var input = ImmutableArray.Create(1, 2, 3).ToEquatableImmutableArray();

            // Act
            var result = input.ToEquatableImmutableArray(value => value * 10);

            // Assert
            Assert.Equal(3, result.Length);
            Assert.Equal(new[] { 10, 20, 30 }, new[] { result[0], result[1], result[2] });
        }

        [Fact]
        public void ToEquatableImmutableArray_EmptySourceWithSelector_ReturnsEmpty()
        {
            // Arrange — the builder is allocated with capacity 0; selector must not be called.
            var input = ImmutableArray<int>.Empty;
            var selectorCalled = false;

            // Act
            var result = input.ToEquatableImmutableArray(value =>
            {
                selectorCalled = true;
                return value;
            });

            // Assert
            Assert.False(selectorCalled);
            Assert.True(result.IsEmpty);
        }
    }
}
