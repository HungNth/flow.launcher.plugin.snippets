# 01: Plugin Settings Storage and Configuration UI

**What to build:** A persistent configuration model for plugin settings (`PluginSettings`) with `AutoPasteEnabled` (boolean, default true) and `PasteDelayMs` (integer, default 100, clamped to 10-1000ms), integrated with Flow Launcher's settings JSON storage and exposed in the WPF `SettingsControl` view with input validation.

**Blocked by:** None (can start immediately)

Status: resolved

- [x] Create `PluginSettings` model with `AutoPasteEnabled` (default: true) and `PasteDelayMs` (default: 100, clamped to 10-1000ms).
- [x] Connect `PluginSettings` to Flow Launcher's `IPublicAPI.LoadSettingJsonStorage<PluginSettings>()` and `SaveSettingJsonStorage<PluginSettings>()`.
- [x] Update `SettingsControl.xaml` and `SettingsControl.xaml.cs` with an Auto-Paste toggle checkbox and a Paste Delay numeric input field.
- [x] Provide input validation for `PasteDelayMs` in the UI to prevent values outside 10ms - 1000ms.
- [x] Add unit tests verifying default values, boundary clamping, and serialization.
