using StoryCADLib.Models;

namespace StoryCADTests.Models;

[TestClass]
public class SettingModelTests
{
    /// <summary>
    ///     The parameterless constructor is used by JSON deserialization, so a legacy .stbx
    ///     predating the Images feature (no "Images" key) must still end up with a non-null
    ///     list, matching every other SettingModel constructor's invariant.
    /// </summary>
    [TestMethod]
    public void Constructor_Parameterless_InitializesImagesToEmptyList()
    {
        var setting = new SettingModel();

        Assert.IsNotNull(setting.Images);
        Assert.AreEqual(0, setting.Images.Count);
    }
}
