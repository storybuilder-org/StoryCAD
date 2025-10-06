using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.ViewModels;

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
}
