namespace Flow.Launcher.Plugin.Snippets.Models;

public record SnippetDocument(int Version, IReadOnlyList<Snippet> Snippets);
