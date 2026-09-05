namespace Flow.Launcher.Plugin.Snippets.Views;

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Snippets.Models;
using Flow.Launcher.Plugin.Snippets.Services;
using Flow.Launcher.Plugin.Snippets.Storage;

public record SnippetDisplayItem(string Key, string Value, string Preview, int Score);

public partial class SnippetManagerWindow : Window
{
    private readonly SnippetStore _store;
    private readonly IPublicAPI? _api;
    private string? _selectedKey;
    private bool _isRefreshing;
    private bool _isThemeSubscribed;

    public Func<string, bool>? ConfirmDeleteDialog { get; set; }

    public System.Windows.Controls.Primitives.Selector SnippetsGrid => SnippetsList;

    public SnippetManagerWindow(SnippetStore store, IPublicAPI? api = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _api = api;
        InitializeComponent();

        Loaded += (_, _) => SubscribeThemeChanged();
        Closed += (_, _) => UnsubscribeThemeChanged();

        SourceInitialized += (_, _) =>
        {
            bool isDark = _api?.IsApplicationDarkTheme() ?? false;
            ApplyTitleBarTheme(isDark);
        };

        RefreshGrid();
        EnterAddMode();
    }

    private void SubscribeThemeChanged()
    {
        if (_api != null && !_isThemeSubscribed)
        {
            _api.ActualApplicationThemeChanged += OnActualApplicationThemeChanged;
            _isThemeSubscribed = true;
        }
    }

    private void UnsubscribeThemeChanged()
    {
        if (_api != null && _isThemeSubscribed)
        {
            _api.ActualApplicationThemeChanged -= OnActualApplicationThemeChanged;
            _isThemeSubscribed = false;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;

    internal void ApplyTitleBarTheme(bool isDark)
    {
        var helper = new WindowInteropHelper(this);
        var hwnd = helper.Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        int useDarkMode = isDark ? 1 : 0;
        try
        {
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }
        }
        catch
        {
            // Non-fatal across Windows versions/environments
        }
    }

    private void OnActualApplicationThemeChanged(object sender, ActualApplicationThemeChangedEventArgs e)
    {
        Dispatcher.InvokeAsync(() => ApplyTitleBarTheme(e.IsDark));
    }

    public void RefreshGrid(string? selectKey = null)
    {
        _isRefreshing = true;
        try
        {
            var filter = FilterBox?.Text?.Trim() ?? "";
            var items = _store.Snippets
                .Where(s => string.IsNullOrEmpty(filter) || s.Key.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .Select(s => new SnippetDisplayItem(s.Key, s.Value, SnippetQueryService.FormatPreview(s.Value), s.Score))
                .ToList();

            SnippetsList.ItemsSource = items;

            if (items.Count == 0)
            {
                EmptyStateContainer.Visibility = Visibility.Visible;
                if (string.IsNullOrEmpty(filter))
                {
                    EmptyStateTitle.Text = "No snippets yet";
                    EmptyStateDescription.Text = "Create your first snippet using the editor.";
                }
                else
                {
                    EmptyStateTitle.Text = "No matching snippets";
                    EmptyStateDescription.Text = "Try a different search term or clear the filter.";
                }
            }
            else
            {
                EmptyStateContainer.Visibility = Visibility.Collapsed;
            }

            var targetKey = selectKey ?? _selectedKey;
            if (!string.IsNullOrEmpty(targetKey))
            {
                var matchingItem = items.FirstOrDefault(i => i.Key.Equals(targetKey, StringComparison.OrdinalIgnoreCase));
                if (matchingItem != null)
                {
                    SnippetsList.SelectedItem = matchingItem;
                    EnterEditMode(matchingItem);
                    return;
                }
            }

            // If selected item is no longer in filtered list or no selection
            if (_selectedKey != null && !string.IsNullOrEmpty(filter))
            {
                EnterAddMode();
            }
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    public void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshGrid();
    }

    public void ClearFilterButton_Click(object sender, RoutedEventArgs e)
    {
        FilterBox.Text = "";
    }

    public void NewSnippetButton_Click(object sender, RoutedEventArgs e)
    {
        EnterAddMode();
        KeyBox.Focus();
    }

    public void SnippetsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRefreshing)
        {
            return;
        }

        if (SnippetsList.SelectedItem is SnippetDisplayItem item)
        {
            EnterEditMode(item);
        }
        else
        {
            EnterAddMode();
        }
    }

    public void AddButton_Click(object sender, RoutedEventArgs e)
    {
        HideError();

        var key = KeyBox.Text;
        var value = ValueBox.Text;

        if (!int.TryParse(ScoreBox.Text.Trim(), out var score))
        {
            ShowError("Score must be a valid integer.");
            return;
        }

        try
        {
            _store.Add(new Snippet(key, value, score));
            RefreshGrid();
            EnterAddMode();
        }
        catch (SnippetException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError($"Unexpected error adding snippet: {ex.Message}");
        }
    }

    public void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedKey == null)
        {
            return;
        }

        HideError();

        var newKey = KeyBox.Text;
        var value = ValueBox.Text;

        if (!int.TryParse(ScoreBox.Text.Trim(), out var score))
        {
            ShowError("Score must be a valid integer.");
            return;
        }

        try
        {
            _store.Update(_selectedKey, new Snippet(newKey, value, score));
            RefreshGrid(selectKey: newKey);
        }
        catch (SnippetException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError($"Unexpected error updating snippet: {ex.Message}");
        }
    }

    public void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedKey == null)
        {
            return;
        }

        var keyToDelete = _selectedKey;
        var shouldDelete = ConfirmDeleteDialog != null
            ? ConfirmDeleteDialog(keyToDelete)
            : MessageBox.Show(
                $"Are you sure you want to delete snippet '{keyToDelete}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes;

        if (!shouldDelete)
        {
            return;
        }

        HideError();

        try
        {
            _store.Delete(keyToDelete);
            _selectedKey = null;
            RefreshGrid();
            EnterAddMode();
        }
        catch (SnippetException ex)
        {
            ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ShowError($"Unexpected error deleting snippet: {ex.Message}");
        }
    }

    public void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        HideError();

        if (_selectedKey != null)
        {
            var snippet = _store.Snippets.FirstOrDefault(s => s.Key.Equals(_selectedKey, StringComparison.OrdinalIgnoreCase));
            if (snippet != null)
            {
                KeyBox.Text = snippet.Key;
                ScoreBox.Text = snippet.Score.ToString();
                ValueBox.Text = snippet.Value;
            }
            FormHeader.Text = "Edit snippet";
        }
        else
        {
            EnterAddMode();
        }
    }

    public void EnterAddMode()
    {
        _selectedKey = null;
        SnippetsList.SelectedItem = null;
        FormHeader.Text = "New snippet";
        KeyBox.Text = "";
        ScoreBox.Text = "0";
        ValueBox.Text = "";

        AddButton.Visibility = Visibility.Visible;
        AddButton.IsEnabled = true;
        SaveButton.Visibility = Visibility.Collapsed;
        SaveButton.IsEnabled = false;
        DeleteButton.Visibility = Visibility.Collapsed;
        DeleteButton.IsEnabled = false;
        CancelButton.Visibility = Visibility.Visible;
        CancelButton.IsEnabled = true;

        HideError();
    }

    public void EnterEditMode(SnippetDisplayItem item)
    {
        _selectedKey = item.Key;
        FormHeader.Text = "Edit snippet";
        KeyBox.Text = item.Key;
        ScoreBox.Text = item.Score.ToString();
        ValueBox.Text = item.Value;

        AddButton.Visibility = Visibility.Collapsed;
        AddButton.IsEnabled = false;
        SaveButton.Visibility = Visibility.Visible;
        SaveButton.IsEnabled = true;
        DeleteButton.Visibility = Visibility.Visible;
        DeleteButton.IsEnabled = true;
        CancelButton.Visibility = Visibility.Visible;
        CancelButton.IsEnabled = true;

        HideError();
    }

    public void ResetForm()
    {
        EnterAddMode();
    }

    public void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorTextBlock.Visibility = Visibility.Visible;
        ErrorContainer.Visibility = Visibility.Visible;
    }

    public void HideError()
    {
        ErrorTextBlock.Text = "";
        ErrorTextBlock.Visibility = Visibility.Collapsed;
        ErrorContainer.Visibility = Visibility.Collapsed;
    }
}
