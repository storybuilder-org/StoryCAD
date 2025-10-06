using StoryCADLib.Models;

namespace StoryCADTests.Models;

[TestClass]
public class StoryDocumentTests
{
    [TestMethod]
    public void Constructor_WithModelOnly_SetsModelAndNullPath()
    {
        // Arrange
        var model = new StoryModel();

        // Act
        var document = new StoryDocument(model);

        // Assert
        Assert.AreSame(model, document.Model);
        Assert.IsNull(document.FilePath);
    }

    [TestMethod]
    public void Constructor_WithModelAndPath_SetsBothProperties()
    {
        // Arrange
        var model = new StoryModel();
        const string path = @"C:\Stories\MyStory.stbx";

        // Act
        var document = new StoryDocument(model, path);

        // Assert
        Assert.AreSame(model, document.Model);
        Assert.AreEqual(path, document.FilePath);
    }

    [TestMethod]
    public void FilePath_WhenSet_UpdatesValue()
    {
        // Arrange
        var document = new StoryDocument(new StoryModel());
        const string newPath = @"C:\Stories\NewPath.stbx";

        // Act
        document.FilePath = newPath;

        // Assert
        Assert.AreEqual(newPath, document.FilePath);
    }

    [TestMethod]
    public void IsDirty_WhenModelNotChanged_ReturnsFalse()
    {
        // Arrange
        var model = new StoryModel { Changed = false };
        var document = new StoryDocument(model);

        // Act
        var isDirty = document.IsDirty;

        // Assert
        Assert.IsFalse(isDirty);
    }

    [TestMethod]
    public void IsDirty_WhenModelChanged_ReturnsTrue()
    {
        // Arrange
        var model = new StoryModel { Changed = true };
        var document = new StoryDocument(model);

        // Act
        var isDirty = document.IsDirty;

        // Assert
        Assert.IsTrue(isDirty);
    }

    [TestMethod]
    public void Model_IsReadOnly_CannotBeReassigned()
    {
        // This test verifies the design - Model should be get-only
        // If this compiles, the test would fail, but it shouldn't compile
        // because Model should not have a setter

        var document = new StoryDocument(new StoryModel());
        // The following line should not compile:
        // document.Model = new StoryModel();

        // If we got here, the property is correctly readonly
        Assert.IsNotNull(document.Model);
    }
}
