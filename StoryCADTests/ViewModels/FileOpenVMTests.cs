using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.ViewModels;

namespace StoryCADTests.ViewModels;

[TestClass]
public partial class FileOpenVMTests
{
    [TestMethod]
    [TestCategory("CrossPlatform")]
    public void FileOpenVM_CanBeCreatedFromIoC()
    {
        // Arrange & Act
        var fileOpenVM = Ioc.Default.GetRequiredService<FileOpenVM>();

        // Assert
        Assert.IsNotNull(fileOpenVM);
    }

    [TestMethod]
    [TestCategory("CrossPlatform")]
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
    [TestCategory("CrossPlatform")]
    public void ProjectTemplateNames_IsInitialized()
    {
        // Arrange & Act
        var fileOpenVM = Ioc.Default.GetRequiredService<FileOpenVM>();

        // Assert
        Assert.IsNotNull(fileOpenVM.ProjectTemplateNames);
        Assert.AreEqual(6, fileOpenVM.ProjectTemplateNames.Count);
        Assert.AreEqual("Blank Outline", fileOpenVM.ProjectTemplateNames[0]);
        Assert.AreEqual("Overview and Story Problem", fileOpenVM.ProjectTemplateNames[1]);
        Assert.AreEqual("Folders", fileOpenVM.ProjectTemplateNames[2]);
        Assert.AreEqual("External and Internal Problems", fileOpenVM.ProjectTemplateNames[3]);
        Assert.AreEqual("Protagonist and Antagonist", fileOpenVM.ProjectTemplateNames[4]);
        Assert.AreEqual("Problems and Characters", fileOpenVM.ProjectTemplateNames[5]);
    }
}
