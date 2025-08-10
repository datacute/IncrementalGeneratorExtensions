<small>Back to Incremental Generator Extensions [README](../README.md)</small>

---
# GeneratorStage.cs and GeneratorStageDescriptions.cs
Purpose: provide the canonical event id set and readable name map that plug directly into `LightweightTrace` so you can capture, aggregate and render meaningful incremental generator instrumentation without inventing your own numbering scheme. You increment counts or add events with `GeneratorStage` values and later resolve names via `GeneratorStageDescriptions.GeneratorStageNameMap` (optionally merged with your custom ids ≥ 100).

- Provides an enum `GeneratorStage` for each common stage of the generator pipeline,
  for use with `LightweightTrace` to track the execution flow.
- The `GeneratorStageDescriptions` provides a mapping of the enum values to their descriptions,
  in English, which can be used when generating diagnostic logs and counters.

# Example Usage
```csharp
// Merge built-in names (optionally append your own >= 100)
static readonly Dictionary<int,string> EventNameMap = new(GeneratorStageDescriptions.GeneratorStageNameMap)
{
  { 100, "My Custom Aggregation" }
};

// Track pipeline stage throughput and events:
var texts = context.AdditionalTextsProvider
    .Select(SelectFileInfo)
    .WithTrackingName(GeneratorStage.AdditionalTextsProviderSelect);
var options = context.AnalyzerConfigOptionsProvider
    .Select(GeneratorOptions.Select)
    .WithTrackingName(GeneratorStage.AnalyzerConfigOptionsProviderSelect);

LightweightTrace.Add(GeneratorStage.SourceProductionContextAddSource);
context.AddSource(hintName, source);

// Later: emit counts with readable names
buffer.AppendDiagnosticsComment(MyCustomExtendedDescriptions.EventNameMap);
```

### Enum Value Guidance
Numbers less than 100 are allocated to built-in lifecycle, provider projection, optional EquatableImmutableArray cache metrics, and generic method entry/exit / call markers. 
Create your own enum using values between 100 and 1023 (to fit `LightweightTrace` expectations) and merge its names into a dictionary copied from `GeneratorStageDescriptions.GeneratorStageNameMap`.

### Recommended Use
Instrument only while diagnosing hotspots (cache misses, excessive provider selects) – remove or exclude for zero cost later.

### Performance
Each counter increment is a single array index increment inside `LightweightTrace`; overhead is negligible relative to typical Roslyn analysis. Excluding `GeneratorStage` (define `DATACUTE_EXCLUDE_GENERATORSTAGE`) compiles out all references in generated code that are guarded.

# Excluding the source files

To disable the inclusion of a specific source file,
define the relevant constant in the consuming project's `.csproj` file:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_GENERATORSTAGE</DefineConstants>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_GENERATORSTAGEDESCRIPTIONS</DefineConstants>
</PropertyGroup>
```

The files will still appear in the project, but will not add anything to the compilation.

### Note: Dependency
Direct:
* `GeneratorStageDescriptions.cs` -> `GeneratorStage.cs`

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>