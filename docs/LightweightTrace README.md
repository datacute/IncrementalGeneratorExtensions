<small>Back to Incremental Generator Extensions [README](../README.md)</small>

---
# LightweightTrace.cs and LightweightTraceExtensions.cs
***todo - expand on copied points below***

- Provides a lightweight tracing mechanism and provides an easy way to integrate
  with the incremental source generator's `WithTrackingName` diagnostic mechanism.
- Usage counters and timing logs can be included as a comment in the generated source.
- Provides an enum `GeneratorStage` with descriptions for each stage of the generator,
  which can be used to track the execution flow.
- The `GeneratorStageDescriptions` provides a mapping of the enum values to their descriptions,
  in English, which can be used when generating diagnostic logs and counters.

# Example Usage

_**todo**_

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
is excluded. (Unless you supply your own implementation of `LightweightTrace<T>`.)

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>