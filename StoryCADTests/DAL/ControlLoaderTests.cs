using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels;

namespace StoryCADTests;

[TestClass]
public class ControlLoaderTests
{
    [TestMethod]
    public void TestRelationTypesLoaded()
    {
        // Get the ControlData instance which loads relationships
        ControlData controlData = Ioc.Default.GetService<ControlData>();
        
        // Verify RelationTypes is not null
        Assert.IsNotNull(controlData.RelationTypes);
        
        // Verify it contains some expected relationships
        Assert.IsTrue(controlData.RelationTypes.Count > 0, "RelationTypes should contain items");
        
        // Check for some specific expected relationships
        Assert.IsTrue(controlData.RelationTypes.Contains("Mother"), "Should contain 'Mother' relationship");
        Assert.IsTrue(controlData.RelationTypes.Contains("Father"), "Should contain 'Father' relationship");
        Assert.IsTrue(controlData.RelationTypes.Contains("Friend"), "Should contain 'Friend' relationship");
        Assert.IsTrue(controlData.RelationTypes.Contains("Partner"), "Should contain 'Partner' relationship");
    }
}