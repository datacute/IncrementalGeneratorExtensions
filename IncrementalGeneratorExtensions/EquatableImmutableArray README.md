<small>Back to Incremental Generator Extensions [README](README.md)</small>

---
# EquatableImmutableArray.cs and EquatableImmutableArrayExtensions.cs
***todo - expand on copied points below***

- Provides an `EquatableImmutableArray<T>` type which enable value-based
  equality comparison of array contents, rather than the reference equality
  of the array instance itself, which is what `ImmutableArray<T>` uses.
- Incremental source generators produce `ImmutableArray<T>` outputs within their
  pipelines, and by converting these to `EquatableImmutableArray<T>` instances,
  the pipeline stages can be correctly identified as having no changes in their
  output.

# Example Usage

_**todo**_ See the doc-comments in the source files for example usage.

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

### Note: Dependency
`EquatableImmutableArrayExtensions.cs` depends on `EquatableImmutableArray.cs` and will **not work** when it
is excluded. (Unless you supply your own implementation of `EquatableImmutableArray<T>`.)

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>