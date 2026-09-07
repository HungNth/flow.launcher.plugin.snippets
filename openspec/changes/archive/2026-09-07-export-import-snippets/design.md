## Context

`SnippetStore` manages snippet persistence, serialization, and validation to `snippets.json`. The settings panel `SettingsControl` is a WPF `UserControl` hosted by Flow Launcher. Users currently lack a built-in way to export their snippets to a standalone file or import an existing file when switching machines.

See `proposal.md` for background and motivation.

## Goals / Non-Goals

**Goals:**
- Provide `Export(string destinationPath)` in [`SnippetStore`](file:///C:/Users/Hung/AppData/Roaming/FlowLauncher/Plugins/flow.launcher.plugin.snippets/Flow.Launcher.Plugin.Snippets/Storage/SnippetStore.cs) to write the current snippet collection as an indented, UTF-8 JSON `SnippetDocument`.
- Provide `Import(string sourcePath)` in [`SnippetStore`](file:///C:/Users/Hung/AppData/Roaming/FlowLauncher/Plugins/flow.launcher.plugin.snippets/Flow.Launcher.Plugin.Snippets/Storage/SnippetStore.cs) using a Replace All strategy: validate the incoming file using existing parsing logic, persist it atomically via `SaveCandidateList`, update in-memory state, and create a `.bak` backup.
- Add `Export JSON` and `Import JSON` buttons in [`SettingsControl.xaml`](file:///C:/Users/Hung/AppData/Roaming/FlowLauncher/Plugins/flow.launcher.plugin.snippets/Flow.Launcher.Plugin.Snippets/Views/SettingsControl.xaml) positioned alongside `Manage Snippets` but separated by a distinct visual margin.
- Use standard Windows dialogs (`SaveFileDialog` and `OpenFileDialog`) with informative error handling and confirmation prompts.

**Non-Goals:**
- Interactive conflict resolution or partial merging (pure Replace All restore).
- Cloud storage or automatic syncing across devices.
- Support for non-JSON import/export formats.

## Decisions

### Decision: Use standard WPF dialogs without external libraries
- **Choice:** Use `Microsoft.Win32.SaveFileDialog` and `Microsoft.Win32.OpenFileDialog`.
- **Rationale:** Native to .NET Desktop / WPF, requires no extra NuGet dependencies, and provides familiar Windows file picker UX.
- **Alternatives Considered:** Custom file picker UI (rejected: excessive complexity, reinventing native controls).

### Decision: Encapsulate export/import persistence in `SnippetStore`
- **Choice:** Implement `SnippetStore.Export(string destinationPath)` and `SnippetStore.Import(string sourcePath)`.
- **Rationale:** Keeps I/O, validation, thread synchronization (`_syncLock`), and atomic file writing inside `SnippetStore`, keeping `SettingsControl` responsible only for UI events and dialog orchestration.
- **Alternatives Considered:** Handling file I/O directly in `SettingsControl.xaml.cs` (rejected: breaks separation of concerns and bypasses lock/validation logic).

### Decision: Destructive replace confirmation in UI
- **Choice:** Prompt the user with a standard `MessageBox` confirmation dialog ("This will replace all your current snippets with X snippets from the file. Do you want to continue?") before performing the import.
- **Rationale:** Prevents accidental data loss while keeping the workflow simple and fast.
- **Alternatives Considered:** Silent overwrite (rejected: dangerous risk of data loss).

## Risks / Trade-offs

- **[Risk]** Destination file cannot be written (disk full, read-only permissions).
  - **→ Mitigation:** Write to a temporary file first before replacing/moving to the target destination; present descriptive error messages if writing fails.
- **[Risk]** User imports a corrupt or incompatible JSON file.
  - **→ Mitigation:** Validate through `SnippetStore.ValidateAndParseDocument` before applying mutations; reject corrupt files with clear error feedback and leave existing snippets intact.
- **[Risk]** User accidentally confirms replacement.
  - **→ Mitigation:** `SnippetStore.SaveCandidateList` automatically preserves the prior valid state in `snippets.json.bak`.
