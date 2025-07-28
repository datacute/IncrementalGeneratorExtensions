using System;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Datacute.IncrementalGeneratorExtensions
{
    [Generator]
    public class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ProcessEmbeddedResources);
        }

        private void ProcessEmbeddedResources(IncrementalGeneratorPostInitializationContext context)
        {
            // Get the assembly where this generator is defined
            var assembly = typeof(Generator).Assembly;

            // Retrieve all embedded resource names
            var resourceNames = assembly.GetManifestResourceNames();

            foreach (var resourceName in resourceNames)
            {
                // Only process .cs files
                if (!resourceName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Extract a suitable file name for the generated source
                var fileName = Path.GetFileNameWithoutExtension(Path.GetFileName(resourceName)) + ".g.cs";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) continue;

                    using (var reader = new StreamReader(stream))
                    {
                        var sourceCode = reader.ReadToEnd();
                        var sourceText = SourceText.From(sourceCode, Encoding.UTF8);

                        // Add the source to the compilation
                        context.AddSource(fileName, sourceText);
                    }
                }
            }
        }
    }
}