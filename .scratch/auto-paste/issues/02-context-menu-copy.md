# 02: Context Menu Copy Action

**What to build:** Context menu support for snippet query results via Flow Launcher's `IContextMenu` interface, allowing users to open the context menu on any snippet result and select "Copy to clipboard" to copy the snippet value directly to the system clipboard without triggering Auto-Paste.

**Blocked by:** None (can start immediately)

Status: resolved

- [x] Implement `IContextMenu` on `Main` with `List<Result> LoadContextMenus(Result selectedResult)`.
- [x] Extract the snippet value from the selected result's contextual data / copy text.
- [x] Return a context menu result with Title "Copy to clipboard" and SubTitle "Copy snippet value without pasting".
- [x] Ensure selecting the context menu item copies the snippet value to the clipboard and closes Flow Launcher without triggering auto-paste.
- [x] Add unit tests verifying that `LoadContextMenus` returns the expected copy-only action and correctly executes clipboard copy.
