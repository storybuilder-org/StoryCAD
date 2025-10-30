#if WINDOWS10_0_22621_0_OR_GREATER
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.ViewModels;

namespace StoryCADTests.ViewModels;

/// <summary>
/// Windows-specific tests for FileOpenVM that require UI controls
/// </summary>
[TestClass]
public partial class FileOpenVMTests
{
    [UITestMethod]
    [TestCategory("Windows")]
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
#endif
