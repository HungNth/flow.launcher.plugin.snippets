namespace Flow.Launcher.Plugin.Snippets.Tests;

using System;
using System.Reflection;
using System.Threading.Tasks;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Snippets.Models;
using Flow.Launcher.Plugin.Snippets.Services;

[TestClass]
public class PasteOrchestratorTests
{
    private class FakePasteSimulator : IPasteSimulator
    {
        public int SimulateCallCount { get; private set; }
        public bool ReturnValue { get; set; } = true;
        public bool ThrowOnSimulate { get; set; }

        public bool SimulatePaste()
        {
            SimulateCallCount++;
            if (ThrowOnSimulate)
            {
                throw new InvalidOperationException("Simulation exception");
            }
            return ReturnValue;
        }
    }

    private class FakePublicApi : DispatchProxy
    {
        public string? CopiedText { get; set; }
        public bool ThrowOnCopy { get; set; }
        public int ShowMsgErrorCount { get; set; }
        public int ShowMsgCount { get; set; }
        public int LogWarnCount { get; set; }
        public int LogErrorCount { get; set; }
        public string? LastMsgTitle { get; set; }
        public string? LastMsgMessage { get; set; }

        public static (IPublicAPI Api, FakePublicApi Handler) Create()
        {
            var api = DispatchProxy.Create<IPublicAPI, FakePublicApi>();
            var handler = (FakePublicApi)(object)api;
            return (api, handler);
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "CopyToClipboard")
            {
                if (ThrowOnCopy)
                {
                    throw new InvalidOperationException("Copy failed");
                }
                CopiedText = args?[0]?.ToString();
                return null;
            }
            if (targetMethod?.Name == "ShowMsgError")
            {
                ShowMsgErrorCount++;
                return null;
            }
            if (targetMethod?.Name == "ShowMsg")
            {
                ShowMsgCount++;
                LastMsgTitle = args?[0]?.ToString();
                LastMsgMessage = args?[1]?.ToString();
                return null;
            }
            if (targetMethod?.Name == "LogWarn")
            {
                LogWarnCount++;
                return null;
            }
            if (targetMethod?.Name == "LogError")
            {
                LogErrorCount++;
                return null;
            }
            return null;
        }
    }

    [TestMethod]
    public void ExecuteSelection_ReturnsFalse_WhenClipboardFails()
    {
        var (api, handler) = FakePublicApi.Create();
        handler.ThrowOnCopy = true;

        var simulator = new FakePasteSimulator();
        var orchestrator = new PasteOrchestrator(api, simulator, ms => Task.CompletedTask);
        var settings = new PluginSettings { AutoPasteEnabled = true };

        var result = orchestrator.ExecuteSelection("test-val", settings);

        Assert.IsFalse(result);
        Assert.AreEqual(1, handler.ShowMsgErrorCount);
        Assert.AreEqual(0, simulator.SimulateCallCount);
    }

    [TestMethod]
    public void ExecuteSelection_CopiesAndReturnsTrue_WithoutPaste_WhenAutoPasteDisabled()
    {
        var (api, handler) = FakePublicApi.Create();
        var simulator = new FakePasteSimulator();
        var orchestrator = new PasteOrchestrator(api, simulator, ms => Task.CompletedTask);
        var settings = new PluginSettings { AutoPasteEnabled = false };

        var result = orchestrator.ExecuteSelection("disabled-paste-val", settings);

        Assert.IsTrue(result);
        Assert.AreEqual("disabled-paste-val", handler.CopiedText);
        Assert.AreEqual(0, simulator.SimulateCallCount);
    }

    [TestMethod]
    public async Task ExecuteSelection_ExecutesPaste_AfterConfiguredDelay_WhenAutoPasteEnabled()
    {
        var (api, handler) = FakePublicApi.Create();
        var simulator = new FakePasteSimulator();
        var delayCompletion = new TaskCompletionSource<bool>();
        int observedDelay = -1;

        var orchestrator = new PasteOrchestrator(api, simulator, ms =>
        {
            observedDelay = ms;
            delayCompletion.TrySetResult(true);
            return Task.CompletedTask;
        });

        var settings = new PluginSettings
        {
            AutoPasteEnabled = true,
            PasteDelayMs = 250
        };

        var result = orchestrator.ExecuteSelection("auto-paste-val", settings);

        Assert.IsTrue(result);
        Assert.AreEqual("auto-paste-val", handler.CopiedText);

        await delayCompletion.Task;
        // Give background continuation a brief moment to finish
        await Task.Delay(50);

        Assert.AreEqual(250, observedDelay);
        Assert.AreEqual(1, simulator.SimulateCallCount);
        Assert.AreEqual(0, handler.ShowMsgCount);
        Assert.AreEqual(0, handler.LogWarnCount);
    }

    [TestMethod]
    public async Task ExecuteSelection_ShowsWarning_WhenSimulationFails()
    {
        var (api, handler) = FakePublicApi.Create();
        var simulator = new FakePasteSimulator { ReturnValue = false };
        var delayCompletion = new TaskCompletionSource<bool>();

        var orchestrator = new PasteOrchestrator(api, simulator, ms =>
        {
            delayCompletion.TrySetResult(true);
            return Task.CompletedTask;
        });

        var settings = new PluginSettings { AutoPasteEnabled = true };

        var result = orchestrator.ExecuteSelection("val", settings);

        Assert.IsTrue(result);
        await delayCompletion.Task;
        await Task.Delay(50);

        Assert.AreEqual(1, simulator.SimulateCallCount);
        Assert.AreEqual(1, handler.LogWarnCount);
        Assert.AreEqual(1, handler.ShowMsgCount);
        Assert.AreEqual("Snippet copied to clipboard, but could not be pasted automatically.", handler.LastMsgMessage);
    }

    [TestMethod]
    public async Task ExecuteSelection_CatchesException_SafelyWithoutCrashing()
    {
        var (api, handler) = FakePublicApi.Create();
        var simulator = new FakePasteSimulator { ThrowOnSimulate = true };
        var delayCompletion = new TaskCompletionSource<bool>();

        var orchestrator = new PasteOrchestrator(api, simulator, ms =>
        {
            delayCompletion.TrySetResult(true);
            return Task.CompletedTask;
        });

        var settings = new PluginSettings { AutoPasteEnabled = true };

        var result = orchestrator.ExecuteSelection("val", settings);

        Assert.IsTrue(result);
        await delayCompletion.Task;
        await Task.Delay(50);

        Assert.AreEqual(1, simulator.SimulateCallCount);
        Assert.AreEqual(1, handler.LogErrorCount);
        Assert.AreEqual(1, handler.ShowMsgCount);
    }
}
