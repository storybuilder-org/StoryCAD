using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.ViewModels;

namespace StoryCADTests.Models;

[TestClass]
public class CharacterModelTests
{
    /// <summary>
    ///     Tests if blank traits can be added.
    ///     (Tests PR #600)
    /// </summary>
    [TestMethod]
    public void TestBlankTraits()
    {
        var CharVM = Ioc.Default.GetRequiredService<CharacterViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var model = new StoryModel();
        appState.CurrentDocument = new StoryDocument(model);
        var x = new CharacterModel("TestCharacter", model, null);
        CharVM.Activate(x);
        CharVM.NewTrait = string.Empty;
        CharVM.AddTraitCommand.Execute(null);
        Assert.IsTrue(CharVM.CharacterTraits.Count == 0);
        CharVM.NewTrait = " TEST ";
        CharVM.AddTraitCommand.Execute(null);
        Assert.IsTrue(CharVM.CharacterTraits.Count == 1);
    }

    /// <summary>
    ///     The parameterless constructor is used by JSON deserialization, so a legacy .stbx
    ///     predating the Images feature (no "Images" key) must still end up with a non-null
    ///     list, matching every other CharacterModel constructor's invariant.
    /// </summary>
    [TestMethod]
    public void Constructor_Parameterless_InitializesImagesToEmptyList()
    {
        var character = new CharacterModel();

        Assert.IsNotNull(character.Images);
        Assert.AreEqual(0, character.Images.Count);
    }
}
