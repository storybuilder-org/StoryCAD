#if WINDOWS10_0_22621_0_OR_GREATER
using StoryCADLib.DAL;

namespace StoryCADTests.DAL;

/// <summary>
/// Windows-specific tests for StoryIO that test Windows-specific path validation
/// </summary>
public partial class StoryIOTests
{
    [TestMethod]
    [TestCategory("Windows")]
    public void IsValidPath_WithInvalidPath_ReturnsFalse()
    {
        // Note: This test only works reliably on Windows where invalid path characters are well-defined
        // On Unix-like systems, most characters are valid in paths, making this test non-portable

        // Arrange - Windows doesn't allow these characters in filenames: < > : | ?
        var invalidPath = Path.Combine(Path.GetTempPath(), "test<>:|?.stbx");

        // Act & Assert
        Assert.IsFalse(StoryIO.IsValidPath(invalidPath));
    }
}
#endif
