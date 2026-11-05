using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;

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
    public void CharacterViewModel_ImplementsIReloadable()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();

        // Act
        var reloadable = viewModel as IReloadable;

        // Assert
        Assert.IsNotNull(reloadable);
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

    [TestMethod]
    public void ReloadFromModel_ExistsAsPublicMethod()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();

        // Act
        var method = viewModel.GetType().GetMethod("ReloadFromModel");

        // Assert
        Assert.IsNotNull(method);
        Assert.IsTrue(method.IsPublic);
    }

    /// <summary>
    ///     Issue #1681: production crashed with ArgumentOutOfRangeException when the
    ///     selected trait number was outside the trait list.
    /// </summary>
    [TestMethod]
    public void RemoveTrait_WithSelectionOutsideTraitList_DoesNotThrow()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
        var model = new StoryModel();
        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(model);
        viewModel.Activate(new CharacterModel("Character", model, null));
        viewModel.ExistingTraitIndex = 0;

        // Act
        viewModel.RemoveTraitCommand.Execute(null);

        // Assert
        Assert.AreEqual(0, viewModel.CharacterTraits.Count);
    }
}
