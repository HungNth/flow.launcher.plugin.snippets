namespace Flow.Launcher.Plugin.Snippets.Tests;

using System.Text.Json;
using Flow.Launcher.Plugin.Snippets.Models;

[TestClass]
public class PluginSettingsTests
{
    [TestMethod]
    public void Defaults_AreAutoPasteEnabledTrue_AndPasteDelay100()
    {
        var settings = new PluginSettings();

        Assert.IsTrue(settings.AutoPasteEnabled);
        Assert.AreEqual(100, settings.PasteDelayMs);
    }

    [TestMethod]
    public void PasteDelayMs_ClampsValues_BelowMinimum()
    {
        var settings = new PluginSettings
        {
            PasteDelayMs = 0
        };

        Assert.AreEqual(PluginSettings.MinPasteDelayMs, settings.PasteDelayMs);

        settings.PasteDelayMs = -50;
        Assert.AreEqual(PluginSettings.MinPasteDelayMs, settings.PasteDelayMs);
    }

    [TestMethod]
    public void PasteDelayMs_ClampsValues_AboveMaximum()
    {
        var settings = new PluginSettings
        {
            PasteDelayMs = 2000
        };

        Assert.AreEqual(PluginSettings.MaxPasteDelayMs, settings.PasteDelayMs);
    }

    [TestMethod]
    public void PasteDelayMs_AllowsValidRange()
    {
        var settings = new PluginSettings
        {
            PasteDelayMs = 250
        };

        Assert.AreEqual(250, settings.PasteDelayMs);
    }

    [TestMethod]
    public void Serialization_RoundTripsAccurately()
    {
        var original = new PluginSettings
        {
            AutoPasteEnabled = false,
            PasteDelayMs = 300
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PluginSettings>(json);

        Assert.IsNotNull(deserialized);
        Assert.IsFalse(deserialized.AutoPasteEnabled);
        Assert.AreEqual(300, deserialized.PasteDelayMs);
    }
}
