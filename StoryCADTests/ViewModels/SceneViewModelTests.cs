using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Models;
using StoryCADLib.Services.Messages;
using StoryCADLib.ViewModels;

#nullable disable

namespace StoryCADTests.ViewModels;

[TestClass]
public class SceneViewModelTests
{
    #region Priority 3: Special Property Behaviors (STUBBED)

    [TestMethod]
    public void Name_WhenChanged_SendsNameChangedMessage()
    {
        // Arrange - Setup ViewModel and register for NameChangedMessage
        _viewModel.Activate(_sceneModel);

        var messageReceived = false;
        NameChangedMessage capturedMessage = null;

        // Register to receive NameChangedMessage
        WeakReferenceMessenger.Default.Register<NameChangedMessage>(this, (r, m) =>
        {
            messageReceived = true;
            capturedMessage = m;
        });

        var oldName = _viewModel.Name;
        var newName = "Updated Scene Name";

        // Act - Change the Name property
        _viewModel.Name = newName;

        // Assert - Verify NameChangedMessage was sent with correct values
        Assert.IsTrue(messageReceived, "NameChangedMessage should be sent when Name changes");
        Assert.IsNotNull(capturedMessage, "Captured message should not be null");
        Assert.IsNotNull(capturedMessage.Value, "Message value should not be null");
        Assert.AreEqual(oldName, capturedMessage.Value.OldName, "Message should contain old name");
        Assert.AreEqual(newName, capturedMessage.Value.NewName, "Message should contain new name");

        // Cleanup - Unregister from messenger
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    [TestMethod]
    public void Name_WhenSetToSameValue_DoesNotSendMessage()
    {
        // Arrange - Setup ViewModel and register for NameChangedMessage
        _viewModel.Activate(_sceneModel);

        var messageReceived = false;
        var initialName = _viewModel.Name;

        // Register to receive NameChangedMessage
        WeakReferenceMessenger.Default.Register<NameChangedMessage>(this, (r, m) =>
        {
            messageReceived = true;
        });

        // Act - Set Name to the same value (should NOT trigger message)
        _viewModel.Name = initialName;

        // Assert - Verify NameChangedMessage was NOT sent
        Assert.IsFalse(messageReceived, "NameChangedMessage should not be sent when Name is set to same value");

        // Cleanup - Unregister from messenger
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    // NOTE: Name_WhenChangeable_LogsNameChange test is intentionally skipped
    // Reason: Testing log calls would require mocking ILogService, which violates
    // the project's "no mocks" principle. Logging behavior is verified through
    // manual testing and production monitoring.

    [TestMethod]
    public void ViewpointCharacter_WhenSet_AddsCharacterToCast()
    {
        // Arrange - Create character with correct parent StoryModel, add to story, activate ViewModel
        var character = new CharacterModel("Viewpoint Character", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);
        var initialCount = _viewModel.CastList.Count;

        // Act - Set ViewpointCharacter to the character's Guid
        _viewModel.ViewpointCharacter = character.Uuid;

        // Assert - Verify character was added to CastList
        Assert.AreEqual(initialCount + 1, _viewModel.CastList.Count,
            "CastList count should increase when ViewpointCharacter is set");
        Assert.IsTrue(_viewModel.CastList.Any(c => c.Uuid == character.Uuid),
            "CastList should contain the viewpoint character");
    }

    [TestMethod]
    public void ViewpointCharacter_WithEmptyGuid_DoesNotAddToCast()
    {
        // Arrange - Activate ViewModel and capture initial CastList count
        _viewModel.Activate(_sceneModel);
        var initialCount = _viewModel.CastList.Count;

        // Act - Set ViewpointCharacter to Guid.Empty
        _viewModel.ViewpointCharacter = Guid.Empty;

        // Assert - Verify CastList count unchanged (empty Guid should not add to cast)
        Assert.AreEqual(initialCount, _viewModel.CastList.Count,
            "CastList count should not change when ViewpointCharacter is set to Guid.Empty");
    }

    [TestMethod]
    public void SelectedViewpointCharacter_WhenSet_UpdatesViewpointCharacterGuid()
    {
        // Arrange - Create character, add to story, activate ViewModel
        var character = CreateTestCharacter("Selected Viewpoint Character");
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);

        // Act - Set SelectedViewpointCharacter to the character
        _viewModel.SelectedViewpointCharacter = character;

        // Assert - Verify ViewpointCharacter Guid was updated
        Assert.AreEqual(character.Uuid, _viewModel.ViewpointCharacter,
            "ViewpointCharacter Guid should match SelectedViewpointCharacter.Uuid");
    }

    [TestMethod]
    public void SelectedViewpointCharacter_WhenSetToNull_ClearsViewpointCharacter()
    {
        // Arrange - Create character, set as viewpoint character, activate ViewModel
        var character = CreateTestCharacter("Viewpoint To Clear");
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);
        _viewModel.SelectedViewpointCharacter = character;

        // Verify initial setup (character is set)
        Assert.AreEqual(character.Uuid, _viewModel.ViewpointCharacter,
            "Setup: ViewpointCharacter should be set to character Guid");

        // Act - Clear SelectedViewpointCharacter by setting to null
        _viewModel.SelectedViewpointCharacter = null;

        // Assert - Verify ViewpointCharacter was cleared to Guid.Empty
        Assert.AreEqual(Guid.Empty, _viewModel.ViewpointCharacter,
            "ViewpointCharacter should be Guid.Empty when SelectedViewpointCharacter is null");
    }

    [TestMethod]
    public void SelectedSetting_WhenSet_UpdatesSettingGuid()
    {
        // Arrange - Create setting, add to story, activate ViewModel
        var setting = CreateTestSetting("Selected Setting");
        _storyModel.StoryElements.Settings.Add(setting);
        _viewModel.Activate(_sceneModel);

        // Act - Set SelectedSetting to the setting
        _viewModel.SelectedSetting = setting;

        // Assert - Verify Setting Guid was updated
        Assert.AreEqual(setting.Uuid, _viewModel.Setting,
            "Setting Guid should match SelectedSetting.Uuid");
    }

    [TestMethod]
    public void SelectedSetting_WhenSetToNull_ClearsSettingGuid()
    {
        // Arrange - Create setting, set as selected setting, activate ViewModel
        var setting = CreateTestSetting("Setting To Clear");
        _storyModel.StoryElements.Settings.Add(setting);
        _viewModel.Activate(_sceneModel);
        _viewModel.SelectedSetting = setting;

        // Verify initial setup (setting is set)
        Assert.AreEqual(setting.Uuid, _viewModel.Setting,
            "Setup: Setting should be set to setting Guid");

        // Act - Clear SelectedSetting by setting to null
        _viewModel.SelectedSetting = null;

        // Assert - Verify Setting was cleared to Guid.Empty
        Assert.AreEqual(Guid.Empty, _viewModel.Setting,
            "Setting should be Guid.Empty when SelectedSetting is null");
    }

    [TestMethod]
    public void SelectedProtagonist_WhenSet_UpdatesProtagonistGuid()
    {
        // Arrange - Create character, add to story, activate ViewModel
        var character = CreateTestCharacter("Selected Protagonist");
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);

        // Act - Set SelectedProtagonist to the character
        _viewModel.SelectedProtagonist = character;

        // Assert - Verify Protagonist Guid was updated
        Assert.AreEqual(character.Uuid, _viewModel.Protagonist,
            "Protagonist Guid should match SelectedProtagonist.Uuid");
    }

    [TestMethod]
    public void SelectedAntagonist_WhenSet_UpdatesAntagonistGuid()
    {
        // Arrange - Create character, add to story, activate ViewModel
        var character = CreateTestCharacter("Selected Antagonist");
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);

        // Act - Set SelectedAntagonist to the character
        _viewModel.SelectedAntagonist = character;

        // Assert - Verify Antagonist Guid was updated
        Assert.AreEqual(character.Uuid, _viewModel.Antagonist,
            "Antagonist Guid should match SelectedAntagonist.Uuid");
    }

    #endregion

    #region Test Infrastructure Fields

    // Primary test subjects
    private SceneViewModel _viewModel;
    private SceneModel _sceneModel;
    private StoryModel _storyModel;

    // Supporting test data
    private CharacterModel _testCharacter1;
    private CharacterModel _testCharacter2;
    private SettingModel _testSetting;

    #endregion

    #region TestInitialize and TestCleanup

    [TestInitialize]
    public void TestInitialize()
    {
        // Create base story structure for testing
        _storyModel = CreateTestStoryModel();
        _sceneModel = CreateTestSceneModel(_storyModel);

        // Setup ViewModel with IoC container (no fake implementations)
        _viewModel = SetupViewModelWithStory();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Unsubscribe from PropertyChanged to prevent memory leaks
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= _viewModel.OnPropertyChanged;
        }

        // Clean up test objects
        _viewModel = null;
        _sceneModel = null;
        _storyModel = null;
        _testCharacter1 = null;
        _testCharacter2 = null;
        _testSetting = null;
    }

    #endregion

    #region Priority 1: Core Lifecycle Tests (FULLY IMPLEMENTED)

    [TestMethod]
    public void Activate_WithValidSceneModel_LoadsAllProperties()
    {
        // Arrange - Create a fresh ViewModel and SceneModel with known values
        var viewModel = Ioc.Default.GetService<SceneViewModel>();
        var sceneModel = CreateTestSceneModel(_storyModel);

        // Set specific values to verify loading
        sceneModel.Name = "Test Scene Name";
        sceneModel.SceneDescription = "Test Scene Description";
        sceneModel.Date = "2025-01-15";
        sceneModel.Time = "Morning";
        sceneModel.SceneType = "Action";
        sceneModel.ValueExchange = "+/-";
        sceneModel.Events = "Important events";
        sceneModel.Consequences = "Major consequences";
        sceneModel.Significance = "Critical significance";
        sceneModel.Realization = "Key realization";
        sceneModel.Review = "Scene review";
        sceneModel.Notes = "Scene notes";

        // Act - Activate the ViewModel with the SceneModel
        viewModel.Activate(sceneModel);

        // Assert - Verify all properties loaded correctly
        Assert.AreEqual(sceneModel.Uuid, viewModel.Uuid, "Uuid should match");
        Assert.AreEqual("Test Scene Name", viewModel.Name, "Name should match");
        Assert.AreEqual("Test Scene Description", viewModel.Description, "Description should match");
        Assert.AreEqual("2025-01-15", viewModel.Date, "Date should match");
        Assert.AreEqual("Morning", viewModel.Time, "Time should match");
        Assert.AreEqual("Action", viewModel.SceneType, "SceneType should match");
        Assert.AreEqual("+/-", viewModel.ValueExchange, "ValueExchange should match");
        Assert.AreEqual("Important events", viewModel.Events, "Events should match");
        Assert.AreEqual("Major consequences", viewModel.Consequences, "Consequences should match");
        Assert.AreEqual("Critical significance", viewModel.Significance, "Significance should match");
        Assert.AreEqual("Key realization", viewModel.Realization, "Realization should match");
        Assert.AreEqual("Scene review", viewModel.Review, "Review should match");
        Assert.AreEqual("Scene notes", viewModel.Notes, "Notes should match");

        // Verify collections are initialized
        Assert.IsNotNull(viewModel.CastList, "CastList should be initialized");
        Assert.IsNotNull(viewModel.ScenePurposes, "ScenePurposes should be initialized");
    }

    [TestMethod]
    public void SaveModel_WithModifiedProperties_UpdatesSceneModel()
    {
        // Arrange - Activate ViewModel with SceneModel
        _viewModel.Activate(_sceneModel);

        // Verify initial state
        var originalName = _sceneModel.Name;
        var originalDescription = _sceneModel.SceneDescription;
        var originalDate = _sceneModel.Date;

        // Act - Modify properties in ViewModel
        _viewModel.Name = "Modified Scene Name";
        _viewModel.Description = "Modified description";
        _viewModel.Date = "2025-10-04";
        _viewModel.Time = "Evening";
        _viewModel.SceneType = "Dialogue";
        _viewModel.ValueExchange = "-/+";
        _viewModel.Events = "Modified events";
        _viewModel.Consequences = "Modified consequences";
        _viewModel.Significance = "Modified significance";
        _viewModel.SaveModel();

        // Assert - Verify SceneModel was updated
        Assert.AreEqual("Modified Scene Name", _sceneModel.Name, "Name should be updated");
        Assert.AreEqual("Modified description", _sceneModel.SceneDescription, "Description should be updated");
        Assert.AreEqual("2025-10-04", _sceneModel.Date, "Date should be updated");
        Assert.AreEqual("Evening", _sceneModel.Time, "Time should be updated");
        Assert.AreEqual("Dialogue", _sceneModel.SceneType, "SceneType should be updated");
        Assert.AreEqual("-/+", _sceneModel.ValueExchange, "ValueExchange should be updated");
        Assert.AreEqual("Modified events", _sceneModel.Events, "Events should be updated");
        Assert.AreEqual("Modified consequences", _sceneModel.Consequences, "Consequences should be updated");
        Assert.AreEqual("Modified significance", _sceneModel.Significance, "Significance should be updated");
    }

    [TestMethod]
    public void Deactivate_AfterModifications_SavesChanges()
    {
        // Arrange - Activate and modify ViewModel
        _viewModel.Activate(_sceneModel);
        var modifiedName = "Changed Scene Name";
        _viewModel.Name = modifiedName;

        // Act - Deactivate triggers SaveModel
        _viewModel.Deactivate(null);

        // Assert - Verify changes were saved to model
        Assert.AreEqual(modifiedName, _sceneModel.Name, "Deactivate should save Name changes");
    }

    [TestMethod]
    public void LoadCastList_WithCastMembers_DoesNotThrowWhenStoryModelIsValid()
    {
        // Arrange - Add a character to the scene's cast members in the model
        var character = new CharacterModel("Cast Member", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(character);
        _sceneModel.CastMembers.Add(character.Uuid);

        // Act & Assert - Activate triggers LoadModel which calls LoadCastList
        // This exercises StoryElement.GetByGuid with _storyModel parameter
        // Should not throw NullReferenceException
        _viewModel.Activate(_sceneModel);

        // Verify cast was loaded
        Assert.AreEqual(1, _viewModel.CastList.Count, "CastList should contain the cast member");
    }

    [TestMethod]
    public void ViewpointCharacter_WhenSetWithValidCharacter_DoesNotThrow()
    {
        // Arrange - This tests the GetByGuid call in ViewpointCharacter setter
        var character = new CharacterModel("VP Character", _storyModel, null);
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);

        // Act & Assert - Should not throw when using _storyModel in GetByGuid
        _viewModel.ViewpointCharacter = character.Uuid;

        // Verify character was added to cast
        Assert.IsTrue(_viewModel.CastList.Any(c => c.Uuid == character.Uuid),
            "Viewpoint character should be added to cast list");
    }

    #endregion

    #region Priority 2: Cast Management Tests (PARTIALLY IMPLEMENTED - 3 examples)

    [TestMethod]
    public void AddCastMember_WithValidCharacter_AddsToList()
    {
        // Arrange - Setup ViewModel with story containing character
        var character = CreateTestCharacter("John Doe");
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);
        var initialCount = _viewModel.CastList.Count;

        // Act - Add character to cast
        _viewModel.AddCastMember(character);

        // Assert - Verify character was added
        Assert.AreEqual(initialCount + 1, _viewModel.CastList.Count, "CastList count should increase");
        Assert.IsTrue(_viewModel.CastList.Contains(character), "CastList should contain the added character");
        Assert.IsTrue(_viewModel.CastList.Any(c => c.Uuid == character.Uuid),
            "CastList should contain character by Uuid");
    }

    [TestMethod]
    public void RemoveCastMember_WithExistingCharacter_RemovesFromList()
    {
        // Arrange - Add character to cast first
        var character = CreateTestCharacter("Jane Smith");
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);
        _viewModel.AddCastMember(character);
        var countAfterAdd = _viewModel.CastList.Count;

        // Act - Remove the character
        _viewModel.RemoveCastMember(character);

        // Assert - Verify character was removed
        Assert.AreEqual(countAfterAdd - 1, _viewModel.CastList.Count, "CastList count should decrease");
        Assert.IsFalse(_viewModel.CastList.Contains(character), "CastList should not contain removed character");
    }

    [TestMethod]
    public void AddCastMember_WithEmptyGuid_DoesNotAdd()
    {
        // Arrange - Create character with empty Guid (invalid character)
        var character = CreateTestCharacter("Invalid Character");
        character.Uuid = Guid.Empty; // This simulates a placeholder character
        _viewModel.Activate(_sceneModel);
        var initialCount = _viewModel.CastList.Count;

        // Act - Attempt to add invalid character
        _viewModel.AddCastMember(character);

        // Assert - Verify character was NOT added
        Assert.AreEqual(initialCount, _viewModel.CastList.Count, "CastList count should not change for empty Guid");
    }

    [TestMethod]
    public void AddCastMember_WithDuplicateCharacter_DoesNotAddAgain()
    {
        // Arrange - Create character and add it to cast once
        var character = CreateTestCharacter("Duplicate Character");
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);
        _viewModel.AddCastMember(character);
        var countAfterFirstAdd = _viewModel.CastList.Count;

        // Act - Try to add the same character again
        _viewModel.AddCastMember(character);

        // Assert - Verify count did not increase (duplicate prevented)
        Assert.AreEqual(countAfterFirstAdd, _viewModel.CastList.Count,
            "CastList count should not increase when adding duplicate character");
        Assert.AreEqual(1, _viewModel.CastList.Count(c => c.Uuid == character.Uuid),
            "Character should only appear once in CastList");
    }

    [TestMethod]
    public void AddCastMember_WithNullElement_HandlesGracefully()
    {
        // Arrange - Setup ViewModel
        _viewModel.Activate(_sceneModel);
        var initialCount = _viewModel.CastList.Count;

        // Act - Try to add null (should not throw exception)
        _viewModel.AddCastMember(null);

        // Assert - Verify no exception thrown and count unchanged
        Assert.AreEqual(initialCount, _viewModel.CastList.Count,
            "CastList count should not change when adding null");
    }

    [TestMethod]
    public void RemoveCastMember_WithNonExistentCharacter_DoesNothing()
    {
        // Arrange - Create character that is NOT in cast list
        var character = CreateTestCharacter("Non-Existent Character");
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);
        var initialCount = _viewModel.CastList.Count;

        // Act - Try to remove character that's not in cast
        _viewModel.RemoveCastMember(character);

        // Assert - Verify count unchanged (silent no-op)
        Assert.AreEqual(initialCount, _viewModel.CastList.Count,
            "CastList count should not change when removing non-existent character");
    }

    [TestMethod]
    public void SwitchCastView_ToAllCharacters_ShowsAllCharacters()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var totalCharacters = _storyModel.StoryElements.Characters.Count;

        // Act
        _viewModel.AllCharacters = true;
        _viewModel.InitializeCharacterList();

        // Assert
        Assert.AreEqual(_viewModel.CharacterList, _viewModel.CastSource);
        Assert.AreEqual(totalCharacters, _viewModel.CastSource.Count);
    }

    [TestMethod]
    public void SwitchCastView_ToCastList_ShowsOnlyCastMembers()
    {
        // Arrange
        var castMember = CreateTestCharacter("Cast Member");
        _storyModel.StoryElements.Characters.Add(castMember);
        _storyModel.StoryElements.Characters.Add(CreateTestCharacter("Not In Cast"));
        _viewModel.Activate(_sceneModel);
        _viewModel.AddCastMember(castMember);

        // Act
        _viewModel.AllCharacters = false;
        _viewModel.InitializeCharacterList();

        // Assert
        Assert.AreEqual(_viewModel.CastList, _viewModel.CastSource);
        Assert.AreEqual(1, _viewModel.CastSource.Count);
    }

    [TestMethod]
    public void CastMemberExists_WithExistingCharacter_ReturnsTrue()
    {
        // Arrange - Add character to cast
        var character = CreateTestCharacter("Existing Cast Member");
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);
        _viewModel.AddCastMember(character);
        var countAfterAdd = _viewModel.CastList.Count;

        // Act - Try to add same character again (tests CastMemberExists indirectly)
        _viewModel.AddCastMember(character);

        // Assert - Verify duplicate was prevented (proves CastMemberExists returned true)
        Assert.AreEqual(countAfterAdd, _viewModel.CastList.Count,
            "Duplicate prevention proves CastMemberExists returned true");
        Assert.IsTrue(_viewModel.CastList.Contains(character),
            "Character should be in CastList");
    }

    [TestMethod]
    public void CastMemberExists_WithNonExistentCharacter_ReturnsFalse()
    {
        // Arrange - Create character but DON'T add to cast
        var character = CreateTestCharacter("New Character");
        _storyModel.StoryElements.Characters.Add(character);
        _viewModel.Activate(_sceneModel);
        var initialCount = _viewModel.CastList.Count;

        // Act - Add character for first time (tests CastMemberExists indirectly)
        _viewModel.AddCastMember(character);

        // Assert - Verify character was added (proves CastMemberExists returned false)
        Assert.AreEqual(initialCount + 1, _viewModel.CastList.Count,
            "Successful add proves CastMemberExists returned false");
        Assert.IsTrue(_viewModel.CastList.Contains(character),
            "Character should now be in CastList");
    }

    #endregion

    #region Priority 4: Property Change Notifications (FULLY IMPLEMENTED)

    [TestMethod]
    public void Description_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Description))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Description = "Updated Description";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Description should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Date_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Date))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Date = "2025-10-15";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Date should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Time_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Time))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Time = "Evening";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Time should raise PropertyChanged when set");
    }

    [TestMethod]
    public void SceneType_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.SceneType))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.SceneType = "Dialogue";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "SceneType should raise PropertyChanged when set");
    }

    [TestMethod]
    public void ValueExchange_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.ValueExchange))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.ValueExchange = "-/+";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "ValueExchange should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Events_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Events))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Events = "Important events occurred";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Events should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Consequences_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Consequences))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Consequences = "Serious consequences";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Consequences should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Significance_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Significance))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Significance = "High significance";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Significance should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Realization_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Realization))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Realization = "Key realization";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Realization should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Review_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Review))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Review = "Scene review notes";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Review should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Notes_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Notes))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Notes = "Additional notes";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Notes should raise PropertyChanged when set");
    }

    [TestMethod]
    public void ProtagEmotion_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.ProtagEmotion))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.ProtagEmotion = "Angry";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "ProtagEmotion should raise PropertyChanged when set");
    }

    [TestMethod]
    public void ProtagGoal_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.ProtagGoal))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.ProtagGoal = "Find the artifact";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "ProtagGoal should raise PropertyChanged when set");
    }

    [TestMethod]
    public void AntagEmotion_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.AntagEmotion))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.AntagEmotion = "Confident";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "AntagEmotion should raise PropertyChanged when set");
    }

    [TestMethod]
    public void AntagGoal_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.AntagGoal))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.AntagGoal = "Stop the protagonist";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "AntagGoal should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Opposition_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Opposition))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Opposition = "Direct conflict";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Opposition should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Outcome_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Outcome))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Outcome = "Protagonist wins";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Outcome should raise PropertyChanged when set");
    }

    [TestMethod]
    public void Emotion_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.Emotion))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.Emotion = "Relieved";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "Emotion should raise PropertyChanged when set");
    }

    [TestMethod]
    public void NewGoal_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.NewGoal))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.NewGoal = "Prepare for next challenge";

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "NewGoal should raise PropertyChanged when set");
    }

    [TestMethod]
    public void AllCharacters_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.AllCharacters))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.AllCharacters = !_viewModel.AllCharacters;

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "AllCharacters should raise PropertyChanged when set");
    }

    [TestMethod]
    public void CastSource_WhenSet_RaisesPropertyChanged()
    {
        // Arrange
        _viewModel.Activate(_sceneModel);
        var propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.CastSource))
            {
                propertyChangedFired = true;
            }
        };

        // Act
        _viewModel.CastSource = new ObservableCollection<StoryElement>();

        // Assert
        Assert.IsTrue(propertyChangedFired,
            "CastSource should raise PropertyChanged when set");
    }

    #endregion

    #region Priority 5: Scene Purposes Management (FULLY IMPLEMENTED)

    [TestMethod]
    public void AddScenePurpose_WithValidPurpose_SendsStatusMessage()
    {
        // Arrange - Activate ViewModel and register for StatusChangedMessage
        _viewModel.Activate(_sceneModel);

        var messageReceived = false;
        StatusChangedMessage capturedMessage = null;
        var testPurpose = CreateTestStringSelection("Advance the Plot", true);

        // Register to receive StatusChangedMessage
        WeakReferenceMessenger.Default.Register<StatusChangedMessage>(this, (r, m) =>
        {
            messageReceived = true;
            capturedMessage = m;
        });

        // Act - Add scene purpose
        _viewModel.AddScenePurpose(testPurpose);

        // Assert - Verify StatusChangedMessage was sent with purpose name
        Assert.IsTrue(messageReceived, "StatusChangedMessage should be sent when AddScenePurpose is called");
        Assert.IsNotNull(capturedMessage, "Captured message should not be null");
        Assert.IsNotNull(capturedMessage.Value, "Message value should not be null");
        Assert.IsTrue(capturedMessage.Value.Status.Contains("Advance the Plot"),
            "Message should contain the purpose name");
        Assert.IsTrue(capturedMessage.Value.Status.Contains("Add Scene Purpose"),
            "Message should indicate purpose was added");

        // Cleanup - Unregister from messenger
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    [TestMethod]
    public void RemoveScenePurpose_WithValidPurpose_SendsStatusMessage()
    {
        // Arrange - Activate ViewModel and register for StatusChangedMessage
        _viewModel.Activate(_sceneModel);

        var messageReceived = false;
        StatusChangedMessage capturedMessage = null;
        var testPurpose = CreateTestStringSelection("Develop Characters", true);

        // Register to receive StatusChangedMessage
        WeakReferenceMessenger.Default.Register<StatusChangedMessage>(this, (r, m) =>
        {
            messageReceived = true;
            capturedMessage = m;
        });

        // Act - Remove scene purpose
        _viewModel.RemoveScenePurpose(testPurpose);

        // Assert - Verify StatusChangedMessage was sent with purpose name
        Assert.IsTrue(messageReceived, "StatusChangedMessage should be sent when RemoveScenePurpose is called");
        Assert.IsNotNull(capturedMessage, "Captured message should not be null");
        Assert.IsNotNull(capturedMessage.Value, "Message value should not be null");
        Assert.IsTrue(capturedMessage.Value.Status.Contains("Develop Characters"),
            "Message should contain the purpose name");
        Assert.IsTrue(capturedMessage.Value.Status.Contains("Remove Scene Purpose"),
            "Message should indicate purpose was removed");

        // Cleanup - Unregister from messenger
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    [TestMethod]
    public void ScenePurposes_LoadFromModel_SetsSelectedCorrectly()
    {
        // Arrange - Add some purposes to the SceneModel before activating ViewModel
        _sceneModel.ScenePurpose.Add("Advance the Plot");
        _sceneModel.ScenePurpose.Add("Develop Characters");

        // Act - Activate ViewModel (triggers LoadModel which populates ScenePurposes)
        _viewModel.Activate(_sceneModel);

        // Assert - Verify ScenePurposes collection is populated
        Assert.IsNotNull(_viewModel.ScenePurposes, "ScenePurposes collection should not be null");
        Assert.IsTrue(_viewModel.ScenePurposes.Count > 0, "ScenePurposes should contain items");

        // Verify selected purposes have Selection=true
        var advancePlotPurpose = _viewModel.ScenePurposes.FirstOrDefault(p => p.StringName == "Advance the Plot");
        Assert.IsNotNull(advancePlotPurpose, "Advance the Plot purpose should be in ScenePurposes");
        Assert.IsTrue(advancePlotPurpose.Selection, "Advance the Plot purpose should be selected");

        var developCharactersPurpose = _viewModel.ScenePurposes.FirstOrDefault(p => p.StringName == "Develop Characters");
        Assert.IsNotNull(developCharactersPurpose, "Develop Characters purpose should be in ScenePurposes");
        Assert.IsTrue(developCharactersPurpose.Selection, "Develop Characters purpose should be selected");

        // Verify at least one unselected purpose exists
        var unselectedPurpose = _viewModel.ScenePurposes.FirstOrDefault(p => !p.Selection);
        Assert.IsNotNull(unselectedPurpose,
            "At least one purpose should be unselected (not in Model.ScenePurpose)");
    }

    [TestMethod]
    public void ScenePurposes_SaveToModel_OnlySavesSelected()
    {
        // Arrange - Activate ViewModel and modify ScenePurposes collection
        _viewModel.Activate(_sceneModel);

        // Clear existing purposes and set specific test purposes
        _viewModel.ScenePurposes.Clear();
        _viewModel.ScenePurposes.Add(CreateTestStringSelection("Advance the Plot", true));
        _viewModel.ScenePurposes.Add(CreateTestStringSelection("Develop Characters", false));
        _viewModel.ScenePurposes.Add(CreateTestStringSelection("Build Suspense", true));

        // Act - Save ViewModel to Model
        _viewModel.SaveModel();

        // Assert - Verify Model.ScenePurpose contains only selected purposes
        Assert.AreEqual(2, _sceneModel.ScenePurpose.Count,
            "Model should contain exactly 2 selected purposes");
        Assert.IsTrue(_sceneModel.ScenePurpose.Contains("Advance the Plot"),
            "Model should contain selected 'Advance the Plot' purpose");
        Assert.IsFalse(_sceneModel.ScenePurpose.Contains("Develop Characters"),
            "Model should NOT contain unselected 'Develop Characters' purpose");
        Assert.IsTrue(_sceneModel.ScenePurpose.Contains("Build Suspense"),
            "Model should contain selected 'Build Suspense' purpose");
    }

    #endregion

    #region Priority 6: Constructor Initialization (FULLY IMPLEMENTED)

    [TestMethod]
    public void Constructor_InitializesAllCollections()
    {
        // Arrange & Act - Create new ViewModel using IoC (constructor executes)
        var viewModel = Ioc.Default.GetService<SceneViewModel>();

        // Assert - Verify all collection properties are initialized
        Assert.IsNotNull(viewModel.CastList, "CastList should be initialized");
        Assert.IsNotNull(viewModel.CharacterList, "CharacterList should be initialized");
        Assert.IsNotNull(viewModel.ScenePurposes, "ScenePurposes should be initialized");

        // Note: Collections may not be empty if AppState has data from other tests
        // The important verification is that they are initialized (not null)

        // ScenePurposes should be populated from ScenePurposeList
        Assert.IsTrue(viewModel.ScenePurposes.Count > 0,
            "ScenePurposes should be populated with items from ScenePurposeList");
    }

    [TestMethod]
    public void Constructor_LoadsComboBoxLists()
    {
        // Arrange & Act - Create new ViewModel using IoC (constructor loads lists)
        var viewModel = Ioc.Default.GetService<SceneViewModel>();

        // Assert - Verify all ComboBox ItemsSource lists are loaded from ListData
        Assert.IsNotNull(viewModel.ViewpointList, "ViewpointList should be loaded");
        Assert.IsTrue(viewModel.ViewpointList.Count > 0,
            "ViewpointList should contain items from Lists.json");

        Assert.IsNotNull(viewModel.SceneTypeList, "SceneTypeList should be loaded");
        Assert.IsTrue(viewModel.SceneTypeList.Count > 0,
            "SceneTypeList should contain items from Lists.json");

        Assert.IsNotNull(viewModel.ScenePurposeList, "ScenePurposeList should be loaded");
        Assert.IsTrue(viewModel.ScenePurposeList.Count > 0,
            "ScenePurposeList should contain items from Lists.json");

        Assert.IsNotNull(viewModel.StoryRoleList, "StoryRoleList should be loaded");
        Assert.IsTrue(viewModel.StoryRoleList.Count > 0,
            "StoryRoleList should contain items from Lists.json");

        Assert.IsNotNull(viewModel.EmotionList, "EmotionList should be loaded");
        Assert.IsTrue(viewModel.EmotionList.Count > 0,
            "EmotionList should contain items from Lists.json");

        Assert.IsNotNull(viewModel.GoalList, "GoalList should be loaded");
        Assert.IsTrue(viewModel.GoalList.Count > 0,
            "GoalList should contain items from Lists.json");

        Assert.IsNotNull(viewModel.OppositionList, "OppositionList should be loaded");
        Assert.IsTrue(viewModel.OppositionList.Count > 0,
            "OppositionList should contain items from Lists.json");

        Assert.IsNotNull(viewModel.OutcomeList, "OutcomeList should be loaded");
        Assert.IsTrue(viewModel.OutcomeList.Count > 0,
            "OutcomeList should contain items from Lists.json");

        Assert.IsNotNull(viewModel.ValueExchangeList, "ValueExchangeList should be loaded");
        Assert.IsTrue(viewModel.ValueExchangeList.Count > 0,
            "ValueExchangeList should contain items from Lists.json");
    }

    [TestMethod]
    public void Constructor_SetsDefaultPropertyValues()
    {
        // Arrange & Act - Create new ViewModel using IoC (constructor sets defaults)
        // Note: Constructor initializes properties to defaults (empty strings and Guid.Empty)
        var viewModel = Ioc.Default.GetService<SceneViewModel>();

        // Assert - Verify AllCharacters is set to true (line 76 in constructor)
        Assert.IsTrue(viewModel.AllCharacters, "AllCharacters should be true");

        // Verify CastSource is initialized (line 75 in constructor)
        Assert.IsNotNull(viewModel.CastSource, "CastSource should be initialized");

        // Verify collections are initialized (lines 23, 25, 44, 73, 74 in constructor)
        Assert.IsNotNull(viewModel.CastList, "CastList should be initialized");
        Assert.IsNotNull(viewModel.CharacterList, "CharacterList should be initialized");
        Assert.IsNotNull(viewModel.ScenePurposes, "ScenePurposes should be initialized");

        // Verify ComboBox lists are loaded from ListData (lines 48-63 in constructor)
        Assert.IsNotNull(viewModel.ViewpointList, "ViewpointList should be loaded");
        Assert.IsNotNull(viewModel.SceneTypeList, "SceneTypeList should be loaded");
        Assert.IsNotNull(viewModel.EmotionList, "EmotionList should be loaded");

        // Note: We cannot reliably test string/Guid default values here because
        // if AppState has a CurrentDocument, the properties may be initialized
        // from that document. The important part is that the constructor completes
        // successfully and initializes all collections and references.
    }

    [TestMethod]
    public void Constructor_SubscribesToPropertyChanged()
    {
        // Arrange - Create new ViewModel using IoC (constructor subscribes to PropertyChanged)
        var viewModel = Ioc.Default.GetService<SceneViewModel>();

        // Activate to enable property change tracking (_changeable = true)
        viewModel.Activate(_sceneModel);

        var propertyChangedFired = false;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(viewModel.Description))
            {
                propertyChangedFired = true;
            }
        };

        // Act - Change a property to verify PropertyChanged event is raised
        viewModel.Description = "Test description to verify PropertyChanged subscription";

        // Assert - Verify PropertyChanged event was raised
        Assert.IsTrue(propertyChangedFired,
            "PropertyChanged event should be raised after constructor subscribes to OnPropertyChanged");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    ///     Creates a SceneViewModel with a properly configured StoryModel in AppState.
    ///     This follows the pattern from ProblemViewModelTests using IoC container.
    /// </summary>
    private SceneViewModel SetupViewModelWithStory()
    {
        // Get AppState from IoC container (no fake implementations)
        var appState = Ioc.Default.GetRequiredService<AppState>();

        // Create and assign a StoryDocument with our test StoryModel
        appState.CurrentDocument = new StoryDocument(_storyModel);

        // Get SceneViewModel from IoC container with all dependencies injected
        var viewModel = Ioc.Default.GetService<SceneViewModel>();

        return viewModel;
    }

    /// <summary>
    ///     Creates a test StoryModel populated with characters and settings.
    ///     This provides realistic test data for ViewModel operations.
    /// </summary>
    private StoryModel CreateTestStoryModel()
    {
        // Create base story model
        var storyModel = new StoryModel();

        // Add test characters to the story
        _testCharacter1 = new CharacterModel("Hero Character", storyModel, null);
        _testCharacter2 = new CharacterModel("Villain Character", storyModel, null);
        storyModel.StoryElements.Characters.Add(_testCharacter1);
        storyModel.StoryElements.Characters.Add(_testCharacter2);

        // Add test setting to the story
        _testSetting = new SettingModel("Castle Throne Room", storyModel, null);
        storyModel.StoryElements.Settings.Add(_testSetting);

        return storyModel;
    }

    /// <summary>
    ///     Creates a test SceneModel with realistic default values.
    ///     Uses the pattern: new SceneModel(name, parent, null)
    /// </summary>
    private SceneModel CreateTestSceneModel(StoryModel parent = null)
    {
        // Use provided parent or create new one
        parent ??= CreateTestStoryModel();

        // Create scene with standard constructor
        var scene = new SceneModel("Test Scene", parent, null);

        // Set common scene properties
        scene.Date = "2025-01-01";
        scene.Time = "Morning";
        scene.SceneDescription = "Test scene description";
        scene.SceneType = "Action";
        scene.ValueExchange = "+/-";
        scene.Events = "Test events";
        scene.Consequences = "Test consequences";

        return scene;
    }

    /// <summary>
    ///     Creates a test CharacterModel with the specified name.
    ///     Character is created as a child of a StoryModel.
    /// </summary>
    private CharacterModel CreateTestCharacter(string name)
    {
        // Create character with parent StoryModel
        // Using pattern: new CharacterModel(name, parent, null)
        var character = new CharacterModel(name, new StoryModel(), null);
        return character;
    }

    /// <summary>
    ///     Creates a test SettingModel with the specified name.
    ///     Setting is created as a child of a StoryModel.
    /// </summary>
    private SettingModel CreateTestSetting(string name)
    {
        // Create setting with parent StoryModel
        // Using pattern: new SettingModel(name, parent, null)
        var setting = new SettingModel(name, new StoryModel(), null);
        return setting;
    }

    /// <summary>
    ///     Creates a test StringSelection for scene purposes testing.
    /// </summary>
    private StringSelection CreateTestStringSelection(string text, bool isSelected = false)
    {
        return new StringSelection(text, isSelected);
    }

    #endregion
}
