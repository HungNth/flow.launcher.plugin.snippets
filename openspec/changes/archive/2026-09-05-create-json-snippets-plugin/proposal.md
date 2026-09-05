## Why

Flow Launcher needs a small, predictable snippets plugin whose library remains portable and human-readable in JSON. The existing installed plugin is being retired because its broader SQLite, auto-paste, and dynamic-variable behavior does not match the desired JSON-only workflow.

## What Changes

- Add a new C# Flow Launcher plugin named Snippets with the action keyword `sp`.
- Persist snippets in a versioned, human-readable `snippets.json` file containing `key`, `value`, and manually assigned `score` fields.
- Search snippet keys with Flow Launcher's fuzzy matcher and rank matches deterministically by relevance, manual score, and key.
- Copy the selected snippet value to the clipboard and close Flow Launcher.
- Add a plugin settings entry that opens a management UI for filtering, adding, editing, and deleting snippets.
- Validate snippet data and save JSON atomically with a backup so malformed input or interrupted writes do not silently destroy the library.
- Exclude SQLite storage, VS Code placeholders, scope, prefix, auto-paste, dynamic variables, folders, quick-add query syntax, import/export, and migration from the retired plugin.

## Capabilities

### New Capabilities

- `snippets`: JSON-backed snippet storage, key search and ranking, clipboard use, and settings-based snippet management.

### Modified Capabilities

None.

## Impact

- Introduces a new .NET/Windows Flow Launcher plugin assembly, manifest, icon resources, and WPF settings/management UI.
- Stores user data under the plugin settings directory in `snippets.json` and `snippets.json.bak`.
- Uses the Flow Launcher C# plugin API for lifecycle, fuzzy search, settings integration, result presentation, reload, logging, and clipboard access.
- Does not require a database, background service, external network access, or data compatibility with the removed Snippets 2.3.8 installation.
