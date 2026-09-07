namespace Flow.Launcher.Plugin.Snippets.Views;

using System;
using System.Windows;
using System.Windows.Controls;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Snippets.Storage;

public partial class SettingsControl : UserControl
{
    private readonly SnippetStore _store;
    private readonly IPublicAPI? _api;
    private SnippetManagerWindow? _window;

    public SettingsControl(SnippetStore store, IPublicAPI? api = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _api = api;
        InitializeComponent();
    }

    public void ManageSnippetsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_window != null && _window.IsLoaded)
        {
            if (_window.WindowState == WindowState.Minimized)
            {
                _window.WindowState = WindowState.Normal;
            }
            _window.Activate();
            return;
        }

        _window = new SnippetManagerWindow(_store, _api);
        _window.Closed += (_, _) => _window = null;
        _window.Show();
    }

    public void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Export Snippets to JSON",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            FileName = "snippets-backup.json",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            ExportSnippets(dialog.FileName);
        }
    }

    public void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Import Snippets from JSON",
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog() == true)
        {
            var confirm = MessageBox.Show(
                "This will replace all your current snippets with the snippets from the selected file.\n\nDo you want to continue?",
                "Confirm Snippets Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                ImportSnippets(dialog.FileName);
            }
        }
    }

    public bool ExportSnippets(string destinationPath)
    {
        try
        {
            _store.Export(destinationPath);
            NotifySuccess("Snippets Exported", $"Successfully exported {_store.Snippets.Count} snippets.");
            return true;
        }
        catch (Exception ex)
        {
            NotifyError("Export Failed", $"Failed to export snippets: {ex.Message}");
            return false;
        }
    }

    public bool ImportSnippets(string sourcePath)
    {
        try
        {
            _store.Import(sourcePath);
            NotifySuccess("Snippets Imported", $"Successfully imported {_store.Snippets.Count} snippets.");
            return true;
        }
        catch (Exception ex)
        {
            NotifyError("Import Failed", $"Failed to import snippets: {ex.Message}");
            return false;
        }
    }

    private void NotifySuccess(string title, string message)
    {
        if (_api != null)
        {
            _api.ShowMsg(title, message);
        }
        else
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void NotifyError(string title, string message)
    {
        if (_api != null)
        {
            _api.ShowMsgError(title, message);
        }
        else
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
