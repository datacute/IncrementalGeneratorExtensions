#if !DATACUTE_EXCLUDE_EMBEDDEDATTRIBUTEEXTENSIONS

// ReSharper disable RedundantNameQualifier including global:: qualifier is good practice when supplying sourcecode to an unknown context
// ReSharper disable UnusedMember.Global project source is used in other projects
namespace Datacute.IncrementalGeneratorExtensions
{
    public static class EmbeddedAttributeExtensions
    {
        private const string EmbeddedAttributeDefinition = @"namespace Microsoft.CodeAnalysis
{
    internal sealed partial class EmbeddedAttribute : global::System.Attribute
    {
    }
}";
        
        /// <summary>
        /// Adds a <see cref="Microsoft.CodeAnalysis.Text.SourceText">SourceText</see> to the compilation containing the definition of <c>Microsoft.CodeAnalysis.EmbeddedAttribute</c>.
        /// The source will have a <c>hintName</c> of Microsoft.CodeAnalysis.EmbeddedAttribute. 
        /// </summary>
        /// <remarks>
        /// This attribute can be used to mark a type as being only visible to the current assembly.
        /// Most commonly, any types provided during this
        /// <see cref="Microsoft.CodeAnalysis.IncrementalGeneratorPostInitializationContext">IncrementalGeneratorPostInitializationContext</see>
        /// should be marked with this attribute to prevent them from being used by other assemblies.
        /// The attribute will prevent any downstream assemblies from consuming the type.
        /// </remarks>
        public static void AddEmbeddedAttributeDefinition(this global::Microsoft.CodeAnalysis.IncrementalGeneratorPostInitializationContext context) 
            => context.AddSource(
                "Microsoft.CodeAnalysis.EmbeddedAttribute", 
                global::Microsoft.CodeAnalysis.Text.SourceText.From(EmbeddedAttributeDefinition, encoding: global::System.Text.Encoding.UTF8));
    }
}
#endif