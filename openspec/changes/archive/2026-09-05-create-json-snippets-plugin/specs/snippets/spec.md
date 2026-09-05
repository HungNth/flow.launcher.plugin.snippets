## Purpose

Provide a local, JSON-backed snippet library that Flow Launcher users can search by key, copy to the clipboard, and manage from the plugin settings UI.

## ADDED Requirements

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

### Requirement: Manage snippets from plugin settings
The plugin SHALL expose a `Manage Snippets` action in its Flow Launcher settings panel. The management surface SHALL support filtering by key and adding, editing, renaming, and deleting snippets.

#### Scenario: Open snippet management
- **WHEN** the user selects `Manage Snippets` from the plugin settings
- **THEN** the plugin displays the current snippets with key, value preview, and score

#### Scenario: Filter managed snippets
- **WHEN** the user enters text in the management filter
- **THEN** the displayed rows are filtered by key without modifying the library

#### Scenario: Edit a snippet
- **WHEN** the user changes a snippet key, multiline value, or score and saves valid input
- **THEN** the plugin persists the updated snippet immediately

#### Scenario: Cancel an edit
- **WHEN** the user cancels an add or edit operation
- **THEN** the library remains unchanged

#### Scenario: Delete a snippet
- **WHEN** the user confirms deletion of a snippet
- **THEN** the plugin removes and persists that snippet immediately

#### Scenario: Cancel deletion
- **WHEN** the user declines the deletion confirmation
- **THEN** the library remains unchanged

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
