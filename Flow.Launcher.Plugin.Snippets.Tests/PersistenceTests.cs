namespace Flow.Launcher.Plugin.Snippets.Tests;

using System.IO;
using System.Text.Json;
using Flow.Launcher.Plugin.Snippets.Models;
using Flow.Launcher.Plugin.Snippets.Storage;

[TestClass]
public class PersistenceTests
{
    private string _tempDir = null!;

    [TestInitialize]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SnippetPersistTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            // Reset readonly attributes if any before deleting
            foreach (var f in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(f, FileAttributes.Normal);
            }
            Directory.Delete(_tempDir, true);
        }
    }

    [TestMethod]
    public void Add_ImmediateSnapshotPublication_AndDeterministicOrdering()
    {
        var store = new SnippetStore(_tempDir);
        store.Load();

        store.Add(new Snippet("beta", "val2", 10));
        store.Add(new Snippet("alpha", "val1", 100));
        store.Add(new Snippet("gamma", "val3", 10));

        // In-memory snapshot published immediately
        Assert.AreEqual(3, store.Snippets.Count);

        // Deterministic ordering: score desc, key asc
        Assert.AreEqual("alpha", store.Snippets[0].Key);
        Assert.AreEqual(100, store.Snippets[0].Score);
        Assert.AreEqual("beta", store.Snippets[1].Key);
        Assert.AreEqual(10, store.Snippets[1].Score);
        Assert.AreEqual("gamma", store.Snippets[2].Key);
        Assert.AreEqual(10, store.Snippets[2].Score);

        // Verify JSON file on disk
        Assert.IsTrue(File.Exists(store.FilePath));
        using var doc = JsonDocument.Parse(File.ReadAllText(store.FilePath));
        var root = doc.RootElement;
        Assert.AreEqual(1, root.GetProperty("version").GetInt32());
        var snippets = root.GetProperty("snippets");
        Assert.AreEqual(3, snippets.GetArrayLength());

        // Check property names in serialized json: only version, snippets, key, value, score
        var first = snippets[0];
        Assert.AreEqual("alpha", first.GetProperty("key").GetString());
        Assert.AreEqual("val1", first.GetProperty("value").GetString());
        Assert.AreEqual(100, first.GetProperty("score").GetInt32());
    }

    [TestMethod]
    public void Save_CreatesBackupFileOnSubsequentMutation()
    {
        var store = new SnippetStore(_tempDir);
        store.Load();

        store.Add(new Snippet("initial", "first version", 0));
        Assert.IsTrue(File.Exists(store.FilePath));
        Assert.IsFalse(File.Exists(store.BackupFilePath));

        var initialContent = File.ReadAllText(store.FilePath);

        store.Add(new Snippet("second", "second version", 5));
        Assert.IsTrue(File.Exists(store.BackupFilePath), "Backup file snippets.json.bak must exist");
        Assert.AreEqual(initialContent, File.ReadAllText(store.BackupFilePath));
    }

    [TestMethod]
    public void Update_ModifiesSnippet_AndSavesAtomically()
    {
        var store = new SnippetStore(_tempDir);
        store.Load();
        store.Add(new Snippet("old-key", "old value", 10));

        store.Update("old-key", new Snippet("new-key", "updated value", 20));

        Assert.AreEqual(1, store.Snippets.Count);
        Assert.AreEqual("new-key", store.Snippets[0].Key);
        Assert.AreEqual("updated value", store.Snippets[0].Value);
        Assert.AreEqual(20, store.Snippets[0].Score);

        // Verify reloaded store observes update
        var reloadedStore = new SnippetStore(_tempDir);
        reloadedStore.Load();
        Assert.AreEqual(1, reloadedStore.Snippets.Count);
        Assert.AreEqual("new-key", reloadedStore.Snippets[0].Key);
    }

    [TestMethod]
    public void Delete_RemovesSnippet_AndSavesAtomically()
    {
        var store = new SnippetStore(_tempDir);
        store.Load();
        store.Add(new Snippet("k1", "v1", 10));
        store.Add(new Snippet("k2", "v2", 20));

        store.Delete("k1");

        Assert.AreEqual(1, store.Snippets.Count);
        Assert.AreEqual("k2", store.Snippets[0].Key);
        Assert.IsTrue(File.Exists(store.BackupFilePath));
    }

    [TestMethod]
    public void Save_WriteFails_RetainsPreviousSnapshotAndPreviousFile()
    {
        var store = new SnippetStore(_tempDir);
        store.Load();
        store.Add(new Snippet("existing", "usable content", 50));

        var validContent = File.ReadAllText(store.FilePath);
        var snapshotBeforeFailure = store.Snippets;

        // Make the directory read-only or lock snippets.json to force write/replace failure
        using (var lockStream = new FileStream(store.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.ThrowsException<SnippetStorageException>(() =>
            {
                store.Add(new Snippet("failed-key", "should not save", 0));
            });
        }

        // Previous snapshot remains published
        Assert.AreEqual(1, store.Snippets.Count);
        Assert.AreEqual("existing", store.Snippets[0].Key);
        Assert.AreSame(snapshotBeforeFailure, store.Snippets);

        // File remains unchanged
        Assert.AreEqual(validContent, File.ReadAllText(store.FilePath));
    }

    [TestMethod]
    public void Add_DuplicateKeyCaseInsensitive_ThrowsValidationException()
    {
        var store = new SnippetStore(_tempDir);
        store.Load();
        store.Add(new Snippet("KeyA", "v1", 0));

        var ex = Assert.ThrowsException<SnippetValidationException>(() =>
        {
            store.Add(new Snippet("keya", "v2", 0));
        });

        StringAssert.Contains(ex.Message.ToLowerInvariant(), "duplicate");
        StringAssert.Contains(ex.Message.ToLowerInvariant(), "keya");
    }
}
