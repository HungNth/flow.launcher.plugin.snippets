namespace Flow.Launcher.Plugin.Snippets.Storage;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Flow.Launcher.Plugin.Snippets.Models;

public class SnippetStore
{
    public const int CurrentVersion = 1;
    public const string FileName = "snippets.json";
    public const string BackupFileName = "snippets.json.bak";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly object _syncLock = new();

    public string SettingsDirectory { get; }
    public string FilePath => Path.Combine(SettingsDirectory, FileName);
    public string BackupFilePath => Path.Combine(SettingsDirectory, BackupFileName);

    public IReadOnlyList<Snippet> Snippets { get; private set; } = ImmutableArray<Snippet>.Empty;
    public string? LastError { get; private set; }

    public SnippetStore(string settingsDirectory)
    {
        SettingsDirectory = settingsDirectory ?? throw new ArgumentNullException(nameof(settingsDirectory));
    }

    public void Load()
    {
        lock (_syncLock)
        {
            if (!File.Exists(FilePath))
            {
                Snippets = ImmutableArray<Snippet>.Empty;
                LastError = null;
                return;
            }

            try
            {
                using var stream = File.OpenRead(FilePath);
                using var doc = JsonDocument.Parse(stream);

                var (valid, error, loadedSnippets) = ValidateAndParseDocument(doc.RootElement, FilePath);
                if (!valid)
                {
                    LastError = error;
                    return;
                }

                Snippets = loadedSnippets!.ToImmutableArray();
                LastError = null;
            }
            catch (JsonException ex)
            {
                LastError = $"Malformed JSON in '{FilePath}': {ex.Message}";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                LastError = $"Failed to read '{FilePath}': {ex.Message}";
            }
        }
    }

    public void Reload()
    {
        Load();
    }

    public void Export(string destinationPath)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException("Destination path cannot be null or whitespace.", nameof(destinationPath));
        }

        lock (_syncLock)
        {
            var destinationDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            var tempPath = Path.Combine(
                string.IsNullOrEmpty(destinationDir) ? SettingsDirectory : destinationDir,
                $".export.{Guid.NewGuid():N}.tmp");

            try
            {
                var doc = new SnippetDocument(CurrentVersion, Snippets);

                using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    JsonSerializer.Serialize(fs, doc, JsonOptions);
                    fs.Flush(true);
                }

                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }
                File.Move(tempPath, destinationPath);
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // Best effort cleanup
                }

                throw new SnippetStorageException($"Failed to export snippets to '{destinationPath}': {ex.Message}", ex);
            }
        }
    }

    public void Import(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("Source path cannot be null or whitespace.", nameof(sourcePath));
        }

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Import file '{sourcePath}' was not found.", sourcePath);
        }

        lock (_syncLock)
        {
            List<Snippet> loadedSnippets;
            try
            {
                using var stream = File.OpenRead(sourcePath);
                using var doc = JsonDocument.Parse(stream);

                var (valid, error, list) = ValidateAndParseDocument(doc.RootElement, sourcePath);
                if (!valid || list == null)
                {
                    throw new SnippetStorageException($"Invalid snippet document in '{sourcePath}': {error}");
                }

                loadedSnippets = list;
            }
            catch (SnippetStorageException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SnippetStorageException($"Failed to read or parse import file '{sourcePath}': {ex.Message}", ex);
            }

            SaveCandidateList(loadedSnippets);
        }
    }

    public void Add(Snippet snippet)
    {
        lock (_syncLock)
        {
            var validSnippet = ValidateSnippet(snippet);

            if (Snippets.Any(s => string.Equals(s.Key, validSnippet.Key, StringComparison.OrdinalIgnoreCase)))
            {
                throw new SnippetValidationException($"Duplicate snippet key detected: '{validSnippet.Key}'. Keys must be unique.");
            }

            var candidateList = Snippets.Concat(new[] { validSnippet }).ToList();
            SaveCandidateList(candidateList);
        }
    }

    public void Update(string originalKey, Snippet updated)
    {
        lock (_syncLock)
        {
            if (string.IsNullOrWhiteSpace(originalKey))
            {
                throw new SnippetValidationException("Original snippet key cannot be empty.");
            }

            var validUpdated = ValidateSnippet(updated);

            var existingIndex = -1;
            for (var i = 0; i < Snippets.Count; i++)
            {
                if (string.Equals(Snippets[i].Key, originalKey, StringComparison.OrdinalIgnoreCase))
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex < 0)
            {
                throw new SnippetValidationException($"Snippet with key '{originalKey}' was not found.");
            }

            // If key changed, verify no collision with other snippets
            if (!string.Equals(validUpdated.Key, originalKey, StringComparison.OrdinalIgnoreCase) &&
                Snippets.Where((_, idx) => idx != existingIndex).Any(s => string.Equals(s.Key, validUpdated.Key, StringComparison.OrdinalIgnoreCase)))
            {
                throw new SnippetValidationException($"Duplicate snippet key detected: '{validUpdated.Key}'. Keys must be unique.");
            }

            var candidateList = Snippets.ToList();
            candidateList[existingIndex] = validUpdated;
            SaveCandidateList(candidateList);
        }
    }

    public void Delete(string key)
    {
        lock (_syncLock)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new SnippetValidationException("Key to delete cannot be empty.");
            }

            var existingIndex = -1;
            for (var i = 0; i < Snippets.Count; i++)
            {
                if (string.Equals(Snippets[i].Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex < 0)
            {
                throw new SnippetValidationException($"Snippet with key '{key}' was not found.");
            }

            var candidateList = Snippets.ToList();
            candidateList.RemoveAt(existingIndex);
            SaveCandidateList(candidateList);
        }
    }

    private static Snippet ValidateSnippet(Snippet snippet)
    {
        ArgumentNullException.ThrowIfNull(snippet);

        if (string.IsNullOrWhiteSpace(snippet.Key))
        {
            throw new SnippetValidationException("Snippet key cannot be empty or whitespace.");
        }

        if (string.IsNullOrEmpty(snippet.Value))
        {
            throw new SnippetValidationException("Snippet value cannot be empty.");
        }

        return snippet with { Key = snippet.Key.Trim() };
    }

    private void SaveCandidateList(List<Snippet> candidates)
    {
        var ordered = candidates
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tempPath = Path.Combine(SettingsDirectory, $"snippets.json.{Guid.NewGuid():N}.tmp");

        try
        {
            Directory.CreateDirectory(SettingsDirectory);

            var doc = new SnippetDocument(CurrentVersion, ordered);

            using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(fs, doc, JsonOptions);
                fs.Flush(true);
            }

            if (File.Exists(FilePath))
            {
                File.Replace(tempPath, FilePath, BackupFilePath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, FilePath);
            }

            Snippets = ordered.ToImmutableArray();
            LastError = null;
        }
        catch (Exception ex)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Best effort cleanup
            }

            throw new SnippetStorageException($"Failed to persist snippets to '{FilePath}': {ex.Message}", ex);
        }
    }

    private static (bool Valid, string? Error, List<Snippet>? Snippets) ValidateAndParseDocument(JsonElement root, string filePath)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return (false, $"Invalid JSON structure in '{filePath}': root must be an object.", null);
        }

        if (!root.TryGetProperty("version", out var versionElem) ||
            versionElem.ValueKind != JsonValueKind.Number ||
            !versionElem.TryGetInt32(out var version))
        {
            return (false, $"Missing or invalid 'version' in '{filePath}'.", null);
        }

        if (version != CurrentVersion)
        {
            return (false, $"Unsupported snippets.json version: {version} in '{filePath}'. Only version {CurrentVersion} is supported.", null);
        }

        if (!root.TryGetProperty("snippets", out var snippetsElem) ||
            snippetsElem.ValueKind != JsonValueKind.Array)
        {
            return (false, $"Missing or invalid 'snippets' array in '{filePath}'.", null);
        }

        var list = new List<Snippet>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in snippetsElem.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                return (false, $"Snippet element in '{filePath}' must be an object.", null);
            }

            if (!item.TryGetProperty("key", out var keyElem) ||
                keyElem.ValueKind != JsonValueKind.String)
            {
                return (false, $"Snippet in '{filePath}' has missing or non-string 'key'.", null);
            }

            var key = keyElem.GetString();
            if (string.IsNullOrWhiteSpace(key))
            {
                return (false, $"Snippet in '{filePath}' has an empty or whitespace-only 'key'.", null);
            }

            var trimmedKey = key.Trim();

            if (!item.TryGetProperty("value", out var valElem) ||
                valElem.ValueKind != JsonValueKind.String)
            {
                return (false, $"Snippet in '{filePath}' has missing or non-string 'value'.", null);
            }

            var value = valElem.GetString();
            if (string.IsNullOrEmpty(value))
            {
                return (false, $"Snippet in '{filePath}' has an empty 'value'.", null);
            }

            if (!item.TryGetProperty("score", out var scoreElem) ||
                scoreElem.ValueKind != JsonValueKind.Number ||
                !scoreElem.TryGetInt32(out var score))
            {
                return (false, $"Snippet in '{filePath}' has missing or non-integer 'score'.", null);
            }

            if (!seenKeys.Add(trimmedKey))
            {
                return (false, $"Duplicate snippet key detected: '{trimmedKey}' in '{filePath}'. Keys must be unique.", null);
            }

            list.Add(new Snippet(trimmedKey, value, score));
        }

        return (true, null, list);
    }
}
