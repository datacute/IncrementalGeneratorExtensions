<small>Back to Incremental Generator Extensions [README](../README.md)</small>

---
# LightweightTrace.cs and LightweightTraceExtensions.cs
LightweightTrace is a zero‑allocation instrumentation layer for incremental source generators: a timestamped ring buffer of events plus composite-key counters (id + optional value + mapping flag) that represent histograms, categorical buckets, and method-call frequencies.
It can emit a single embedded diagnostics comment (counters + trace) into generated source so you can understand pipeline behavior (frequency, timing patterns, entry/exit flow) without external tooling. LightweightTraceExtensions wires this into IncrementalValue/Values providers and adds cancellation logging helpers.
## Sample Diagnostics Output
```text
/* Diagnostics
Counters:
[011] ForAttributeWithMetadataName Predicate: 6
[012] ForAttributeWithMetadataName Transform: 6
[018] EquatableImmutableArray Cache Hit: 3
[019] EquatableImmutableArray Cache Miss: 5
[021] EquatableImmutableArray Length (1): 1
[021] EquatableImmutableArray Length (2): 4
[021] EquatableImmutableArray Length (6): 2
[021] EquatableImmutableArray Length (8): 1
[050] Method Call (Generator Initialize): 2
[050] Method Call (Register Post Initialization Output): 2
[050] Method Call (Register Source Output): 6
[050] Method Call (Source Production Context Add Source): 5
[050] Method Call (ForAttributeWithMetadataName Pipeline Output): 6
[050] Method Call (AdditionalTextsProvider Select): 2
[050] Method Call (AnalyzerConfigOptionsProvider Select): 2
[050] Method Call (Combined Attributes and Options): 12
[050] Method Call (Selected Attribute Glob Info (Path/Ext)): 12
[050] Method Call (Selected Attribute Globs (Path/Ext)): 12
[050] Method Call (Combined File Info and Resource Globs): 2
[050] Method Call (Filtered Files Matching Globs): 2
[050] Method Call (Generating Doc Comment): 2
[050] Method Call (Extracted EmbeddedResource (with File/Glob info)): 2
[050] Method Call (Combined Resource/File Data and All Attribute Glob Info): 2
[050] Method Call (Selected Matching (AttributeContext, EmbeddedResource)): 8
[050] Method Call (Grouped Resources by AttributeContext into Lookup): 1
[050] Method Call (Prepared Final Generation Input (AttrContext, Resources, Options)): 6

Trace Log:
2025-07-26T05:01:43.6826743Z [000] Generator Initialize (Method Entry)
2025-07-26T05:01:43.7094197Z [000] Generator Initialize (Method Exit)
2025-07-26T05:01:43.7116005Z [002] Register Post Initialization Output (Method Entry)
2025-07-26T05:01:43.7168645Z [002] Register Post Initialization Output (Method Exit)
2025-07-26T05:01:44.3301246Z [010] ForAttributeWithMetadataName Pipeline Output
2025-07-26T05:01:44.3301294Z [010] ForAttributeWithMetadataName Pipeline Output
2025-07-26T05:01:44.3308954Z [130] Combined Attributes and Options (Method Entry)
2025-07-26T05:01:44.3313608Z [130] Combined Attributes and Options (Method Entry)
2025-07-26T05:01:44.3537540Z [130] Combined Attributes and Options (Method Exit)
2025-07-26T05:01:44.3539443Z [130] Combined Attributes and Options (Method Exit)
2025-07-26T05:01:44.3362945Z [016] AnalyzerConfigOptionsProvider Select (Method Entry)
2025-07-26T05:01:44.3420436Z [016] AnalyzerConfigOptionsProvider Select (Method Exit)
2025-07-26T05:01:44.4064968Z [141] Selected Attribute Glob Info (Path/Ext) (Method Entry)
2025-07-26T05:01:44.4065008Z [141] Selected Attribute Glob Info (Path/Ext) (Method Entry)
2025-07-26T05:01:44.4123922Z [141] Selected Attribute Glob Info (Path/Ext) (Method Exit)
2025-07-26T05:01:44.4125724Z [141] Selected Attribute Glob Info (Path/Ext) (Method Exit)
2025-07-26T05:01:44.4129990Z [142] Selected Attribute Globs (Path/Ext) (Method Entry)
2025-07-26T05:01:44.4130013Z [142] Selected Attribute Globs (Path/Ext) (Method Entry)
2025-07-26T05:01:44.4181419Z [142] Selected Attribute Globs (Path/Ext) (Method Exit)
2025-07-26T05:01:44.4183188Z [142] Selected Attribute Globs (Path/Ext) (Method Exit)
2025-07-26T05:01:44.4376394Z [143] Combined File Info and Resource Globs
2025-07-26T05:01:44.4459070Z [144] Filtered Files Matching Globs
2025-07-26T05:01:44.4469755Z [145] Generating Doc Comment (99)
2025-07-26T05:01:44.4494728Z [145] Generating Doc Comment (100)
2025-07-26T05:01:44.4530661Z [146] Extracted EmbeddedResource (with File/Glob info)
2025-07-26T05:01:44.4773410Z [147] Combined Resource/File Data and All Attribute Glob Info
2025-07-26T05:01:44.4886634Z [148] Selected Matching (AttributeContext, EmbeddedResource)
2025-07-26T05:01:44.4886679Z [148] Selected Matching (AttributeContext, EmbeddedResource)
2025-07-26T05:01:44.5108575Z [150] Grouped Resources by AttributeContext into Lookup
2025-07-26T05:01:44.5237805Z [160] Prepared Final Generation Input (AttrContext, Resources, Options)
2025-07-26T05:01:44.5247888Z [003] Register Source Output
2025-07-26T05:01:44.5417853Z [007] Source Production Context Add Source
2025-07-26T05:01:44.5418159Z [003] Register Source Output
2025-07-26T05:01:44.5419170Z [007] Source Production Context Add Source
*/
```
## Example Calls Producing Parts Of That Output
```csharp
// ForAttributeWithMetadataName Predicate / Transform (counters & pipeline outputs)
LightweightTrace.IncrementCount(GeneratorStage.ForAttributeWithMetadataNamePredicate);
LightweightTrace.Add(GeneratorStage.ForAttributeWithMetadataNamePipelineOutput);

// EquatableImmutableArray Length histogram bucket
LightweightTrace.IncrementCount(GeneratorStage.EquatableImmutableArrayLength, values.Length);

// Method call mapping (shows up as Method Call (...))
LightweightTrace.Add(GeneratorStage.RegisterSourceOutput);
LightweightTrace.IncrementCount(GeneratorStage.MethodCall, (int)GeneratorStage.RegisterSourceOutput, mapValue: true);

// Method Entry / Exit wrapping a stage
LightweightTrace.MethodEntry(GeneratorStage.Initialize);
LightweightTrace.MethodExit(GeneratorStage.Initialize);
```
## Capabilities
- Single-int composite key packs id + optional value (+ flag) to avoid allocations.
- Counters (simple, histogram buckets, mapped enum categories).
- Time-stamped rolling trace (ring buffer) with method entry/exit tagging.
- Automatic method call counting (except MethodExit) for frequency insight.
- Name mapping for ids and (optionally flagged) values via built-in GeneratorStage / GeneratorStageDescriptions plus your own merged enum map.
- AppendDiagnosticsComment combines counters + trace into one embeddable block.
- Cancellation helpers record where cancellation was observed.
## Encoding (Brief)
Composite key: composite = id + (value * CompositeValueShift) + (mapped ? MapValueFlag : 0). Decode splits into id, value, mapped flag. This keeps storage dense and lookups cheap.

## Extending GeneratorStage With Your Own Events
You get a built-in baseline enum (GeneratorStage) and a name map (GeneratorStageDescriptions.GeneratorStageNameMap). Extend by defining your own enum with distinct numeric values (gaps are fine) and merge its names into a lazy dictionary so both built-in and custom events share one lookup.

Example enum (abbreviated, made-up stages):
```csharp
public enum MyPipelineStage
{
    FooParsed = 200,
    FooAndBarCombined = 210,
    BarFiltered = 220,
    BazGrouped = 230,
    OutputComposed = 240,
    DiagnosticsEmitted = 250
}
```
Name map merging built-in GeneratorStage names with custom names:
```csharp
public static class MyPipelineStageDescriptions
{
    public static Dictionary<int,string> EventNameMap => _lazy.Value;
    private static readonly Lazy<Dictionary<int,string>> _lazy = new Lazy<Dictionary<int,string>>(Create);
    private static Dictionary<int,string> Create()
    {
        var map = new Dictionary<int,string>(GeneratorStageDescriptions.GeneratorStageNameMap)
        {
            { (int)MyPipelineStage.FooParsed, "Foo Parsed" },
            { (int)MyPipelineStage.FooAndBarCombined, "Foo & Bar Combined" },
            { (int)MyPipelineStage.BarFiltered, "Bar Filtered" },
            { (int)MyPipelineStage.BazGrouped, "Baz Grouped" },
            { (int)MyPipelineStage.OutputComposed, "Output Composed" },
            { (int)MyPipelineStage.DiagnosticsEmitted, "Diagnostics Emitted" },
        };
        return map;
    }
}
```
Using custom events alongside GeneratorStage:
```csharp
LightweightTrace.Add(MyPipelineStage.FooParsed);                              // event
LightweightTrace.IncrementCount(MyPipelineStage.BarFiltered);                 // counter
LightweightTrace.MethodEntry(MyPipelineStage.OutputComposed);                 // entry
LightweightTrace.MethodExit(MyPipelineStage.OutputComposed);                  // exit
LightweightTrace.IncrementCount(GeneratorStage.MethodCall,
    (int)MyPipelineStage.OutputComposed, mapValue: true);                      // categorical mapping
buffer.AppendDiagnosticsComment(MyPipelineStageDescriptions.EventNameMap);    // merged names
```
Result: diagnostics output shows both core GeneratorStage events and your custom stages with readable names.

## Recommended Numeric ID Ranges
CompositeValueShift = 1024, so valid base event/counter IDs (the id portion) are 0–1023.

| Range     | Intended Use                                                |
|-----------|-------------------------------------------------------------|
| 0–99      | Built-in / core GeneratorStage (leave gaps for expansion).  |
| 100–1023  | Your custom stages (pick sparse numbers, avoid collisions). |

Beyond 1023 (>= CompositeValueShift) cannot be used for the base id field unless you change CompositeValueShift (must remain a power of two).  

Value (the second part of a composite key: id + value*CompositeValueShift [+ MapValueFlag]) occupies higher bits:
* Unmapped keys (flag off) must remain < MapValueFlag (1 << 28). This yields a maximum raw value of (MapValueFlag / CompositeValueShift) - 1 = 262,143 (2^18 - 1) before the flag bit would be set.
* Mapped keys add MapValueFlag, so their value portion can extend further; maximum safe mapped value ≈ floor((Int32.MaxValue - MapValueFlag) / CompositeValueShift) = 1,835,007.

Guidelines:
1. Keep base IDs < 1024; raise CompositeValueShift only if you truly need more distinct IDs.
2. Reserve low IDs for framework / shared semantics; start custom enums at 100 or above.
3. Leave numeric gaps for future stages (e.g., 100,110,120...) to insert later steps cleanly.
4. Bucket numbers (value argument) are independent of the base ID range; keep them small for readability even though large values are supported.
5. When mapping enum values (mapValue: true) ensure those enum members also have numeric values < CompositeValueShift so they fit the id naming space of the map.
6. Avoid redefining an existing numeric ID with a new meaning once shipped; allocate a new ID instead.
 
# Excluding the source files

To disable the inclusion of a specific source file,
define the relevant constant in the consuming project's `.csproj` file:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE</DefineConstants>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_LIGHTWEIGHTTRACEEXTENSIONS</DefineConstants>
</PropertyGroup>
```

The files will still appear in the project, but will not add anything to the compilation.

### Note: Dependency
`LightweightTraceExtensions.cs` depends on `LightweightTrace.cs` and will **not work** when it
is excluded. (Unless you supply your own implementation of `LightweightTrace`.)

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>