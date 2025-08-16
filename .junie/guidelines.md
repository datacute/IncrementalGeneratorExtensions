# Internal Agent Instructions

Purpose: Helper source files (embedded) for .NET incremental generators; keep output deterministic and public API stable.

## 1. Scope
Chat = terse (summary + reason + minimal sample). Repo docs = rich (rationale, examples, edge cases, perf). Additive = new files/members only; no deletions, renames, or moves unless explicitly requested.

## 2. Platform & Stability
netstandard2.0 / C# 7.3. No newer BCL APIs. Preserve public type & member names.

## 3. Guards
Each helper file can be excluded from the build using a guard.
Nest or document dependencies using guards, depending on how closely related the helper file is to the dependency.
Closely related examples:
```
#if !DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYEXTENSIONS // Feature: EquatableImmutableArrayExtensions
#if !DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAY // Dependency: EquatableImmutableArray
// contents
#endif // !DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAY
#endif // !DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYEXTENSIONS
```
Not closely related examples:
```
#if !DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA // Feature: AttributeContextAndData
#if DATACUTE_EXCLUDE_TYPECONTEXT
#error AttributeContextAndData requires TypeContext (remove DATACUTE_EXCLUDE_TYPECONTEXT or also exclude DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA)
#endif
#if DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAY
#error AttributeContextAndData requires EquatableImmutableArray (remove DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAY or also exclude DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA)
#endif
// contents
#endif // !DATACUTE_EXCLUDE_ATTRIBUTECONTEXTANDDATA
```

## 4. Editing & Samples
Show only example code snippets (no diffs) with ≤3 lines of surrounding context when needed. Fenced blocks for multi‑line examples. Use code samples to illustrate changes; do not use diff syntax in chat. Use language tags: csharp for C# code, xml for XML docs, and text for plain text. Short reason (perf / determinism / clarity / bug fix). Keep commentary in the chat. Code changes must omit commentary. Resulting code must look like it was always written that way.

When showing a change, prefer Before/After paired samples:
- Before: the minimal original snippet required for context.
- After: the revised snippet in full.

## 5. Public API Doc Comments
For every public type or member emit an XML doc summary (no placeholders). Include <param>/<typeparam>/<returns> when present; only add <remarks> if non-trivial behaviour (threading, allocation, ordering, invariants).

## 6. Documentation
UK English. XML doc first sentence = summary; follow with behaviour / perf / pitfalls if useful. READMEs may include: Overview, Why, Key APIs, Examples, Performance, Dependencies, Exclusion Flags, Instrumentation. Document public surface only (internal/private only if cross‑file contract). Chat brevity does not apply to repo docs.

## 7. Chat Response Format
1. Summary (1–2 lines)
2. Example code snippet(s) only (no diff format)
3. Optional next steps (label “Optional”).

## 8. Assumptions
If unspecified, state one reversible assumption in the chat and proceed.
