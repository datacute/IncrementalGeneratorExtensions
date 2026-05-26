using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Datacute.IncrementalGeneratorExtensions.Tests
{
    internal static class RoslynHelpers
    {
        public static CSharpCompilation Compile(string source)
        {
            var tree = CSharpSyntaxTree.ParseText(source,
                new CSharpParseOptions(LanguageVersion.LatestMajor));
            var trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            var refs = trustedAssemblies
                .Split(Path.PathSeparator)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
                .ToArray();
            return CSharpCompilation.Create("Test", new[] { tree }, refs);
        }

        public static ITypeSymbol GetType(string source, string metadataName)
        {
            var compilation = Compile(source);
            var symbol = compilation.GetTypeByMetadataName(metadataName);
            if (symbol == null)
                throw new InvalidOperationException(
                    $"Could not find type '{metadataName}'. Diagnostics: " +
                    string.Join("; ", compilation.GetDiagnostics().Select(d => d.ToString())));
            return symbol;
        }
    }
}
