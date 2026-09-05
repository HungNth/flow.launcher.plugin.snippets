---
name: openspec-agy-delivery
description: Deliver an explicitly approved OpenSpec change by having OMP coordinate an Antigravity CLI worker through Herdr, then independently verify, remediate, synchronize specs, and archive. Use only when the current agent is OMP and the user asks to implement, apply, ship, or deliver an approved OpenSpec change through AGY or Antigravity. Never use inside an AGY implementation worker, during explore or proposal work, before separate post-planning approval, or for small tasks the user wants OMP to implement directly.
compatibility: Requires OMP running inside Herdr with the herdr, agy, openspec, and git CLIs installed; AGY must be authenticated. Supports native Windows, macOS, and Linux hosts supported by those tools.
---

# OpenSpec AGY Delivery

Deliver one approved OpenSpec change end to end. OpenSpec artifacts define the work; AGY implements it; Herdr owns terminal and agent lifecycle; OMP owns judgment, verification, remediation, spec synchronization, and archive.

## Agent-role preflight

This is an OMP orchestration workflow, never an AGY implementation workflow. Before any OpenSpec command or Herdr mutation, confirm that the current runtime identifies this agent as OMP and that `HERDR_ENV` is `1`. Skill availability, repository location, or an implementation request does not prove the OMP role.

If the current agent is AGY or cannot confirm that it is OMP, stop without splitting a pane, starting or prompting an agent, or running the delivery lifecycle. Reply with the exact literal skill name `openspec-apply-change` as the required workflow for the named change; do not replace it with a path, link label, or generic `SKILL.md`. State that `openspec-agy-delivery` is OMP-owned.

## Approval boundary

Planning authorization never authorizes implementation. Start this workflow only after the planning workflow has stopped and the user sends a separate request to deliver a named approved change.

That delivery request authorizes:

- implementation of the approved tasks
- up to three bounded remediation rounds
- the recommended spec synchronization
- archive after every blocking gate passes

It never authorizes credentials, destructive operations, permission escalation, deployment, publishing, requirement decisions, or scope expansion. Stop and ask the user for any of those.

## Cross-platform command rule

Run each CLI operation separately through the host command tool and parse returned JSON directly. Treat command examples as argument sequences, not shell programs. Use host-native quoting and environment access; do not require a POSIX shell, inline shell functions, pipelines, heredocs, or command substitution. A host-native multiline string literal is acceptable only when it is passed as one CLI argument rather than executed as a shell program.

Use resolved absolute paths when a command accepts a cwd. Never infer OpenSpec locations from conventional directory names.

## Ownership

- **OpenSpec:** proposal, specs, design, implementation tasks, schema, roots, and action context.
- **AGY:** scoped implementation, task completion markers, and implementation-time checks.
- **Herdr:** pane layout, process launch, agent identity, lifecycle state, and transcript access.
- **OMP:** preflight, worker brief, diff review, `/opsx-verify`, fresh project checks, remediation decisions, and archive.

AGY must not create another plan, redesign the change, commit, synchronize specs, or archive.

## 1. Resolve the change and root

If the user names a standalone store, resolve it before listing or selecting changes:

1. Run `openspec store list --json` and resolve its registered id.
2. Add `--store <id>` to every applicable OpenSpec command for the rest of this delivery.
3. Include the selected store id in the AGY brief and every verification, remediation, and archive handoff.

Then resolve exactly one change inside the selected planning root. If ambiguous, run `openspec list --json [--store <id>]` and ask the user to choose from that result.

Announce the selected change and store, if any.

Run:

```text
openspec status --change <change> --json [--store <id>]
openspec instructions apply --change <change> --json [--store <id>]
openspec validate <change> --type change --strict --no-interactive [--store <id>]
```

Use the returned `schemaName`, `planningHome`, `changeRoot`, `actionContext`, apply `state`, `contextFiles`, task list, and dynamic instruction as authoritative.

- `blocked`: stop before starting AGY and report the missing planning prerequisite.
- `all_done`: skip AGY implementation and proceed to independent verification.
- `ready`: read every path under `contextFiles`, then continue.

Apply relevant runtime `context` and compatible `operationGuidance`. They do not override explicit user choices, CLI state, resolved paths, or built-in workflow contracts.

## 2. Check prerequisites and protect user work

Confirm all of the following before delegation:

1. The agent-role preflight confirmed OMP, and OMP is inside Herdr with `HERDR_ENV=1`.
2. `herdr --help`, `agy help`, `agy models`, `openspec --help`, and `git --version` succeed.
3. AGY is authenticated. Resolve the repository root with `git rev-parse --show-toplevel` and keep the returned absolute path for every cwd-sensitive Herdr operation.
4. The selected change is valid and apply state is `ready` or `all_done`; start AGY only for `ready`.
5. The working tree can be attributed safely.

These are the required runtime prerequisites: Herdr, AGY, OpenSpec, git, and AGY authentication. Supplemental TDD, debugging, review, verification, or worktree skills may be used when installed and applicable, but their absence never blocks delivery; follow the explicit checks in this workflow instead.

Inspect tracked, staged, and untracked changes. On a fresh delivery, pre-existing changes may remain only inside the selected `changeRoot`. If unrelated paths are already dirty, stop and list them; never clean, reset, stash, switch branches, or overwrite user work.

When explicitly continuing a partial delivery, treat existing implementation edits as baseline only when repository evidence ties them to the same selected change. If attribution is uncertain, stop.

Discover the project's exact lint, typecheck, test, build, and behavioral smoke commands from root `AGENTS.md`, repository instructions, documentation, and manifests. Do not ask AGY to guess them.

## 3. Start a dedicated AGY worker through Herdr

Follow the installed `herdr` skill. Inspect the current pane and layout:

```text
herdr pane current --current
herdr pane layout --current
herdr agent list
```

Prefer a new worker for each delivery. Derive a valid name matching Herdr's agent name format `[a-z][a-z0-9_-]{0,31}` (lowercase alphanumeric, hyphen, underscore, up to 32 characters, e.g. `agy-<change>`), and add a short suffix on collision. Reuse a worker only when it was created earlier in this same delivery; remediation must keep that conversation.

Choose the split direction from the current geometry: split a wide pane right, otherwise split down. Preserve the repository root and user focus:

```text
herdr pane split --current --direction <right|down> --cwd <repository-root> --no-focus
```

Read the new pane id from `.result.pane.pane_id`, then start AGY with explicit model and reasoning effort passed after `--` (Herdr passes native agent arguments only after `--`):

```text
herdr agent start <worker-name> --kind agy --pane <pane-id> -- --model gemini-3.8-flash-high --effort high
```

If the user requested a specific model or provider, substitute that model from `agy models`. Otherwise, default to `gemini-3.8-flash-high` with `--effort high`.

Use the existing repository working tree for the default single-worker path after the attribution preflight passes. Do not create another workspace, tab, worktree, or cwd unless the user explicitly requested that topology or an approved concurrent-worker topology requires it.

## 4. Send a self-contained implementation brief

AGY has a separate conversation. Include every load-bearing fact in one compact brief:

```xml
<role>
You are the AGY implementation worker, not the OMP delivery orchestrator.
Use the workflow named exactly openspec-apply-change for the named change; do not describe it only as SKILL.md.
Never invoke openspec-agy-delivery or create another AGY worker.
</role>

<task>
Implement the approved OpenSpec change: <change>.
Use the selected store: <store-id or none>.
Read root AGENTS.md for shared project policy.
Run OpenSpec status and apply instructions for that exact change and store.
Read every path returned in contextFiles and follow the dynamic apply instruction.
Implement every remaining task until all are complete or you are blocked.
</task>

<scope>
Treat the approved proposal, specs, design, tasks, and action context as authoritative.
Do not create a second plan, broaden scope, or perform unrelated cleanup.
Update only completion markers required by the apply workflow.
Do not commit, synchronize specs, or archive; OMP owns those actions.
</scope>

<verification_loop>
Run and fix failures from these exact project commands before finishing:
<commands discovered by OMP>
Confirm the working tree contains only intended delivery changes.
</verification_loop>

<decision_safety>
Stop and report any credential, destructive action, permission escalation,
deployment, publishing, requirement decision, design conflict, or scope expansion.
Do not guess or auto-approve it.
</decision_safety>

<report>
End with: work completed, files touched, task progress, exact check outcomes,
deviations, blockers, and remaining concerns.
</report>
```

Pass the brief as one argument through the host command tool:

```text
herdr agent prompt <worker-name> <brief> --wait --timeout 3600000
```

The default wait already accepts `idle`, `done`, or `blocked`; do not repeat those states unless a narrower wait is intentional.

## 5. Handle the settled worker state

Read `.result.agent.status` from the prompt response.

- `blocked`: inspect `herdr agent get` and `herdr agent read`. Answer only safe mechanical prompts. Escalate protected decisions to the user.
- `idle` or `done`: collect the report and continue to verification.
- `unknown`: not completion evidence; inspect the agent and wait for a known state.
- command error or timeout: inspect the agent, terminal output, and working tree before deciding whether to retry.

Read the transcript with:

```text
herdr agent read <worker-name> --source recent-unwrapped --lines 200
```

If a complete report is unavailable, ask the idle worker to write its full report to a temporary Markdown file and reply only with that path, then read the file directly.

## 6. Verify independently

AGY's report is a claim, never completion evidence.

1. Inspect the complete working tree, including staged and untracked files. Review edits to existing tests before trusting any gate; weakened assertions, skips, or disabled tests are blocking findings.
2. Compare implementation and task markers against every approved context file.
3. Invoke the installed `openspec-verify-change` workflow for the same change and selected store. In OMP, use `/opsx-verify <change>`.
4. Run the canonical project lint, typecheck, test, and build commands again from OMP.
5. Exercise the changed behavior on its real surface with the smallest relevant smoke scenario.

A CRITICAL OpenSpec finding, incomplete required task, weakened test, failed project command, failed smoke scenario, or unresolved protected decision blocks archive. Report warnings and suggestions; block on them only when they expose a required behavior or gate failure.

## 7. Remediate failures

Send concrete delta findings to the same AGY worker conversation:

```text
herdr agent prompt <worker-name> <delta-findings> --wait --timeout 3600000
```

The delta prompt must identify exact files, failing commands, OpenSpec findings, and required outcomes. It must prohibit redesign and scope expansion.

After every remediation:

1. inspect the complete diff again
2. rerun `/opsx-verify` for the same change and store
3. rerun all failed and required project gates
4. rerun the behavioral smoke scenario

Stop after three failed remediation rounds or earlier when the same blocker repeats without progress. Preserve the working tree and report the remaining findings; do not archive.

## 8. Synchronize and archive

Archive only when:

- AGY has settled successfully or implementation was already complete
- every required task is complete
- `/opsx-verify` has no CRITICAL finding
- fresh project verification and smoke checks pass
- no protected decision remains

Then follow the installed `openspec-archive-change` workflow for the exact change and selected store. Use its recommended spec synchronization when delta specs require it. OMP performs synchronization and archive; AGY never does.

Do not commit unless the user separately requests a commit.

After archive or upon terminating the delivery, optionally close the worker pane (`herdr pane close <pane-id>`) to free resources, unless the user wants to retain terminal output for inspection.

## 9. Report the outcome

On success, report:

- change, schema, store, and AGY worker state
- major files or components changed
- remediation rounds used
- fresh commands and observed results
- OpenSpec verification result
- spec synchronization and archive result

On a blocked or failed delivery, report the exact blocker, preserved working-tree state, why archive was skipped, and one concrete action required from the user.
