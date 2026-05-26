using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    public class TypeContextTests
    {
        [Fact]
        public void TypeDeclaration_PublicStaticPartialGenericClass_FormatsAllModifiersInOrder()
        {
            // Arrange
            var parameters = ImmutableArray.Create("T", "U").ToEquatableImmutableArray();
            var ctx = new TypeContext(
                @namespace: "Ns", name: "Foo",
                isStatic: true, isPartial: true, isAbstract: false, isSealed: false,
                accessibility: Accessibility.Public,
                typeDeclarationKeyword: "class",
                typeParameterNames: parameters);

            // Act
            var result = ctx.TypeDeclaration();

            // Assert
            Assert.Equal("public static partial class Foo<T,U>", result);
        }

        [Fact]
        public void TypeDeclaration_Interface_SuppressesAbstractModifier()
        {
            // Arrange — interfaces are implicitly abstract; the keyword must be omitted
            // even when IsAbstract is true on the symbol.
            var ctx = new TypeContext(
                "", "IFoo",
                isStatic: false, isPartial: false, isAbstract: true, isSealed: false,
                Accessibility.Public, "interface", EquatableImmutableArray<string>.Empty);

            // Act
            var result = ctx.TypeDeclaration();

            // Assert
            Assert.Equal("public interface IFoo", result);
        }

        [Theory]
        [InlineData("struct")]
        [InlineData("record struct")]
        public void TypeDeclaration_StructForms_SuppressSealedModifier(string keyword)
        {
            // Arrange — structs are implicitly sealed; the keyword must be omitted
            // even when IsSealed is true on the symbol.
            var ctx = new TypeContext(
                "", "Foo",
                isStatic: false, isPartial: false, isAbstract: false, isSealed: true,
                Accessibility.Public, keyword, EquatableImmutableArray<string>.Empty);

            // Act
            var result = ctx.TypeDeclaration();

            // Assert
            Assert.Equal($"public {keyword} Foo", result);
        }

        [Fact]
        public void TypeDeclaration_SealedRecordClass_KeepsSealedModifier()
        {
            // Arrange — record classes CAN be subclassed by default, so 'sealed' must be preserved
            // (this is the discriminator that the struct-suppression rule must not over-match on).
            var ctx = new TypeContext(
                "", "Foo",
                isStatic: false, isPartial: false, isAbstract: false, isSealed: true,
                Accessibility.Public, "record", EquatableImmutableArray<string>.Empty);

            // Act
            var result = ctx.TypeDeclaration();

            // Assert
            Assert.Equal("public sealed record Foo", result);
        }

        [Theory]
        [InlineData("public class TypeA { }", "class")]
        [InlineData("public struct TypeA { }", "struct")]
        [InlineData("public interface TypeA { }", "interface")]
        [InlineData("public enum TypeA { A }", "enum")]
        [InlineData("public delegate void TypeA();", "delegate")]
        [InlineData("public record class TypeA;", "record")]
        [InlineData("public record struct TypeA;", "record struct")]
        public void GetTypeDeclarationKeyword_DispatchesOnTypeKind(string source, string expected)
        {
            // Arrange
            var typeSymbol = RoslynHelpers.GetType(source, "TypeA");

            // Act
            var keyword = TypeContext.GetTypeDeclarationKeyword(typeSymbol);

            // Assert
            Assert.Equal(expected, keyword);
        }

        [Fact]
        public void GetTypeParameterList_TwoParameters_JoinsWithComma()
        {
            // Arrange
            var parameters = ImmutableArray.Create("T", "U").ToEquatableImmutableArray();

            // Act
            var result = TypeContext.GetTypeParameterList(parameters);

            // Assert
            Assert.Equal("<T,U>", result);
        }

        [Fact]
        public void GetNameWithTypeParametersForHint_TwoParameters_JoinsWithUnderscores()
        {
            // Arrange
            var parameters = ImmutableArray.Create("T", "U").ToEquatableImmutableArray();

            // Act
            var result = TypeContext.GetNameWithTypeParametersForHint("MyType", parameters);

            // Assert
            Assert.Equal("MyType_T_U", result);
        }
    }
}
