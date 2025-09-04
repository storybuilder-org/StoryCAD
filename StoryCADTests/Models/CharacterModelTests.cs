using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using System;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCADTests;

[TestClass]
public class CharacterModelTests
{
    /// <summary>
    /// Tests if blank traits can be added.
    /// (Tests PR #600)
    /// </summary>
    [TestMethod]
    public void TestBlankTraits()
    {
        CharacterViewModel CharVM = Ioc.Default.GetRequiredService<CharacterViewModel>();
        var appState = Ioc.Default.GetRequiredService<StoryCAD.Models.AppState>();
        var model = new StoryModel();
        appState.CurrentDocument = new StoryCAD.Models.StoryDocument(model, null);
        var x = new CharacterModel("TestCharacter", model, null);
        CharVM.Activate(x);
        CharVM.NewTrait = String.Empty;
        CharVM.AddTraitCommand.Execute(null);
        Assert.IsTrue(CharVM.CharacterTraits.Count == 0);
        CharVM.NewTrait = " TEST ";
        CharVM.AddTraitCommand.Execute(null);
        Assert.IsTrue(CharVM.CharacterTraits.Count == 1);
    }
}
