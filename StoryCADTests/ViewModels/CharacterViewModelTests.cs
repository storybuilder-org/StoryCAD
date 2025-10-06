using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Services;
using StoryCAD.ViewModels;

namespace StoryCADTests.ViewModels;

[TestClass]
public class CharacterViewModelTests
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
