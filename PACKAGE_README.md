Provides extension methods and helper classes designed to
simplify the development of .NET Incremental Source Generators.

It adds a directory of source files directly to your project,
included in your build, making it easier to package your
incremental source code generator.

## Features Included

**EmbeddedAttribute Support**:
    
- Adds support for Microsoft's `EmbeddedAttribute` when running in older
  SDK & Roslyn versions.

**EquatableImmutableArray**:

- Provides an `EquatableImmutableArray<T>` type which enables value-based
  equality comparison of array contents, rather than the reference equality
  of the array instance itself, which is what `ImmutableArray<T>` uses.
- Incremental source generators produce new `ImmutableArray<T>` outputs within their
  pipelines, and by converting these to `EquatableImmutableArray<T>` instances,
  the pipeline stages can be correctly identified as having no changes in their
  output.

**Attribute Context and Data**

- Adds types and extension methods to simplify collecting data about each use of a marker attribute.
- `TypeContext` captures the type information.
- `AttributeContextAndData` captures the attribute data, which includes the `TypeContext` of the type marked by 
  the attribute, and the `TypeContext` of each of the containing types.
- `AttributeContextAndData` has a generic type argument which is your type that holds
  information collected for the attribute, such as its positional and named arguments.

**Indented StringBuilder**:
- Provides a customisable `IndentingLineAppender` class that wraps a `StringBuilder` and adds
  auto-indentation support, making it easier to generate indented source code.

- **Lightweight Tracing**:

- A simple tracing mechanism that integrates with the incremental source generator's
  `WithTrackingName` API, making it easier to diagnose and debug your generator's execution.
- Usage counters and timing logs can be included as a comment in the generated source.
- Provides an enum `GeneratorStage` with descriptions for common stages of the generator pipeline.
