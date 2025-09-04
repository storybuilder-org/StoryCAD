using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryCADTests;

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
}