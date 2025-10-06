using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.ViewModels;

namespace StoryCADTests.ViewModels;

[TestClass]
public class FileOpenVMTests
{
    [TestMethod]
    public void FileOpenVM_CanBeCreatedFromIoC()
    {
        // Arrange & Act
        var fileOpenVM = Ioc.Default.GetRequiredService<FileOpenVM>();

        // Assert
        Assert.IsNotNull(fileOpenVM);
    }

    [TestMethod]
    public void FileOpenVM_InitializesPropertiesCorrectly()
    {
        // Arrange & Act
        var fileOpenVM = Ioc.Default.GetRequiredService<FileOpenVM>();

        // Assert
        Assert.IsNotNull(fileOpenVM.SampleNames);
        Assert.IsTrue(fileOpenVM.SampleNames.Count > 0, "Should have sample stories loaded");
        Assert.AreEqual(-1, fileOpenVM.SelectedRecentIndex);
        Assert.AreEqual(string.Empty, fileOpenVM.OutlineName);
    }

    [TestMethod]
    [Ignore("Requires UI thread - NavigationViewItem cannot be created in unit test context")]
    public void ConfirmClicked_WithNoRecentIndex_DoesNotThrow()
    {
        // Tests the FileOpenVM ConfirmClicked with no recent file selected
        // Issue: This test requires NavigationViewItem which needs UI thread initialization
        // Related PR: https://github.com/storybuilder-org/StoryCAD/pull/971
        //
        // Expected behavior (verified by code review):
        // When CurrentTab.Tag == "Recent" and SelectedRecentIndex == -1,
        // ConfirmClicked() returns early without throwing (line 355 in FileOpenVM.cs)

        // Arrange
        var fileOpenVM = Ioc.Default.GetRequiredService<FileOpenVM>();
        fileOpenVM.SelectedRecentIndex = -1;

        // Set up CurrentTab to simulate "Recent" tab selected
        var recentTab = new NavigationViewItem { Tag = "Recent" };
        fileOpenVM.CurrentTab = recentTab;

        // Act & Assert - Should not throw (should return early when SelectedRecentIndex is -1)
        fileOpenVM.ConfirmClicked();
    }
}
