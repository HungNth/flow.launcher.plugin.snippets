namespace Flow.Launcher.Plugin.Snippets;

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Snippets.Services;
using Flow.Launcher.Plugin.Snippets.Storage;

public class Main : IPlugin, IReloadable, ISettingProvider
{
    public const string PluginName = "Snippets";

    private PluginInitContext _context = null!;
    private SnippetStore _store = null!;

    public SnippetStore Store => _store;

    public void Init(PluginInitContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        var settingsDir = !string.IsNullOrWhiteSpace(context.CurrentPluginMetadata?.PluginSettingsDirectoryPath)
            ? context.CurrentPluginMetadata.PluginSettingsDirectoryPath
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FlowLauncher", "Settings", "Plugins", "Flow.Launcher.Plugin.Snippets");

        _store = new SnippetStore(settingsDir);
        _store.Load();

        if (_store.LastError != null)
        {
            _context.API.LogError(PluginName, $"Initial load error: {_store.LastError}");
        }
    }

    public List<Result> Query(Query query)
    {
        return SnippetQueryService.Query(
            query,
            _store.Snippets,
            (q, target) => _context.API.FuzzySearch(q, target),
            CopySnippetToClipboard,
            _store.LastError);
    }

    public void ReloadData()
    {
        _store.Reload();

        if (_store.LastError != null)
        {
            _context.API.LogError(PluginName, $"Failed to reload snippets: {_store.LastError}");
            _context.API.ShowMsgError("Snippets Reload Failed", _store.LastError);
        }
        else
        {
            _context.API.LogInfo(PluginName, "Snippets reloaded successfully.");
            _context.API.ShowMsg("Snippets", $"Reloaded {_store.Snippets.Count} snippets.");
        }
    }

    public Control CreateSettingPanel()
    {
        return new Views.SettingsControl(_store, _context?.API);
    }

    private bool CopySnippetToClipboard(string value)
    {
        try
        {
            _context.API.CopyToClipboard(value);
            return true;
        }
        catch (Exception ex)
        {
            // Do not include snippet value in logs or error messages (may contain sensitive data)
            _context.API.LogError(PluginName, $"Failed to copy snippet to clipboard: {ex.Message}");
            _context.API.ShowMsgError("Clipboard Error", "Failed to copy snippet to clipboard.");
            return false;
        }
    }
}
