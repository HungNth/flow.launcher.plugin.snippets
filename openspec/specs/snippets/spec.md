# Snippets Specification

## Purpose

Provide a local, JSON-backed snippet library that Flow Launcher users can search by key, copy to the clipboard, and manage from the plugin settings UI.

## Requirements

### Requirement: Versioned JSON snippet library
The plugin SHALL store its library in a human-readable `snippets.json` file within the plugin settings directory. The root object SHALL contain `version` and `snippets`, and each snippet SHALL contain `key`, `value`, and `score`.

#### Scenario: Load a valid library
- **WHEN** `snippets.json` contains supported, valid snippet data
- **THEN** the plugin loads all snippets with their exact values and manual scores

#### Scenario: Start without a data file
- **WHEN** `snippets.json` does not exist
- **THEN** the plugin starts with an empty library without reporting an error
- **AND** the plugin creates the file only when the library is first changed

#### Scenario: Preserve multiline content
- **WHEN** a snippet value contains line breaks or leading and trailing whitespace
- **THEN** the plugin preserves that content exactly when loading and saving the library

#### Scenario: Reject an unsupported file version
- **WHEN** `snippets.json` declares a version the plugin does not support
- **THEN** the plugin does not load or overwrite that file
- **AND** the plugin exposes an actionable error to the user

### Requirement: Valid and unique snippets
The plugin SHALL require every snippet to have a non-empty trimmed key, a non-empty value, and an integer score. Keys SHALL be unique using an ordinal, case-insensitive comparison while preserving the entered casing for display.

#### Scenario: Add a valid snippet
- **WHEN** the user provides a new key, a non-empty value, and a valid score
- **THEN** the plugin adds the snippet and preserves the displayed key casing

#### Scenario: Reject a duplicate key
- **WHEN** the user adds or renames a snippet to a key that differs from an existing key only by letter casing
- **THEN** the plugin rejects the change and identifies the duplicate key

#### Scenario: Default a new score
- **WHEN** the user starts creating a snippet
- **THEN** the management UI initializes its score to `0`

#### Scenario: Reject invalid persisted data
- **WHEN** the JSON library contains a missing key, empty value, non-integer score, or duplicate key
- **THEN** the plugin treats the library as invalid and does not silently discard or merge the invalid entries

### Requirement: Reliable JSON persistence
The plugin SHALL persist each accepted library mutation as one complete replacement of `snippets.json` and SHALL retain the previous valid file as `snippets.json.bak` when a previous file exists.

#### Scenario: Save a library mutation
- **WHEN** an add, edit, or delete operation passes validation
- **THEN** the complete updated library is written successfully before it becomes the active in-memory library
- **AND** subsequent queries observe the change without restarting Flow Launcher

#### Scenario: Fail during save
- **WHEN** the updated library cannot be written or replace the existing file
- **THEN** the previous file and active in-memory library remain usable
- **AND** the management UI reports the failure

#### Scenario: Recover the prior file
- **WHEN** a valid `snippets.json` is replaced by a newly saved version
- **THEN** `snippets.json.bak` contains the immediately previous valid file

### Requirement: Search snippets by key
The plugin SHALL use the action keyword `sp` and SHALL interpret all text after the keyword only as a key search query. Querying SHALL never create or modify snippets.

#### Scenario: List snippets without a search term
- **WHEN** the user enters `sp` with no search text
- **THEN** the plugin returns all snippets ordered by manual score descending and then key ascending using an ordinal, case-insensitive comparison

#### Scenario: Fuzzy-search snippet keys
- **WHEN** the user enters search text after `sp`
- **THEN** the plugin returns only snippets whose keys match Flow Launcher's fuzzy matching behavior
- **AND** matches are ordered by fuzzy relevance descending, manual score descending, and key ascending

#### Scenario: Do not search snippet values
- **WHEN** the search text occurs only in a snippet value and not in its key
- **THEN** that snippet is not returned for the query

#### Scenario: No matching snippets
- **WHEN** no snippet key matches the search text
- **THEN** the plugin returns a non-actionable empty-state result
- **AND** the query does not offer to create a snippet

### Requirement: Present recognizable snippet results
Each matching result SHALL identify the snippet key as its title and provide a single-line preview of its value without modifying the stored value.

#### Scenario: Present a multiline snippet
- **WHEN** a matching snippet value spans multiple lines
- **THEN** its result subtitle displays a single-line preview
- **AND** its full stored value remains unchanged

#### Scenario: Complete a result query
- **WHEN** the user accepts autocomplete for a snippet result
- **THEN** Flow Launcher completes the query as `sp <key>`

### Requirement: Copy the selected value
Selecting a snippet result SHALL copy the exact stored value to the clipboard without expanding placeholders, variables, or other syntax.

#### Scenario: Copy a snippet successfully
- **WHEN** the user selects a snippet result and the clipboard operation succeeds
- **THEN** the clipboard contains the exact stored value, including line breaks and whitespace
- **AND** Flow Launcher's window closes

#### Scenario: Clipboard operation fails
- **WHEN** the selected value cannot be copied to the clipboard
- **THEN** Flow Launcher's window remains open
- **AND** the plugin presents an actionable error instead of reporting success

### Requirement: Theme-coherent snippet management
The plugin settings panel and Manage Snippets window SHALL follow Flow Launcher's active light or dark theme and SHALL remain readable without requiring a Flow Launcher restart.

#### Scenario: Open in light theme
- **WHEN** the user opens plugin settings or Manage Snippets while Flow Launcher uses a light theme
- **THEN** backgrounds, cards, borders, text, fields, list rows, selection, errors, and actions use a coherent light appearance with readable contrast
- **AND** the native window title bar does not visually conflict with the light content

#### Scenario: Open in dark theme
- **WHEN** the user opens plugin settings or Manage Snippets while Flow Launcher uses a dark theme
- **THEN** backgrounds, cards, borders, text, fields, list rows, selection, errors, and actions use a coherent dark appearance with readable contrast
- **AND** the native window title bar does not remain visually light against dark content

#### Scenario: Change theme while the manager is open
- **WHEN** Flow Launcher's active theme changes while Manage Snippets remains open
- **THEN** theme-dependent manager colors update to the new theme without restarting Flow Launcher

### Requirement: Adaptive and accessible management surface
The Manage Snippets window SHALL keep its searchable list, editor inputs, status feedback, and applicable actions visible and keyboard-accessible at its supported minimum size and common Windows display scaling levels.

#### Scenario: Use the minimum window size
- **WHEN** the window is displayed at its supported minimum width and height
- **THEN** the snippet list and editor remain usable
- **AND** the applicable primary, cancel, and delete actions are not clipped or hidden outside the usable content area

#### Scenario: Use elevated display scaling
- **WHEN** Windows display scaling is set between 125 percent and 200 percent
- **THEN** required text, inputs, list content, error feedback, and actions remain visible or reachable through an intentional scroll region
- **AND** controls do not overlap

#### Scenario: Navigate using the keyboard
- **WHEN** the user navigates the manager without a pointing device
- **THEN** focus moves through search, snippet selection, editor fields, and applicable actions in a logical order
- **AND** the focused control has a visible focus indicator

#### Scenario: Identify controls through accessibility APIs
- **WHEN** assistive technology inspects the manager
- **THEN** search, key, score, value, add, save, delete, cancel, and clear-filter controls expose descriptive accessible names

#### Scenario: Display an error
- **WHEN** an add, edit, delete, or persistence operation fails
- **THEN** the manager presents an inline error associated with the editor
- **AND** the message remains readable in both light and dark themes

### Requirement: Manage snippets from plugin settings
The plugin SHALL expose a `Manage Snippets` action in its Flow Launcher settings panel. The management window SHALL present a two-column split view with a searchable snippet list on the left and an add/edit editor on the right. It SHALL support filtering by key and adding, editing, renaming, and deleting snippets without changing their persistence behavior.

#### Scenario: Open snippet management
- **WHEN** the user selects `Manage Snippets` from the plugin settings
- **THEN** the plugin displays the current snippets in the list with key, single-line value preview, and score
- **AND** the editor is visible alongside the list

#### Scenario: Filter managed snippets
- **WHEN** the user enters text in the management filter
- **THEN** the displayed rows are filtered by key without modifying the library

#### Scenario: Show an empty library
- **WHEN** the snippet library contains no snippets
- **THEN** the list presents a composed empty state explaining how to add the first snippet
- **AND** the editor remains available for creating that snippet

#### Scenario: Begin adding a snippet
- **WHEN** no existing snippet is selected or the user starts a new snippet
- **THEN** the editor clearly indicates add mode
- **AND** the add action is presented as the primary action
- **AND** edit-only actions are not presented as available

#### Scenario: Select a snippet for editing
- **WHEN** the user selects a snippet row
- **THEN** the selected row is visually distinct
- **AND** the editor clearly indicates edit mode and displays the exact key, value, and score
- **AND** save and delete actions are presented for the selected snippet

#### Scenario: Edit a snippet
- **WHEN** the user changes a snippet key, multiline value, or score and saves valid input
- **THEN** the plugin persists the updated snippet immediately
- **AND** the list refreshes while preserving a clear selection state

#### Scenario: Cancel an edit
- **WHEN** the user cancels an add or edit operation
- **THEN** the library remains unchanged
- **AND** the editor returns to a clear add or selected-item state

#### Scenario: Delete a snippet
- **WHEN** the user confirms deletion of a snippet
- **THEN** the plugin removes and persists that snippet immediately
- **AND** the editor returns to add mode

#### Scenario: Cancel deletion
- **WHEN** the user declines the deletion confirmation
- **THEN** the library and current editor state remain unchanged

### Requirement: Reload externally changed data
The plugin SHALL support Flow Launcher's plugin-data reload operation so a user can deliberately reload a manually edited `snippets.json` without restarting Flow Launcher.

#### Scenario: Reload a valid external edit
- **WHEN** the user invokes Flow Launcher's plugin-data reload operation after valid external changes
- **THEN** subsequent queries and the management UI use the reloaded library

#### Scenario: Reload malformed JSON
- **WHEN** the user invokes reload and the file is malformed or invalid
- **THEN** the plugin keeps the last valid in-memory library
- **AND** the plugin does not overwrite the malformed file
- **AND** the plugin exposes an actionable error

### Requirement: Export snippet library to file
The plugin SHALL allow users to export the complete in-memory snippet library to an external JSON file in the standard SnippetDocument format.

#### Scenario: Successfully export snippet library
- **WHEN** the user selects Export JSON and chooses a destination file path
- **THEN** the plugin writes the current snippets to the selected file formatted with version and snippets array
- **AND** notifies the user that the export was successful

#### Scenario: Cancel export file dialog
- **WHEN** the user cancels the save file dialog
- **THEN** no file is written and no error is reported

#### Scenario: Export fails due to I/O error
- **WHEN** writing to the destination path fails due to permissions or I/O failure
- **THEN** the plugin reports an actionable error and does not corrupt existing snippets

### Requirement: Import snippet library from file
The plugin SHALL allow users to import a snippet library from an external JSON file, replacing the active snippet library after user confirmation.

#### Scenario: Successfully import valid snippet file with Replace All
- **WHEN** the user selects Import JSON, chooses a valid JSON snippet file, and confirms the replacement warning
- **THEN** the plugin replaces the current in-memory snippets and snippets.json with the imported snippets
- **AND** creates or updates snippets.json.bak with the prior valid library
- **AND** subsequent queries immediately reflect the imported snippets

#### Scenario: Cancel import file dialog or confirmation
- **WHEN** the user cancels the open file dialog or declines the replacement confirmation
- **THEN** the current snippet library and data files remain completely unchanged

#### Scenario: Reject invalid or malformed import file
- **WHEN** the chosen import file does not exist, has an unsupported version, contains invalid JSON syntax, or contains duplicate keys
- **THEN** the plugin rejects the import and displays an actionable error
- **AND** the existing active snippet library and data files remain intact

### Requirement: Settings panel backup actions
The plugin settings panel SHALL expose `Export JSON` and `Import JSON` actions visually grouped separately from the `Manage Snippets` action.

#### Scenario: Visual separation of settings actions
- **WHEN** the user views the plugin settings panel
- **THEN** `Manage Snippets` is displayed in a distinct primary management group
- **AND** `Export JSON` and `Import JSON` are displayed together in a distinct backup/restore group with a wider visual separation

