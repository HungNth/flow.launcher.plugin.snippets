namespace Flow.Launcher.Plugin.Snippets.Tests;

using System;
using System.Collections.Generic;
using System.Reflection;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Snippets.Models;

[TestClass]
public class ContextMenuTests
{
    private class TestContextFactory
    {
        public static (PluginInitContext Context, TestApiProxy Handler) Create()
        {
            var api = DispatchProxy.Create<IPublicAPI, TestApiProxy>();
            var apiProxy = (TestApiProxy)(object)api;

            var context = new PluginInitContext();
            typeof(PluginInitContext).GetProperty("API")!.SetValue(context, api);

            var metadata = new PluginMetadata();
            var dirPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SnippetsCtxTest_" + Guid.NewGuid().ToString("N"));
            typeof(PluginMetadata).GetProperty("PluginSettingsDirectoryPath")!.SetValue(metadata, dirPath);
            typeof(PluginInitContext).GetProperty("CurrentPluginMetadata")!.SetValue(context, metadata);

            return (context, apiProxy);
        }
    }

    private class TestApiProxy : DispatchProxy
    {

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "CopyToClipboard")
            {
                if (ThrowOnCopy)
                {
                    throw new InvalidOperationException("Clipboard error");
                }
                CopiedText = args?[0]?.ToString();
                return null;
            }
            if (targetMethod?.Name == "LoadSettingJsonStorage")
            {
                return new PluginSettings();
            }
            return null;
        }

        public string? CopiedText { get; set; }
        public bool ThrowOnCopy { get; set; }
    }

    [TestMethod]
    public void LoadContextMenus_ReturnsCopyItem_WhenSnippetContextDataPresent()
    {
        var main = new Main();
        var (context, _) = TestContextFactory.Create();
        main.Init(context);

        var snippet = new Snippet("greeting", "Hello, World!");
        var result = new Result
        {
            Title = snippet.Key,
            ContextData = snippet,
            CopyText = snippet.Value
        };

        var contextMenus = main.LoadContextMenus(result);

        Assert.AreEqual(1, contextMenus.Count);
        Assert.AreEqual("Copy to clipboard", contextMenus[0].Title);
        Assert.AreEqual("Copy snippet value without pasting", contextMenus[0].SubTitle);
    }

    [TestMethod]
    public void LoadContextMenus_ReturnsCopyItem_WhenOnlyCopyTextPresent()
    {
        var main = new Main();
        var (context, _) = TestContextFactory.Create();
        main.Init(context);

        var result = new Result
        {
            Title = "snippet-key",
            CopyText = "Just Copy Text"
        };

        var contextMenus = main.LoadContextMenus(result);

        Assert.AreEqual(1, contextMenus.Count);
        Assert.AreEqual("Copy to clipboard", contextMenus[0].Title);
    }

    [TestMethod]
    public void LoadContextMenus_ReturnsEmpty_WhenNoValuePresent()
    {
        var main = new Main();
        var (context, _) = TestContextFactory.Create();
        main.Init(context);

        var result = new Result
        {
            Title = "empty"
        };

        var contextMenus = main.LoadContextMenus(result);

        Assert.AreEqual(0, contextMenus.Count);
    }

    [TestMethod]
    public void LoadContextMenus_Action_CopiesValueToClipboard()
    {
        var main = new Main();
        var api = DispatchProxy.Create<IPublicAPI, TestApiProxy>();
        var apiProxy = (TestApiProxy)(object)api;

        var context = new PluginInitContext();
        typeof(PluginInitContext).GetProperty("API")!.SetValue(context, api);
        var metadata = new PluginMetadata();
        var dirPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SnippetsCtxTest_" + Guid.NewGuid().ToString("N"));
        typeof(PluginMetadata).GetProperty("PluginSettingsDirectoryPath")!.SetValue(metadata, dirPath);
        typeof(PluginInitContext).GetProperty("CurrentPluginMetadata")!.SetValue(context, metadata);

        main.Init(context);

        var snippet = new Snippet("key1", "expected-clipboard-content");
        var result = new Result
        {
            Title = snippet.Key,
            ContextData = snippet
        };

        var contextMenus = main.LoadContextMenus(result);
        var actionResult = contextMenus[0].Action(null!);

        Assert.IsTrue(actionResult);
        Assert.AreEqual("expected-clipboard-content", apiProxy.CopiedText);
    }
}
