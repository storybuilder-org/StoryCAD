﻿using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models.Tools;

namespace StoryCADTests;

[TestClass]
public class ToolLoaderTests
{
    [TestMethod]
    public void TestToolCounts()
    {
        ToolsData toolsdata = Ioc.Default.GetService<ToolsData>();
        Assert.AreEqual(5, toolsdata.KeyQuestionsSource.Keys.Count);
        Assert.AreEqual(11, toolsdata.StockScenesSource.Keys.Count);
        Assert.AreEqual(9, toolsdata.TopicsSource.Count);
        Assert.AreEqual(18, toolsdata.MasterPlotsSource.Count);
        Assert.AreEqual(36, toolsdata.DramaticSituationsSource.Count);
        Assert.AreEqual(24, toolsdata.KeyQuestionsSource["Story Overview"].Count);
        Assert.AreEqual(11, toolsdata.StockScenesSource["Chase Scenes"].Count);
    }
}
