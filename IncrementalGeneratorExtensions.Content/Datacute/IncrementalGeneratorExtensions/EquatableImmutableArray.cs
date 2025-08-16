#if !DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAY
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Datacute.IncrementalGeneratorExtensions
{
    /// <summary>
    /// Represents an immutable array that implements IEquatable for value equality.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array, which must implement IEquatable&lt;T&gt;.</typeparam>
    public sealed class EquatableImmutableArray<T> : IEquatable<EquatableImmutableArray<T>>, IReadOnlyList<T>
        where T : IEquatable<T>
    {
        /// <summary>
        /// Gets a shared empty instance of <see cref="EquatableImmutableArray{T}"/>.
        /// </summary>
        public static EquatableImmutableArray<T> Empty { get; } = new EquatableImmutableArray<T>(ImmutableArray<T>.Empty, 0);

        // Static factory method with singleton handling
        /// <summary>
        /// Creates an <see cref="EquatableImmutableArray{T}"/> for the provided immutable array, reusing a cached instance when possible.
        /// </summary>
        /// <param name="values">The immutable array backing the equatable wrapper.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>An instance representing the supplied values (possibly a cached singleton for empty).</returns>
        public static EquatableImmutableArray<T> Create(ImmutableArray<T> values, CancellationToken cancellationToken = default)
        {
            if (values.IsEmpty)
                return Empty;
#if DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE
            // Cache disabled: compute hash directly and return a new instance (no reuse / instrumentation events)
            var comparer = EqualityComparer<T>.Default;
            int hash = CalculateHashCode(values, comparer, 0, 0);
            return new EquatableImmutableArray<T>(values, hash);
#else
            return EquatableImmutableArrayInstanceCache<T>.GetOrCreate(values, cancellationToken);
#endif
        }

        private readonly ImmutableArray<T> _values;
        private readonly int _hashCode;

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        /// <param name="index">Zero-based index.</param>
        public T this[int index] => _values[index];
        /// <summary>
        /// Gets the number of elements in the array.
        /// </summary>
        public int Count => _values.Length;

        // Properties Duplicated from ImmutableArray<T>

        /// <summary>
        /// Gets the number of elements in the array (alias of <see cref="Count"/>).
        /// </summary>
        public int Length => _values.Length;
        /// <summary>
        /// True if the underlying array has length 0.
        /// </summary>
        public bool IsEmpty => _values.Length == 0;
        /// <summary>
        /// True if the underlying immutable array is in its default (uninitialised) state.
        /// </summary>
        public bool IsDefault => _values.IsDefault;
        /// <summary>
        /// True if the underlying immutable array is either default or empty.
        /// </summary>
        public bool IsDefaultOrEmpty => _values.IsDefaultOrEmpty;

        internal EquatableImmutableArray(ImmutableArray<T> values, int hashCode)
        {
            _values = values;
            _hashCode = hashCode;
        }
        
        /// <summary>
        /// Determines value equality with another <see cref="EquatableImmutableArray{T}"/>.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns>True if both contain the same sequence of values; otherwise false.</returns>
        public bool Equals(EquatableImmutableArray<T> other)
        {
            // Fast reference equality check
            if (ReferenceEquals(this, other)) return true;

            if (other is null) return false;

            // If hash codes are different, arrays can't be equal
            if (_hashCode != other._hashCode) return false;

            // We're really unlikely to get here, as we're using an instance cache
            // so we've probably encountered a hash collision,
            // or the instance cache is disabled.

            // Compare array lengths
            if (_values.Length != other._values.Length) return false;

            // If both are empty, they're equal
            if (_values.Length == 0) return true;

            // Element-by-element comparison
            var comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < _values.Length; i++)
            {
                if (!comparer.Equals(_values[i], other._values[i]))
                    return false;
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is EquatableImmutableArray<T> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => _hashCode;

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();

        internal static int CalculateHashCode(ImmutableArray<T> values, EqualityComparer<T> comparer, int currentHash, int startIndex)
        {
            int hash = currentHash;
            for (var index = startIndex; index < values.Length; index++)
            {
                var value = values[index];
                hash = HashHelpers_Combine(hash, value == null ? 0 : comparer.GetHashCode(value));
            }
            return hash;
        }
        internal static int HashHelpers_Combine(int h1, int h2)
        {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }
}
#endif