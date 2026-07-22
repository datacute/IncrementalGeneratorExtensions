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

## SourceTextGenerator Base Class

- Provides a base class for incremental source generators that handles the boilerplate
  of generating a partial class (or similar) file for an instance of a marker attribute.

## EquatableImmutableArray

- Provides value-based equality comparison for immutable arrays
- Helps maintain the incremental nature of your generators by correctly identifying unchanged outputs
- Adds extension methods `CombineEquatable` and `CollectEquatable` to simplify use within incremental pipelines

## Attribute Context and Data

- Adds types and extension methods to simplify collecting data about each use of a marker attribute.
- `TypeContext` captures the type information.
- `AttributeContextAndData` captures the attribute data, which includes the `TypeContext` of the type marked by
  the attribute, and the `TypeContext` of each of the containing types.
- `AttributeContextAndData` has a generic type argument which is your type that holds
  information collected for the attribute, such as its positional and named arguments.

## Indented StringBuilder

- Provides a customisable `IndentingLineAppender` class that wraps a `StringBuilder` and adds
  auto-indentation support, making it easier to generate indented source code.
- Includes a `TabIndentingLineAppender` customisation that uses tabs for indentation.

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

Composite key encoding (overview):
- ID and optional value are packed into a single int using a stride and a flag for value-name mapping.
- Use CompositeValueShift and MapValueFlag to understand the packing; EncodeKey/DecodeKey helpers are available.

### EventSource Integration

`LightweightTrace` always records trace calls in its ring buffer and maintains counters. For real-time diagnostic event streaming, define `DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE`, capture EventSource configuration early in `IIncrementalGenerator.Initialize`, then explicitly enable publication. Initialization captures the EventSource name, level, and optional name map; it does not create an EventSource or publish events.

```xml
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE</DefineConstants>
</PropertyGroup>
```

**EventSource Extension Methods** (in `LightweightTraceExtensions`): The initialization methods are extensions on `IncrementalGeneratorInitializationContext` so EventSource setup is declared alongside the rest of the generator pipeline setup, and the conditional method can read the consuming project's MSBuild properties from the context.

**Conditional configuration and enablement** — captures configuration immediately, then reads an MSBuild property to enable or disable publication:
```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    context.InitializeEventSourceIfEnabled("DatacuteGeneratorUseEventSource");
}
```

Then in the consuming project's `.csproj`:
```xml
<PropertyGroup>
  <DatacuteGeneratorUseEventSource>true</DatacuteGeneratorUseEventSource>
</PropertyGroup>
```

> **Note:** MSBuild properties are available through an incremental generator provider, so conditional enablement is implemented as an additional source-generator pipeline. EventSource publication can change only when that pipeline runs; events generated earlier remain in the ring buffer.

**Unconditional initialization** — capture parameters and enable publication by default:
```csharp
context.InitializeEventSource(
    eventSourceName: "MyGenerator-Trace",
    eventLevel: EventLevel.Informational,
    eventNameMap: MyEventDescriptions.EventNameMap);
```

Pass `enableEventSource: false` to `InitializeEventSource(...)` when configuration should be captured without enabling publication. Both methods return `context` for method chaining. `LightweightTrace` records trace calls and counters regardless of EventSource configuration or enablement. `EnableEventSource()` lazily creates the EventSource and publishes subsequent events to active diagnostic listeners. `DisableEventSource()` stops publication while ring-buffer tracing and counters continue. The configured `eventLevel` applies to every event emitted by `LightweightTrace`, not individual event types. Changing the configured EventSource name disables and disposes the current source; call `EnableEventSource()` again to create the new one.

For detailed EventSource documentation, see the [extended docs](https://github.com/datacute/IncrementalGeneratorExtensions/blob/main/docs/LightweightTrace%20README.md).



# Additional Resources

- [.NET Source Generator Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)
- Andrew Lock | .NET Escapades - Series: [Creating a source generator](https://andrewlock.net/series/creating-a-source-generator/)

---

*Datacute - Acute Information Revelation Tools*
