## 1. Plugin Foundation

- [x] 1.1 Create the C# solution, plugin project, and focused MSTest project using the current stable `Flow.Launcher.Plugin` package and its compatible Windows target framework; verify `dotnet restore` and an empty `dotnet build` succeed.
- [x] 1.2 Add `plugin.json`, a newly generated stable plugin ID, action keyword `sp`, plugin icon resources, and project content-copy settings; verify a Debug win-x64 publish contains the matching plugin DLL, manifest, and icon at the manifest paths.

## 2. JSON Store

- [x] 2.1 Add focused storage tests for a missing file, valid version-1 JSON, exact multiline preservation, unsupported versions, invalid fields, and case-insensitive duplicate keys; verify the new tests initially exercise every specified load and validation outcome.
- [x] 2.2 Implement the immutable snippet/document models plus `SnippetStore` loading and validation with `System.Text.Json`; verify the storage tests pass and invalid files remain unchanged.
- [x] 2.3 Add focused persistence tests for deterministic output, immediate snapshot publication after success, `snippets.json.bak` creation, and retaining the previous snapshot when a write fails; verify the tests exercise real temporary-directory file operations.
- [x] 2.4 Implement serialized store mutations and same-directory atomic file replacement without a storage interface or database dependency; verify all persistence tests pass and the generated JSON contains only `version`, `snippets`, `key`, `value`, and `score`.

## 3. Flow Query and Clipboard Behavior

- [x] 3.1 Add focused tests for empty-query score ordering, fuzzy-result tuple ordering with controlled match scores, key-only filtering, stable key tie-breaking, multiline preview formatting, and non-actionable no-match results; verify each test would fail if its observable ordering or filtering rule were reversed.
- [x] 3.2 Implement the Flow entry class with `IPlugin`, `IReloadable`, key fuzzy matching, deterministic result scoring, title highlighting, autocomplete, and exact clipboard actions; verify the query tests pass and the plugin project builds without query-time file I/O.
- [x] 3.3 Implement actionable load, reload, save, and clipboard error reporting without including snippet values in logs; verify malformed reload keeps the prior snapshot and clipboard failure leaves the result action open in focused tests or a small executable smoke harness.

## 4. Settings Management UI

- [x] 4.1 Add the `ISettingProvider` settings control with a `Manage Snippets` button and a Flow-themed WPF management window containing key filtering, the snippet grid, and add/edit fields; verify the XAML compiles and the window opens from the installed plugin settings panel.
- [x] 4.2 Wire add, edit, rename, cancel, and confirmed delete operations directly to `SnippetStore`, including inline validation and default score `0`; verify each successful operation persists immediately and appears in a subsequent `sp` query without restarting Flow Launcher.
- [x] 4.3 Verify management failure paths in the installed plugin: duplicate keys are rejected, cancelled edits/deletes do not change the file, and a forced save failure leaves both the displayed library and prior JSON usable.

## 5. Integration and Review

- [x] 5.1 Run the focused test suite, Release build, and Debug win-x64 publish; verify all commands pass and perform manifest sanity checks for required fields, semantic version, `Language: "csharp"`, matching execute filename, and present icon.
- [x] 5.2 Install the publish output into Flow Launcher, restart Flow, and smoke-test `sp`, fuzzy key-only search, score ordering, no-match state, exact multiline clipboard copy, settings CRUD, external-file reload, malformed-file preservation, and `.bak` recovery; verify Flow logs contain no plugin load or unhandled runtime errors.
- [x] 5.3 Review the implementation against `proposal.md`, `specs/snippets/spec.md`, and `design.md`, resolve correctness or maintainability findings, then repeat the affected tests, build, publish checks, and Flow smoke scenarios.
