## Why

Users currently have no integrated mechanism within Flow Launcher to export or import their snippets when migrating between computers or creating backups. Providing dedicated Export and Import actions ensures seamless snippet backup and restoration without requiring users to manually hunt for `%APPDATA%` paths.

## What Changes

- Add an **Export JSON** action in the plugin settings panel that prompts the user for a destination file and exports the entire snippet library in the standard `SnippetDocument` format.
- Add an **Import JSON** action in the plugin settings panel that prompts the user to select a JSON file, validates its structure, requests confirmation to replace all existing snippets, and restores the library.
- Group the settings actions cleanly: "Manage Snippets" in one group, and "Export JSON" / "Import JSON" in a separate backup group with a distinct visual gap.
- Update `SnippetStore` with atomic file export and replacement-based import capabilities.

### Non-Goals
- Cloud synchronization or automatic periodic backups.
- Conflict-based merging or granular snippet selection during import (Replace All is used).
- Exporting to non-JSON formats (CSV, XML, YAML).

## Capabilities

### New Capabilities
<!-- None -->

### Modified Capabilities
- `snippets`: Add requirements for exporting snippet documents to external JSON files and importing external JSON snippet documents via Replace All strategy.

## Impact

- **Storage**: [`SnippetStore`](file:///C:/Users/Hung/AppData/Roaming/FlowLauncher/Plugins/flow.launcher.plugin.snippets/Flow.Launcher.Plugin.Snippets/Storage/SnippetStore.cs) adds `Export(string destinationPath)` and `Import(string sourcePath)` methods.
- **UI**: [`SettingsControl.xaml`](file:///C:/Users/Hung/AppData/Roaming/FlowLauncher/Plugins/flow.launcher.plugin.snippets/Flow.Launcher.Plugin.Snippets/Views/SettingsControl.xaml) updates layout to separate the Management and Backup action groups, and [`SettingsControl.xaml.cs`](file:///C:/Users/Hung/AppData/Roaming/FlowLauncher/Plugins/flow.launcher.plugin.snippets/Flow.Launcher.Plugin.Snippets/Views/SettingsControl.xaml.cs) implements Open/Save file dialogs and confirmation messaging.
- **Dependencies**: Uses built-in WPF `Microsoft.Win32.OpenFileDialog` and `Microsoft.Win32.SaveFileDialog`; no external packages needed.
