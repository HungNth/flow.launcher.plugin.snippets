namespace Flow.Launcher.Plugin.Snippets.Models;

public class PluginSettings
{
    public const int MinPasteDelayMs = 10;
    public const int MaxPasteDelayMs = 1000;
    public const int DefaultPasteDelayMs = 100;

    private int _pasteDelayMs = DefaultPasteDelayMs;

    public bool AutoPasteEnabled { get; set; } = true;

    public int PasteDelayMs
    {
        get => _pasteDelayMs;
        set => _pasteDelayMs = ClampPasteDelay(value);
    }

    public static int ClampPasteDelay(int value)
    {
        if (value < MinPasteDelayMs)
        {
            return MinPasteDelayMs;
        }

        if (value > MaxPasteDelayMs)
        {
            return MaxPasteDelayMs;
        }

        return value;
    }
}
