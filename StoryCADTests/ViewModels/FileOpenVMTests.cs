using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

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
    [Ignore("Test temporarily disabled")]
    public void ConfirmClicked_WithNoRecentIndex_DoesNotThrow()
    {
        // Tests the FileOpenVM with no issue to ensure it works correctly
        // https://github.com/storybuilder-org/StoryCAD/pull/971
        
        // Arrange
        var fileOpenVM = Ioc.Default.GetRequiredService<FileOpenVM>();
        fileOpenVM.SelectedRecentIndex = -1;
        
        // Act & Assert - Should not throw
        fileOpenVM.ConfirmClicked();
    }
}