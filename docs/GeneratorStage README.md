<small>Back to Incremental Generator Extensions [README](../README.md)</small>

---
# GeneratorStage.cs and GeneratorStageDescriptions.cs
***todo - expand on copied points below***

- Provides an enum `GeneratorStage` for each common stage of the generator pipeline,
  for use with Lightweight Tracing to track the execution flow.
- The `GeneratorStageDescriptions` provides a mapping of the enum values to their descriptions,
  in English, which can be used when generating diagnostic logs and counters.

# Example Usage

_**todo**_

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
`GeneratorStageDescriptions.cs` depends on `GeneratorStage.cs` and will **not work** when it
is excluded.

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>