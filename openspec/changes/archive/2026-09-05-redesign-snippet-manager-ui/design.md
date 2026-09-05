## Context

The current manager is a standard WPF `Window` loaded inside the Flow Launcher process. Its root uses the native default window background while child controls inherit Flow's globally loaded control templates; it also hard-codes a light border and error color. That mixture produces the observed white canvas, dark fields and table header, missing visual hierarchy, and action clipping.

Flow Launcher 2.1.3 already supplies a Windows 11-oriented theme through iNKORE UI WPF Modern plus stable application resource keys. Its built-in settings pages use `Color01B` for page backgrounds, `Color00B` for card/list surfaces, `Color03B` for borders, `Color04B` and `Color05B` for secondary and primary text, `BasicSystemAccentColor` for system accent, and `AccentButtonStyle` for primary actions. The plugin runs in-process, so standard WPF controls can consume these resources without adding a package reference.

The current code-behind owns filtering and CRUD directly and focused STA tests cover those behaviors. The redesign must preserve that small structure and the existing `SnippetStore` contract. See `specs/snippets/spec.md` for the new presentation and accessibility requirements.

## Goals / Non-Goals

**Goals:**

- Make the settings control and manager look native to Flow Launcher and Windows 11 in both light and dark themes.
- Keep list browsing and editing visible simultaneously through the confirmed split-view layout.
- Keep the applicable editor actions visible at the minimum supported window size.
- Clarify add, edit, selected, empty, validation-error, and persistence-error states.
- Preserve keyboard focus behavior and expose useful accessibility names.
- Limit implementation changes to the settings and manager surfaces plus focused tests.

**Non-Goals:**

- Adding or directly referencing the iNKORE UI package from the plugin.
- Creating a reusable plugin-wide design system or a general theme abstraction.
- Replacing the current event-driven WPF code-behind with MVVM or dependency injection.
- Changing JSON data, persistence, query results, ranking, clipboard actions, or reload behavior.
- Adding animations, acrylic, Mica, custom window chrome, or new icons as a prerequisite for the redesign.
- Making the window responsive below the stated minimum size by collapsing into a one-column layout.

## Decisions

### Reuse Flow dynamic resources with standard WPF controls

The manager root will use `DynamicResource` rather than hard-coded colors:

| Role | Flow resource |
|---|---|
| Page background | `Color01B` |
| Card and list surface | `Color00B` |
| Border and separator | `Color03B` |
| Primary text | `Color05B` |
| Secondary text | `Color04B` |
| Accent | `BasicSystemAccentColor` |
| Destructive/error color | `Color10B` |
| Primary button | `AccentButtonStyle` |

`DynamicResource` allows an open window to update when Flow changes theme. Local styles in the window will compose those values for cards, labels, list rows, empty state, and error state. The window will use the Windows 11 system font family (`Segoe UI Variable Text`) with the WPF/system fallback when unavailable.

Alternative considered: hard-coded Windows 11 light and dark palettes selected by code. That duplicates Flow's theme ownership and will drift when the user chooses a custom Flow theme.

Alternative considered: add iNKORE package references and use `SettingsCard`, `InfoBar`, and other modern controls directly. Flow already hosts those assemblies, but compiling the plugin against a second copy or a pinned package would introduce version coupling for a redesign that standard WPF controls can express.

### Keep the native title bar and synchronize its color mode

The existing native title bar remains because it provides correct Windows caption buttons, resizing, keyboard behavior, and accessibility without custom chrome code. The manager will receive the existing `IPublicAPI` instance and use `IsApplicationDarkTheme()` plus `ActualApplicationThemeChanged` to keep the native title bar's immersive dark-mode attribute aligned with the content. The event handler will marshal to the window dispatcher and unsubscribe when the window closes.

The implementation will call `DwmSetWindowAttribute` with the current Windows immersive-dark-mode attribute and a compatible fallback for older supported builds. Failure to set the attribute is non-fatal; the content remains themed and no custom title bar is introduced.

Alternative considered: a custom `WindowChrome` title bar using Flow's button templates. It can match Flow exactly but recreates native caption behavior, resize hit testing, accessibility, and Windows 11 interactions for little product value.

### Use a two-column layout with independently constrained regions

The manager defaults to approximately `960 x 600` device-independent units and remains resizable, with a minimum around `760 x 480`. The content uses a grid:

```text
+-------------------------------------------------------------+
| page heading / supporting text                              |
+----------------------------+--------------------------------+
| search and snippet list    | editor heading and mode        |
|                            | key + score                     |
| scrollable list            | scrollable value/editor body   |
|                            | inline error                    |
|                            | fixed action footer             |
+----------------------------+--------------------------------+
```

The list and value field own their scroll regions. The right card uses auto-sized heading and footer rows with a star-sized editor body, so actions cannot be displaced below the window by list growth. A narrow separator column provides a fixed 16-unit gap; the left side receives slightly more width for useful previews.

At elevated DPI the WPF device-independent layout remains the same, while the chosen minimum height keeps the fixed footer reachable on common displays. If editor content needs more vertical space, only the editor body scrolls; the action footer remains present.

Alternative considered: preserve the current vertical list-over-editor layout. It requires either a tall window or outer scrolling and caused the observed action clipping because the star-sized list competes with auto-sized editor content.

### Replace DataGrid chrome with a selectable ListBox row template

The existing table does not provide inline editing, sorting, or column manipulation, so `DataGrid` supplies unnecessary table behavior and inherits visually incompatible header chrome. It will be replaced by a standard `ListBox` named consistently with the current selection code where practical.

A static header grid and each item template share three columns: key, one-line preview, and score. Rows use a transparent/default background, a subtle Flow theme hover/selection surface, and a narrow accent indicator for selection. Key text is semibold, preview uses secondary text, and score is right-aligned with tabular-number rendering. The list has no decorative grid lines.

An overlay inside the list card presents the empty state when the filtered collection contains no items. The message distinguishes an empty library from a filter with no matches where practical, but both keep the editor reachable.

Alternative considered: locally restyle every DataGrid header, row, cell, selection, grid line, scrollbar, and focus state. That retains unused table complexity and is more fragile across Flow theme changes.

### Make editor mode explicit and show only applicable actions

The window keeps two explicit presentation modes:

- **Add mode:** no existing snippet is selected; the editor heading says `New snippet`; Add is the primary action; Save and Delete are collapsed; Cancel clears draft fields.
- **Edit mode:** a row is selected; the editor heading says `Edit snippet`; Save is the primary action; Delete and Cancel are available; Add is collapsed.

A top-level `New snippet` action clears the list selection, resets validation, enters add mode, and focuses the key field. Selecting a row enters edit mode and copies the exact stored key, value, and score into the fields.

After a successful edit, the list refreshes, reselects the updated key, and remains in edit mode. Cancelling edit restores the selected snippet's stored values rather than clearing selection. Confirmed deletion returns to add mode; declined deletion preserves the selected row and draft. If filtering removes the selected row from view, selection is cleared and the editor returns to add mode.

These are simple code-behind state transitions. No view-model or state-machine abstraction is needed.

### Use local Windows 11 spacing and typography styles

The window will define a small set of local styles rather than repeat raw values:

- Page padding: 20-24 units.
- Major gap: 16 units; field gap: 12; label-to-control gap: 6-8.
- Card corner radius: 8; card border: 1.
- Window title/heading: approximately 20, semibold.
- Editor title and key: 14, semibold.
- Body: 13-14; supporting and preview text: 12.
- Input and button minimum height: 32-36.

Labels use sentence case without explanatory text embedded in the label. Short supporting descriptions carry details such as key uniqueness, multiline values, and manual score priority. The score input remains a standard TextBox because integer validation already exists; introducing a number control would require another control dependency.

### Keep validation and errors inside the editor card

The existing error TextBlock moves into the editor card between the editable body and action footer. Its container uses theme-aware error text, a subtle themed surface, padding, and a descriptive automation name. The error remains collapsed when empty and does not resize the whole window unexpectedly.

Input validation remains the responsibility of the existing handlers and `SnippetStore`. This change does not create per-field validation rules or duplicate storage validation in XAML.

### Align the embedded plugin settings panel with Flow conventions

`SettingsControl.xaml` will use Flow's `SettingPanelMargin`, `SettingPanelItemTopBottomMargin`, primary/secondary text resources, and existing button style instead of an isolated 16-unit panel. It remains a small description plus one Manage Snippets action; it will not introduce a nested card or a second settings framework.

### Preserve names and logic where that reduces risk

Existing named inputs and action buttons will be retained when their semantics remain valid, allowing focused tests and code-behind to evolve rather than be rewritten. New named elements are limited to the New action, empty state, and mode-specific containers. No storage or query service will be referenced by new UI-only styling code beyond the current store interactions.

## Risks / Trade-offs

- **Flow resource keys are host-provided rather than owned by the plugin** -> Use only keys confirmed in Flow Launcher 2.1.3 and keep them as dynamic lookups so missing optional styles fall back to standard WPF behavior rather than preventing load.
- **DWM title-bar attributes vary by Windows build** -> Treat title-bar theming as best effort with the supported attribute and fallback; never fail manager creation when the call is unavailable.
- **Theme-change events can retain a closed window** -> Unsubscribe from `ActualApplicationThemeChanged` during `Closed` and marshal callbacks through the dispatcher.
- **A custom ListBox row loses DataGrid's automatic column sizing behavior** -> Use one shared three-column definition pattern with fixed key/score constraints and a star-sized preview, verified at the minimum width.
- **The two-column layout requires a meaningful minimum width** -> Enforce the confirmed minimum rather than adding an unrequested one-column responsive mode.
- **Visual correctness cannot be proven by XAML compilation alone** -> Verify the installed window on the actual Flow surface in light and dark themes, at minimum size, and at elevated Windows scaling; retain focused tests for mode and CRUD behavior.

## Migration Plan

No data or settings migration is required. Publish and install the updated assembly over the same plugin ID, restart Flow Launcher, and open the existing Manage Snippets action. Existing `snippets.json` and `.bak` files remain untouched.

Rollback is a binary rollback to the prior plugin build; the persisted data format and storage path are unchanged.
