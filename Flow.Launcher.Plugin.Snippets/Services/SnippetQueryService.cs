namespace Flow.Launcher.Plugin.Snippets.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.SharedModels;
using Flow.Launcher.Plugin.Snippets.Models;

public static class SnippetQueryService
{
    public const string IconPath = "Images\\Snippets.png";

    public static List<Result> Query(
        Query query,
        IReadOnlyList<Snippet> snippets,
        Func<string, string, MatchResult> fuzzySearch,
        Func<string, bool>? copyAction = null,
        string? loadError = null)
    {
        if (loadError != null && snippets.Count == 0)
        {
            return
            [
                new Result
                {
                    Title = "Snippet Library Error",
                    SubTitle = loadError,
                    IcoPath = IconPath,
                    Score = 0,
                    Action = _ => false
                }
            ];
        }

        var search = query?.Search?.Trim() ?? "";

        // Empty search: list all snippets ordered by manual score desc, then key asc
        if (string.IsNullOrEmpty(search))
        {
            if (snippets.Count == 0)
            {
                return [];
            }

            var ordered = snippets
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var results = new List<Result>(ordered.Count);
            for (var i = 0; i < ordered.Count; i++)
            {
                var s = ordered[i];
                results.Add(new Result
                {
                    Title = s.Key,
                    SubTitle = FormatPreview(s.Value),
                    IcoPath = IconPath,
                    ContextData = s,
                    CopyText = s.Value,
                    AutoCompleteText = $"sp {s.Key}",
                    Score = 1000 - i,
                    Action = _ => copyAction != null ? copyAction(s.Value) : true
                });
            }

            return results;
        }

        // Search text provided: filter by key ONLY (do not search values)
        var matches = new List<(Snippet Snippet, int FuzzyScore, List<int> MatchData)>();

        foreach (var s in snippets)
        {
            var match = fuzzySearch(search, s.Key);
            if (match is { Success: true })
            {
                matches.Add((s, match.Score, match.MatchData ?? []));
            }
        }

        if (matches.Count == 0)
        {
            return
            [
                new Result
                {
                    Title = "No matching snippets",
                    SubTitle = $"No snippet keys matched '{search}'",
                    IcoPath = IconPath,
                    Score = 0,
                    Action = _ => false
                }
            ];
        }

        // Ordered by:
        // 1. Fuzzy relevance descending
        // 2. Manual score descending
        // 3. Key ascending (ordinal case-insensitive)
        var sortedMatches = matches
            .OrderByDescending(m => m.FuzzyScore)
            .ThenByDescending(m => m.Snippet.Score)
            .ThenBy(m => m.Snippet.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var queryResults = new List<Result>(sortedMatches.Count);
        for (var i = 0; i < sortedMatches.Count; i++)
        {
            var item = sortedMatches[i];
            queryResults.Add(new Result
            {
                Title = item.Snippet.Key,
                TitleHighlightData = item.MatchData,
                SubTitle = FormatPreview(item.Snippet.Value),
                IcoPath = IconPath,
                ContextData = item.Snippet,
                CopyText = item.Snippet.Value,
                AutoCompleteText = $"sp {item.Snippet.Key}",
                Score = 1000 - i,
                Action = _ => copyAction != null ? copyAction(item.Snippet.Value) : true
            });
        }

        return queryResults;
    }

    public static string FormatPreview(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
    }
}
