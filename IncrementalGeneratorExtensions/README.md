The source files in this directory have been added to the project
by the `Datacute.IncrementalGeneratorExtensions` package.

# Contents

- [EmbeddedAttribute Support](EmbeddedAttribute%20README.md)
    - Adds support for Microsoft's `EmbeddedAttribute` when running in older
      SDK & Roslyn versions. This can help with the behaviour of marker attributes.
- [EquatableImmutableArray](EquatableImmutableArray%20README.md)
    - Provides an `EquatableImmutableArray<T>` type which enable value-based
      equality comparison of array contents, rather than the reference equality
      of the array instance itself, which is what `ImmutableArray<T>` uses.
    - Incremental source generators produce `ImmutableArray<T>` outputs within their
      pipelines, and by converting these to `EquatableImmutableArray<T>` instances,
      the pipeline stages can be correctly identified as having no changes in their
      output.
- [Lightweight Tracing](LightweightTrace%20README.md)
    - Provides a lightweight tracing mechanism and provides an easy way to integrate
      with the incremental source generator's `WithTrackingName` diagnostic mechanism.

# Configuring the directory name

By default, the added source files appear
alongside the project's source,
in a directory named `packageSource`

The name of this directory can be changed by
adding a property to the `.csproj` file:

```xml
<PropertyGroup>
  <Datacute_PackageSource_DirName>includes</Datacute_PackageSource_DirName>
</PropertyGroup>
```

# Excluding individual source files

Each included source file is enclosed in an `#if` directive:

```csharp
#if !DATACUTE_EXCLUDE_EMBEDDEDATTRIBUTE

... rest of EmbeddedAttribute file ...

#endif
```

To disable the inclusion of a specific source file,
define the relevant constant in the `.csproj` file:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_EMBEDDEDATTRIBUTE</DefineConstants>
</PropertyGroup>
```

The file will still appear in the project,
but it will not add anything to the compilation.

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>