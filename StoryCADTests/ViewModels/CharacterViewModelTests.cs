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

    private static CharacterModel CreateCharacter(string name, StoryModel model, params string[] traits)
    {
        var character = new CharacterModel(name, model, null);
        character.TraitList.AddRange(traits);
        return character;
    }

    /// <summary>
    ///     Issue #1681: the trait selection from one character must not carry over
    ///     to the next character, whose trait list may be shorter.
    /// </summary>
    [TestMethod]
    public void RemoveTrait_AfterSwitchingToCharacterWithFewerTraits_DoesNotThrow()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
        var model = new StoryModel();
        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(model);
        var first = CreateCharacter("First", model, "Brave", "Loyal", "Stubborn");
        var second = CreateCharacter("Second", model, "Shy");
        viewModel.Activate(first);
        viewModel.ExistingTraitIndex = 2;
        viewModel.Deactivate(second);
        viewModel.Activate(second);

        // Act
        viewModel.RemoveTraitCommand.Execute(null);

        // Assert
        CollectionAssert.AreEqual(new[] { "Shy" }, viewModel.CharacterTraits.ToArray());
    }

    /// <summary>
    ///     Issue #1681: after the last trait is removed, the index points past the end
    ///     of the list; a second remove must not throw.
    /// </summary>
    [TestMethod]
    public void RemoveTrait_AfterLastTraitRemoved_DoesNotThrow()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
        var model = new StoryModel();
        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(model);
        viewModel.Activate(CreateCharacter("Character", model, "Brave", "Loyal"));
        viewModel.ExistingTraitIndex = 1;
        viewModel.RemoveTraitCommand.Execute(null);

        // Act
        viewModel.RemoveTraitCommand.Execute(null);

        // Assert
        CollectionAssert.AreEqual(new[] { "Brave" }, viewModel.CharacterTraits.ToArray());
    }

    /// <summary>
    ///     Issue #1681: a selection index that is valid for the next character must
    ///     still be cleared, or Remove Trait deletes a trait the user did not pick.
    /// </summary>
    [TestMethod]
    public void Activate_AfterTraitSelectedOnPreviousCharacter_ClearsTraitSelection()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
        var model = new StoryModel();
        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(model);
        var first = CreateCharacter("First", model, "Brave", "Loyal");
        var second = CreateCharacter("Second", model, "Shy", "Kind", "Proud");
        viewModel.Activate(first);
        viewModel.ExistingTraitIndex = 1;
        viewModel.Deactivate(second);

        // Act
        viewModel.Activate(second);
        viewModel.RemoveTraitCommand.Execute(null);

        // Assert
        Assert.AreEqual(-1, viewModel.ExistingTraitIndex);
        CollectionAssert.AreEqual(new[] { "Shy", "Kind", "Proud" }, viewModel.CharacterTraits.ToArray());
    }

    /// <summary>
    ///     Issue #1681: index 0 on an empty trait list (the index's starting value)
    ///     must not throw.
    /// </summary>
    [TestMethod]
    public void RemoveTrait_OnCharacterWithNoTraits_DoesNotThrow()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
        var model = new StoryModel();
        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(model);
        viewModel.Activate(CreateCharacter("Character", model));
        viewModel.ExistingTraitIndex = 0;

        // Act
        viewModel.RemoveTraitCommand.Execute(null);

        // Assert
        Assert.AreEqual(0, viewModel.CharacterTraits.Count);
    }

    [TestMethod]
    public void RemoveTrait_WithTraitSelected_RemovesSelectedTrait()
    {
        // Arrange
        var viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
        var model = new StoryModel();
        Ioc.Default.GetRequiredService<AppState>().CurrentDocument = new StoryDocument(model);
        viewModel.Activate(CreateCharacter("Character", model, "Brave", "Loyal", "Stubborn"));
        viewModel.ExistingTraitIndex = 1;

        // Act
        viewModel.RemoveTraitCommand.Execute(null);

        // Assert
        CollectionAssert.AreEqual(new[] { "Brave", "Stubborn" }, viewModel.CharacterTraits.ToArray());
    }
}
