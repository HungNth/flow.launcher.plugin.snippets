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
}
