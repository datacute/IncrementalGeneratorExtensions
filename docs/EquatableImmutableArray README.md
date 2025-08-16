<small>Back to Incremental Generator Extensions [README](../README.md)</small>

---
# EquatableImmutableArray.cs and EquatableImmutableArrayExtensions.cs

Efficient value-based collection equality for incremental generator pipelines.

`ImmutableArray<T>` compares by reference; two arrays with identical elements are
considered different. In a Roslyn incremental pipeline that causes unnecessary downstream
re-execution. `EquatableImmutableArray<T>` wraps an `ImmutableArray<T>` and implements
`IEquatable<EquatableImmutableArray<T>>` by structural comparison (fast‑path hash + instance cache).

Benefits in generator graphs:
* Stable equality: identical logical sequences short‑circuit change detection, trimming downstream recomputation (CPU) and reallocation (GC).
* Small wrapper: only original array reference + cached hash + length; negligible overhead versus element storage.
* Optional instance cache: reuses wrappers for identical sequences so repeated projections or orderings avoid re-hashing and element scans.
* Tracing hooks (with LightweightTrace + GeneratorStage): surface cache hits/misses and typical array length distribution to guide optimisation.

Extensions (`EquatableImmutableArrayExtensions`) integrate easily with Roslyn providers: `CollectEquatable`, `CombineEquatable`, `ToEquatableImmutableArray`.

# Example Usage

### 1. Converting an existing ImmutableArray after building it
```csharp
var builder = ImmutableArray.CreateBuilder<ParentInfo>(count);
// ... populate builder ...
ParentInfos = builder.MoveToImmutable().ToEquatableImmutableArray();
```

### 2. Projecting while converting (ImmutableArray<TSource> -> EquatableImmutableArray<T>)
```csharp
EquatableImmutableArray<string> names = symbols.ToEquatableImmutableArray(s => s.Name);
```

### 3. Chaining inside an incremental pipeline
```csharp
var combined = leftProvider
  .CombineEquatable(rightProvider)          // right collected + converted
  .Select(static tuple => Process(tuple.Left, tuple.Right));
```

### 4. Collecting values and keeping value equality
```csharp
IncrementalValueProvider<EquatableImmutableArray<MyItem>> allItems = sourceValues.CollectEquatable();
```

### 5. Disabling the cache (compile constant) for debugging / deterministic profiling
```xml
<DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE</DefineConstants>
```
Now each `Create` call allocates a fresh wrapper; equality still works (hash computed once per wrapper) but no cache hit/miss counters are recorded.

---
### API Highlights
Factory:
* `EquatableImmutableArray<T>.Empty` – canonical empty instance (never allocate another empty wrapper).
* `EquatableImmutableArray<T>.Create(ImmutableArray<T>, CancellationToken)` – convert + (optionally) cache; central entry point used by all extension helpers.

Extensions (selected):
* `ImmutableArray<T>.ToEquatableImmutableArray()` / projecting overload – wrap in value-equality immediately (optionally transform elements).
* `IEnumerable<T>.ToEquatableImmutableArray()` – materialise + wrap in one call.
* `IncrementalValuesProvider<T>.CollectEquatable()` – `Collect()` then wrap so downstream stages see stable equality.
* `IncrementalValuesProvider<TLeft>.CombineEquatable(IncrementalValuesProvider<TRight>)` – pairs each left with an equatable collected right set.
* `IncrementalValueProvider<TLeft>.CombineEquatable(IncrementalValuesProvider<TRight>)` – value + collected equatable values for fan-out scenarios.

Equality Implementation Outline:
1. Instance cache (length -> first element hash -> weak list) narrows candidates.
2. If candidate hash matches cached hash, element compare only if needed.
3. Hash stored; subsequent comparisons often short‑circuit on hash & reference.

When the cache is excluded the structural hash is computed once per wrapper; equality still uses hash then element compare.

### Choosing When To Convert
Convert to `EquatableImmutableArray<T>` every time an `ImmutableArray<T>` value emerges in your pipeline or local code that could participate in equality-based change detection. Uniform conversion keeps semantics simple (arrays are always value-equal) and avoids accidental missed optimisation points. The wrapper + hash cost is tiny relative to typical generator work; consistency wins.

### Performance Notes
* Hash combine uses cheap rotate‑and‑xor (similar to .NET HashHelpers) for low per-element cost.
* Cache reduces wrapper allocations & equality cost when identical sequences recur (common for symbol/type parameter lists).
* Weak references allow reclaimed wrappers; stale slots removed opportunistically during candidate scans.
* Optional cancellation checks (with tracing) keep long conversions responsive to rapid edit iterations.

### Tracing (Optional)
With LightweightTrace + GeneratorStage included you may see counters:
* `EquatableImmutableArrayCacheHit` – structural reuse (fast path success).
* `EquatableImmutableArrayCacheMiss` – new distinct sequence added to cache.
* `EquatableImmutableArrayCacheWeakReferenceRemoved` – stale wrapper slot cleaned.
* `EquatableImmutableArrayLength` – length histogram to spot pathological sizes.
Disable by excluding `LightweightTrace` or `GeneratorStage`, or by disabling the cache (miss counters suppressed in non‑cache mode).

### Memory Considerations
Only additional data beyond the wrapped `ImmutableArray<T>` is a cached hash (`int`).

# Excluding the source files

To disable the inclusion of a specific source file,
define the relevant constant in the consuming project's `.csproj` file:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAY</DefineConstants>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYEXTENSIONS</DefineConstants>
</PropertyGroup>
```

The files will still appear in the project, but will not add anything to the compilation.

### Note: Dependencies
Direct:
* `EquatableImmutableArrayExtensions.cs` -> `EquatableImmutableArray.cs`

Internal Implementation Detail (optional performance):
* `EquatableImmutableArrayInstanceCache.cs` (used by factory; excluding it forfeits caching but core correctness remains). Define `DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE` to bypass caching; each Create() call allocates a fresh wrapper and no cache hit/miss counters are recorded.

Behavior:
* Extensions file only guards the core type; no transitive dependencies.

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>