# Glossary

## Snippets Domain

### Snippet
A saved text item identified by a unique key, containing a text value, an optional display score, and metadata (e.g. usage statistics or modification timestamps).

### Snippet Key
A unique identifier for a Snippet used for matching and querying.

### Snippet Value
The full textual content of a Snippet that is placed onto the clipboard or inserted into the target application.

### Auto-Paste
The action of automatically simulating a paste command (`Ctrl+V`) into the previously active window immediately after copying a Snippet's value to the clipboard and hiding the launcher window.

### Paste Delay
The configurable time duration (in milliseconds, bounded between 10ms and 1000ms, default 100ms) waited asynchronously after hiding the launcher window to ensure the previous target window regains focus before the paste command is simulated.

### Plugin Settings
User-configurable options managed via Flow Launcher's setting storage, including the `AutoPasteEnabled` toggle and `PasteDelayMs` duration.

### Context Menu Copy
A secondary action exposed via Flow Launcher's context menu (`IContextMenu`) allowing the user to copy a Snippet's value directly to the clipboard without triggering Auto-Paste.
