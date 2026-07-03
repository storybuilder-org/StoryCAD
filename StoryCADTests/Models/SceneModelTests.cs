using StoryCADLib.Models;

namespace StoryCADTests.Models;

[TestClass]
public class SceneModelTests
{
    /// <summary>
    ///     The parameterless constructor is used by JSON deserialization, so a legacy .stbx
    ///     predating the Images feature (no "Images" key) must still end up with a non-null
    ///     list, matching every other SceneModel constructor's invariant.
    /// </summary>
    [TestMethod]
    public void Constructor_Parameterless_InitializesImagesToEmptyList()
    {
        var scene = new SceneModel();

        Assert.IsNotNull(scene.Images);
        Assert.AreEqual(0, scene.Images.Count);
    }
}
