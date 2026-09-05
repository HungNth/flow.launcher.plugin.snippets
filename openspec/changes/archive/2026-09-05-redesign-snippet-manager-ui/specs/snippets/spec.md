## ADDED Requirements

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

## MODIFIED Requirements

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
