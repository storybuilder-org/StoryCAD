using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.ViewModels;

[TestClass]
public class CharacterViewModelTests
{
    private CharacterModel _characterModel;
    private StoryModel _storyModel;
    private CharacterViewModel _viewModel;
    private AppState _appState;

    [TestInitialize]
    public void TestInitialize()
    {
        // Create a test story model and character model
        _storyModel = new StoryModel();
        _characterModel = new CharacterModel("Test Character", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(_characterModel);

        // Get AppState and set up the current document
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _appState.CurrentDocument = new StoryDocument(_storyModel);

        // Initialize the view model
        _viewModel = Ioc.Default.GetRequiredService<CharacterViewModel>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _viewModel = null;
        _characterModel = null;
        _storyModel = null;
    }

    #region ISaveable Implementation Tests

    [TestMethod]
    public void CharacterViewModel_ImplementsISaveable()
    {
        // Arrange - ViewModel already created in TestInitialize

        // Act
        var saveable = _viewModel as ISaveable;

        // Assert
        Assert.IsNotNull(saveable);
    }

    [TestMethod]
    public void SaveModel_ExistsAsPublicMethod()
    {
        // Arrange - ViewModel already created in TestInitialize

        // Act
        var method = _viewModel.GetType().GetMethod("SaveModel");

        // Assert
        Assert.IsNotNull(method);
        Assert.IsTrue(method.IsPublic);
    }

    #endregion

    #region GetByGuid Null-Safety Tests (Bug Fix Verification)

    [TestMethod]
    public void LoadModel_WithRelationships_DoesNotThrowWhenStoryModelIsValid()
    {
        // Arrange - Create a partner character and add a relationship
        var partner = new CharacterModel("Partner Character", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(partner);

        // Add relationship to the character model
        var relationship = new RelationshipModel(_characterModel.Uuid, "Friend");
        relationship.PartnerUuid = partner.Uuid;
        _characterModel.RelationshipList.Add(relationship);

        // Act & Assert - Activate triggers LoadModel which loads relationships
        // This exercises StoryElement.GetByGuid with _storyModel parameter
        // Should not throw NullReferenceException
        _viewModel.Activate(_characterModel);

        // Verify relationship was loaded with partner populated
        Assert.AreEqual(1, _viewModel.CharacterRelationships.Count,
            "Should have loaded the relationship");
        Assert.IsNotNull(_viewModel.CharacterRelationships[0].Partner,
            "Partner should be populated via GetByGuid");
    }

    [TestMethod]
    public void Deactivate_AfterActivate_DoesNotThrow()
    {
        // Arrange - Simulate normal navigation lifecycle
        _viewModel.Activate(_characterModel);
        _viewModel.Name = "Modified Character Name";

        // Act & Assert - Deactivate triggers SaveModel, should not throw
        _viewModel.Deactivate(null);

        // Verify changes were saved
        Assert.AreEqual("Modified Character Name", _characterModel.Name);
    }

    #endregion

    #region Core Lifecycle Tests

    [TestMethod]
    public void Activate_WithValidCharacterModel_LoadsProperties()
    {
        // Arrange - Set up character model with known values
        _characterModel.Name = "Hero";
        _characterModel.Role = "Protagonist";
        _characterModel.StoryRole = "Main Character";

        // Act
        _viewModel.Activate(_characterModel);

        // Assert
        Assert.AreEqual("Hero", _viewModel.Name);
        Assert.AreEqual("Protagonist", _viewModel.Role);
        Assert.AreEqual("Main Character", _viewModel.StoryRole);
    }

    [TestMethod]
    public void SaveModel_WithModifiedProperties_UpdatesCharacterModel()
    {
        // Arrange
        _viewModel.Activate(_characterModel);

        // Act - Modify properties
        _viewModel.Name = "Modified Name";
        _viewModel.Role = "Antagonist";
        _viewModel.SaveModel();

        // Assert
        Assert.AreEqual("Modified Name", _characterModel.Name);
        Assert.AreEqual("Antagonist", _characterModel.Role);
    }

    #endregion
}
