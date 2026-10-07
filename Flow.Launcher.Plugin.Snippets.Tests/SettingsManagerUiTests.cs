namespace Flow.Launcher.Plugin.Snippets.Tests;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using Flow.Launcher.Plugin;
using Flow.Launcher.Plugin.Snippets.Models;
using Flow.Launcher.Plugin.Snippets.Storage;
using Flow.Launcher.Plugin.Snippets.Views;

[TestClass]
[DoNotParallelize]
public class SettingsManagerUiTests
{
    private string _tempDir = null!;

    [TestInitialize]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SnippetUiTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            foreach (var f in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(f, FileAttributes.Normal);
            }
            Directory.Delete(_tempDir, true);
        }
    }

    private static void RunInSta(Action action)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (exception != null)
        {
            throw exception;
        }
    }

    [TestMethod]
    public void Window_InitialState_DefaultsScoreToZeroAndAddEnabled()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            var window = new SnippetManagerWindow(store);

            Assert.AreEqual("0", window.ScoreBox.Text);
            Assert.AreEqual("", window.KeyBox.Text);
            Assert.AreEqual("", window.ValueBox.Text);
            Assert.IsTrue(window.AddButton.IsEnabled);
            Assert.IsFalse(window.SaveButton.IsEnabled);
            Assert.IsFalse(window.DeleteButton.IsEnabled);
        });
    }

    [TestMethod]
    public void Filter_FiltersGridByKey_WithoutModifyingStore()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("apple", "val1", 0));
            store.Add(new Snippet("banana", "val2", 0));
            store.Add(new Snippet("cherry", "val3", 0));

            var window = new SnippetManagerWindow(store);
            var initialItems = (window.SnippetsGrid.ItemsSource as System.Collections.IEnumerable)?.Cast<SnippetDisplayItem>().ToList();
            Assert.AreEqual(3, initialItems?.Count);

            window.FilterBox.Text = "ban";
            var filteredItems = (window.SnippetsGrid.ItemsSource as System.Collections.IEnumerable)?.Cast<SnippetDisplayItem>().ToList();
            Assert.AreEqual(1, filteredItems?.Count);
            Assert.AreEqual("banana", filteredItems?[0].Key);

            // Store remains unmodified
            Assert.AreEqual(3, store.Snippets.Count);

            window.FilterBox.Text = "";
            var restoredItems = (window.SnippetsGrid.ItemsSource as System.Collections.IEnumerable)?.Cast<SnippetDisplayItem>().ToList();
            Assert.AreEqual(3, restoredItems?.Count);
        });
    }

    [TestMethod]
    public void Add_ValidSnippet_PersistsImmediatelyAndReflectsInStore()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            var window = new SnippetManagerWindow(store);

            window.KeyBox.Text = "ui-add";
            window.ValueBox.Text = "line 1\r\nline 2";
            window.ScoreBox.Text = "42";

            window.AddButton_Click(window.AddButton, new System.Windows.RoutedEventArgs());

            Assert.AreEqual(1, store.Snippets.Count);
            Assert.AreEqual("ui-add", store.Snippets[0].Key);
            Assert.AreEqual("line 1\r\nline 2", store.Snippets[0].Value);
            Assert.AreEqual(42, store.Snippets[0].Score);

            // Fields reset after add
            Assert.AreEqual("", window.KeyBox.Text);
            Assert.AreEqual("", window.ValueBox.Text);
            Assert.AreEqual("0", window.ScoreBox.Text);
            Assert.AreEqual(System.Windows.Visibility.Collapsed, window.ErrorTextBlock.Visibility);
        });
    }

    [TestMethod]
    public void Edit_AndRename_PersistsImmediately()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("original-key", "original value", 10));

            var window = new SnippetManagerWindow(store);
            // Select row
            window.SnippetsGrid.SelectedIndex = 0;

            Assert.AreEqual("original-key", window.KeyBox.Text);
            Assert.AreEqual("original value", window.ValueBox.Text);
            Assert.AreEqual("10", window.ScoreBox.Text);
            Assert.IsTrue(window.SaveButton.IsEnabled);

            // Rename key and update value
            window.KeyBox.Text = "renamed-key";
            window.ValueBox.Text = "updated multiline\r\nvalue";
            window.ScoreBox.Text = "99";

            window.SaveButton_Click(window.SaveButton, new System.Windows.RoutedEventArgs());

            Assert.AreEqual(1, store.Snippets.Count);
            Assert.AreEqual("renamed-key", store.Snippets[0].Key);
            Assert.AreEqual("updated multiline\r\nvalue", store.Snippets[0].Value);
            Assert.AreEqual(99, store.Snippets[0].Score);
        });
    }

    [TestMethod]
    public void Delete_Confirmed_RemovesSnippet_WhileDeclinedKeepsSnippet()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("key-del", "value", 10));

            var window = new SnippetManagerWindow(store);
            window.SnippetsGrid.SelectedIndex = 0;

            // 1. Decline deletion
            window.ConfirmDeleteDialog = _ => false;
            window.DeleteButton_Click(window.DeleteButton, new System.Windows.RoutedEventArgs());
            Assert.AreEqual(1, store.Snippets.Count, "Declining delete must keep snippet");

            // 2. Confirm deletion
            window.ConfirmDeleteDialog = _ => true;
            window.DeleteButton_Click(window.DeleteButton, new System.Windows.RoutedEventArgs());
            Assert.AreEqual(0, store.Snippets.Count, "Confirming delete must remove snippet");
        });
    }

    [TestMethod]
    public void Cancel_ResetsFormWithoutModifyingStore()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("stay-key", "stay val", 5));

            var window = new SnippetManagerWindow(store);
            window.SnippetsGrid.SelectedIndex = 0;

            window.KeyBox.Text = "changed-in-form";
            window.ValueBox.Text = "not saved";

            window.CancelButton_Click(window.CancelButton, new System.Windows.RoutedEventArgs());

            Assert.AreEqual("stay-key", window.KeyBox.Text);
            Assert.AreEqual("5", window.ScoreBox.Text);
            Assert.AreEqual(1, store.Snippets.Count);
            Assert.AreEqual("stay-key", store.Snippets[0].Key);
            Assert.AreEqual("stay val", store.Snippets[0].Value);
        });
    }

    [TestMethod]
    public void Add_DuplicateKey_DisplaysErrorAndLeavesStoreUnchanged()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("ExistingKey", "val", 0));

            var window = new SnippetManagerWindow(store);
            window.KeyBox.Text = "existingkey";
            window.ValueBox.Text = "new val";
            window.ScoreBox.Text = "1";

            window.AddButton_Click(window.AddButton, new System.Windows.RoutedEventArgs());

            Assert.AreEqual(System.Windows.Visibility.Visible, window.ErrorTextBlock.Visibility);
            StringAssert.Contains(window.ErrorTextBlock.Text.ToLowerInvariant(), "duplicate");
            Assert.AreEqual(1, store.Snippets.Count);
        });
    }

    [TestMethod]
    public void SaveFailure_SurfacesErrorAndLeavesStoreAndDiskUsable()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("valid-key", "valid value", 10));

            var window = new SnippetManagerWindow(store);

            // Lock file to force save failure
            using (var lockStream = new FileStream(store.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                window.KeyBox.Text = "another-key";
                window.ValueBox.Text = "val";
                window.ScoreBox.Text = "0";

                window.AddButton_Click(window.AddButton, new System.Windows.RoutedEventArgs());

                Assert.AreEqual(System.Windows.Visibility.Visible, window.ErrorTextBlock.Visibility);
                StringAssert.Contains(window.ErrorTextBlock.Text.ToLowerInvariant(), "failed");
            }

            // Store and prior snapshot remain intact
            Assert.AreEqual(1, store.Snippets.Count);
            Assert.AreEqual("valid-key", store.Snippets[0].Key);
        });
    }

    [TestMethod]
    public void InitialState_EntersAddMode_WithModeSpecificActionVisibility()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            var window = new SnippetManagerWindow(store);

            Assert.AreEqual("New snippet", window.FormHeader.Text);
            Assert.AreEqual(Visibility.Visible, window.AddButton.Visibility);
            Assert.AreEqual(Visibility.Collapsed, window.SaveButton.Visibility);
            Assert.AreEqual(Visibility.Collapsed, window.DeleteButton.Visibility);
            Assert.AreEqual(Visibility.Visible, window.CancelButton.Visibility);
        });
    }

    [TestMethod]
    public void RowSelection_EntersEditMode_AndNewSnippetReturnsToAddMode()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("test-key", "test-val", 5));
            var window = new SnippetManagerWindow(store);

            var list = (window.FindName("SnippetsList") ?? window.FindName("SnippetsGrid")) as System.Windows.Controls.Primitives.Selector;
            Assert.IsNotNull(list);
            list.SelectedIndex = 0;

            Assert.AreEqual("Edit snippet", window.FormHeader.Text);
            Assert.AreEqual(Visibility.Collapsed, window.AddButton.Visibility);
            Assert.AreEqual(Visibility.Visible, window.SaveButton.Visibility);
            Assert.AreEqual(Visibility.Visible, window.DeleteButton.Visibility);
            Assert.AreEqual(Visibility.Visible, window.CancelButton.Visibility);
            Assert.AreEqual("test-key", window.KeyBox.Text);
            Assert.AreEqual("test-val", window.ValueBox.Text);
            Assert.AreEqual("5", window.ScoreBox.Text);

            var newSnippetBtn = window.FindName("NewSnippetButton") as Button;
            Assert.IsNotNull(newSnippetBtn, "NewSnippetButton must be present");
            newSnippetBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Assert.AreEqual(-1, list.SelectedIndex);
            Assert.IsNull(list.SelectedItem);
            Assert.AreEqual("New snippet", window.FormHeader.Text);
            Assert.AreEqual(Visibility.Visible, window.AddButton.Visibility);
            Assert.AreEqual(Visibility.Collapsed, window.SaveButton.Visibility);
            Assert.AreEqual(Visibility.Collapsed, window.DeleteButton.Visibility);
            Assert.AreEqual("", window.KeyBox.Text);
            Assert.AreEqual("", window.ValueBox.Text);
            Assert.AreEqual("0", window.ScoreBox.Text);
        });
    }

    [TestMethod]
    public void Cancel_InEditMode_RestoresSelectedSnippetValuesAndKeepsSelection()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("stay-key", "stay val", 5));

            var window = new SnippetManagerWindow(store);
            var list = (window.FindName("SnippetsList") ?? window.FindName("SnippetsGrid")) as System.Windows.Controls.Primitives.Selector;
            Assert.IsNotNull(list);
            list.SelectedIndex = 0;

            window.KeyBox.Text = "changed-in-form";
            window.ValueBox.Text = "not saved";
            window.ScoreBox.Text = "999";

            window.CancelButton_Click(window.CancelButton, new RoutedEventArgs());

            Assert.AreEqual(0, list.SelectedIndex);
            Assert.AreEqual("stay-key", window.KeyBox.Text);
            Assert.AreEqual("stay val", window.ValueBox.Text);
            Assert.AreEqual("5", window.ScoreBox.Text);
            Assert.AreEqual("Edit snippet", window.FormHeader.Text);
            Assert.AreEqual(1, store.Snippets.Count);
            Assert.AreEqual("stay-key", store.Snippets[0].Key);
        });
    }

    [TestMethod]
    public void Cancel_InAddMode_ClearsDraftFields()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            var window = new SnippetManagerWindow(store);

            window.KeyBox.Text = "draft-key";
            window.ValueBox.Text = "draft-val";
            window.ScoreBox.Text = "42";

            window.CancelButton_Click(window.CancelButton, new RoutedEventArgs());

            Assert.AreEqual("", window.KeyBox.Text);
            Assert.AreEqual("", window.ValueBox.Text);
            Assert.AreEqual("0", window.ScoreBox.Text);
            Assert.AreEqual("New snippet", window.FormHeader.Text);
            Assert.AreEqual(Visibility.Visible, window.AddButton.Visibility);
        });
    }

    [TestMethod]
    public void Save_SuccessfulEdit_ReselectsUpdatedSnippetAndPreservesEditMode()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("old-key", "old val", 10));

            var window = new SnippetManagerWindow(store);
            var list = (window.FindName("SnippetsList") ?? window.FindName("SnippetsGrid")) as System.Windows.Controls.Primitives.Selector;
            Assert.IsNotNull(list);
            list.SelectedIndex = 0;

            window.KeyBox.Text = "new-key";
            window.ValueBox.Text = "new val";
            window.ScoreBox.Text = "20";

            window.SaveButton_Click(window.SaveButton, new RoutedEventArgs());

            Assert.AreEqual(1, store.Snippets.Count);
            Assert.AreEqual("new-key", store.Snippets[0].Key);
            var selectedItem = list.SelectedItem as SnippetDisplayItem;
            Assert.IsNotNull(selectedItem);
            Assert.AreEqual("new-key", selectedItem.Key);
            Assert.AreEqual("Edit snippet", window.FormHeader.Text);
            Assert.AreEqual(Visibility.Visible, window.SaveButton.Visibility);
            Assert.AreEqual(Visibility.Collapsed, window.AddButton.Visibility);
        });
    }

    [TestMethod]
    public void Delete_Confirmed_RemovesSnippetAndReturnsToAddMode()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("del-key", "del val", 10));

            var window = new SnippetManagerWindow(store);
            var list = (window.FindName("SnippetsList") ?? window.FindName("SnippetsGrid")) as System.Windows.Controls.Primitives.Selector;
            Assert.IsNotNull(list);
            list.SelectedIndex = 0;

            window.ConfirmDeleteDialog = _ => true;
            window.DeleteButton_Click(window.DeleteButton, new RoutedEventArgs());

            Assert.AreEqual(0, store.Snippets.Count);
            Assert.AreEqual(-1, list.SelectedIndex);
            Assert.AreEqual("New snippet", window.FormHeader.Text);
            Assert.AreEqual(Visibility.Visible, window.AddButton.Visibility);
            Assert.AreEqual(Visibility.Collapsed, window.SaveButton.Visibility);
            Assert.AreEqual(Visibility.Collapsed, window.DeleteButton.Visibility);
        });
    }

    [TestMethod]
    public void Filter_WhenSelectedSnippetFilteredAway_ClearsSelectionAndReturnsToAddMode()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("apple", "val1", 0));
            store.Add(new Snippet("banana", "val2", 0));

            var window = new SnippetManagerWindow(store);
            var list = (window.FindName("SnippetsList") ?? window.FindName("SnippetsGrid")) as System.Windows.Controls.Primitives.Selector;
            Assert.IsNotNull(list);
            list.SelectedIndex = 0; // apple
            Assert.AreEqual("Edit snippet", window.FormHeader.Text);

            window.FilterBox.Text = "banana";

            Assert.IsNull(list.SelectedItem);
            Assert.AreEqual("New snippet", window.FormHeader.Text);
            Assert.AreEqual(Visibility.Visible, window.AddButton.Visibility);
            Assert.AreEqual(Visibility.Collapsed, window.SaveButton.Visibility);
            Assert.AreEqual(Visibility.Collapsed, window.DeleteButton.Visibility);
        });
    }

    [TestMethod]
    public void Accessibility_ControlsExposeDescriptiveAutomationNames()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            var window = new SnippetManagerWindow(store);

            var filterName = AutomationProperties.GetName(window.FilterBox);
            Assert.IsFalse(string.IsNullOrWhiteSpace(filterName), "FilterBox should have an automation name");

            var clearName = AutomationProperties.GetName(window.ClearFilterButton);
            Assert.IsFalse(string.IsNullOrWhiteSpace(clearName), "ClearFilterButton should have an automation name");

            var keyName = AutomationProperties.GetName(window.KeyBox);
            Assert.IsFalse(string.IsNullOrWhiteSpace(keyName), "KeyBox should have an automation name");

            var scoreName = AutomationProperties.GetName(window.ScoreBox);
            Assert.IsFalse(string.IsNullOrWhiteSpace(scoreName), "ScoreBox should have an automation name");

            var valueName = AutomationProperties.GetName(window.ValueBox);
            Assert.IsFalse(string.IsNullOrWhiteSpace(valueName), "ValueBox should have an automation name");

            var addName = AutomationProperties.GetName(window.AddButton);
            Assert.IsFalse(string.IsNullOrWhiteSpace(addName), "AddButton should have an automation name");

            var saveName = AutomationProperties.GetName(window.SaveButton);
            Assert.IsFalse(string.IsNullOrWhiteSpace(saveName), "SaveButton should have an automation name");

            var deleteName = AutomationProperties.GetName(window.DeleteButton);
            Assert.IsFalse(string.IsNullOrWhiteSpace(deleteName), "DeleteButton should have an automation name");

            var cancelName = AutomationProperties.GetName(window.CancelButton);
            Assert.IsFalse(string.IsNullOrWhiteSpace(cancelName), "CancelButton should have an automation name");

            var newButton = window.FindName("NewSnippetButton") as DependencyObject;
            Assert.IsNotNull(newButton, "NewSnippetButton should exist");
            var newName = AutomationProperties.GetName(newButton);
            Assert.IsFalse(string.IsNullOrWhiteSpace(newName), "NewSnippetButton should have an automation name");
        });
    }

    [TestMethod]
    public void Accessibility_ControlsHaveLogicalTabOrder()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            var window = new SnippetManagerWindow(store);

            Assert.IsTrue(window.FilterBox.IsTabStop);
            Assert.IsTrue(window.ClearFilterButton.IsTabStop);
            Assert.IsTrue(window.KeyBox.IsTabStop);
            Assert.IsTrue(window.ScoreBox.IsTabStop);
            Assert.IsTrue(window.ValueBox.IsTabStop);
            Assert.IsTrue(window.AddButton.IsTabStop);
            Assert.IsTrue(window.CancelButton.IsTabStop);

            var listControl = (window.FindName("SnippetsList") ?? window.FindName("SnippetsGrid")) as Control;
            Assert.IsNotNull(listControl);
            Assert.IsTrue(listControl.IsTabStop);

            var newButton = window.FindName("NewSnippetButton") as Control;
            Assert.IsNotNull(newButton);
            Assert.IsTrue(newButton.IsTabStop);

            // Verify logical sequential tab order metadata
            Assert.IsTrue(KeyboardNavigation.GetTabIndex(window.FilterBox) < KeyboardNavigation.GetTabIndex(window.ClearFilterButton));
            Assert.IsTrue(KeyboardNavigation.GetTabIndex(window.ClearFilterButton) < KeyboardNavigation.GetTabIndex(newButton));
            Assert.IsTrue(KeyboardNavigation.GetTabIndex(newButton) < KeyboardNavigation.GetTabIndex(listControl));
            Assert.IsTrue(KeyboardNavigation.GetTabIndex(listControl) < KeyboardNavigation.GetTabIndex(window.KeyBox));
            Assert.IsTrue(KeyboardNavigation.GetTabIndex(window.KeyBox) < KeyboardNavigation.GetTabIndex(window.ScoreBox));
            Assert.IsTrue(KeyboardNavigation.GetTabIndex(window.ScoreBox) < KeyboardNavigation.GetTabIndex(window.ValueBox));
            Assert.IsTrue(KeyboardNavigation.GetTabIndex(window.ValueBox) < KeyboardNavigation.GetTabIndex(window.AddButton));
        });
    }

    [TestMethod]
    public void Layout_AtMinimumSupportedWindowSize_ApplicableActionsReceiveNonZeroArrangedBounds()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("sample", "content", 1));
            var window = new SnippetManagerWindow(store);

            Assert.AreEqual(760, window.MinWidth, 1.0);
            Assert.AreEqual(480, window.MinHeight, 1.0);

            window.Width = window.MinWidth;
            window.Height = window.MinHeight;
            window.Show();
            window.UpdateLayout();

            var editorCard = window.FindName("EditorCard") as FrameworkElement;
            Assert.IsNotNull(editorCard, "EditorCard should exist");
            var editorBounds = new Rect(0, 0, editorCard.ActualWidth, editorCard.ActualHeight);

            // Add mode: AddButton and CancelButton must be fully contained within editorCard and not overlap
            var addTransform = window.AddButton.TransformToAncestor(editorCard);
            var addRect = addTransform.TransformBounds(new Rect(0, 0, window.AddButton.ActualWidth, window.AddButton.ActualHeight));

            var cancelTransform = window.CancelButton.TransformToAncestor(editorCard);
            var cancelRect = cancelTransform.TransformBounds(new Rect(0, 0, window.CancelButton.ActualWidth, window.CancelButton.ActualHeight));

            Assert.IsTrue(addRect.Width > 0 && addRect.Height > 0, "AddButton must have positive size");
            Assert.IsTrue(cancelRect.Width > 0 && cancelRect.Height > 0, "CancelButton must have positive size");
            Assert.IsTrue(editorBounds.Contains(addRect), "AddButton must be fully contained in editor card viewport");
            Assert.IsTrue(editorBounds.Contains(cancelRect), "CancelButton must be fully contained in editor card viewport");
            Assert.IsFalse(addRect.IntersectsWith(cancelRect), "AddButton and CancelButton must not overlap");

            // Switch to edit mode
            var list = (window.FindName("SnippetsList") ?? window.FindName("SnippetsGrid")) as System.Windows.Controls.Primitives.Selector;
            Assert.IsNotNull(list);
            list.SelectedIndex = 0;
            window.UpdateLayout();

            var saveTransform = window.SaveButton.TransformToAncestor(editorCard);
            var saveRect = saveTransform.TransformBounds(new Rect(0, 0, window.SaveButton.ActualWidth, window.SaveButton.ActualHeight));

            var deleteTransform = window.DeleteButton.TransformToAncestor(editorCard);
            var deleteRect = deleteTransform.TransformBounds(new Rect(0, 0, window.DeleteButton.ActualWidth, window.DeleteButton.ActualHeight));

            var cancelEditTransform = window.CancelButton.TransformToAncestor(editorCard);
            var cancelEditRect = cancelEditTransform.TransformBounds(new Rect(0, 0, window.CancelButton.ActualWidth, window.CancelButton.ActualHeight));

            Assert.IsTrue(saveRect.Width > 0 && saveRect.Height > 0, "SaveButton must have positive size");
            Assert.IsTrue(deleteRect.Width > 0 && deleteRect.Height > 0, "DeleteButton must have positive size");
            Assert.IsTrue(cancelEditRect.Width > 0 && cancelEditRect.Height > 0, "CancelButton must have positive size");

            Assert.IsTrue(editorBounds.Contains(saveRect), "SaveButton must be fully contained in editor card viewport");
            Assert.IsTrue(editorBounds.Contains(deleteRect), "DeleteButton must be fully contained in editor card viewport");
            Assert.IsTrue(editorBounds.Contains(cancelEditRect), "CancelButton must be fully contained in editor card viewport");

            Assert.IsFalse(saveRect.IntersectsWith(deleteRect), "SaveButton and DeleteButton must not overlap");
            Assert.IsFalse(saveRect.IntersectsWith(cancelEditRect), "SaveButton and CancelButton must not overlap");
            Assert.IsFalse(deleteRect.IntersectsWith(cancelEditRect), "DeleteButton and CancelButton must not overlap");

            window.Close();
        });
    }

    [TestMethod]
    public void SettingsControl_CanBeConstructed_InStaThread()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            var control = new SettingsControl(store);
            Assert.IsNotNull(control);
            Assert.IsNotNull(control.ManageSnippetsButton);
            Assert.IsNotNull(control.ExportButton);
            Assert.IsNotNull(control.ImportButton);
        });
    }

    [TestMethod]
    public void SettingsControl_ExportSnippets_ValidDestination_CallsStoreAndNotifiesApi()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Add(new Snippet("test-k", "test-v", 10));

            var (proxyApi, proxyHandler) = TestPublicApiProxy.Create();
            var control = new SettingsControl(store, proxyApi);

            var exportPath = Path.Combine(_tempDir, "ui-export.json");
            var result = control.ExportSnippets(exportPath);

            Assert.IsTrue(result);
            Assert.IsTrue(File.Exists(exportPath));
            Assert.AreEqual("Snippets Exported", proxyHandler.LastSuccessTitle);
        });
    }

    [TestMethod]
    public void SettingsControl_ImportSnippets_ValidFile_CallsStoreAndNotifiesApi()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            var (proxyApi, proxyHandler) = TestPublicApiProxy.Create();
            var control = new SettingsControl(store, proxyApi);

            var importPath = Path.Combine(_tempDir, "ui-import.json");
            File.WriteAllText(importPath, """
{
  "version": 1,
  "snippets": [
    { "key": "imported-key", "value": "imported-val", "score": 42 }
  ]
}
""");

            var result = control.ImportSnippets(importPath);

            Assert.IsTrue(result);
            Assert.AreEqual(1, store.Snippets.Count);
            Assert.AreEqual("imported-key", store.Snippets[0].Key);
            Assert.AreEqual("Snippets Imported", proxyHandler.LastSuccessTitle);
        });
    }

    [TestMethod]
    public void SettingsControl_ImportSnippets_InvalidFile_ReportsErrorAndReturnsFalse()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            var (proxyApi, proxyHandler) = TestPublicApiProxy.Create();
            var control = new SettingsControl(store, proxyApi);

            var result = control.ImportSnippets(Path.Combine(_tempDir, "nonexistent.json"));

            Assert.IsFalse(result);
            Assert.AreEqual("Import Failed", proxyHandler.LastErrorTitle);
        });
    }

    [TestMethod]
    public void SettingsControl_FileDialogFilter_SyntaxIsValid()
    {
        RunInSta(() =>
        {
            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };
            Assert.AreEqual("JSON files (*.json)|*.json|All files (*.*)|*.*", saveDialog.Filter);

            var openDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };
            Assert.AreEqual("JSON files (*.json)|*.json|All files (*.*)|*.*", openDialog.Filter);
        });
    }

    [TestMethod]
    public void ThemeSubscription_SubscribesWhenOpen_AndUnsubscribesWhenClosed()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();

            var (proxyApi, proxyHandler) = TestPublicApiProxy.Create();

            // Constructed-never-shown window must not subscribe
            var window = new SnippetManagerWindow(store, proxyApi);
            Assert.AreEqual(0, proxyHandler.SubscribeCount, "Constructed window must not subscribe before loading/showing");
            Assert.AreEqual(0, proxyHandler.UnsubscribeCount);

            // Subscribes when loaded/shown
            window.Show();
            Assert.AreEqual(1, proxyHandler.SubscribeCount, "Window must subscribe when shown");
            Assert.AreEqual(0, proxyHandler.UnsubscribeCount);

            window.Close();
            Assert.AreEqual(1, proxyHandler.UnsubscribeCount, "Window must unsubscribe when closed");

            // Verify manager reuse creates new subscription cleanly
            var window2 = new SnippetManagerWindow(store, proxyApi);
            Assert.AreEqual(1, proxyHandler.SubscribeCount, "Constructed reused window must not subscribe prematurely");
            window2.Show();
            Assert.AreEqual(2, proxyHandler.SubscribeCount, "Reused window subscribes when shown");
            window2.Close();
            Assert.AreEqual(2, proxyHandler.UnsubscribeCount, "Reused window unsubscribes when closed");
        });
    }

    [TestMethod]
    public void FocusNavigation_TabNavigatesOutOfValueBox()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            var window = new SnippetManagerWindow(store);
            window.Show();

            Assert.IsFalse(window.ValueBox.AcceptsTab, "ValueBox must have AcceptsTab=false to allow Tab navigation");
            Assert.IsTrue(window.ValueBox.AcceptsReturn, "ValueBox must preserve AcceptsReturn=true for multiline content");

            window.ValueBox.Focus();
            Assert.IsTrue(window.ValueBox.IsFocused, "ValueBox should be focused initially");

            var request = new TraversalRequest(FocusNavigationDirection.Next);
            bool moved = window.ValueBox.MoveFocus(request);
            Assert.IsTrue(moved, "Focus should successfully move out of ValueBox on Tab");

            var focusedElement = FocusManager.GetFocusedElement(window) as UIElement;
            Assert.IsNotNull(focusedElement, "An element must receive focus");
            Assert.AreNotEqual(window.ValueBox, focusedElement, "Focus must have moved out of ValueBox");
            Assert.AreEqual(window.AddButton, focusedElement, "Focus should land on AddButton in add mode");

            window.Close();
        });
    }

    [TestMethod]
    public void Delete_Declined_LeavesErrorFeedbackAndDraftUnchanged()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            store.Add(new Snippet("key-del", "original-val", 10));

            var window = new SnippetManagerWindow(store);
            window.SnippetsGrid.SelectedIndex = 0;

            window.KeyBox.Text = "modified-key";
            window.ValueBox.Text = "modified-val";
            window.ShowError("Validation failure occurred");

            window.ConfirmDeleteDialog = _ => false;
            window.DeleteButton_Click(window.DeleteButton, new RoutedEventArgs());

            Assert.AreEqual(Visibility.Visible, window.ErrorTextBlock.Visibility);
            Assert.AreEqual("Validation failure occurred", window.ErrorTextBlock.Text);
            Assert.AreEqual("modified-key", window.KeyBox.Text);
            Assert.AreEqual("modified-val", window.ValueBox.Text);
            Assert.AreEqual(1, store.Snippets.Count);
        });
    }

    [TestMethod]
    public void SettingsControl_InitializesFromPassedSettings_AndMutatesSharedInstance()
    {
        RunInSta(() =>
        {
            var store = new SnippetStore(_tempDir);
            store.Load();
            var (api, handler) = TestPublicApiProxy.Create();
            var settings = new PluginSettings
            {
                AutoPasteEnabled = true,
                PasteDelayMs = 150
            };

            var control = new SettingsControl(store, settings, api);
            Assert.IsTrue(control.AutoPasteCheckBox.IsChecked);
            Assert.AreEqual("150", control.PasteDelayTextBox.Text);

            // Mutate checkbox
            control.AutoPasteCheckBox.IsChecked = false;
            control.AutoPasteCheckBox_Click(control.AutoPasteCheckBox, new RoutedEventArgs());
            Assert.IsFalse(settings.AutoPasteEnabled);
            Assert.AreEqual(1, handler.SaveSettingsCount);

            // Mutate delay text box to valid value
            control.PasteDelayTextBox.Text = "250";
            control.PasteDelayTextBox_LostFocus(control.PasteDelayTextBox, new RoutedEventArgs());
            Assert.AreEqual(250, settings.PasteDelayMs);
            Assert.AreEqual("250", control.PasteDelayTextBox.Text);
            Assert.AreEqual(2, handler.SaveSettingsCount);

            // Mutate delay text box to clamped value (> 1000)
            control.PasteDelayTextBox.Text = "5000";
            control.PasteDelayTextBox_LostFocus(control.PasteDelayTextBox, new RoutedEventArgs());
            Assert.AreEqual(PluginSettings.MaxPasteDelayMs, settings.PasteDelayMs);
            Assert.AreEqual(PluginSettings.MaxPasteDelayMs.ToString(), control.PasteDelayTextBox.Text);
            Assert.AreEqual(3, handler.SaveSettingsCount);

            // Mutate delay text box to invalid string - retains current clamped value
            control.PasteDelayTextBox.Text = "invalid";
            control.PasteDelayTextBox_LostFocus(control.PasteDelayTextBox, new RoutedEventArgs());
            Assert.AreEqual(PluginSettings.MaxPasteDelayMs.ToString(), control.PasteDelayTextBox.Text);
            Assert.AreEqual(4, handler.SaveSettingsCount);
        });
    }
}

public class TestPublicApiProxy : DispatchProxy
{
    public int SubscribeCount { get; set; }
    public int UnsubscribeCount { get; set; }
    public string? LastSuccessTitle { get; set; }
    public string? LastSuccessMessage { get; set; }
    public string? LastErrorTitle { get; set; }
    public string? LastErrorMessage { get; set; }
    public int SaveSettingsCount { get; set; }

    public static (IPublicAPI Api, TestPublicApiProxy Handler) Create()
    {
        var api = DispatchProxy.Create<IPublicAPI, TestPublicApiProxy>();
        var handler = (TestPublicApiProxy)(object)api;
        return (api, handler);
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name == "add_ActualApplicationThemeChanged")
        {
            SubscribeCount++;
            return null;
        }
        if (targetMethod?.Name == "remove_ActualApplicationThemeChanged")
        {
            UnsubscribeCount++;
            return null;
        }
        if (targetMethod?.Name == "IsApplicationDarkTheme")
        {
            return false;
        }
        if (targetMethod?.Name == "ShowMsg")
        {
            LastSuccessTitle = args?[0]?.ToString();
            LastSuccessMessage = args?[1]?.ToString();
            return null;
        }
        if (targetMethod?.Name == "ShowMsgError")
        {
            LastErrorTitle = args?[0]?.ToString();
            LastErrorMessage = args?[1]?.ToString();
            return null;
        }
        if (targetMethod?.Name == "SaveSettingJsonStorage")
        {
            SaveSettingsCount++;
            return null;
        }
        return null;
    }
}


