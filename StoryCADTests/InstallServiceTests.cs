﻿using System.Linq;
using System.Reflection;
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
        //TODO: Verify GlobalData.RootDirectory contains all resources
        Assert.AreEqual(0, 0);
    }

    public InstallServiceTests()
    {
        libAssembly = Assembly.GetAssembly(typeof(StoryModel));
        libResources = libAssembly.GetManifestResourceNames();
    }

}
