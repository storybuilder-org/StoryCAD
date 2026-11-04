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

    #region New outline validation (#1682)

    private string _savedOutlineFolder;
    private string _savedOutlineName;

    // FileOpenVM is a singleton: put back what these tests change so later tests see the real values.
    [TestInitialize]
    public void SaveSharedState()
    {
        var vm = Ioc.Default.GetRequiredService<FileOpenVM>();
        _savedOutlineFolder = vm.OutlineFolder;
        _savedOutlineName = vm.OutlineName;
    }

    [TestCleanup]
    public void RestoreSharedState()
    {
        var vm = Ioc.Default.GetRequiredService<FileOpenVM>();
        vm.OutlineFolder = _savedOutlineFolder;
        vm.OutlineName = _savedOutlineName;
        vm.ShowWarning = false;
        vm.WarningText = null;
    }

    private static FileOpenVM NewOutlineVM(string folder, string name)
    {
        var vm = Ioc.Default.GetRequiredService<FileOpenVM>();
        vm.OutlineFolder = folder;
        vm.OutlineName = name;
        vm.ShowWarning = false;
        vm.WarningText = null;
        return vm;
    }

    [TestMethod]
    [TestCategory("CrossPlatform")]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(".stbx")]
    [DataRow(".STBX")]
    [DataRow("  .stbx")]
    public void ValidateNewOutline_BlankName_ShowsNameWarning(string name)
    {
        // Arrange
        var vm = NewOutlineVM(Path.GetTempPath(), name);

        // Act
        var valid = vm.ValidateNewOutline();

        // Assert
        Assert.IsFalse(valid);
        Assert.IsTrue(vm.ShowWarning);
        Assert.AreEqual(FileOpenVM.BlankNameWarning, vm.WarningText);
    }

    [TestMethod]
    [TestCategory("CrossPlatform")]
    [DataRow(".45 Magnum")]
    [DataRow(".hack")]
    public void ValidateNewOutline_NameStartingWithDot_IsAccepted(string name)
    {
        // Arrange - only a trailing ".stbx" is an extension; ".hack" is a title (#1682)
        var vm = NewOutlineVM(Path.GetTempPath(), name);

        // Act
        var valid = vm.ValidateNewOutline();

        // Assert
        Assert.IsTrue(valid, $"Warning shown: {vm.WarningText}");
    }

    [TestMethod]
    [TestCategory("CrossPlatform")]
    public void ValidateNewOutline_DisallowedCharacterInName_ShowsCharacterWarning()
    {
        // Arrange - '/' is not allowed in a file name on any platform
        var vm = NewOutlineVM(Path.GetTempPath(), "Bad/Name");

        // Act
        var valid = vm.ValidateNewOutline();

        // Assert
        Assert.IsFalse(valid);
        Assert.IsTrue(vm.ShowWarning);
        Assert.AreEqual(FileOpenVM.DisallowedCharactersWarning, vm.WarningText);
    }

    [TestMethod]
    [TestCategory("CrossPlatform")]
    [DataRow("")]
    [DataRow("Invalid\0Folder")]
    public void ValidateNewOutline_InvalidFolder_ShowsFolderWarning(string folder)
    {
        // Arrange
        var vm = NewOutlineVM(folder, "Good Name");

        // Act
        var valid = vm.ValidateNewOutline();

        // Assert
        Assert.IsFalse(valid);
        Assert.IsTrue(vm.ShowWarning);
        Assert.AreEqual(FileOpenVM.InvalidFolderWarning, vm.WarningText);
    }

    [TestMethod]
    [TestCategory("CrossPlatform")]
    public void ValidateNewOutline_ValidNameAndFolder_ReturnsTrueAndHidesWarning()
    {
        // Arrange
        var vm = NewOutlineVM(Path.GetTempPath(), "Good Name");
        vm.ShowWarning = true;

        // Act
        var valid = vm.ValidateNewOutline();

        // Assert
        Assert.IsTrue(valid);
        Assert.IsFalse(vm.ShowWarning);
    }

    [TestMethod]
    [TestCategory("CrossPlatform")]
    public async Task CreateFile_BlankName_ReturnsEmptyAndShowsNameWarning()
    {
        // Arrange
        var folder = Path.Combine(Path.GetTempPath(), $"StoryCADTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(folder);
        var vm = NewOutlineVM(folder, "");

        try
        {
            // Act
            var result = await vm.CreateFile();

            // Assert
            Assert.AreEqual("", result);
            Assert.AreEqual(FileOpenVM.BlankNameWarning, vm.WarningText);
            Assert.AreEqual(0, Directory.GetFiles(folder).Length);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    #endregion
}
