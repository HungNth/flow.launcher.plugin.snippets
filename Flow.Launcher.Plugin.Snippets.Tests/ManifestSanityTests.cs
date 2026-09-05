namespace Flow.Launcher.Plugin.Snippets.Tests;

using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

[TestClass]
public class ManifestSanityTests
{
    private static string GetProjectDirectory()
    {
        var baseDir = AppContext.BaseDirectory;
        // Walk up to find Flow.Launcher.Plugin.Snippets directory
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Flow.Launcher.Plugin.Snippets.sln")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new DirectoryNotFoundException("Could not find solution root directory.");
        }

        return Path.Combine(dir.FullName, "Flow.Launcher.Plugin.Snippets");
    }

    [TestMethod]
    public void Manifest_SanityChecks_PassAllRequirements()
    {
        var projectDir = GetProjectDirectory();
        var manifestPath = Path.Combine(projectDir, "plugin.json");

        Assert.IsTrue(File.Exists(manifestPath), $"plugin.json must exist at '{manifestPath}'");

        var json = File.ReadAllText(manifestPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Required fields
        Assert.IsTrue(root.TryGetProperty("ID", out var idElem) && !string.IsNullOrWhiteSpace(idElem.GetString()), "ID is required");
        var id = idElem.GetString()!;
        Assert.AreNotEqual("4FD15DF08F7146B3AC36AB5F143D8058", id, "Must not reuse retired plugin ID");
        Assert.IsTrue(Guid.TryParse(id, out _), "ID must be a valid GUID");

        Assert.IsTrue(root.TryGetProperty("ActionKeyword", out var kwElem) && kwElem.GetString() == "sp", "ActionKeyword must be 'sp'");
        Assert.IsTrue(root.TryGetProperty("Name", out var nameElem) && nameElem.GetString() == "Snippets", "Name must be 'Snippets'");
        Assert.IsTrue(root.TryGetProperty("Language", out var langElem) && langElem.GetString() == "csharp", "Language must be 'csharp'");

        Assert.IsTrue(root.TryGetProperty("Version", out var verElem) && !string.IsNullOrWhiteSpace(verElem.GetString()), "Version is required");
        var version = verElem.GetString()!;
        Assert.IsTrue(Regex.IsMatch(version, @"^\d+\.\d+\.\d+$"), "Version must be 3-segment semver (e.g. 1.0.0)");

        Assert.IsTrue(root.TryGetProperty("ExecuteFileName", out var execElem) && execElem.GetString() == "Flow.Launcher.Plugin.Snippets.dll", "ExecuteFileName must match DLL");

        Assert.IsTrue(root.TryGetProperty("IcoPath", out var icoElem) && !string.IsNullOrWhiteSpace(icoElem.GetString()), "IcoPath is required");
        var icoPath = Path.Combine(projectDir, icoElem.GetString()!);
        Assert.IsTrue(File.Exists(icoPath), $"Icon file must exist at '{icoPath}'");
    }
}
