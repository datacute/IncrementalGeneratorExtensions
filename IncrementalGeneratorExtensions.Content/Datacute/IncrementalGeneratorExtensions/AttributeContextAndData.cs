#if !DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA // Feature: AttributeContextAndData
#if DATACUTE_EXCLUDE_TYPECONTEXT
#error AttributeContextAndData requires TypeContext (remove DATACUTE_EXCLUDE_TYPECONTEXT or also exclude DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA)
#endif
#if DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAY
#error AttributeContextAndData requires EquatableImmutableArray (remove DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAY or also exclude DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA)
#endif
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Datacute.IncrementalGeneratorExtensions
{
    /// <summary>
    /// Represents the context and data of an attribute in a source generator.
    /// </summary>
    /// <typeparam name="T">The type of the attribute data, which must implement <see cref="IEquatable{T}"/>.</typeparam>
    public readonly struct AttributeContextAndData<T> : IEquatable<AttributeContextAndData<T>> 
        where T : IEquatable<T>
    {
        /// <summary>
        /// The context of the type to which the attribute is applied.
        /// </summary>
        public readonly TypeContext Context;
        /// <summary>
        /// A collection of contexts for the containing types of the attribute's target symbol.
        /// </summary>
        public readonly EquatableImmutableArray<TypeContext> ContainingTypes;
        /// <summary>
        /// The data associated with the attribute, which is typically collected from the attribute's syntax context.
        /// </summary>
        public readonly T AttributeData;
        /// <summary>
        /// Indicates whether the attribute is in a file with a file-scoped namespace.
        /// </summary>
        public readonly bool IsInFileScopedNamespace;
        /// <summary>
        /// True if the nullable context is enabled at the location of the attribute.
        /// </summary>
        public readonly bool IsNullableContextEnabled;

        /// <summary>
        /// Indicates whether the containing namespace is the global namespace.
        /// </summary>
        public bool ContainingNamespaceIsGlobalNamespace => string.IsNullOrEmpty(Context.Namespace);
        /// <summary>
        /// The display string of the containing namespace.
        /// </summary>
        public string ContainingNamespaceDisplayString => Context.Namespace;
        /// <summary>
        /// Indicates whether the attribute's target symbol has any containing types.
        /// </summary>
        public bool HasContainingTypes => ContainingTypes.Length > 0;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeContextAndData{T}"/> struct.
        /// </summary>
        /// <param name="context">The context of the type to which the attribute is applied.</param>
        /// <param name="containingTypes">A collection of contexts for the containing types of the attribute's target symbol.</param>
        /// <param name="attributeData">The data associated with the attribute, which is typically collected from the attribute's syntax context.</param>
        /// <param name="isInFileScopedNamespace">True if the attribute is in a file with a file-scoped namespace</param>
        /// <param name="isNullableContextEnabled">True if the nullable context is enabled at the location of the attribute.</param>
        public AttributeContextAndData(
            TypeContext context, 
            EquatableImmutableArray<TypeContext> containingTypes, 
            T attributeData,
            bool isInFileScopedNamespace,
            bool isNullableContextEnabled)
        {
            Context = context;
            ContainingTypes = containingTypes;
            AttributeData = attributeData;
            IsInFileScopedNamespace = isInFileScopedNamespace;
            IsNullableContextEnabled = isNullableContextEnabled;
        }

        /// <inheritdoc />
        public bool Equals(AttributeContextAndData<T> other) => 
            Context.Equals(other.Context) && 
            Equals(ContainingTypes, other.ContainingTypes) &&
            EqualityComparer<T>.Default.Equals(AttributeData, other.AttributeData) &&
            IsInFileScopedNamespace == other.IsInFileScopedNamespace;

        /// <inheritdoc />
        public override bool Equals(object obj) => 
            obj is AttributeContextAndData<T> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Context.GetHashCode();
                hashCode = (hashCode * 397) ^ ContainingTypes.GetHashCode();
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(AttributeData);
                hashCode = (hashCode * 397) ^ (IsInFileScopedNamespace ? 1 : 0);
                return hashCode;
            }
        }
        
        /// <summary>
        /// Creates a file-safe hint name for a generated source file based on the type's context.
        /// </summary>
        /// <param name="generatorName">A specific name for your generator, e.g., "JsonSerializable"</param>
        /// <returns>A hint name, e.g., "My.Namespace.MyType-1.MyNestedType.JsonSerializable.g.cs"</returns>
        public string CreateHintName(string generatorName)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(Context.Namespace))
            {
                sb.Append(Context.Namespace).Append(".");
            }

            foreach (var containingType in ContainingTypes)
            {
                sb.Append(containingType.GetNameWithTypeParametersForHint()).Append(".");
            }

            sb.Append(Context.GetNameWithTypeParametersForHint());
            
            sb.Append(".").Append(generatorName).Append(".g.cs");

            return sb.ToString();
        }
        
        /// <summary>
        /// Predicate to determine if the syntax node is a partial type declaration syntax.
        /// </summary>
        /// <param name="syntaxNode">The syntax node to check.</param>
        /// <param name="token">The cancellation token to observe for cancellation requests.</param>
        /// <returns> True if the syntax node is a type declaration syntax, and is partial, otherwise false.</returns>
        /// <remarks>
        /// This supports structs, classes, records, and interfaces that are declared as partial.
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// context.SyntaxProvider
        ///     .ForAttributeWithMetadataName(
        ///         "YourFullyQualifiedMetadataName",
        ///         predicate: AttributeContextAndData&lt;YourAttributeDataType&gt;.Predicate,
        ///         transform: (syntaxContext, token) =>
        ///             AttributeContextAndData&lt;YourAttributeDataType&gt;.Transform(syntaxContext, yourAttributeDataCollector, token))
        ///     .WithTrackingName(GeneratorStage.ForAttributeWithMetadataName);
        /// </code>
        /// </example>
        /// <seealso cref="AttributeContextAndDataExtensions.SelectAttributeContexts{T}"/>
        public static bool Predicate(SyntaxNode syntaxNode, CancellationToken token)
        {
#if !DATACUTE_EXCLUDE_GENERATORSTAGE && !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE
            LightweightTrace.IncrementCount(GeneratorStage.ForAttributeWithMetadataNamePredicate);
#endif
            // We are only interested in partial type declarations
            if (!(syntaxNode is TypeDeclarationSyntax typeDeclaration))
                return false; // Not a type declaration

            if (!typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                return false; // Must be partial

            // Now, ensure all containing types are also partial.
            // This is necessary to be able to generate code for partial types correctly.
            SyntaxNode parent = typeDeclaration.Parent;
            while (parent is TypeDeclarationSyntax containingTypeDeclaration)
            {
                if (!containingTypeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return false;
                }
                parent = containingTypeDeclaration.Parent;
            }

            return true;
        }

        /// <summary>
        /// Transforms the <see cref="GeneratorAttributeSyntaxContext"/> into an <see cref="AttributeContextAndData{T}"/> instance.
        /// </summary>
        /// <param name="generatorAttributeSyntaxContext">The context of the attribute syntax, which includes information about the target symbol and the attribute itself.</param>
        /// <param name="attributeDataCollector">A function that collects the attribute data from the <see cref="GeneratorAttributeSyntaxContext"/>.</param>
        /// <param name="token">The cancellation token to observe for cancellation requests.</param>
        /// <returns>An instance of <see cref="AttributeContextAndData{T}"/> containing the attribute context and data.</returns>
        /// <example>
        /// <code lang="csharp">
        /// context.SyntaxProvider
        ///     .ForAttributeWithMetadataName(
        ///         "YourFullyQualifiedMetadataName",
        ///         predicate: AttributeContextAndData&lt;YourAttributeDataType&gt;.Predicate,
        ///         transform: (syntaxContext, token) =>
        ///             AttributeContextAndData&lt;YourAttributeDataType&gt;.Transform(syntaxContext, yourAttributeDataCollector, token))
        ///     .WithTrackingName(GeneratorStage.ForAttributeWithMetadataName);
        /// </code>
        /// </example>
        /// <seealso cref="AttributeContextAndDataExtensions.SelectAttributeContexts{T}"/>
        public static AttributeContextAndData<T> Transform(
            GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext, 
            Func<GeneratorAttributeSyntaxContext, T> attributeDataCollector, 
            CancellationToken token)
        {
#if !DATACUTE_EXCLUDE_GENERATORSTAGE && !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE
            LightweightTrace.IncrementCount(GeneratorStage.ForAttributeWithMetadataNameTransform);
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACEEXTENSIONS
            token.ThrowIfCancellationRequested(GeneratorStage.ForAttributeWithMetadataNameTransform);
#else
            token.ThrowIfCancellationRequested();
#endif
#endif
            T attributeData = attributeDataCollector(generatorAttributeSyntaxContext);

            var attributeTargetSymbol = (ITypeSymbol)generatorAttributeSyntaxContext.TargetSymbol;
            var typeDeclaration = (TypeDeclarationSyntax)generatorAttributeSyntaxContext.TargetNode;

            var isInFileScopedNamespace = HasFileScopedNamespace(typeDeclaration.SyntaxTree, token);

            var isNullableContextEnabled = GetIsNullableContextEnabled(generatorAttributeSyntaxContext.SemanticModel, typeDeclaration.SpanStart);

            EquatableImmutableArray<string> typeParameterNames;
            if (generatorAttributeSyntaxContext.TargetSymbol is INamedTypeSymbol namedTypeTargetSymbol)
            {
                ImmutableArray<ITypeParameterSymbol> typeParameters = namedTypeTargetSymbol.TypeParameters;
                typeParameterNames = typeParameters.Length > 0
                    ? typeParameters.ToEquatableImmutableArray(tp => tp.Name)
                    : EquatableImmutableArray<string>.Empty;
            }
            else
            {
                typeParameterNames = EquatableImmutableArray<string>.Empty;
            }

            var typeContext = CreateTypeContext(attributeTargetSymbol, isPartial: true, typeParameterNames);

            // Parse parent classes from symbol's containing types
            var parentClassCount = 0;
            var containingType = attributeTargetSymbol.ContainingType;
            // Count the number of parent classes
            while (containingType != null)
            {
                parentClassCount++;
                containingType = containingType.ContainingType;
            }

            EquatableImmutableArray<TypeContext> containingTypes;
            if (parentClassCount > 0)
            {
                containingType = attributeTargetSymbol.ContainingType;
                var containingTypesImmutableArrayBuilder = ImmutableArray.CreateBuilder<TypeContext>(parentClassCount);
                for (var i = 0; i < parentClassCount; i++)
                {
                    var typeParameters = containingType.TypeParameters;
                    var containingTypeTypeParameterNames = typeParameters.Length > 0
                        ? typeParameters.ToEquatableImmutableArray(tp => tp.Name)
                        : EquatableImmutableArray<string>.Empty;

                    // The predicate has already confirmed that this type is partial.
                    const bool containingTypeIsPartial = true;
                    var containingTypeContext = CreateTypeContext(containingType, containingTypeIsPartial, containingTypeTypeParameterNames);
                    containingTypesImmutableArrayBuilder.Insert(0, containingTypeContext);
                    containingType = containingType.ContainingType;
                }

                containingTypes = containingTypesImmutableArrayBuilder.MoveToImmutable().ToEquatableImmutableArray();
            }
            else
            {
                containingTypes = EquatableImmutableArray<TypeContext>.Empty;
            }

            return new AttributeContextAndData<T>(
                typeContext,
                containingTypes,
                attributeData,
                isInFileScopedNamespace,
                isNullableContextEnabled);
        }
        private static TypeContext CreateTypeContext(
            ITypeSymbol symbol,
            bool isPartial,
            EquatableImmutableArray<string> typeParameterNames)
        {
            return new TypeContext(
                TypeContext.GetNamespaceDisplayString(symbol.ContainingNamespace),
                symbol.Name,
                symbol.IsStatic,
                isPartial,
                symbol.IsAbstract,
                symbol.IsSealed,
                symbol.DeclaredAccessibility,
                TypeContext.GetTypeDeclarationKeyword(symbol),
                typeParameterNames);
        }

        private static bool HasFileScopedNamespace(SyntaxTree syntaxTree, CancellationToken token)
        {
            if (!(syntaxTree.GetRoot(token) is CompilationUnitSyntax root))
                return false;

            foreach (var member in root.Members)
            {
                if (member.GetType().Name == "FileScopedNamespaceDeclarationSyntax")
                    return true;
            }

            return false;
        }

        // This delegate is initialized to point to the bootstrap method. 
        // After the first run, it will point to the final, efficient implementation.
        // ReSharper disable once StaticMemberInGenericType There's typically only one instance.
        // ReSharper disable once InconsistentNaming purposely named like a method
        private static Func<SemanticModel, int, bool> GetIsNullableContextEnabled = BootstrapGetIsNullableContextEnabled;

        private static bool BootstrapGetIsNullableContextEnabled(SemanticModel semanticModel, int position)
        {
            var getNullableContextMethod = typeof(SemanticModel).GetMethod("GetNullableContext", new[] { typeof(int) });

            if (getNullableContextMethod != null)
            {
                // The GetNullableContext method exists.
                // Replace the delegate with an implementation that uses reflection to get the value.
                GetIsNullableContextEnabled = (sm, pos) =>
                {
                    var nullableContext = getNullableContextMethod.Invoke(sm, new object[] { pos });
                    // NullableContext.AnnotationsEnabled is 1 << 1 = 2
                    return ((int)nullableContext & 2) != 0;
                };
            }
            else
            {
                // The method doesn't exist on this version of Roslyn.
                // Replace the delegate with an implementation that always returns false.
                GetIsNullableContextEnabled = (sm, pos) => false;
            }

            // Call the newly assigned delegate to return the result for the current invocation.
            return GetIsNullableContextEnabled(semanticModel, position);
        }
    }
}
#endif // !DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA