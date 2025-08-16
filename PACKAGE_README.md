Fast-start helper set for building .NET incremental source generators: drop-in source files (added to your compilation) that cover attribute data collection, value-equality wrappers, structured emission, indentation, and lightweight tracing.

## Features
* SourceTextGenerator base class
* EquatableImmutableArray
* Attribute Context and Data (with TypeContext)
* IndentingLineAppender (and tab variant)
* LightweightTrace & GeneratorStage enum

More details, examples and exclusion symbols: https://github.com/datacute/IncrementalGeneratorExtensions

## Quick Example (minimal, trimmed)
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

### With a simple attribute:
```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenerateSomethingAttribute : Attribute
{
  public GenerateSomethingAttribute(string name) => Name = name;
  public string Name { get; }
}
```

### Collecting attribute constructor arguments:
```csharp
sealed class GenerateSomethingData : IEquatable<GenerateSomethingData>
{
  public GenerateSomethingData(string name) => Name = name;
  public string Name { get; }

  // Equals and GetHashCode not shown to keep the example brief

  public static GenerateSomethingData Collect(GeneratorAttributeSyntaxContext c)
    => new GenerateSomethingData((string)c.Attributes[0].ConstructorArguments[0].Value);
}
```

### Using the simplest source generator:
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
