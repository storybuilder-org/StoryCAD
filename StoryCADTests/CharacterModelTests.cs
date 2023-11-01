using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using System;

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
        Ioc.Default.GetService<ShellViewModel>().StoryModel = new();
        var x = new CharacterModel(Ioc.Default.GetService<ShellViewModel>().StoryModel);
        CharVM.Activate(x);
        CharVM.NewTrait = String.Empty;
        CharVM.AddTraitCommand.Execute(null);

    }
}
