#if !DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATAEXTENSIONS // Feature: AttributeContextAndDataExtensions
#if !DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA // Dependency: AttributeContextAndData
using System;
using Microsoft.CodeAnalysis;

namespace Datacute.IncrementalGeneratorExtensions
{
    /// <summary>
    /// Extension methods for wiring up <see cref="AttributeContextAndData{T}"/> collection into an incremental generator pipeline.
    /// </summary>
    public static class AttributeContextAndDataExtensions
    {
        /// <summary>
        /// Selects attribute contexts and their associated data from the syntax tree.
        /// </summary>
        /// <param name="context">The <see cref="IncrementalGeneratorInitializationContext"/></param>
        /// <param name="fullyQualifiedMetadataName">The fully qualified name of the marker attribute to select.</param>
        /// <param name="attributeDataCollector">A function that collects the attribute data from the <see cref="GeneratorAttributeSyntaxContext"/> of the context's SyntaxProvider.</param>
        /// <typeparam name="T">Type of the data associated with the attribute context.</typeparam>
        /// <returns>An <see cref="IncrementalValuesProvider{T}"/> that provides <see cref="AttributeContextAndData{T}"/>.</returns>
        /// <remarks>
        /// This method is a shortcut for using <see cref="AttributeContextAndData{T}"/>
        /// with the <see cref=" SyntaxValueProvider.ForAttributeWithMetadataName{T}"/> method.
        /// </remarks>
        /// <example>
        /// <code lang="csharp">
        /// var attributeContexts =
        ///     context.SelectAttributeContexts(
        ///         Templates.AttributeFullyQualified,
        ///         c => new AttributeData(c));
        /// </code>
        /// </example>
        public static IncrementalValuesProvider<AttributeContextAndData<T>> SelectAttributeContexts<T>(
            this IncrementalGeneratorInitializationContext context,
            string fullyQualifiedMetadataName,
            Func<GeneratorAttributeSyntaxContext, T> attributeDataCollector)
            where T : IEquatable<T>
            => context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName,
                    predicate: AttributeContextAndData<T>.Predicate,
                    transform: (syntaxContext, token) =>
                        AttributeContextAndData<T>.Transform(syntaxContext, attributeDataCollector, token))
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACEEXTENSIONS && !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE && !DATACUTE_EXCLUDE_GENERATORSTAGE
                .WithTrackingName(GeneratorStage.ForAttributeWithMetadataName);
#else
                ;
#endif
    }
}
#endif // !DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA
#endif // !DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATAEXTENSIONS
