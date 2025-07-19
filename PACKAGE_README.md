Provides extension methods and helper classes designed to
simplify the development of .NET Incremental Source Generators.

It adds a directory of source files directly to your project,
included in your build, making it easier to package your
incremental source code generator.

## Features Included

**EmbeddedAttribute Support**:
    
* Adds support for Microsoft's `EmbeddedAttribute` when running in older
  SDK & Roslyn versions.

**EquatableImmutableArray**:

- Provides an `EquatableImmutableArray<T>` type which enables value-based
  equality comparison of array contents, rather than the reference equality
  of the array instance itself, which is what `ImmutableArray<T>` uses.
- Incremental source generators produce new `ImmutableArray<T>` outputs within their
  pipelines, and by converting these to `EquatableImmutableArray<T>` instances,
  the pipeline stages can be correctly identified as having no changes in their
  output.

**Lightweight Tracing**:

* A simple tracing mechanism that integrates with the incremental source generator's `WithTrackingName` API, making it easier to diagnose and debug your generator's execution.
