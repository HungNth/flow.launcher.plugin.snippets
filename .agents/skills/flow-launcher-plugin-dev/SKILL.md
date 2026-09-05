---
name: "flow-launcher-plugin-dev"
description: "Guide for developing Flow Launcher C# plugins. Invoke when creating, modifying, or debugging Flow Launcher plugins, or when user asks about Flow Launcher plugin development."
---

# Flow Launcher C# Plugin Development Guide

Flow Launcher is a Windows quick file search & app launcher written in C#. C# plugins communicate directly with Flow without extra protocols (like JSON-RPC), offering the best performance.

## 1. Project Creation

### Option A: Official dotnet template (recommended)

```bash
dotnet new install Flow.Launcher.Plugin.Templates
dotnet new flowplugin -n Flow.Launcher.Plugin.YourPlugin
```

### Option B: Manual creation

```bash
dotnet new classlib -n Flow.Launcher.Plugin.YourPlugin
cd Flow.Launcher.Plugin.YourPlugin
dotnet add package Flow.Launcher.Plugin
```

## 2. Plugin Directory Structure

```
Flow.Launcher.Plugin.YourPlugin/
├── Images/                  # Icon resources (256x256px PNG recommended)
│   └── plugin_icon.png
├── Languages/               # i18n support (optional)
│   ├── en.xaml
│   └── zh-cn.xaml
├── plugin.json              # Plugin metadata (required)
├── Main.cs                  # Core logic (required, class MUST be named "Main")
└── YourPlugin.csproj        # Project config
```

## 3. plugin.json Configuration

```json
{
  "ID": "CEA0FDFC6D3B4085823D60DC76F28855",
  "ActionKeywords": ["*"],
  "Name": "YourPlugin",
  "Description": "Plugin description",
  "Author": "Your Name",
  "Version": "1.0.0",
  "Language": "csharp",
  "Website": "https://github.com/you/your-plugin",
  "ExecuteFileName": "Flow.Launcher.Plugin.YourPlugin.dll",
  "IcoPath": "Images\\plugin_icon.png",
  "MinimumAppVersion": "1.14.0"
}
```

| Field | Description |
|-------|-------------|
| `ID` | Unique identifier, use GUID. MUST match plugin folder name (case-sensitive) |
| `ActionKeywords` | Trigger keywords. `*` means global plugin |
| `Name` | Plugin name shown in plugin store |
| `Version` | Semantic version (major.minor.patch, must be 3 segments) |
| `Language` | `"csharp"` for C# plugins |
| `ExecuteFileName` | Compiled DLL filename |
| `IcoPath` | Plugin icon path |
| `MinimumAppVersion` | Minimum compatible Flow Launcher version |

## 4. Core Interfaces

### IPlugin (Synchronous)

For simple, fast query tasks:

```csharp
using Flow.Launcher.Plugin;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.YourPlugin
{
    public class Main : IPlugin
    {
        private PluginInitContext _context;

        public void Init(PluginInitContext context)
        {
            _context = context;
            // Do initialization here (load config, cache data)
            // Prefer expensive operations here over constructor
            // Init runs in parallel with other plugins
        }

        public List<Result> Query(Query query)
        {
            // Called when user activates plugin with ActionKeyword
            // query.Search -> actual search part (no ActionKeyword)
            // query.RawQuery -> raw query string
            // query.FirstSearch / query.SecondToEndSearch -> segmented access

            return new List<Result>
            {
                new Result
                {
                    Title = "Result Title",
                    SubTitle = "Result Subtitle",
                    IcoPath = "Images\\icon.png",
                    Action = c =>
                    {
                        // Action when user selects this result
                        // Return true to close Flow window
                        return true;
                    }
                }
            };
        }
    }
}
```

### IAsyncPlugin (Recommended)

For network requests, complex calculations, or any async operations:

```csharp
using Flow.Launcher.Plugin;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.YourPlugin
{
    public class Main : IAsyncPlugin
    {
        private PluginInitContext _context;

        public Task InitAsync(PluginInitContext context)
        {
            _context = context;
            return Task.CompletedTask;
        }

        public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
        {
            // token allows checking if user typed a new query
            // if (token.IsCancellationRequested) return results;

            var data = await FetchDataAsync(query.Search, token);

            return data.Select(item => new Result
            {
                Title = item.Name,
                SubTitle = item.Description,
                IcoPath = "Images\\icon.png",
                AsyncAction = async c =>
                {
                    await ProcessResult(item);
                    return true;
                }
            }).ToList();
        }
    }
}
```

## 5. Query Object Key Properties

| Property | Description |
|----------|-------------|
| `RawQuery` | Raw query string including ActionKeyword |
| `Search` | Actual search part, excluding ActionKeyword |
| `SearchTerms` | Search string split into array |
| `ActionKeyword` | The action keyword that triggered this plugin |
| `FirstSearch` | First part of the search |
| `SecondToEndSearch` | Second part onwards of the search |
| `IsReQuery` | Whether user force-refreshed query (e.g. Ctrl+R) |

## 6. Result Object Key Properties

| Property | Description |
|----------|-------------|
| `Title` | Main title (required) |
| `SubTitle` | Subtitle |
| `IcoPath` | Icon path |
| `Action` | Sync action on select, `Func<ActionContext, bool>` |
| `AsyncAction` | Async action on select |
| `Score` | Priority score, affects display order |
| `CopyText` | Text copyable via Ctrl+C |
| `AutoCompleteText` | Auto-complete suggestion text |
| `ContextData` | Context data for later use |
| `ProgressBar` | Progress bar value (0-100) |
| `Glyph` | Font icon info (`GlyphInfo`) |

## 7. Extension Interfaces (IFeatures)

Implement these in the **same class** that implements IPlugin/IAsyncPlugin:

| Interface | Method | Description |
|-----------|--------|-------------|
| `IContextMenu` | `LoadContextMenus(Result)` | Right-click context menu, returns `List<Result>` |
| `IReloadable` / `IAsyncReloadable` | `ReloadData()` / `ReloadDataAsync()` | Called when user clicks "Reload Plugin Data" |
| `IPluginI18n` | `GetTranslatedPluginTitle()` / `GetTranslatedPluginDescription()` | i18n support, works with `/Languages` directory |
| `IResultUpdated` | Fire `ResultUpdated` event | Early return partial query results (long-running queries) |
| `ISettingProvider` | `CreateSettingPanel()` | Custom settings panel UI |
| `IDisposable` | `Dispose()` | Release unmanaged resources on Flow exit (v1.8.0+) |

### Context Menu Example

```csharp
public class Main : IPlugin, IContextMenu
{
    public List<Result> LoadContextMenus(Result selectedResult)
    {
        return new List<Result>
        {
            new Result
            {
                Title = "Copy",
                Action = c =>
                {
                    _context.API.CopyToClipboard(selectedResult.Title);
                    return false;
                }
            }
        };
    }
}
```

### IResultUpdated Example (for long-running queries)

```csharp
public class Main : IAsyncPlugin, IResultUpdated
{
    public event ResultUpdatedEventHandler ResultUpdated;

    public async Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var allResults = new List<Result>();

        var batch1 = await GetFirstBatch(query);
        allResults.AddRange(batch1);
        ResultUpdated?.Invoke(this, new ResultUpdatedEventArgs(query, batch1));

        var batch2 = await GetSecondBatch(query);
        allResults.AddRange(batch2);
        ResultUpdated?.Invoke(this, new ResultUpdatedEventArgs(query, batch2));

        return allResults;
    }
}
```

## 8. PluginInitContext API

The `PluginInitContext` received in Init/InitAsync exposes:

| API | Description |
|-----|-------------|
| `context.API` | `IPublicAPI` interface, main program public API |
| `context.API.ShowMsg(title, text)` | Show message notification |
| `context.API.CopyToClipboard(text)` | Copy text to clipboard |
| `context.API.GetTranslation(key)` | Get translated text (requires IPluginI18n) |
| `context.API.LoadSettingJsonStorage<T>()` | Load plugin settings |
| `context.API.SaveSettingJsonStorage<T>()` | Save plugin settings |
| `context.API.LogInfo(pluginName, msg)` | Write info log |
| `context.API.LogError(pluginName, msg)` | Write error log |
| `context.CurrentPluginMetadata` | Current plugin metadata |

## 9. Project Configuration (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0-windows</TargetFramework>
    <EnableDefaultItems>true</EnableDefaultItems>
    <EnableDefaultEmbeddedResourceItems>false</EnableDefaultEmbeddedResourceItems>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Flow.Launcher.Plugin" Version="4.0.0" />
  </ItemGroup>
</Project>
```

Note: `TargetFramework` must match the current Flow Launcher version (currently .NET 9 based).

## 10. Debug & Deploy

1. **Build**: `dotnet build -c Release`
2. **Deploy**: Copy build output to `%APPDATA%\FlowLauncher\Plugins\<PluginID>\`
3. **Reload**: Type `reload` in Flow Launcher
4. **Logs**: `%APPDATA%\FlowLauncher\Logs\`

## 11. Critical Rules

1. Entry class MUST be named `Main` — Flow Launcher uses reflection to find class named `Main`
2. `plugin.json` `ID` field MUST match the plugin folder name (case-sensitive)
3. `ExecuteFileName` DLL MUST exist in the plugin directory
4. `Version` MUST be 3-segment semantic version (e.g. `1.0.0`, not `1.0`)
5. Put expensive initialization in `Init`/`InitAsync`, NOT in the constructor
6. Prefer `IAsyncPlugin` over `IPlugin` to avoid blocking the UI thread
7. All extension interfaces (IContextMenu, IReloadable, etc.) MUST be implemented in the same class as IPlugin/IAsyncPlugin
8. Language resource files use WPF XAML resource dictionary format with key/value pairs
9. Language resource keys should be prefixed with `flowlauncher_plugin_{pluginname}_`

## 12. i18n Language Resources

Language files go in the `Languages/` directory, named with the language code and `.xaml` suffix:

```xml
<!-- Languages/en.xaml -->
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:s="clr-namespace:System;assembly=mscorlib">
    <s:String x:Key="flowlauncher_plugin_yourplugin_title">Your Plugin</s:String>
    <s:String x:Key="flowlauncher_plugin_yourplugin_description">Description here</s:String>
</ResourceDictionary>
```

Available language codes are defined in [AvailableLanguages.cs](https://github.com/Flow-Launcher/Flow.Launcher/blob/master/Flow.Launcher.Infrastructure/Language/AvailableLanguages.cs).

## 13. References

- Official docs: https://www.flowlauncher.com/docs/#/develop-dotnet-plugins
- API Reference: https://www.flowlauncher.com/docs/#/api-reference
- Sample C# Plugin: https://github.com/Flow-Launcher/Flow.Launcher.Plugin.Calculator
- Plugin Template: https://github.com/Flow-Launcher/Flow.Launcher.Plugin.DotnetTemplate
