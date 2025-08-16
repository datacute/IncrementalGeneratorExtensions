<small>Back to Incremental Generator Extensions [README](../README.md)</small>

---
# IndentingLineAppender.cs and TabIndentingLineAppender.cs
`IndentingLineAppender` is a focused helper for generating structured / indented source without littering your code with manual space concatenations. It wraps one `StringBuilder`, maintains an `IndentLevel`, and offers:
* Block helpers: `AppendStartBlock()` / `AppendEndBlock()` (writes delimiters and adjusts indent).
* Multi-line helpers: `AppendLines()` and `AppendFormatLines()` (each line indented, blank lines preserved).
* Fluent chaining so familiar `StringBuilder` patterns are usable.
* The `TabIndentingLineAppender` subclass is identical except it uses a single tab character per indentation level.


## Example Usage
```csharp
var a = new IndentingLineAppender();
a.AppendLine("namespace Demo")
 .AppendStartBlock()           // {
 .AppendLine("internal static class C")
 .AppendStartBlock()           // {
 .AppendLine("public static void M() {}");
a.AppendEndBlock()             // }
 .AppendEndBlock();            // }

string code = a.ToString();

// Using tabs (identical API, different indentation style):
var tabs = new TabIndentingLineAppender();
tabs.AppendLine("class T")
  .AppendStartBlock()
  .AppendLine("int x;")
  .AppendEndBlock();
```

Result (`code` variable) roughly:
```text
namespace Demo
{
  internal static class C
  {
    public static void M() {}
  }
}
```

## Key APIs
* `AppendLine(string)` – append one line with current indent.
* `AppendStartBlock()` / `AppendEndBlock()` – write block delimiters and adjust `IndentLevel` automatically.
* `AppendLines(string)` – append multi-line content (each non-empty line prefixed with current indent).
* `AppendFormatLines(format, params object[])` – format then treat the result as multi-line.
* `IndentLevel` (get/set) – manual control (normally let block helpers change it).
* `Clear()` – reuse the same instance for multiple emission passes.
* `Direct` – access the underlying `StringBuilder` for an uncommon operation.

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
Direct:
* `TabIndentingLineAppender.cs` -> `IndentingLineAppender.cs`

Behavior:
* `TabIndentingLineAppender.cs` emits a fail‑fast `#error` if the base is excluded.

---
<small>
<small>
<small>
Datacute - Acute Information Revelation Tools
</small>
</small>
</small>