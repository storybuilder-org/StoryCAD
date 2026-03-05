using StoryCADLib.Models.Tools;

namespace StoryCADTests.Models;

[TestClass]
public class ToolsDataTests
{
    [TestMethod]
    public void ResolveTopicPath_UnixRootedPath_ReturnsOriginalPath()
    {
        if (OperatingSystem.IsWindows()) return;

        string unixPath = "/Users/foo/bar.txt";
        string result = ToolsData.ResolveTopicPath(unixPath);
        Assert.AreEqual(unixPath, result);
    }

    [TestMethod]
    public void ResolveTopicPath_WindowsRootedPath_ReturnsOriginalPath()
    {
        if (!OperatingSystem.IsWindows()) return;

        string windowsPath = @"C:\Users\foo\bar.txt";
        string result = ToolsData.ResolveTopicPath(windowsPath);
        Assert.AreEqual(windowsPath, result);
    }

    [TestMethod]
    public void ResolveTopicPath_BareFilename_CombinesWithTempPath()
    {
        string bareFilename = "bar.txt";
        string result = ToolsData.ResolveTopicPath(bareFilename);
        string expected = Path.Combine(Path.GetTempPath(), bareFilename);
        Assert.AreEqual(expected, result);
    }
}
