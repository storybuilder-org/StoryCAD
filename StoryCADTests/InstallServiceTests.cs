using System.Linq;
using System.Reflection;
using System.IO;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;

namespace StoryCADTests;

[TestClass]
public class InstallServiceTests
{   
    private readonly Assembly libAssembly;
    private string[] libResources;

    [TestMethod]
    public void TestResources()
    {
        Assert.AreEqual(28, libResources.Length);
		Assert.IsTrue(libResources.Contains("StoryCAD.Assets.Install.Bibliog.txt"));
		Assert.IsTrue(libResources.Contains("StoryCAD.Assets.Install.samples.A Doll's House.stbx"));
		Assert.IsTrue(libResources.Contains("StoryCAD.Assets.Install.samples.The Old Man and the Sea.stbx"));
    }

    [TestMethod]
    public void TestRootDirectory()
    {
        var rootDir = Ioc.Default.GetRequiredService<AppState>().RootDirectory;
        foreach (string resource in libResources)
        {
            // convert manifest name to relative file path
            var segments = resource.Split('.').Skip(2).ToArray();
            string fileName = string.Join('.', segments[^2], segments[^1]);
            string relativePath = Path.Combine(Path.Combine(segments.Take(segments.Length - 2).ToArray()), fileName);
            string diskPath = Path.Combine(rootDir, relativePath);
            Assert.IsTrue(File.Exists(diskPath), $"Missing resource on disk: {diskPath}");
        }
    }

    public InstallServiceTests()
    {
        libAssembly = Assembly.GetAssembly(typeof(StoryModel));
        libResources = libAssembly.GetManifestResourceNames();
    }

}
