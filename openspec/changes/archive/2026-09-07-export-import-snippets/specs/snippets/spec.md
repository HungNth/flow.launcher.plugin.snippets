## ADDED Requirements

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
