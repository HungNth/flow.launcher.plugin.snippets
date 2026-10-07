# 03: End-to-End Enter Auto-Paste with Win32 Simulation and Safety

**What to build:** Complete end-to-end Auto-Paste functionality when selecting a snippet with Enter in Flow Launcher: copy snippet value to clipboard, immediately close Flow Launcher, wait for the configured Paste Delay, and simulate `Ctrl+V` into the previous window via Win32 `SendInput`. Respects `AutoPasteEnabled` and `PasteDelayMs` from `PluginSettings`, with non-fatal notifications on paste simulation failure and unit tests using an injected simulator seam.

**Blocked by:** 01 (Plugin Settings Storage and Configuration UI)

Status: resolved

- [x] Implement an `IPasteSimulator` abstraction and production Win32 `SendInput` simulator for `Ctrl+V`.
- [x] Connect `Main.Query` / `SnippetQueryService` selection action to respect `PluginSettings.AutoPasteEnabled` and `PluginSettings.PasteDelayMs`.
- [x] When Auto-Paste is enabled: copy to clipboard, return `true` to close Flow Launcher, schedule asynchronous background delay, and trigger paste simulation.
- [x] When Auto-Paste is disabled: copy to clipboard and return `true` without triggering paste simulation.
- [x] If clipboard copy fails: show error notification and return `false`.
- [x] If paste simulation fails or encounters permission constraints: log warning and show non-fatal notification that snippet is copied but could not be pasted automatically.
- [x] Ensure all background exceptions are safely handled and never become unobserved background crashes.
- [x] Add unit tests verifying selection actions, delay scheduling, and error handling using test fakes without depending on real OS input.
