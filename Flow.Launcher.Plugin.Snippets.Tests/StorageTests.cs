namespace Flow.Launcher.Plugin.Snippets.Tests;

using System.IO;
using Flow.Launcher.Plugin.Snippets.Models;
using Flow.Launcher.Plugin.Snippets.Storage;

[TestClass]
public class StorageTests
{
    private string _tempDir = null!;

    [TestInitialize]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SnippetTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [TestMethod]
    public void Load_MissingFile_ProducesEmptySnippetsAndNoError()
    {
        var store = new SnippetStore(_tempDir);
        store.Load();

        Assert.AreEqual(0, store.Snippets.Count);
        Assert.IsNull(store.LastError);
        Assert.IsFalse(File.Exists(store.FilePath));
    }

    [TestMethod]
    public void Load_ValidVersion1Json_LoadsAllSnippetsWithExactValuesAndScores()
    {
        var json = """
{
  "version": 1,
  "snippets": [
    {
      "key": "git-c",
      "value": "git commit -m \"test\"",
      "score": 100
    },
    {
      "key": "hello",
      "value": "world",
      "score": 0
    }
  ]
}
""";
        var store = new SnippetStore(_tempDir);
        File.WriteAllText(store.FilePath, json);

        store.Load();

        Assert.IsNull(store.LastError);
        Assert.AreEqual(2, store.Snippets.Count);
        Assert.AreEqual("git-c", store.Snippets[0].Key);
        Assert.AreEqual("git commit -m \"test\"", store.Snippets[0].Value);
        Assert.AreEqual(100, store.Snippets[0].Score);
        Assert.AreEqual("hello", store.Snippets[1].Key);
        Assert.AreEqual("world", store.Snippets[1].Value);
        Assert.AreEqual(0, store.Snippets[1].Score);
    }

    [TestMethod]
    public void Load_ExactMultilinePreservation_PreservesNewlinesAndWhitespace()
    {
        var multilineValue = "  line 1  \r\n\tline 2\n  line 3\r\n  ";
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            version = 1,
            snippets = new[]
            {
                new { key = "multi", value = multilineValue, score = 5 }
            }
        });
        var store = new SnippetStore(_tempDir);
        File.WriteAllText(store.FilePath, json);

        store.Load();

        Assert.IsNull(store.LastError);
        Assert.AreEqual(1, store.Snippets.Count);
        Assert.AreEqual(multilineValue, store.Snippets[0].Value);
    }

    [TestMethod]
    public void Load_UnsupportedVersion_SetsErrorAndDoesNotOverwrite()
    {
        var json = """
{
  "version": 2,
  "snippets": [
    { "key": "k", "value": "v", "score": 1 }
  ]
}
""";
        var store = new SnippetStore(_tempDir);
        File.WriteAllText(store.FilePath, json);

        store.Load();

        Assert.IsNotNull(store.LastError);
        StringAssert.Contains(store.LastError, "version");
        Assert.AreEqual(0, store.Snippets.Count);
        Assert.AreEqual(json, File.ReadAllText(store.FilePath));
    }

    [TestMethod]
    [DataRow("""{"version": 1, "snippets": [ {"key": "", "value": "v", "score": 1} ]}""", "Empty key")]
    [DataRow("""{"version": 1, "snippets": [ {"key": "   ", "value": "v", "score": 1} ]}""", "Whitespace key")]
    [DataRow("""{"version": 1, "snippets": [ {"key": "k", "value": "", "score": 1} ]}""", "Empty value")]
    [DataRow("""{"version": 1, "snippets": [ {"key": "k", "value": "v", "score": "not-an-int"} ]}""", "Non-integer score")]
    [DataRow("""{"version": 1, "snippets": [ {"score": 1} ]}""", "Missing key and value")]
    public void Load_InvalidFields_SetsErrorAndKeepsFileUnchanged(string invalidJson, string description)
    {
        var store = new SnippetStore(_tempDir);
        File.WriteAllText(store.FilePath, invalidJson);

        store.Load();

        Assert.IsNotNull(store.LastError, $"Expected error for {description}");
        Assert.AreEqual(0, store.Snippets.Count);
        Assert.AreEqual(invalidJson, File.ReadAllText(store.FilePath));
    }

    [TestMethod]
    public void Load_DuplicateKeysCaseInsensitive_SetsErrorAndIdentifiesKey()
    {
        var json = """
{
  "version": 1,
  "snippets": [
    { "key": "TestKey", "value": "val1", "score": 1 },
    { "key": "testkey", "value": "val2", "score": 2 }
  ]
}
""";
        var store = new SnippetStore(_tempDir);
        File.WriteAllText(store.FilePath, json);

        store.Load();

        Assert.IsNotNull(store.LastError);
        StringAssert.Contains(store.LastError.ToLowerInvariant(), "duplicate");
        StringAssert.Contains(store.LastError.ToLowerInvariant(), "testkey");
        Assert.AreEqual(0, store.Snippets.Count);
        Assert.AreEqual(json, File.ReadAllText(store.FilePath));
    }

    [TestMethod]
    public void Reload_MalformedFile_KeepsPriorValidSnapshotAndSetsError()
    {
        var validJson = """
{
  "version": 1,
  "snippets": [
    { "key": "valid", "value": "val", "score": 10 }
  ]
}
""";
        var store = new SnippetStore(_tempDir);
        File.WriteAllText(store.FilePath, validJson);
        store.Load();
        Assert.IsNull(store.LastError);
        Assert.AreEqual(1, store.Snippets.Count);

        // Now externally corrupt the file
        var malformedJson = "{ invalid-json ";
        File.WriteAllText(store.FilePath, malformedJson);

        store.Reload();

        Assert.IsNotNull(store.LastError);
        Assert.AreEqual(1, store.Snippets.Count, "Prior snapshot must be retained");
        Assert.AreEqual("valid", store.Snippets[0].Key);
        Assert.AreEqual(malformedJson, File.ReadAllText(store.FilePath), "Corrupt file must remain untouched");
    }

    [TestMethod]
    public void Export_ValidLibrary_WritesSnippetDocumentJsonToDestinationPath()
    {
        var store = new SnippetStore(_tempDir);
        store.Add(new Snippet("k1", "val1", 5));
        store.Add(new Snippet("k2", "val2", 10));

        var exportFile = Path.Combine(_tempDir, "export", "backup.json");
        store.Export(exportFile);

        Assert.IsTrue(File.Exists(exportFile));
        var content = File.ReadAllText(exportFile);
        StringAssert.Contains(content, "\"version\": 1");
        StringAssert.Contains(content, "\"k1\"");
        StringAssert.Contains(content, "\"val1\"");
        StringAssert.Contains(content, "\"k2\"");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Export_NullOrEmptyPath_ThrowsArgumentException()
    {
        var store = new SnippetStore(_tempDir);
        store.Export("");
    }

    [TestMethod]
    public void Import_ValidFile_ReplacesAllSnippetsAndCreatesBackup()
    {
        var store = new SnippetStore(_tempDir);
        store.Add(new Snippet("oldKey", "oldValue", 1));

        var importFile = Path.Combine(_tempDir, "external.json");
        var importJson = """
{
  "version": 1,
  "snippets": [
    { "key": "new1", "value": "val1", "score": 10 },
    { "key": "new2", "value": "val2", "score": 20 }
  ]
}
""";
        File.WriteAllText(importFile, importJson);

        store.Import(importFile);

        Assert.AreEqual(2, store.Snippets.Count);
        Assert.AreEqual("new2", store.Snippets[0].Key, "Snippets should be ordered by score desc");
        Assert.AreEqual("new1", store.Snippets[1].Key);
        Assert.IsTrue(File.Exists(store.BackupFilePath), "Backup file must be created on replace");
        StringAssert.Contains(File.ReadAllText(store.BackupFilePath), "oldKey");
    }

    [TestMethod]
    public void Import_MalformedJson_ThrowsSnippetStorageExceptionAndPreservesCurrentSnippets()
    {
        var store = new SnippetStore(_tempDir);
        store.Add(new Snippet("keepMe", "keepValue", 5));

        var importFile = Path.Combine(_tempDir, "broken.json");
        File.WriteAllText(importFile, "{ invalid json ");

        Assert.ThrowsException<SnippetStorageException>(() => store.Import(importFile));

        Assert.AreEqual(1, store.Snippets.Count);
        Assert.AreEqual("keepMe", store.Snippets[0].Key);
    }

    [TestMethod]
    public void Import_NonExistentFile_ThrowsFileNotFoundException()
    {
        var store = new SnippetStore(_tempDir);
        var missingFile = Path.Combine(_tempDir, "does-not-exist.json");

        Assert.ThrowsException<FileNotFoundException>(() => store.Import(missingFile));
    }
}
