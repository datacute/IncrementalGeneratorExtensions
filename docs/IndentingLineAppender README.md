<small>Back to Incremental Generator Extensions [README](../README.md)</small>

---
# IndentingLineAppender.cs and TabIndentingLineAppender.cs
***todo - expand on copied points below***

- Provides a customisable `IndentingLineAppender` class that wraps a `StringBuilder` and adds
  auto-indentation support, making it easier to generate indented source code.
- Includes a `TabIndentingLineAppender` customisation that uses tabs for indentation.


# Example Usage

_**todo**_

# Excluding the source files

To disable the inclusion of a specific source file,
define the relevant constant in the consuming project's `.csproj` file:

```XML
<PropertyGroup>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_INDENTINGLINEAPPENDER</DefineConstants>
  <DefineConstants>$(DefineConstants);DATACUTE_EXCLUDE_TABINDENTINGLINEAPPENDER</DefineConstants>
</PropertyGroup>
```

The files will still appear in the project, but will not add anything to the compilation.

### Note: Dependency
`TabIndentingLineAppender.cs` depends on `IndentingLineAppender.cs` and will **not work** when it
is excluded.

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>