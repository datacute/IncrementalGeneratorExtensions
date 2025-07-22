# Datacute.IncrementalGeneratorExtensions

Online Documentation: [GitHub Repository](https://github.com/datacute/IncrementalGeneratorExtensions)

This README is automatically included in your project by the NuGet package.

(Some IDEs may mistakenly make this file editable, but it is intended to be a read-only quick intro.)

## How to Disable the inclusion of this README

When you don't want this README file to be included in your project, you disable it by adding the following property to your project file (.csproj):

```xml
<PropertyGroup>
    <Datacute_IncludeReadmeFile>false</Datacute_IncludeReadmeFile>
</PropertyGroup>
```

# NuGet Package Contents

Datacute.IncrementalGeneratorExtensions provides utility methods and classes to simplify the development of .NET Incremental Source Generators. The package contains several features to enhance your source generator development experience.

## EmbeddedAttribute Support

- Adds the `EmbeddedAttribute` definition for use in older .NET SDK versions
- Includes the `AddEmbeddedAttributeDefinition` extension methods to simplify adding the attribute definition to your generator

## EquatableImmutableArray

- Provides value-based equality comparison for immutable arrays
- Helps maintain the incremental nature of your generators by correctly identifying unchanged outputs
- Adds extension methods `CombineEquatable` and `CollectEquatable` to simplify use within incremental pipelines

## Lightweight Tracing and Generator Stages

It can be challenging to get runtime diagnostics for your incremental source generator.
The `LightweightTrace` classes provides a simple way to trace the execution of your generator stages,
making it easier to debug and understand the flow of your generator.

- Simple diagnostic tools to track generator execution
  ```csharp
  LightweightTrace.Add(GeneratorStage.RegisterPostInitializationOutput);
  ``` 
- Overloads `WithTrackingName` method to take an enum
  ```csharp
  var texts =
    context.AdditionalTextsProvider
      .Select(SelectFileInfo)
      .WithTrackingName(GeneratorStage.AdditionalTextsProviderSelect) // tracing the first stage output
      .CombineEquatable(attributeGlobs)
      .WithTrackingName(YourCustomTrackingNamesEnum.FileInfoAndGlobsCombined); // tracing the second stage output
  ``` 
- Small circular buffer: 1024 tuples of (long timestamp, int eventId) 
- Maintains usage counts
- Enum and Descriptions for generator stages to provide context in logs
- Diagnostic logs can be included in your generator's output for easier analysis:
  ```csharp
  _buffer.AppendDiagnosticsComment(GeneratorStageDescriptions.GeneratorStageNameMap);
  ```


# Additional Resources

- [.NET Source Generator Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- Andrew Lock | .NET Escapades - Series: [Creating a source generator](https://andrewlock.net/series/creating-a-source-generator/)

---

*Datacute - Acute Information Revelation Tools*