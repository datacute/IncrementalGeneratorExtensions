<small>Back to Incremental Generator Extensions [README](../README.md)</small>

---
# AttributeContextAndData.cs, TypeContext.cs and AttributeContextAndDataExtensions.cs
***todo - expand on copied points below***

- Adds types and extension methods to simplify collecting data about each use of a marker attribute.
- `TypeContext` captures the type information.
- `AttributeContextAndData` captures the attribute data, which includes the `TypeContext` of the type marked by
  the attribute, and the `TypeContext` of each of the containing types.
- `AttributeContextAndData` has a generic type argument which is your type that holds
  information collected for the attribute, such as its positional and named arguments.

# Example Usage

_**todo**_

# Excluding the source files

To disable the inclusion of a specific source file,
define the relevant constant in the consuming project's `.csproj` file:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA</DefineConstants>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATAEXTENSIONS</DefineConstants>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_TYPECONTEXT</DefineConstants>
</PropertyGroup>
```

The files will still appear in the project, but will not add anything to the compilation.

### Note: Dependency
`AttributeContextAndDataExtensions.cs` depends on `AttributeContextAndData.cs` which in turn depends on `TypeContext.cs`

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>