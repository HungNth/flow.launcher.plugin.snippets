namespace Flow.Launcher.Plugin.Snippets.Tests;

using System;
using System.Runtime.InteropServices;
using Flow.Launcher.Plugin.Snippets.Services;

[TestClass]
public class WindowsPasteSimulatorTests
{
    [TestMethod]
    public void InputStructSize_On64Bit_MustBe40Bytes()
    {
        if (Environment.Is64BitProcess)
        {
            var size = WindowsPasteSimulator.GetInputStructSize();
            Assert.AreEqual(40, size, "Win32 user32!SendInput requires sizeof(INPUT) to be 40 bytes on x64 platforms.");
        }
    }
}
