## Why

The current Manage Snippets window mixes the native Windows title bar, Flow Launcher control templates, and hard-coded light colors, producing unreadable controls, a disconnected black table header, excessive empty space, and clipped actions. The manager needs a coherent Windows 11-style surface that follows Flow Launcher's active light or dark theme without changing snippet behavior.

## What Changes

- Redesign Manage Snippets as a two-column split view with the searchable snippet list on the left and the add/edit editor on the right.
- Replace the default WPF `DataGrid` presentation with a themed selectable list that shows key, one-line preview, and score without the current table chrome.
- Use Flow Launcher dynamic colors and control styles so the manager and plugin settings panel update with the active light or dark theme.
- Keep the native Windows title bar and synchronize its light/dark appearance with the themed content.
- Make add, edit, delete, cancel, empty, selected, and error states visually distinct and ensure only relevant actions are shown for the current editor mode.
- Keep management actions visible and usable at the supported minimum window size and common Windows DPI scaling levels.
- Add keyboard, focus, automation-label, and contrast requirements for the management workflow.
- Preserve all existing JSON storage, search, ranking, reload, clipboard, validation, and CRUD behavior.
- Do not add a UI framework package, change the snippets JSON format, or redesign Flow's main query results.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `snippets`: Strengthen snippet-management requirements for adaptive Windows 11 presentation, automatic Flow theme synchronization, visible editor actions, clear UI states, and keyboard accessibility.

## Impact

- Updates the WPF settings control and Manage Snippets window layout, theme resources, editor state presentation, and native title-bar theme handling.
- Updates the management-window code-behind only where needed to expose add/edit states, action visibility, keyboard behavior, and theme synchronization.
- Extends focused WPF UI tests and requires visual verification in the installed Flow Launcher under light and dark themes plus elevated DPI scaling.
- Does not affect the plugin manifest identity, action keyword, storage service, JSON files, query service, or clipboard behavior.
