namespace Flow.Launcher.Plugin.Snippets.Views;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Snippets.Models;
using Flow.Launcher.Plugin.Snippets.Storage;
public partial class SettingsControl : UserControl
{
    private readonly SnippetStore _store;
    private readonly PluginSettings _settings;
    private readonly IPublicAPI? _api;
    private SnippetManagerWindow? _window;

    public SettingsControl(SnippetStore store, PluginSettings? settings = null, IPublicAPI? api = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _settings = settings ?? new PluginSettings();
        _api = api;
        InitializeComponent();
        InitializeSettingsState();
    }

    public SettingsControl(SnippetStore store, IPublicAPI? api) : this(store, null, api)
    {
    }


    private void InitializeSettingsState()
    {
        AutoPasteCheckBox.IsChecked = _settings.AutoPasteEnabled;
        PasteDelayTextBox.Text = _settings.PasteDelayMs.ToString();
        DataObject.AddPastingHandler(PasteDelayTextBox, PasteDelayTextBox_Pasting);
    }

    public void AutoPasteCheckBox_Click(object sender, RoutedEventArgs e)
    {
        _settings.AutoPasteEnabled = AutoPasteCheckBox.IsChecked ?? true;
        _api?.SaveSettingJsonStorage<PluginSettings>();
    }

    public void PasteDelayTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(PasteDelayTextBox.Text.Trim(), out var parsedValue))
        {
            _settings.PasteDelayMs = parsedValue;
        }

        PasteDelayTextBox.Text = _settings.PasteDelayMs.ToString();
        _api?.SaveSettingJsonStorage<PluginSettings>();
    }

    public void PasteDelayTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsAsciiDigit);
    }

    private void PasteDelayTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            var text = e.DataObject.GetData(DataFormats.Text) as string;
            if (string.IsNullOrEmpty(text) || !text.All(char.IsAsciiDigit))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
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
