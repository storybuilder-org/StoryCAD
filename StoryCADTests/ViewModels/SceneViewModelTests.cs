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

    // TODO: Priority 3 - Name Property Tests
    // - Name_WhenSetToSameValue_DoesNotSendMessage
    // - Name_WhenChangeable_LogsNameChange

    // TODO: Priority 3 - ViewpointCharacter Property Tests
    // - ViewpointCharacter_WhenSet_AddsCharacterToCast
    // - ViewpointCharacter_WithEmptyGuid_DoesNotAddToCast
    // - SelectedViewpointCharacter_WhenSet_UpdatesViewpointCharacterGuid
    // - SelectedViewpointCharacter_WhenSetToNull_ClearsViewpointCharacter

    // TODO: Priority 3 - Setting Property Tests
    // - SelectedSetting_WhenSet_UpdatesSettingGuid
    // - SelectedSetting_WhenSetToNull_ClearsSettingGuid

    // TODO: Priority 3 - Protagonist/Antagonist Property Tests
    // - SelectedProtagonist_WhenSet_UpdatesProtagonistGuid
    // - SelectedAntagonist_WhenSet_UpdatesAntagonistGuid

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

    #region Priority 4: Property Change Notifications (STUBBED)

    // TODO: Priority 4 - Property Change Notification Tests
    // Follow ProblemViewModelTests pattern for each property:
    // - Description_WhenSet_RaisesPropertyChanged
    // - Date_WhenSet_RaisesPropertyChanged
    // - Time_WhenSet_RaisesPropertyChanged
    // - SceneType_WhenSet_RaisesPropertyChanged
    // - ValueExchange_WhenSet_RaisesPropertyChanged
    // - Events_WhenSet_RaisesPropertyChanged
    // - Consequences_WhenSet_RaisesPropertyChanged
    // - Significance_WhenSet_RaisesPropertyChanged
    // - Realization_WhenSet_RaisesPropertyChanged
    // - Review_WhenSet_RaisesPropertyChanged
    // - Notes_WhenSet_RaisesPropertyChanged
    // - ProtagEmotion_WhenSet_RaisesPropertyChanged
    // - ProtagGoal_WhenSet_RaisesPropertyChanged
    // - AntagEmotion_WhenSet_RaisesPropertyChanged
    // - AntagGoal_WhenSet_RaisesPropertyChanged
    // - Opposition_WhenSet_RaisesPropertyChanged
    // - Outcome_WhenSet_RaisesPropertyChanged
    // - Emotion_WhenSet_RaisesPropertyChanged
    // - NewGoal_WhenSet_RaisesPropertyChanged
    // - AllCharacters_WhenSet_RaisesPropertyChanged
    // - CastSource_WhenSet_RaisesPropertyChanged

    #endregion

    #region Priority 5: Scene Purposes Management (STUBBED)

    // TODO: Priority 5 - Scene Purposes Tests
    // - AddScenePurpose_WithValidPurpose_SendsStatusMessage
    // - RemoveScenePurpose_WithValidPurpose_SendsStatusMessage
    // - ScenePurposes_LoadFromModel_SetsSelectedCorrectly
    // - ScenePurposes_SaveToModel_OnlySavesSelected

    #endregion

    #region Priority 6: Constructor Initialization (STUBBED)

    // TODO: Priority 6 - Constructor Tests
    // - Constructor_InitializesAllCollections
    // - Constructor_LoadsComboBoxLists
    // - Constructor_SetsDefaultPropertyValues
    // - Constructor_SubscribesToPropertyChanged

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
