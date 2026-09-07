## 1. Storage Layer

- [x] 1.1 Add unit tests for `SnippetStore.Export` in `StorageTests.cs` and verify export assertions fail before implementation
- [x] 1.2 Implement `SnippetStore.Export(string destinationPath)` in `SnippetStore.cs` and verify export unit tests pass
- [x] 1.3 Add unit tests for `SnippetStore.Import` in `StorageTests.cs` covering Replace All, `.bak` creation, and malformed file rejection, verifying tests fail before implementation
- [x] 1.4 Implement `SnippetStore.Import(string sourcePath)` in `SnippetStore.cs` and verify all storage import tests pass

## 2. Settings UI Layer

- [x] 2.1 Update `SettingsControl.xaml` to place `Manage Snippets` in group 1 and `Export JSON` / `Import JSON` in group 2 separated by a wide visual margin, verifying XAML compiles cleanly
- [x] 2.2 Implement export and import button click handlers with `SaveFileDialog`, `OpenFileDialog`, confirmation prompt, and error reporting in `SettingsControl.xaml.cs`, verifying with unit tests in `SettingsManagerUiTests.cs`

## 3. Solution Verification

- [x] 3.1 Execute `dotnet test` across the entire solution and verify all test suites pass with zero failures
- [x] 3.2 Validate change coherence with `openspec validate export-import-snippets`
