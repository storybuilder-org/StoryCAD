using System.Reflection;
using StoryCADLib.Models;

#nullable disable

namespace StoryCADTests;

[TestClass]
public class InstallServiceTests
{
    private readonly Assembly libAssembly;
    private readonly string[] libResources;

    public InstallServiceTests()
    {
        libAssembly = Assembly.GetAssembly(typeof(StoryModel));
        libResources = libAssembly.GetManifestResourceNames();
    }

    [TestMethod]
    public void TestResources()
    {
        Assert.IsTrue(libResources.Length >= 23,
            $"Expected at least 23 resources, but found {libResources.Length}. Resources: {string.Join(", ", libResources)}");

        // Check for key resources - use Contains with partial match since UNO may change resource names
        Assert.IsTrue(libResources.Any(r => r.Contains("Bibliog.txt")),
            $"Should contain Bibliog.txt. Available: {string.Join(", ", libResources)}");
        Assert.IsTrue(libResources.Any(r => r.Contains("A Doll") || r.Contains("ADoll")),
            "Should contain A Doll's House sample");
        Assert.IsTrue(libResources.Any(r => r.Contains("Old Man") || r.Contains("OldMan")),
            "Should contain The Old Man and the Sea sample");
    }
}
