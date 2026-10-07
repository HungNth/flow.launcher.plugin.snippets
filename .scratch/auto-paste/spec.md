Status: ready-for-agent

# Specification: Auto-Paste on Enter with Retained Copy Behavior

## Problem Statement

When using Flow Launcher to search for and select snippets, users must press Enter to copy the snippet value to the clipboard and then manually switch back to their target application to press `Ctrl+V`. This manual second step disrupts flow, slows down repetitive text expansion workflows, and requires extra keystrokes. Users want selecting a snippet in Flow Launcher to immediately paste its content directly into the active application where they were typing, while still maintaining the ability to copy snippets without pasting when desired.

## Solution

Introduce an **Auto-Paste** capability:
- When a user selects a snippet by pressing `Enter`, the plugin copies the **Snippet Value** to the system clipboard and, when enabled, triggers an automated paste sequence into the target application.
- The action returns `true` so Flow Launcher immediately hides its main window, and a background task waits for the configured **Paste Delay** before simulating `Ctrl+V` into the previously active window.
- The user can configure whether **Auto-Paste** is enabled and adjust the **Paste Delay** (bounded between 10ms and 1000ms, defaulting to 100ms) in the plugin's settings panel.
- To preserve the option to only copy without pasting, users can use the standard `Ctrl+C` shortcut or select the new **Context Menu Copy** item ("Copy to clipboard") from Flow Launcher's context menu (`IContextMenu.LoadContextMenus`).
- If clipboard copy fails or paste simulation fails (e.g. the target application runs in an elevated Administrator / UAC context), the plugin notifies the user appropriately without crashing or corrupting state.

## User Stories

1. As a power user, I want selecting a snippet and pressing Enter to paste its content directly into my active document or editor, so that I can insert boilerplate text without an extra keystroke.
2. As a power user, I want the snippet value to remain on my clipboard after auto-pasting, so that I can paste it again elsewhere if needed.
3. As a user, I want a settings toggle to enable or disable Auto-Paste, so that I can decide whether Enter pastes or only copies.
4. As a user, I want Auto-Paste enabled by default, so that I get fast expansion out of the box without manual configuration.
5. As a user, I want to configure the Paste Delay (between 10ms and 1000ms), so that I can accommodate slower machines or heavy terminal/IDE focus transitions.
6. As a user, I want input validation on the Paste Delay setting, so that invalid or out-of-range values fall back gracefully or are prevented from causing issues.
7. As a user, I want a context menu action to copy a snippet without triggering Auto-Paste, so that I can save a snippet to my clipboard for later use without pasting it into my current editor.
8. As a keyboard user, I want to press `Ctrl+C` on a snippet result in Flow Launcher to copy it directly without pasting, so that my muscle memory continues to work.
9. As a user working in an elevated/Administrator window, I want to receive a clear notification if Auto-Paste cannot simulate keystrokes due to permissions, so that I know the snippet is already safely in my clipboard.
10. As a user whose clipboard operation fails, I want to receive a clear error notification and have auto-paste aborted, so that no stale clipboard contents are pasted into my document.
11. As a user inspecting plugin settings, I want settings changes to be persisted immediately across Flow Launcher restarts, so that my preferences are retained.

## Implementation Decisions

- **Domain Model Alignment**: Adhere strictly to terms defined in `GLOSSARY.md` (`Snippet`, `Snippet Key`, `Snippet Value`, `Auto-Paste`, `Paste Delay`, `Plugin Settings`, `Context Menu Copy`).
- **Plugin Settings Storage**:
  - Implement a dedicated settings model (`PluginSettings`) persisted through Flow Launcher's standard JSON settings storage (`API.LoadSettingJsonStorage<PluginSettings>()` and `API.SaveSettingJsonStorage<PluginSettings>()`).
  - `AutoPasteEnabled`: boolean, default `true`.
  - `PasteDelayMs`: integer, default `100`, bounded between `10` and `1000`.
- **Keyboard Simulation & Paste Orchestration Seam**:
  - Abstract paste execution behind an interface (e.g. `IPasteExecutor` or `IPasteSimulator`) with an injectable delay mechanism so tests do not rely on wall-clock `Task.Delay` or Win32 OS APIs.
  - The production simulator uses Win32 `SendInput` (synthesizing `VK_CONTROL` down, `'V'` down, `'V'` up, `VK_CONTROL` up).
- **Execution Lifecycle & Return Contract**:
  - In `Result.Action`:
    1. Synchronously copy the snippet value to the clipboard via `API.CopyToClipboard`.
    2. If copy fails, report an error toast and return `false` (keeps Flow Launcher open).
    3. If copy succeeds:
       - If `AutoPasteEnabled` is false: return `true` (Flow Launcher closes, snippet is in clipboard).
       - If `AutoPasteEnabled` is true: schedule the asynchronous paste operation (wait `PasteDelayMs` -> simulate `Ctrl+V`), and return `true` (Flow Launcher immediately hides itself, allowing the previous window to regain input focus during the delay).
       - If paste simulation reports failure (e.g., SendInput returns 0), surface a notification that the snippet is on the clipboard but could not be pasted automatically.
- **Context Menu Integration**:
  - Implement `IContextMenu` on `Main`: `List<Result> LoadContextMenus(Result selectedResult)`.
  - Provide a context menu result: Title "Copy to clipboard", SubTitle "Copy snippet value without pasting", with an action that performs only clipboard copy and returns `true`.
- **Settings UI**:
  - Update `SettingsControl.xaml` and `SettingsControl.xaml.cs` to add:
    - Checkbox for `Auto-Paste on Enter`.
    - TextBox / numeric input for `Paste Delay (ms)` with validation (clamped to 10-1000ms).
    - Immediate save via `API.SaveSettingJsonStorage<PluginSettings>()` on change.

## Testing Decisions

- **External Behavior Testing**:
  - Test only observable external behavior, state changes, and emitted contracts, not internal private implementation details.
  - Test framework: MSTest 3.x (existing project standard, standard assertions without extra third-party test libraries).
- **Modules Tested**:
  - `SnippetQueryService`: Verify results contain correct titles, subtitles, scores, and executable actions.
  - `PluginSettings`: Verify default values (`AutoPasteEnabled = true`, `PasteDelayMs = 100`) and boundary clamping / validation logic.
  - `PasteOrchestration`: Verify that when Auto-Paste is enabled, copy succeeds, delay is scheduled, and paste simulation is triggered; verify that when Auto-Paste is disabled, only copy is executed.
  - `IContextMenu.LoadContextMenus`: Verify that snippet results yield a copy-only context menu result.
- **Prior Art**:
  - `Flow.Launcher.Plugin.Snippets.Tests/QueryTests.cs` and `StorageTests.cs`.

## Out of Scope

- Gõ từng ký tự trực tiếp (typing snippet characters one-by-one via Unicode key events) thay cho `Ctrl+V`.
- Tự động phát hiện loại ứng dụng đích (e.g. Vim, terminal) để chuyển sang `Ctrl+Shift+V` (người dùng terminal có thể cấu hình hoặc dùng clipboard thông thường).
- Dynamic placeholder/snippet macro templating.

## Further Notes

- Win32 `SendInput` requires foreground focus on the target window. Letting Flow Launcher close via returning `true` and waiting `PasteDelayMs` ensures reliable window activation.
