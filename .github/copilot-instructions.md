# Internal Agent Instructions

Purpose: Helper source files (embedded) for .NET incremental generators; keep output deterministic and public API stable.

## 1. Scope
Chat = terse (summary + reason + minimal diff). Repo docs = rich (rationale, examples, edge cases, perf). Additive edits only.

## 2. Platform & Stability
netstandard2.0 / C# 7.3. No newer BCL APIs. Preserve public type & member names.

## 3. Guards
Each helper file:
```
#if !DATACUTE_EXCLUDE_<UPPERCASENAME>
// contents
#endif
```
Nest or document dependencies.

## 4. Editing & Diffs
Show only changed lines (≤3 lines context). Fenced blocks for multi‑line examples. Short reason (perf / determinism / clarity / bug fix). No change‑log comments. UK English.

## 5. Documentation
XML doc first sentence = summary; follow with behaviour / perf / pitfalls if useful. READMEs may include: Overview, Why, Key APIs, Examples, Performance, Dependencies, Exclusion Flags, Instrumentation. Document public surface only (internal/private only if cross‑file contract). Chat brevity does not apply to repo docs.

## 6. Chat Response Format
1. Summary (1–2 lines)  2. Diff (changed lines) OR fenced example  3. Optional next steps (label “Optional”).

## 7. Assumptions
If unspecified, state one reversible assumption and proceed.
