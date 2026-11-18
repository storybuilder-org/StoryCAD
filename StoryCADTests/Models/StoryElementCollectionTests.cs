using StoryCADLib.Models;

#nullable disable

namespace StoryCADTests.Models;

[TestClass]
public class StoryElementCollectionTests
{
    [TestMethod]
    public void Remove_EachFiledType_LeavesItsDerivativeList()
    {
        var model = new StoryModel();
        var elements = model.StoryElements;
        var character = new CharacterModel("Character", model, null);
        var setting = new SettingModel("Setting", model, null);
        var problem = new ProblemModel("Problem", model, null);
        var scene = new SceneModel("Scene", model, null);

        Assert.IsTrue(elements.Characters.Contains(character));
        Assert.IsTrue(elements.Settings.Contains(setting));
        Assert.IsTrue(elements.Problems.Contains(problem));
        Assert.IsTrue(elements.Scenes.Contains(scene));

        elements.Remove(character);
        elements.Remove(setting);
        elements.Remove(problem);
        elements.Remove(scene);

        Assert.IsFalse(elements.Characters.Contains(character));
        Assert.IsFalse(elements.Settings.Contains(setting));
        Assert.IsFalse(elements.Problems.Contains(problem));
        Assert.IsFalse(elements.Scenes.Contains(scene), "A deleted scene must leave the Scenes list");
        Assert.IsFalse(elements.StoryElementGuids.ContainsKey(scene.Uuid));
    }
}
