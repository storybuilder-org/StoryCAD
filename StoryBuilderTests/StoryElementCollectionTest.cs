using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryBuilder.Models;

namespace StoryBuilderTests;

[TestClass]
public class StoryElementCollectionTest
{
    private StoryElementCollection elements;
    private StoryElement element;

    [TestMethod]
    public void TestStoryElements()
    {
        StoryModel model = new();
        elements = new StoryElementCollection();
        Assert.IsNotNull(elements.StoryElementGuids);
        Assert.IsNotNull(elements.Characters);
        Assert.IsNotNull(elements.Settings);
        Assert.IsNotNull(elements.Problems);
        Assert.AreEqual(0, elements.Count);
        Assert.AreEqual(0, elements.StoryElementGuids.Values.Count);
        Assert.AreEqual(0, elements.Characters.Count);
        Assert.AreEqual(0, elements.Problems.Count);
        // Test adding elements
        element = new StoryElement("Protagonist", StoryItemType.Character, model);
        elements.Add(element);
        Assert.AreEqual(1, elements.Count);
        Assert.AreEqual(1, elements.StoryElementGuids.Values.Count);
        Assert.AreEqual(1, elements.Characters.Count);
        Assert.AreEqual(0, elements.Problems.Count);
        // Test deleting elements            
        int i = elements.IndexOf(element);
        elements.RemoveAt(i);
        Assert.AreEqual(0, elements.Count);
        Assert.AreEqual(0, elements.StoryElementGuids.Values.Count);
        Assert.AreEqual(0, elements.Characters.Count);
        //TODO: Test Reset function
    }

}