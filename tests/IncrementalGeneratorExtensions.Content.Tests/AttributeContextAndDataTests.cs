using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    public class AttributeContextAndDataTests
    {
        // ----- Predicate (the partial-everywhere walker) -----

        [Fact]
        public void Predicate_PartialTopLevelClass_ReturnsTrue()
        {
            // Arrange
            var node = ParseFirstTypeDeclaration("public partial class Foo { }");

            // Act
            var result = AttributeContextAndData<int>.Predicate(node, CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Predicate_NonPartialTopLevelClass_ReturnsFalse()
        {
            // Arrange
            var node = ParseFirstTypeDeclaration("public class Foo { }");

            // Act
            var result = AttributeContextAndData<int>.Predicate(node, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Predicate_NestedPartialInsideNonPartialOuter_ReturnsFalse()
        {
            // Arrange — Inner is partial but Outer is not; the parent-walk must catch this.
            var node = ParseLastTypeDeclaration(
                "public class Outer { public partial class Inner { } }");

            // Act
            var result = AttributeContextAndData<int>.Predicate(node, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Predicate_NestedPartialInsidePartialOuter_ReturnsTrue()
        {
            // Arrange
            var node = ParseLastTypeDeclaration(
                "public partial class Outer { public partial class Inner { } }");

            // Act
            var result = AttributeContextAndData<int>.Predicate(node, CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Predicate_NonTypeDeclarationNode_ReturnsFalse()
        {
            // Arrange
            var tree = CSharpSyntaxTree.ParseText("namespace Foo {}");
            var nsNode = tree.GetRoot().DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().First();

            // Act
            var result = AttributeContextAndData<int>.Predicate(nsNode, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        // ----- CreateHintName (the dotted-path assembly with generic suffixes) -----

        [Fact]
        public void CreateHintName_WithNamespaceNoContainingTypes_ConcatenatesDotted()
        {
            // Arrange
            var data = MakeData(
                ctx: MakeTypeContext("My.Ns", "Foo"),
                containing: EquatableImmutableArray<TypeContext>.Empty);

            // Act
            var result = data.CreateHintName("MyGen");

            // Assert
            Assert.Equal("My.Ns.Foo.MyGen.g.cs", result);
        }

        [Fact]
        public void CreateHintName_GlobalNamespace_OmitsLeadingDot()
        {
            // Arrange
            var data = MakeData(
                ctx: MakeTypeContext("", "Foo"),
                containing: EquatableImmutableArray<TypeContext>.Empty);

            // Act
            var result = data.CreateHintName("MyGen");

            // Assert
            Assert.Equal("Foo.MyGen.g.cs", result);
        }

        [Fact]
        public void CreateHintName_ContainingTypeWithTypeParameters_UsesUnderscoreSuffix()
        {
            // Arrange — Outer<T> nests Inner; the containing-type segment must use the
            // hint-name encoding (Outer_T) rather than the angle-bracket form.
            var outerParameters = ImmutableArray.Create("T").ToEquatableImmutableArray();
            var outer = new TypeContext("My.Ns", "Outer",
                isStatic: false, isPartial: true, isAbstract: false, isSealed: false,
                Accessibility.Public, "class", outerParameters);
            var containing = ImmutableArray.Create(outer).ToEquatableImmutableArray();

            var data = MakeData(
                ctx: MakeTypeContext("My.Ns", "Inner"),
                containing: containing);

            // Act
            var result = data.CreateHintName("Gen");

            // Assert
            Assert.Equal("My.Ns.Outer_T.Inner.Gen.g.cs", result);
        }

        // ----- Equality (incremental-pipeline correctness) -----

        [Fact]
        public void Equals_DiffersOnIsNullableContextEnabled_ShouldReturnFalse()
        {
            // Arrange — two contexts differing ONLY in IsNullableContextEnabled. The
            // generator emits "#nullable enable" based on this flag, so the produced
            // source differs. Equal pipeline values would cause incremental caching to
            // skip regeneration when the nullable context changes.
            var ctx = MakeTypeContext("Ns", "Foo");
            var a = new AttributeContextAndData<int>(ctx,
                EquatableImmutableArray<TypeContext>.Empty,
                attributeData: 0,
                isInFileScopedNamespace: false,
                isNullableContextEnabled: false);
            var b = new AttributeContextAndData<int>(ctx,
                EquatableImmutableArray<TypeContext>.Empty,
                attributeData: 0,
                isInFileScopedNamespace: false,
                isNullableContextEnabled: true);

            // Act
            var result = a.Equals(b);

            // Assert
            Assert.False(result);
        }

        // ----- helpers -----

        private static TypeDeclarationSyntax ParseFirstTypeDeclaration(string source)
            => CSharpSyntaxTree.ParseText(source).GetRoot()
                .DescendantNodes().OfType<TypeDeclarationSyntax>().First();

        private static TypeDeclarationSyntax ParseLastTypeDeclaration(string source)
            => CSharpSyntaxTree.ParseText(source).GetRoot()
                .DescendantNodes().OfType<TypeDeclarationSyntax>().Last();

        private static TypeContext MakeTypeContext(string @namespace, string name)
            => new TypeContext(@namespace, name,
                isStatic: false, isPartial: true, isAbstract: false, isSealed: false,
                Accessibility.Public, "class", EquatableImmutableArray<string>.Empty);

        private static AttributeContextAndData<int> MakeData(
            TypeContext ctx, EquatableImmutableArray<TypeContext> containing)
            => new AttributeContextAndData<int>(ctx, containing,
                attributeData: 0,
                isInFileScopedNamespace: false,
                isNullableContextEnabled: false);
    }
}
