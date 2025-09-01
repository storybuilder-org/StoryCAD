using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services;
using StoryCAD.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;

namespace StoryCAD.Tests.ViewModels;

[TestClass]
public class CharacterViewModelISaveableTests
{
    [TestMethod]
    public void CharacterViewModel_ImplementsISaveable()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
        
        // Act
        var saveable = viewModel as ISaveable;
        
        // Assert
        Assert.IsNotNull(saveable);
    }
    
    [TestMethod]
    public void SaveModel_ExistsAsPublicMethod()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
        
        // Act
        var method = viewModel.GetType().GetMethod("SaveModel");
        
        // Assert
        Assert.IsNotNull(method);
        Assert.IsTrue(method.IsPublic);
    }
}