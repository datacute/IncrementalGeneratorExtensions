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

The NuGet package is a style of **source-only** package, which means that it adds 
source code to be compiled in with the consuming project's own source files.

Normally packages provide compiled binaries, but this package is intended for 
environments where things work so much more smoothly
when there are no third party dependencies in the output.

## What the added source files do

> ... that provide additional functionality
> for incremental source generators in .NET.

The added source files are various extensions methods and supporting classes
to help with the development of incremental source generators in .NET.

- [EmbeddedAttribute Support](IncrementalGeneratorExtensions/EmbeddedAttribute%20README.md)
  - Adds support for Microsoft's `EmbeddedAttribute` when running in older
    SDK & Roslyn versions. This can help with the behaviour of marker attributes.
- [EquatableImmutableArray](IncrementalGeneratorExtensions/EquatableImmutableArray%20README.md)
  - Provides an `EquatableImmutableArray<T>` type which enables value-based
    equality comparison of array contents, rather than the reference equality
    of the array instance itself, which is what `ImmutableArray<T>` uses.
  - Incremental source generators produce new `ImmutableArray<T>` outputs within their
    pipelines, and by converting these to `EquatableImmutableArray<T>` instances,
    the pipeline stages can be correctly identified as having no changes in their
    output.
- [Lightweight Tracing](IncrementalGeneratorExtensions/LightweightTrace%20README.md)
  - Provides a lightweight tracing mechanism and provides an easy way to integrate
    with the incremental source generator's `WithTrackingName` diagnostic mechanism.

## So is this a Source Generator?

Not quite. While it could have been implemented as a source generator,
it doesn't use the context within the consuming project
to change the source files which it makes available.
(Except that it can disable the inclusion of individual source files.)

The way that this package adds source files is by adding to the dotnet project's contents.

## So is it a standard source-only package?

Not quite. They typically use a `contentFiles` directory for each target framework,
and while this package is targeting source code generators which use netstandard2.0,
and would work as a standard source-only package,
the way that various IDEs automatically include and display the included source files
does not provide the flexibility that this package desires.
(Specifically: renaming the directory where the source files appear.)

## Customizing the experience

### Configuring the directory name

By default, the source files appear alongside the project's source,
in a directory named `packageSource`

The consuming project can change the name of this directory by
adding a property to its `.csproj` file:

```xml
<PropertyGroup>
  <Datacute_PackageSource_DirName>includes</Datacute_PackageSource_DirName>
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

The file will still appear in the project, but it will not add anything to the compilation.

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>