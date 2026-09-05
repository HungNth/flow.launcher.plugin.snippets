## Context

The repository contains OpenSpec and tooling configuration but no plugin source, tests, manifest, or established application architecture. The retired Snippets 2.3.8 installation demonstrates the relevant Flow Launcher integration points, but this change is a new plugin with a new stable ID and no migration contract. See `proposal.md` for motivation and `specs/snippets/spec.md` for observable behavior.

The plugin is Windows-only because Flow Launcher loads C# plugins in-process and settings UI is provided through WPF. Queries must remain fast while the user types, and user-authored JSON must never be silently replaced after a parse, validation, or write failure.

## Goals / Non-Goals

**Goals:**

- Keep search fully in memory and deterministic.
- Keep the persisted model small, versioned, readable, and safe to replace.
- Make every accepted management operation immediately visible to queries.
- Use Flow Launcher APIs and the .NET standard library instead of additional runtime dependencies.
- Keep the code structure small enough to understand without a repository layer, dependency injection container, or UI framework.

**Non-Goals:**

- Automatic synchronization with a file edited while the management window is open.
- Continuous file watching or per-query disk reads.
- Multiple storage backends or a storage abstraction intended for future backends.
- Usage analytics, automatic score mutation, cloud sync, snippet expansion, or keyboard simulation.
- Compatibility or data migration from the removed plugin installation.
- A separate settings template; C# plugins integrate settings through `ISettingProvider`.

## Decisions

### Use a native C# plugin with synchronous in-memory queries

The entry class will implement `IPlugin`, `ISettingProvider`, and `IReloadable`. Initialization and reload read the JSON file, while `Query` only reads an in-memory snapshot and calls Flow Launcher's fuzzy matcher. A synchronous plugin avoids async state-machine overhead where the query path performs no I/O or long-running work.

Implementation will reference the current `Flow.Launcher.Plugin` package version and use the compatible Windows target framework reported by that package at implementation time. The manifest will use a newly generated stable plugin ID, `Language: "csharp"`, action keyword `sp`, and an execute filename matching the assembly.

Alternative considered: `IAsyncPlugin`. It is appropriate for network or query-time file I/O, neither of which exists here, so it would add machinery without improving responsiveness.

### Use one versioned JSON document with explicit snippet objects

The data file will be `<PluginSettingsDirectoryPath>/snippets.json`:

```json
{
  "version": 1,
  "snippets": [
    {
      "key": "git-commit",
      "value": "git add -A && git commit -m \"message\"",
      "score": 100
    }
  ]
}
```

The implementation will use `System.Text.Json`, camel-case property names, UTF-8, and indented output. `version` provides an explicit compatibility boundary. `score` is a signed integer used only for comparison, so no arithmetic or score normalization is required. Before serialization, snippets will be ordered by score descending and key ascending to keep file output deterministic; array order is not part of the behavior contract.

Alternative considered: a JSON object keyed by snippet key. That shape resembles VS Code but makes renaming a key a structural property change and gives the management UI a different model from the persisted data. An array keeps `key`, `value`, and `score` as one explicit record and binds directly to a grid.

Alternative considered: Flow's generic settings JSON storage. A dedicated file is preferred because the snippet library is user data with a defined filename, version, backup, and atomic replacement contract rather than plugin preferences.

### Keep one store and publish immutable snapshots

A single `SnippetStore` will own load, validation, mutation, reload, and persistence. There will be no storage interface because only one backend exists.

The active library will be exposed to queries as an array of immutable snippet records. A query reads the current array reference and never observes objects being edited in place. Load and mutation operations build a complete candidate array; the store publishes that array only after validation and, for mutations, successful persistence. A small lock will serialize store load/save operations, while queries remain lock-free against the published snapshot.

Validation will:

- trim keys before storage;
- preserve values exactly;
- reject empty keys and values;
- reject duplicate keys with `StringComparer.OrdinalIgnoreCase`;
- require the supported document version and integer scores.

A missing file produces an empty snapshot. A malformed, unsupported, or invalid file records an actionable load error without overwriting the file. Failed reloads retain the prior valid snapshot.

Alternative considered: mutating an `ObservableCollection` shared by queries and WPF. That creates cross-thread enumeration and partial-update risks. Publishing a replacement array keeps the query-side contract simpler.

### Replace files atomically and keep one backup

For an accepted mutation, the store will:

1. Build and validate the complete candidate document.
2. Serialize it to a temporary file in the same directory as `snippets.json`.
3. Flush and close the temporary file.
4. Replace the existing file while writing the previous file to `snippets.json.bak`, or move the temporary file into place when no data file exists.
5. Publish the candidate in-memory snapshot only after the file operation succeeds.

If any step fails, the previous active snapshot remains published and the temporary file is removed on a best-effort basis. The management UI reports the failure. One backup generation is sufficient for interrupted writes and accidental recent replacement without introducing backup rotation policy.

Alternative considered: serializing directly to `snippets.json`. It is shorter but can leave truncated JSON after a process or machine interruption, violating the data-preservation requirement.

### Rank first, then assign Flow result scores

For an empty search, candidates are ordered by manual score descending and key ascending. For a non-empty search, each key is evaluated by `IPublicAPI.FuzzySearch`; successful candidates are ordered by fuzzy score descending, manual score descending, and key ascending.

The plugin will not add fuzzy and manual scores together because their scales have different meanings and could cause manual priority to overpower a substantially better match. After ordering, results receive descending Flow `Result.Score` values based on final position so Flow preserves the plugin's tuple ordering. Fuzzy match indexes will be supplied as title highlight data when available.

Each actionable result will set:

- `Title` to the key;
- `SubTitle` to a single-line value preview;
- `CopyText` to the exact value;
- `AutoCompleteText` to `sp <key>`;
- an action that copies the exact value through Flow's clipboard API and returns `true` only on success.

No-match and load-error entries will be non-actionable results.

Alternative considered: searching values as well as keys. Values may be long and produce noisy matches; the confirmed product model treats the key as the lookup name.

### Use a small WPF management surface without an additional UI framework

`CreateSettingPanel` will return a lightweight WPF control containing the `Manage Snippets` button. The button opens one management window that uses Flow-compatible dynamic theme resources.

The window will contain:

- a key filter;
- a read-only grid showing key, single-line value preview, and score;
- add and edit forms with an editable key, multiline value, and integer score;
- delete with explicit confirmation.

Code-behind will coordinate the form and the shared `SnippetStore`. Each successful add, edit, rename, or delete calls one store mutation and refreshes the displayed snapshot. Invalid input is reported before persistence. This UI is small and event-driven; adding a view-model framework or dependency injection container would increase structure without isolating meaningful complexity.

### Reload only through explicit lifecycle actions

Initialization loads `snippets.json` once. The settings window reads the current store snapshot when opened, and successful UI mutations publish immediately. `IReloadable.ReloadData` handles deliberate external JSON edits through Flow Launcher's Reload Plugin Data command.

No file watcher will run. This avoids lifetime, debounce, partial-write, and cross-thread UI concerns for a workflow where direct file editing is secondary. A user who edits the file externally reloads plugin data before continuing management in the UI.

### Surface failures without destroying usable state

Initialization, reload, save, and clipboard failures will be logged through Flow's API. Save validation errors remain in the management window. Clipboard failure keeps Flow open and displays an error. A failed initial load yields an actionable, non-copyable error result; a failed later reload keeps the last valid query data and reports the reload error.

Error messages will include the relevant file path and a concise cause but will not include snippet values, which may contain sensitive text.

## Risks / Trade-offs

- **Linear fuzzy search is O(n) per query** -> Keep the library in memory and avoid indexing until measured query latency shows a real need; the intended local snippet collection is small.
- **A single backup protects only the immediately previous file** -> Keep one deterministic `.bak` file for the requested safety boundary; add history only if users demonstrate a recovery need.
- **External edits can race with an open management window** -> Treat the UI as the primary writer, document explicit reload behavior, and ensure atomic replacement preserves the previous disk file as the backup.
- **Flow theme resources or settings hosting can differ across Flow versions** -> Build against the current package and verify the actual settings panel and management window in the installed Flow Launcher.
- **The Windows clipboard can be temporarily unavailable** -> Report failure and keep Flow open rather than claiming success or adding retry policy without evidence.
- **Returning every snippet for an empty query scales linearly** -> Preserve complete, predictable listing initially; introduce a limit only if a real library size makes Flow interaction slow.

## Migration Plan

This is a greenfield plugin with a new stable ID and no data migration. Deployment consists of publishing the plugin, installing the published directory under Flow Launcher's plugins directory, removing any conflicting retired plugin, restarting Flow Launcher, and exercising `sp` plus the settings manager.

Rollback consists of removing the new plugin directory and restarting Flow Launcher. The plugin settings directory, `snippets.json`, and `snippets.json.bak` can remain untouched so data is available for reinstall or manual recovery.
