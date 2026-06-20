<small>Back to Incremental Generator Extensions [README](../README.md)</small>

---
# LightweightTrace.cs and LightweightTraceExtensions.cs
LightweightTrace is a zero‑allocation instrumentation layer for incremental source generators supporting two complementary modes:

- **Standard mode** (default): Ring-buffer timestamped events + composite-key counters for embedded diagnostics
- **EventSource mode** (cross-platform): Direct integration with .NET diagnostic events (ETW on Windows; diagnostic tools on Linux/macOS)

Both modes share the same public API and output formats. You get a timestamped event trace, composite-key counters (id + optional value + mapping flag) for histograms and categorical buckets, automatic method-call frequency counting, and unified AppendDiagnosticsComment output embeddable in generated code. EventSource mode optionally streams events to real-time diagnostic listeners. LightweightTraceExtensions wires this into IncrementalValue/Values providers and adds cancellation logging helpers.
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

// Instance cache size metrics for a generic type
LightweightTrace.IncrementCount(GeneratorStage.EquatableImmutableArrayInstanceCacheSize, typeMapId, true);

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
- Name mapping for ids and mapped values from three sources: per-call map, shared custom map, and runtime-registered names.
- AppendDiagnosticsComment combines counters + trace into one embeddable block.
- Cancellation helpers record where cancellation was observed.

## Name Mapping Sources
When diagnostics text is rendered, name lookup uses this precedence:
1. The map passed into `AppendCounts`, `AppendTrace`, or `AppendDiagnosticsComment`.
2. The shared custom map set once with `LightweightTrace.SetCustomEventNames(...)`.
3. Runtime registrations created via `LightweightTrace.RegisterName(...)`.

This allows a mixed model where stable pipeline/event names are supplied up front, while runtime-discovered names, such as generic type names used by instance-cache metrics, are registered dynamically.

Example setup:
```csharp
LightweightTrace.SetCustomEventNames(GeneratorStageDescriptions.GeneratorStageNameMap);

var typeMapId = LightweightTrace.RegisterName(typeof(MyType).ToString());
LightweightTrace.IncrementCount(GeneratorStage.EquatableImmutableArrayInstanceCacheSize, typeMapId, mapValue: true);
```
## Encoding (Brief)
Composite key: composite = id + (value * CompositeValueShift) + (mapped ? MapValueFlag : 0). Decode splits into id, value, mapped flag. This keeps storage dense and lookups cheap.

## Extending GeneratorStage With Your Own Events
You get a built-in baseline enum (`GeneratorStage`) and a name map (`GeneratorStageDescriptions.GeneratorStageNameMap`). Extend by defining your own enum with distinct numeric values (gaps are fine) and merge its names into a dictionary so both built-in and custom events share one lookup.

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
Result: diagnostics output shows both core `GeneratorStage` events and your custom stages with readable names.

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

## EventSource Mode (Cross-Platform Diagnostic Events)

By default, LightweightTrace uses a ring-buffer implementation. You can optionally enable EventSource-based diagnostic event tracing by defining `DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE`:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_LIGHTWEIGHTTRACE_USE_EVENTSOURCE</DefineConstants>
</PropertyGroup>
```

### Benefits
- **Integrates with platform diagnostic systems**: ETW on Windows, diagnostic listeners on Linux/macOS
- **Lower latency**: Events are streamed to listeners in real time (when listeners are active)
- **Same API**: Both modes share identical public methods; swap modes without code changes
- **No divergence**: Single implementation prevents drift between modes
- **Zero overhead when disabled**: No encode/decode cycles when EventSource is inactive

### Usage
When using EventSource mode, initialize tracing once at startup before any trace calls:

```csharp
// Initialize once at startup
LightweightTrace.InitializeEtw(
    eventSourceName: "MyGenerator-Trace",
    eventLevel: EventLevel.Informational,
    eventNameMap: MyGeneratorStageDescriptions.EventNameMap
);

// Use normally; events flow to both ring-buffer and active listeners
LightweightTrace.Add(GeneratorStage.Processing);
LightweightTrace.IncrementCount(GeneratorStage.ItemsProcessed, itemCount);
```

### Event Levels
When using EventSource mode, you can control verbosity per event type:

- **Critical**: Fatal errors requiring immediate attention
- **Error**: Error conditions that may affect functionality
- **Warning**: Potentially problematic conditions
- **Informational** (default): General informational messages
- **Verbose**: Detailed tracing for deep investigation

Example: enable only warnings and above:
```csharp
LightweightTrace.InitializeEtw(eventLevel: EventLevel.Warning);
```

### Embedding Diagnostics vs. Real-Time Events
- **Standard mode**: All tracing goes into the ring-buffer; call `AppendDiagnosticsComment()` to embed in generated source
- **EventSource mode**: Events flow to both the ring-buffer (for embedding) and active diagnostic listeners simultaneously

This means EventSource mode is ideal for **live profiling during development** while keeping the option to embed diagnostics for investigation in running code. When no listeners are active, EventSource calls are gated by `IsEnabled()` checks, minimizing overhead.

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>