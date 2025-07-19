<small>Back to Incremental Generator Extensions [README](README.md)</small>

---
# EmbeddedAttribute.cs and EmbeddedAttributeExtensions.cs
`EmbeddedAttribute` is a repeat of a standard Microsoft attribute.

This attribute can be used to mark a type as being only visible to the current assembly.

Most commonly, any types provided as sources during the
`context.RegisterPostInitializationOutput` call should be marked with this attribute to
prevent them from being used by other assemblies. The attribute will prevent any downstream
assemblies from consuming the type.

In NET-SDK-9.0.300 the `AddEmbeddedAttributeDefinition()` method was added to the
`IncrementalGeneratorPostInitializationContext` class, which is the type of the input
parameter provided to the `context.RegisterPostInitializationOutput()` callback.

`EmbeddedAttributeExtensions.cs` provides `AddEmbeddedAttributeDefinition()` as an
extension method so that it can be used in earlier versions of the .NET SDK.

`EmbeddedAttribute.cs` is also made available so that the attribute can be
used for other purposes.

See the portion of the Incremental Generators Cookbook in the Roslyn docs: [Put Microsoft.CodeAnalysis.EmbeddedAttribute on generated marker types](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.cookbook.md#put-microsoftcodeanalysisembeddedattribute-on-generated-marker-types)

# Example Usage
```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    context.RegisterPostInitializationOutput(static postInitializationContext =>
    {
        postInitializationContext.AddEmbeddedAttributeDefinition();
        //    THIS IS AVAILABLE --^

        postInitializationContext.AddSource(
            "MyCustomMarkerAttribute.g.cs", 
            """
            using System;
            using Microsoft.CodeAnalysis;
            // ... namespace, doc-comments, etc.
            [AttributeUsage(AttributeTargets.Class), Embedded]
            //                   THIS IS AVAILABLE --^
            internal sealed class MyCustomMarkerAttribute : Attribute
            {
                // ... constructor, properties, etc.
            }
            """);
    });
    // ... pipeline setup, etc
}
```

# Excluding the source files

To disable the inclusion of a specific source file,
define the relevant constant in the consuming project's `.csproj` file:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_EMBEDDEDATTRIBUTE</DefineConstants>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_EMBEDDEDATTRIBUTEEXTENSIONS</DefineConstants>
</PropertyGroup>
```

The files will still appear in the project, but will not add anything to the compilation.

### Note: No Dependency
`EmbeddedAttributeExtensions.cs` does not depend on `EmbeddedAttribute.cs` and will still work when it
is excluded.

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>