# Datacute Incremental Generator Extensions

[![Build](https://github.com/datacute/IncrementalGeneratorExtensions/actions/workflows/ci.yml/badge.svg)](https://github.com/datacute/IncrementalGeneratorExtensions/actions/workflows/ci.yml)

> Source for the `Datacute.IncrementalGeneratorExtensions` NuGet package,
  which adds source files that provide additional functionality
  for incremental source generators in .NET.

---
Breaking down the tagline...

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

- [SourceTextGenerator Base Class](docs/SourceTextGeneratorBase%20README.md)
  - Provides a base class for incremental source generators that handles the boilerplate
    of generating a partial class (or similar) file for an instance of a marker attribute.
- [EquatableImmutableArray](docs/EquatableImmutableArray%20README.md)
  - Provides an `EquatableImmutableArray<T>` type which enables value-based
    equality comparison of array contents, rather than the reference equality
    of the array instance itself, which is what `ImmutableArray<T>` uses.
  - Incremental source generators produce new `ImmutableArray<T>` outputs within their
    pipelines, and by converting these to `EquatableImmutableArray<T>` instances,
    the pipeline stages can be correctly identified as having no changes in their
    output.
- [Attribute Context and Data](docs/AttributeContextAndData%20README.md)
  - Adds types and extension methods to simplify collecting data about each use of a marker attribute.
  - `TypeContext` captures the type information.
  - `AttributeContextAndData` captures the attribute data, which includes the `TypeContext` of the type marked by
    the attribute, and the `TypeContext` of each of the containing types.
  - `AttributeContextAndData` has a generic type argument which is your type that holds
    information collected for the attribute, such as its positional and named arguments.
- [Indented StringBuilder](docs/IndentingLineAppender%20README.md)
  - Provides a customisable `IndentingLineAppender` class that wraps a `StringBuilder` and adds
    auto-indentation support, making it easier to generate indented source code.
  - Includes a `TabIndentingLineAppender` customisation that uses tabs for indentation.
- [Lightweight Tracing](docs/LightweightTrace%20README.md)
  - Provides a lightweight tracing mechanism and provides an easy way to integrate
    with the incremental source generator's `WithTrackingName` diagnostic mechanism.
  - Supports usage counters and timing logs can be included as a comment in the generated source.
- [Pipeline Generator Stages Enum](docs/GeneratorStage%20README.md)
  - Provides an enum `GeneratorStage` with descriptions for each stage of the generator,
    which can be used to track the execution flow in the Lightweight Tracing methods.

## Minimal Example

### Generator wiring
```csharp
[Generator]
public sealed class DemoGenerator : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    var usages = context.SelectAttributeContexts(
      "MyNamespace.GenerateSomethingAttribute", 
      GenerateSomethingData.Collect);

    context.RegisterSourceOutput(usages, static (spc, usage) =>
    {
      var gen = new GenerateSomethingSource(in usage, in spc.CancellationToken);
      spc.AddSource(usage.CreateHintName("GenerateSomething"), gen.GetSourceText());
    });
  }
}
```

### Simple marker attribute
```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerateSomethingAttribute : Attribute
{
  public GenerateSomethingAttribute(string name) => Name = name;
  public string Name { get; }
}
```

### Collect projected payload
```csharp
sealed class GenerateSomethingData : IEquatable<GenerateSomethingData>
{
  public GenerateSomethingData(string name) => Name = name;
  public string Name { get; }
  // Equals / GetHashCode omitted for brevity
  public static GenerateSomethingData Collect(GeneratorAttributeSyntaxContext c)
    => new GenerateSomethingData((string)c.Attributes[0].ConstructorArguments[0].Value);
}
```

### Emit using the base class
```csharp
sealed class GenerateSomethingSource : SourceTextGeneratorBase<GenerateSomethingData>
{
  readonly GenerateSomethingData _data;

  public GenerateSomethingSource(
    in AttributeContextAndData<GenerateSomethingData> usage,
    in CancellationToken token)
    : base(in usage, in token) => _data = usage.AttributeData;

  protected override void AppendCustomMembers()
    => Buffer.AppendLine($"public static string GeneratedName => \"{_data.Name}\";");
}
```

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
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE

// ... rest of LightweightTrace.cs file ...

#endif
```

To disable the inclusion of a specific source file,
define the relevant constant in the consuming project's `.csproj` file:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE</DefineConstants>
</PropertyGroup>
```

The file will still be generated, but it will not add anything to the compilation.

## Document Roles
Each README targets a specific audience:

* `README.md` (this doc)
  * Audience: GitHub viewers, contributors and evaluators.
  * Purpose: Explain project scope, features, architecture links, build & contribution basics.
* `PACKAGE_README.md`
  * Audience: Potential NuGet consumers browsing nuget.org packages.
  * Purpose: Quick value summary, barest minimal example.
* `content/Datacute.IncrementalGeneratorExtensions.README.md`
  * Audience: Existing users inside their IDE after install.
  * Purpose: Fast in‑project reference: feature list, exclusion flags, how to hide file, doc links.
* `docs` directory
  * Audience: Anyone needing more detail than the brief READMEs.
  * Purpose: Online extended docs for each helper.

Guideline: Depth lives in `docs/`; keep package README short; keep content README scannable.

# Thanks

- Andrew Lock | .NET Escapades
  - Series: [Creating a source generator](https://andrewlock.net/series/creating-a-source-generator/)
  - In particular the ['Watch out for collection types'](https://andrewlock.net/creating-a-source-generator-part-9-avoiding-performance-pitfalls-in-incremental-generators/#4-watch-out-for-collection-types)
    portion of part 9 *Avoiding performance pitfalls in incremental generators*

---

*_Datacute - Acute Information Revelation Tools_*