# Datacute.IncrementalGeneratorExtensions

Online Documentation: [GitHub Repository](https://github.com/datacute/IncrementalGeneratorExtensions)

This README is automatically included in your project by the NuGet package.

(Your IDE may mistakenly make this file editable, but it is intended to be a read-only reference.)

## How to Disable the inclusion of this README

When you don't want this README file to be included in your project, you disable it by adding the following property to your project file (.csproj):

```xml
<PropertyGroup>
    <Datacute_IncludeReadmeFile>false</Datacute_IncludeReadmeFile>
</PropertyGroup>
```

# NuGet Package Contents

Datacute.IncrementalGeneratorExtensions provides utility methods and classes to simplify the development of .NET Incremental Source Generators. The package contains several features to enhance your source generator development experience.

### EmbeddedAttribute Support

- Adds the `EmbeddedAttribute` definition for use in older .NET SDK versions
- Includes extension methods to simplify adding the attribute definition to your generator

### EquatableImmutableArray

- Provides value-based equality comparison for immutable arrays
- Helps maintain the incremental nature of your generators by correctly identifying unchanged outputs

### Lightweight Tracing and Generator Stages

- Simple diagnostic tools to track generator execution
- Integrates with the `WithTrackingName` API for better debugging
- Enum and Descriptions for generator stages to provide context in logs


# Additional Resources

- [.NET Source Generator Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- Andrew Lock | .NET Escapades - Series: [Creating a source generator](https://andrewlock.net/series/creating-a-source-generator/)

---

*Datacute - Acute Information Revelation Tools*