<small>Back to Incremental Generator Extensions [README](../README.md)</small>

---
# SourceTextGeneratorBase.cs
***todo - expand on copied points below***

- Provides a base class for incremental source generators that handles the boilerplate
  of generating a partial class (or similar) file for an instance of a marker attribute.

# Example Usage

_**todo**_

# Excluding the source files

To disable the inclusion of a specific source file,
define the relevant constant in the consuming project's `.csproj` file:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_SOURCETEXTGENERATORBASE</DefineConstants>
</PropertyGroup>
```

The files will still appear in the project, but will not add anything to the compilation.

### Note: Dependency
`SourceTextGeneratorBase.cs` depends on `AttributeContextAndData.cs` and `IndentingLineAppender.cs`
and will **not work** when either of them are excluded.

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>