# Datacute Incremental Generator Extensions

[![Build](https://github.com/datacute/IncrementalGeneratorExtensions/actions/workflows/ci.yml/badge.svg)](https://github.com/datacute/IncrementalGeneratorExtensions/actions/workflows/ci.yml)

Source for the `Datacute.IncrementalGeneratorExtensions` NuGet package,
which adds source files that provide additional functionality
for incremental source generators in .NET.

---
Explanation of the above...

## The NuGet package

> Source for the `Datacute.IncrementalGeneratorExtensions` NuGet package ...

This GitHub repository is for the development of a NuGet package named
[`Datacute.IncrementalGeneratorExtensions`](https://www.nuget.org/packages/Datacute.IncrementalGeneratorExtensions).

## What the NuGet package does

> ... which adds source files ...

The NuGet package provides an incremental source generator, which adds 
source code to be compiled in with the consuming project's own source files.

## What the added source files do

> ... that provide additional functionality
> for incremental source generators in .NET.

The added source files are various extensions methods and supporting classes
to help with the development of incremental source generators in .NET.

- [EmbeddedAttribute Support](docs/EmbeddedAttribute%20README.md)
  - Adds support for Microsoft's `EmbeddedAttribute` when running in older
    SDK & Roslyn versions. This can help with the behaviour of marker attributes.
- [EquatableImmutableArray](docs/EquatableImmutableArray%20README.md)
  - Provides an `EquatableImmutableArray<T>` type which enables value-based
    equality comparison of array contents, rather than the reference equality
    of the array instance itself, which is what `ImmutableArray<T>` uses.
  - Incremental source generators produce new `ImmutableArray<T>` outputs within their
    pipelines, and by converting these to `EquatableImmutableArray<T>` instances,
    the pipeline stages can be correctly identified as having no changes in their
    output.
- [Lightweight Tracing](docs/LightweightTrace%20README.md)
  - Provides a lightweight tracing mechanism and provides an easy way to integrate
    with the incremental source generator's `WithTrackingName` diagnostic mechanism.

## Customizing the experience

### Hiding the README file

When the NuGet package is installed, a README file appears in the consuming project.

That README file contains details on how to hide it, and a brief introduction to the package's
contents, and some links to further documentation.

The file can be hidden by adding a property to the `.csproj` file:

```xml
<PropertyGroup>
  <Datacute_IncludeReadmeFile>false</Datacute_IncludeReadmeFile>
</PropertyGroup>
```

### Excluding individual source files

Each included source file is enclosed in an `#if` directive:

```csharp
#if !DATACUTE_EXCLUDE_EMBEDDEDATTRIBUTE

// ... rest of EmbeddedAttribute file ...

#endif
```

To disable the inclusion of a specific source file,
define the relevant constant in the consuming project's `.csproj` file:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_EMBEDDEDATTRIBUTE</DefineConstants>
</PropertyGroup>
```

The file will still be generated, but it will not add anything to the compilation.

# Thanks

- Andrew Lock | .NET Escapades
  - Series: [Creating a source generator](https://andrewlock.net/series/creating-a-source-generator/)
  - In particular the ['Watch out for collection types'](https://andrewlock.net/creating-a-source-generator-part-9-avoiding-performance-pitfalls-in-incremental-generators/#4-watch-out-for-collection-types)
    portion of part 9 *Avoiding performance pitfalls in incremental generators*

---

*_Datacute - Acute Information Revelation Tools_*