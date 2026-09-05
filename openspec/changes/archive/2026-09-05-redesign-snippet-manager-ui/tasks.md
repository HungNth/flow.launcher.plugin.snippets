## 1. UI Behavior Tests

- [x] 1.1 Extend the focused STA manager tests for initial add mode, row selection entering edit mode, New Snippet returning to add mode, mode-specific action visibility, cancel semantics, successful edit reselection, confirmed delete returning to add mode, and filtering away the selected row; verify the new assertions fail against the current manager before implementation.
- [x] 1.2 Add focused accessibility and minimum-layout tests for descriptive automation names, logical keyboard tab order metadata, and applicable action controls receiving non-zero arranged bounds at the supported minimum window size; verify the tests exercise the real WPF window on an STA thread rather than inspecting XAML text.

## 2. Flow Theme Integration

- [x] 2.1 Pass `IPublicAPI` from the plugin entry point through the settings control to the manager window, subscribe only while the window is open to Flow's actual-application-theme event, and apply the native title-bar light/dark mode with a best-effort DWM call; verify the plugin builds and closing the window removes the theme subscription without affecting manager reuse.
- [x] 2.2 Restyle the embedded plugin settings control with Flow's standard settings margins, primary and secondary text resources, and existing button style; verify the settings control compiles without a new package reference and remains constructible in the focused WPF tests.

## 3. Split-View Manager Redesign

- [x] 3.1 Replace the vertical DataGrid layout with the two-column list/editor grid, local Flow-resource-based card and typography styles, a selectable ListBox row template, composed empty state, inline error surface, and fixed editor action footer; verify `dotnet build` succeeds and the minimum-layout test confirms the applicable actions are not clipped.
- [x] 3.2 Implement explicit add and edit presentation states in the existing code-behind, including New Snippet focus behavior, selection-preserving edits, cancel restoration, delete-to-add transition, filtered-selection handling, and collapsed irrelevant actions; verify all focused manager state tests pass.
- [x] 3.3 Apply accessible labels and automation names to search, key, score, value, clear, add, save, delete, cancel, and new controls while retaining visible focus behavior from Flow's standard control styles; verify the accessibility tests pass and keyboard operation can complete add, select, edit, cancel, and delete flows.

## 4. Verification and Visual Review

- [x] 4.1 Run the complete `dotnet test`, Debug and Release builds, and Debug win-x64 publish; verify all commands pass, publish output is complete, and no storage, JSON format, query, ranking, reload, clipboard, manifest identity, or action-keyword behavior changed.
- [x] 4.2 Install the publish output over the existing plugin ID, restart Flow Launcher, and visually verify the plugin settings control plus Manage Snippets in both light and dark Flow themes; change theme while the manager remains open and verify content colors and the native title bar update without reopening the window.
- [x] 4.3 Visually verify the manager at its minimum size and at an available Windows scaling level between 125 and 200 percent, capturing evidence that list content, editor inputs, inline errors, and applicable actions remain visible or intentionally scrollable without overlap. (User waived Windows scaling changes; verified via the real-window minimum-layout containment test plus a 760x480 live resize at the current 100 percent scale.)
- [x] 4.4 Smoke-test the installed split-view workflow with an empty library and populated library: filter, add, select, edit, rename, cancel, confirm and decline delete, trigger a validation or save error, and verify every successful mutation still appears in `sp` queries immediately.
- [x] 4.5 Review the implementation against the proposal, delta spec, and design, resolve correctness, accessibility, theme, or maintainability findings, then repeat the affected tests, builds, publish check, theme switch, minimum-size inspection, and CRUD smoke scenarios.
