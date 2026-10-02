using StoryCADLib.Models;

namespace StoryCADTests.Models;

[TestClass]
public class SettingModelTests
{
    [TestInitialize]
    public void ClearSettingNames()
    {
        SettingModel.SettingNames.Clear();
    }

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

    [TestMethod]
    public void RenameInNameList_UnknownOldName_DoesNotThrow()
    {
        SettingModel.RenameInNameList("Dodger Stadium", "Chavez Ravine");
        Assert.AreEqual(0, SettingModel.SettingNames.Count);
    }

    [TestMethod]
    public void RenameInNameList_KnownOldName_ReplacesEntry()
    {
        SettingModel.SettingNames.Add("Dodger Stadium");
        SettingModel.RenameInNameList("Dodger Stadium", "Chavez Ravine");
        Assert.AreEqual(1, SettingModel.SettingNames.Count);
        Assert.AreEqual("Chavez Ravine", SettingModel.SettingNames[0]);
    }

    [TestMethod]
    public void RenameInNameList_NullOldName_DoesNotThrow()
    {
        SettingModel.RenameInNameList(null, "Chavez Ravine");
        Assert.AreEqual(0, SettingModel.SettingNames.Count);
    }
}
