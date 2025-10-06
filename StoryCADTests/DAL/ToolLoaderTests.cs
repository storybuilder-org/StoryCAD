using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models.Tools;

#nullable disable

namespace StoryCADTests.DAL;

[TestClass]
public class ToolLoaderTests
{
    [TestMethod]
    public void TestToolCounts()
    {
        var toolsdata = Ioc.Default.GetService<ToolsData>();
        Assert.AreEqual(5, toolsdata.KeyQuestionsSource.Keys.Count);
        Assert.AreEqual(11, toolsdata.StockScenesSource.Keys.Count);
        Assert.AreEqual(9, toolsdata.TopicsSource.Count);
        Assert.AreEqual(18, toolsdata.MasterPlotsSource.Count);
        Assert.AreEqual(36, toolsdata.DramaticSituationsSource.Count);
        Assert.AreEqual(9, toolsdata.KeyQuestionsSource["Story Overview"].Count);
        Assert.AreEqual(11, toolsdata.StockScenesSource["Chase Scenes"].Count);

        // Test the newly added name lists and relationships
        Assert.IsNotNull(toolsdata.MaleFirstNamesSource);
        Assert.IsTrue(toolsdata.MaleFirstNamesSource.Count > 0);
        Assert.IsTrue(toolsdata.MaleFirstNamesSource.Contains("James"));

        Assert.IsNotNull(toolsdata.FemaleFirstNamesSource);
        Assert.IsTrue(toolsdata.FemaleFirstNamesSource.Count > 0);
        Assert.IsTrue(toolsdata.FemaleFirstNamesSource.Contains("Mary"));

        Assert.IsNotNull(toolsdata.LastNamesSource);
        Assert.IsTrue(toolsdata.LastNamesSource.Count > 0);
        Assert.IsTrue(toolsdata.LastNamesSource.Contains("Smith"));

        Assert.IsNotNull(toolsdata.RelationshipsSource);
        Assert.IsTrue(toolsdata.RelationshipsSource.Count > 0);
        Assert.IsTrue(toolsdata.RelationshipsSource.Contains("Mother"));
    }
}
