# AGENTS.md

- Do not preserve backward compatibility. Remove obsolete paths instead of adding compatibility layers, fallbacks, or migrations.
- Choose the simplest implementation that fully meets the current requirements. Avoid speculative abstractions, configuration, and indirection .
- Grow the system in layers. Start from the smallest version that works end to end, and add each new capability on top of a product that already works. Never trade a working product for unfinished complexity.
- Keep components modular and concerns clearly separated.
- Prefer established, well-maintained libraries when they reduce overall complexity or improve reliability. Do not reimplement common functionality without a clear reason.
- Lean on the dependencies already in the project before writing your own implementation or adding packages. Do not assume a library lacks a capability without checking its documentation and types.
- Make architectural decisions for the long term. Do not accept a stopgap that only works for now and is meant to be replaced later.
- Study how established products solve the problem before designing a solution. Adopt their proven patterns and conventions rather than inventing an approach from scratch.

## Output style

The reader has ADHD. Shape every response so it can be acted on:

1. Lead with the answer or next action: command, path, or snippet first.
2. Number multi-step work; one bounded action per step.
3. End with one next action doable in under two minutes.
4. Finish the current issue before raising a new one.
5. Restate progress each turn ("step 3 of 5 done").
6. Give time estimates in concrete units, never "a bit".
7. After a change, show what now works.
8. Errors: state location, cause, and fix. No drama.
9. Cap lists at 5 items.
10. No preamble, no recaps, no closers.

Exceptions: explain fully when asked to explain. Confirm before destructive actions. After three failed fixes, stop and name the doubtful assumption. If the request is ambiguous, ask one short question.

## Spec-Driven Development

OpenSpec is the single source of truth for change planning and lifecycle.
`AGENTS.md` remains the authoritative source for project-wide engineering
principles and conventions.

### Change Planning

For changes managed under `openspec/changes/`:

- Use `/opsx:explore` when the problem, scope, constraints, or solution
  approach is not yet clear. Skip it when the change is already well understood.
- Use OpenSpec artifacts as the only source of truth for the change's
  proposal, requirements, design, and implementation tasks.
- Use `/opsx:propose` as the default workflow for normal changes.
- Use `/opsx:new` and `/opsx:continue` when a complex, risky, or architectural
  change requires reviewing individual artifacts before implementation.
- Do not create separate spec, design, or implementation-plan documents
  outside OpenSpec for the same change.
- Do not begin implementation until the OpenSpec artifacts have been reviewed
  and approved.

### Implementation

During implementation:

- Treat the current OpenSpec specs and design as authoritative.
- Use `superpowers:test-driven-development` for behavior changes when applicable.
- Use `superpowers:systematic-debugging` for bugs, failing tests, build failures,
  or unexpected behavior.
- If implementation reveals that a spec, design decision, or task is incorrect
  or incomplete, update the OpenSpec artifacts before continuing.
- Do not silently introduce new scope, unrelated refactoring, speculative
  abstractions, or infrastructure. Update the change artifacts first if the
  additional work is genuinely required.
- Do not mark an implementation task complete until its required verification
  has passed with fresh evidence.

### Review and Completion

After implementation:

- Use `superpowers:requesting-code-review` for significant changes and before
  integration.
- Use `superpowers:receiving-code-review` when acting on review feedback.
- Use `superpowers:verification-before-completion` before claiming the change
  is complete.
- Run the project's required tests, linting, type checking, build, and other
  applicable verification.
- Run `/opsx:verify` to verify implementation against the OpenSpec artifacts.
- Resolve any meaningful drift between specs, design, tasks, and implementation
  before integration.

A change is complete only when both fresh project verification and OpenSpec
verification pass.

Archive the OpenSpec change only after the implementation has been accepted
and no known spec/design/implementation drift remains.

<!-- gitnexus:start -->
# GitNexus — Code Intelligence

This project is indexed by GitNexus as **flow.launcher.plugin.snippets** (10 symbols, 7 relationships, 0 execution flows).

> Index stale? Run `node .gitnexus/run.cjs analyze --index-only` from the project root — it auto-selects an available runner. No `.gitnexus/run.cjs` yet? Bootstrap with `npx`, `bunx`, or `pnpm dlx` — e.g. `bunx gitnexus@latest analyze` (npm 11 npx crash; #1939).

## Always Do

- **MUST run impact analysis before editing.** Use `impact({target: "symbolName", direction: "upstream"})` (MCP) or `node .gitnexus/run.cjs impact "symbolName" --direction upstream --repo .` (CLI fallback); report callers, processes, and risk. Never substitute grep for graph analysis.
- **MUST analyze graph changes before committing.** Use `detect_changes({scope: "all"})` (MCP) or `node .gitnexus/run.cjs detect-changes --scope all --repo .` (CLI fallback). `partial: true` or `truncated: true` is not a clean check — a zero means unseen, not unaffected; re-run it. For regression review: `detect_changes({scope: "compare", base_ref: "main"})` or `node .gitnexus/run.cjs detect-changes --scope compare --base-ref "main" --repo .`.
- **MUST warn the user** if impact analysis returns HIGH or CRITICAL risk before proceeding with edits.
- **MUST treat `risk: UNKNOWN` as unresolved, not as low.** An empty caller set is not evidence the symbol is unused — it can also mean the callers are not resolvable by the index (plain-object property access, dynamic dispatch, cross-language calls). `impact` pairs `UNKNOWN` with a `riskNote` saying so. Confirm with a text search before treating the symbol as safe to change or delete; do not proceed on the strength of a zero.
- When exploring unfamiliar code, use `query({search_query: "concept"})` to find execution flows instead of grepping. It returns process-grouped results ranked by relevance.
- When you need full context on a specific symbol — callers, callees, which execution flows it participates in — use `context({name: "symbolName"})`.
- For security review, `explain({target: "fileOrSymbol"})` lists taint findings (source→sink flows; needs `analyze --pdg`).

## Never Do

- NEVER edit a function, class, or method before MCP/CLI impact analysis.
- NEVER ignore HIGH or CRITICAL risk warnings from impact analysis, and never read `UNKNOWN` as an all-clear — it means the walk could not answer, which is the one verdict that requires confirming by other means.
- NEVER rename symbols with find-and-replace — use `rename` which understands the call graph.
- NEVER commit before MCP/CLI graph change analysis.

## Resources

| Resource | Use for |
| --- | --- |
| `gitnexus://repo/flow.launcher.plugin.snippets/context` | Codebase overview, check index freshness |
| `gitnexus://repo/flow.launcher.plugin.snippets/clusters` | All functional areas |
| `gitnexus://repo/flow.launcher.plugin.snippets/processes` | All execution flows |
| `gitnexus://repo/flow.launcher.plugin.snippets/process/{name}` | Step-by-step execution trace |

## CLI

| Task | Read this skill file |
| --- | --- |
| Understand architecture / "How does X work?" | `.claude/skills/gitnexus-exploring/SKILL.md` |
| Blast radius / "What breaks if I change X?" | `.claude/skills/gitnexus-impact-analysis/SKILL.md` |
| Trace bugs / "Why is X failing?" | `.claude/skills/gitnexus-debugging/SKILL.md` |
| Rename / extract / split / refactor | `.claude/skills/gitnexus-refactoring/SKILL.md` |
| Tools, resources, schema reference | `.claude/skills/gitnexus-guide/SKILL.md` |
| Index, status, clean, wiki CLI commands | `.claude/skills/gitnexus-cli/SKILL.md` |

<!-- gitnexus:end -->
