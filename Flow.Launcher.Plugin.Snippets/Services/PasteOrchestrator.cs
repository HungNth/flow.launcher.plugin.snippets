namespace Flow.Launcher.Plugin.Snippets.Services;

using System;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Snippets.Models;

public class PasteOrchestrator
{
    private readonly IPublicAPI _api;
    private readonly IPasteSimulator _simulator;
    private readonly Func<int, Task> _delayFunc;

    public PasteOrchestrator(
        IPublicAPI api,
        IPasteSimulator? simulator = null,
        Func<int, Task>? delayFunc = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _simulator = simulator ?? new WindowsPasteSimulator();
        _delayFunc = delayFunc ?? (ms => Task.Delay(ms));
    }

    public bool ExecuteSelection(string value, PluginSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            _api.CopyToClipboard(value);
        }
        catch (Exception ex)
        {
            _api.LogError("Snippets", $"Failed to copy snippet to clipboard: {ex.Message}");
            _api.ShowMsgError("Clipboard Error", "Failed to copy snippet to clipboard.");
            return false;
        }

        if (!settings.AutoPasteEnabled)
        {
            return true;
        }

        var delay = settings.PasteDelayMs;
        _ = Task.Run(async () =>
        {
            try
            {
                await _delayFunc(delay).ConfigureAwait(false);
                var success = _simulator.SimulatePaste();
                if (!success)
                {
                    _api.LogWarn("Snippets", "Paste simulation failed or returned zero events. Target application may require elevated privileges.");
                    _api.ShowMsg("Snippets", "Snippet copied to clipboard, but could not be pasted automatically.");
                }
            }
            catch (Exception ex)
            {
                _api.LogError("Snippets", $"Unexpected error during paste simulation: {ex.Message}");
                _api.ShowMsg("Snippets", "Snippet copied to clipboard, but could not be pasted automatically.");
            }
        });

        return true;
    }
}
