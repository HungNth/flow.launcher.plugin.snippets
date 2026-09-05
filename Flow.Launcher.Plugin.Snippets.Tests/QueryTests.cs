namespace Flow.Launcher.Plugin.Snippets.Tests;

using System.Collections.Generic;
using System.Reflection;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.SharedModels;
using Flow.Launcher.Plugin.Snippets.Models;
using Flow.Launcher.Plugin.Snippets.Services;

[TestClass]
public class QueryTests
{
    private static Query CreateQuery(string search)
    {
        var query = new Query();
        typeof(Query).GetProperty("Search")!.SetValue(query, search);
        return query;
    }

    private static MatchResult CreateMatch(bool success, int score = 0, List<int>? matchData = null)
    {
        return new MatchResult(success, SearchPrecisionScore.Regular, matchData ?? new List<int>(), score);
    }

    [TestMethod]
    public void EmptyQuery_OrdersByManualScoreDescending_ThenKeyAscending()
    {
        var snippets = new List<Snippet>
        {
            new("zebra", "val-z", 10),
            new("apple", "val-a", 10),
            new("banana", "val-b", 50),
            new("cherry", "val-c", 0)
        };

        var results = SnippetQueryService.Query(
            CreateQuery(""),
            snippets,
            (q, target) => CreateMatch(true, 100));

        Assert.AreEqual(4, results.Count);
        Assert.AreEqual("banana", results[0].Title);
        Assert.AreEqual("apple", results[1].Title);
        Assert.AreEqual("zebra", results[2].Title);
        Assert.AreEqual("cherry", results[3].Title);

        // Scores in Flow must be strictly descending to preserve ordering
        Assert.IsTrue(results[0].Score > results[1].Score);
        Assert.IsTrue(results[1].Score > results[2].Score);
        Assert.IsTrue(results[2].Score > results[3].Score);
    }

    [TestMethod]
    public void FuzzyQuery_OrdersByTuple_FuzzyScoreDesc_ThenManualScoreDesc_ThenKeyAsc()
    {
        var snippets = new List<Snippet>
        {
            new("test-low-match", "val1", 500),
            new("test-high-manual", "val2", 100),
            new("test-best-match", "val3", 10),
            new("match-b", "val4", 10),
            new("match-a", "val5", 20)
        };

        var matchScores = new Dictionary<string, int>
        {
            ["test-best-match"] = 95,
            ["test-high-manual"] = 80,
            ["test-low-match"] = 70,
            ["match-a"] = 80,
            ["match-b"] = 80
        };

        var results = SnippetQueryService.Query(
            CreateQuery("test"),
            snippets,
            (q, target) => CreateMatch(
                matchScores.ContainsKey(target),
                matchScores.GetValueOrDefault(target, 0),
                new List<int> { 0, 1, 2, 3 }));

        Assert.AreEqual(5, results.Count);
        // Best fuzzy match (95) comes first despite manual score 10
        Assert.AreEqual("test-best-match", results[0].Title);
        // Next tier is fuzzy score 80: tie broken by manual score ("match-a" 20 vs "match-b" 10 vs "test-high-manual" 100)
        Assert.AreEqual("test-high-manual", results[1].Title); // Fuzzy 80, Manual 100
        Assert.AreEqual("match-a", results[2].Title);          // Fuzzy 80, Manual 20
        Assert.AreEqual("match-b", results[3].Title);          // Fuzzy 80, Manual 10
        // Lowest fuzzy match (70) comes last despite manual score 500
        Assert.AreEqual("test-low-match", results[4].Title);

        // Verify Flow scores are strictly descending
        for (var i = 0; i < results.Count - 1; i++)
        {
            Assert.IsTrue(results[i].Score > results[i + 1].Score, $"Result score at {i} must be > score at {i + 1}");
        }
    }

    [TestMethod]
    public void KeyOnlyFiltering_IgnoresSearchMatchInSnippetValue()
    {
        var snippets = new List<Snippet>
        {
            new("deploy-app", "special secret password", 10),
            new("secret-key", "public value", 10)
        };

        var results = SnippetQueryService.Query(
            CreateQuery("secret"),
            snippets,
            (q, target) => CreateMatch(target.Contains(q), target.Contains(q) ? 100 : 0));

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("secret-key", results[0].Title);
    }

    [TestMethod]
    public void StableKeyTieBreaking_CaseInsensitiveOrdinalAscending()
    {
        var snippets = new List<Snippet>
        {
            new("Beta", "val-b", 10),
            new("alpha", "val-a", 10),
            new("Alpha2", "val-a2", 10)
        };

        var results = SnippetQueryService.Query(
            CreateQuery(""),
            snippets,
            (q, target) => CreateMatch(true, 100));

        Assert.AreEqual(3, results.Count);
        Assert.AreEqual("alpha", results[0].Title);
        Assert.AreEqual("Alpha2", results[1].Title);
        Assert.AreEqual("Beta", results[2].Title);
    }

    [TestMethod]
    public void MultilinePreviewFormatting_FormatsSingleLinePreview_PreservingStoredValue()
    {
        var multiline = "First line\r\nSecond line\nThird line\r\n  Fourth";
        var snippets = new List<Snippet>
        {
            new("multi", multiline, 0)
        };

        var results = SnippetQueryService.Query(
            CreateQuery(""),
            snippets,
            (q, target) => CreateMatch(true, 100));

        Assert.AreEqual(1, results.Count);
        var res = results[0];
        Assert.IsFalse(res.SubTitle.Contains('\r'), "SubTitle must not contain CR");
        Assert.IsFalse(res.SubTitle.Contains('\n'), "SubTitle must not contain LF");
        Assert.AreEqual(multiline, res.CopyText, "CopyText must preserve exact multiline content");
        Assert.AreEqual("sp multi", res.AutoCompleteText);
    }

    [TestMethod]
    public void NoMatch_ReturnsNonActionableEmptyStateResult()
    {
        var snippets = new List<Snippet>
        {
            new("existing-key", "value", 0)
        };

        var results = SnippetQueryService.Query(
            CreateQuery("missing"),
            snippets,
            (q, target) => CreateMatch(false, 0));

        Assert.AreEqual(1, results.Count);
        var noMatch = results[0];
        Assert.IsTrue(noMatch.Title.ToLowerInvariant().Contains("no"), "Title should indicate no matches");
        // Verify action is non-actionable (returns false to keep window open / does nothing)
        Assert.IsNotNull(noMatch.Action);
        Assert.IsFalse(noMatch.Action(new ActionContext()));
    }

    [TestMethod]
    public void CopyAction_WhenClipboardSucceeds_ReturnsTrue_ClosingWindow()
    {
        var snippets = new List<Snippet> { new("key1", "val1", 10) };
        var copiedValue = "";
        var results = SnippetQueryService.Query(
            CreateQuery(""),
            snippets,
            (q, target) => CreateMatch(true, 100),
            copyAction: val => { copiedValue = val; return true; });

        Assert.AreEqual(1, results.Count);
        var actionResult = results[0].Action(new ActionContext());
        Assert.IsTrue(actionResult, "Action must return true on successful copy to close Flow window");
        Assert.AreEqual("val1", copiedValue);
    }

    [TestMethod]
    public void CopyAction_WhenClipboardFails_ReturnsFalse_KeepingWindowOpen()
    {
        var snippets = new List<Snippet> { new("key1", "val1", 10) };
        var results = SnippetQueryService.Query(
            CreateQuery(""),
            snippets,
            (q, target) => CreateMatch(true, 100),
            copyAction: val => false);

        Assert.AreEqual(1, results.Count);
        var actionResult = results[0].Action(new ActionContext());
        Assert.IsFalse(actionResult, "Action must return false on failed copy to keep Flow window open");
    }

    [TestMethod]
    public void Query_WhenLoadErrorAndNoSnippets_ReturnsActionableErrorResult()
    {
        var results = SnippetQueryService.Query(
            CreateQuery(""),
            [],
            (q, target) => CreateMatch(true, 100),
            loadError: "Malformed JSON in 'snippets.json'");

        Assert.AreEqual(1, results.Count);
        var err = results[0];
        Assert.AreEqual("Snippet Library Error", err.Title);
        StringAssert.Contains(err.SubTitle, "Malformed JSON");
        Assert.IsFalse(err.Action(new ActionContext()));
    }
}
