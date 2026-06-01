using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Datacute.IncrementalGeneratorExtensions.Benchmarks
{
    internal static class ImmutableArrayFactory
    {
        public static ImmutableArray<TypeContext> BuildAscending(int length)
        {
            var builder = ImmutableArray.CreateBuilder<TypeContext>(length);
            for (int i = 0; i < length; i++)
            {
                builder.Add(CreateTypeContext(i));
            }
            return builder.MoveToImmutable();
        }

        public static ImmutableArray<TypeContext> BuildDescending(int length)
        {
            var builder = ImmutableArray.CreateBuilder<TypeContext>(length);
            for (int i = length - 1; i >= 0; i--)
            {
                builder.Add(CreateTypeContext(i));
            }
            return builder.MoveToImmutable();
        }

        public static ImmutableArray<TypeContext> BuildWithUniqueFirstElement(int length, int firstElementId)
        {
            var builder = ImmutableArray.CreateBuilder<TypeContext>(length);
            builder.Add(CreateTypeContext(firstElementId));
            for (int i = 1; i < length; i++)
            {
                builder.Add(CreateTypeContext(i));
            }
            return builder.MoveToImmutable();
        }

        public static ImmutableArray<TypeContext> BuildWithUniqueLastElement(int length, int lastElementId)
        {
            var builder = ImmutableArray.CreateBuilder<TypeContext>(length);
            for (int i = 0; i < length - 1; i++)
            {
                builder.Add(CreateTypeContext(i));
            }
            builder.Add(CreateTypeContext(lastElementId));
            return builder.MoveToImmutable();
        }

        /// <summary>
        /// Build an array where the middle element is unique (specified by <paramref name="middleElementId"/>),
        /// all other elements follow a predictable sequence.
        /// </summary>
        public static ImmutableArray<TypeContext> BuildWithUniqueMiddleElement(int length, int middleElementId)
        {
            var builder = ImmutableArray.CreateBuilder<TypeContext>(length);
            int middleIndex = length / 2; // for even lengths this chooses the upper-middle index
            for (int i = 0; i < length; i++)
            {
                if (i == middleIndex)
                {
                    builder.Add(CreateTypeContext(middleElementId));
                }
                else
                {
                    builder.Add(CreateTypeContext(i));
                }
            }
            return builder.MoveToImmutable();
        }

        public static ImmutableArray<TypeContext> BuildShort(params int[] ids)
        {
            var builder = ImmutableArray.CreateBuilder<TypeContext>(ids.Length);
            foreach (var id in ids)
            {
                builder.Add(CreateTypeContext(id));
            }
            return builder.MoveToImmutable();
        }

        private static TypeContext CreateTypeContext(int i)
        {
            return new TypeContext(
                "MyNamespace",
                "Class" + i,
                isStatic: false,
                isPartial: true,
                isAbstract: false,
                isSealed: false,
                Accessibility.Public,
                "class",
                EquatableImmutableArray<string>.Empty);
        }
    }
}
