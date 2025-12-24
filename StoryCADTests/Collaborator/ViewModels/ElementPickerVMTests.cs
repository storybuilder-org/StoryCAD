using StoryCADLib.Collaborator.ViewModels;
using StoryCADLib.Models;

#nullable disable

namespace StoryCADTests.Collaborator.ViewModels;

/// <summary>
/// Unit tests for ElementPickerVM
/// </summary>
[TestClass]
public class ElementPickerVMTests
{
    private ElementPickerVM _viewModel;
    private StoryModel _storyModel;

    [TestInitialize]
    public void Setup()
    {
        _viewModel = new ElementPickerVM();
        _storyModel = new StoryModel();
    }

    #region Constructor Tests

    [TestMethod]
    public void Constructor_WhenCalled_CreatesInstance()
    {
        // Act
        var vm = new ElementPickerVM();

        // Assert
        Assert.IsNotNull(vm);
    }

    [TestMethod]
    public void Constructor_WhenCalled_PropertiesAreNull()
    {
        // Act
        var vm = new ElementPickerVM();

        // Assert
        Assert.IsNull(vm.StoryModel);
        Assert.IsNull(vm.SelectedType);
        Assert.IsNull(vm.SelectedElement);
        Assert.IsNull(vm.NewNodeName);
        Assert.IsNull(vm.ForcedType);
        Assert.IsNull(vm.PickerLabel);
    }

    #endregion

    #region StoryModel Property Tests

    [TestMethod]
    public void StoryModel_WhenSet_ReturnsSetValue()
    {
        // Arrange
        var storyModel = new StoryModel();

        // Act
        _viewModel.StoryModel = storyModel;

        // Assert
        Assert.AreSame(storyModel, _viewModel.StoryModel);
    }

    [TestMethod]
    public void StoryModel_WhenSetToNull_ReturnsNull()
    {
        // Arrange
        _viewModel.StoryModel = new StoryModel();

        // Act
        _viewModel.StoryModel = null;

        // Assert
        Assert.IsNull(_viewModel.StoryModel);
    }

    #endregion

    #region SelectedType Property Tests

    [TestMethod]
    public void SelectedType_WhenSet_ReturnsSetValue()
    {
        // Arrange
        var type = StoryItemType.Character;

        // Act
        _viewModel.SelectedType = type;

        // Assert
        Assert.AreEqual(type, _viewModel.SelectedType);
    }

    [TestMethod]
    public void SelectedType_WhenSetToNull_ReturnsNull()
    {
        // Arrange
        _viewModel.SelectedType = StoryItemType.Scene;

        // Act
        _viewModel.SelectedType = null;

        // Assert
        Assert.IsNull(_viewModel.SelectedType);
    }

    [TestMethod]
    public void SelectedType_CanBeSetToVariousTypes()
    {
        // Test common StoryItemType values
        var types = new object[]
        {
            StoryItemType.StoryOverview,
            StoryItemType.Problem,
            StoryItemType.Character,
            StoryItemType.Setting,
            StoryItemType.Scene,
            StoryItemType.Folder
        };

        foreach (var type in types)
        {
            // Act
            _viewModel.SelectedType = type;

            // Assert
            Assert.AreEqual(type, _viewModel.SelectedType);
        }
    }

    #endregion

    #region SelectedElement Property Tests

    [TestMethod]
    public void SelectedElement_WhenSet_ReturnsSetValue()
    {
        // Arrange
        var element = new CharacterModel("Test Character", _storyModel, null);

        // Act
        _viewModel.SelectedElement = element;

        // Assert
        Assert.AreSame(element, _viewModel.SelectedElement);
    }

    [TestMethod]
    public void SelectedElement_WhenSetToNull_ReturnsNull()
    {
        // Arrange
        _viewModel.SelectedElement = new CharacterModel("Test", _storyModel, null);

        // Act
        _viewModel.SelectedElement = null;

        // Assert
        Assert.IsNull(_viewModel.SelectedElement);
    }

    [TestMethod]
    public void SelectedElement_CanBeSetToCharacterModel()
    {
        // Arrange
        var character = new CharacterModel("Hero", _storyModel, null);

        // Act
        _viewModel.SelectedElement = character;

        // Assert
        Assert.IsInstanceOfType(_viewModel.SelectedElement, typeof(CharacterModel));
    }

    [TestMethod]
    public void SelectedElement_CanBeSetToSceneModel()
    {
        // Arrange
        var scene = new SceneModel("Opening Scene", _storyModel, null);

        // Act
        _viewModel.SelectedElement = scene;

        // Assert
        Assert.IsInstanceOfType(_viewModel.SelectedElement, typeof(SceneModel));
    }

    [TestMethod]
    public void SelectedElement_CanBeSetToProblemModel()
    {
        // Arrange
        var problem = new ProblemModel("Main Conflict", _storyModel, null);

        // Act
        _viewModel.SelectedElement = problem;

        // Assert
        Assert.IsInstanceOfType(_viewModel.SelectedElement, typeof(ProblemModel));
    }

    [TestMethod]
    public void SelectedElement_CanBeSetToSettingModel()
    {
        // Arrange
        var setting = new SettingModel("Castle", _storyModel, null);

        // Act
        _viewModel.SelectedElement = setting;

        // Assert
        Assert.IsInstanceOfType(_viewModel.SelectedElement, typeof(SettingModel));
    }

    #endregion

    #region NewNodeName Property Tests

    [TestMethod]
    public void NewNodeName_WhenSet_ReturnsSetValue()
    {
        // Act
        _viewModel.NewNodeName = "New Character";

        // Assert
        Assert.AreEqual("New Character", _viewModel.NewNodeName);
    }

    [TestMethod]
    public void NewNodeName_WhenSetToEmpty_ReturnsEmpty()
    {
        // Act
        _viewModel.NewNodeName = string.Empty;

        // Assert
        Assert.AreEqual(string.Empty, _viewModel.NewNodeName);
    }

    [TestMethod]
    public void NewNodeName_WhenSetToNull_ReturnsNull()
    {
        // Arrange
        _viewModel.NewNodeName = "Some Name";

        // Act
        _viewModel.NewNodeName = null;

        // Assert
        Assert.IsNull(_viewModel.NewNodeName);
    }

    #endregion

    #region ForcedType Property Tests

    [TestMethod]
    public void ForcedType_WhenSetToCharacter_ReturnsCharacter()
    {
        // Act
        _viewModel.ForcedType = StoryItemType.Character;

        // Assert
        Assert.AreEqual(StoryItemType.Character, _viewModel.ForcedType);
    }

    [TestMethod]
    public void ForcedType_WhenSetToNull_ReturnsNull()
    {
        // Arrange
        _viewModel.ForcedType = StoryItemType.Scene;

        // Act
        _viewModel.ForcedType = null;

        // Assert
        Assert.IsNull(_viewModel.ForcedType);
    }

    [TestMethod]
    public void ForcedType_InitiallyIsNull()
    {
        // Arrange & Act
        var vm = new ElementPickerVM();

        // Assert
        Assert.IsNull(vm.ForcedType);
    }

    [TestMethod]
    public void ForcedType_CanBeSetToAllPickableTypes()
    {
        var pickableTypes = new[]
        {
            StoryItemType.Problem,
            StoryItemType.Character,
            StoryItemType.Setting,
            StoryItemType.Scene
        };

        foreach (var type in pickableTypes)
        {
            // Act
            _viewModel.ForcedType = type;

            // Assert
            Assert.AreEqual(type, _viewModel.ForcedType);
        }
    }

    #endregion

    #region PickerLabel Property Tests

    [TestMethod]
    public void PickerLabel_WhenSet_ReturnsSetValue()
    {
        // Act
        _viewModel.PickerLabel = "Protagonist";

        // Assert
        Assert.AreEqual("Protagonist", _viewModel.PickerLabel);
    }

    [TestMethod]
    public void PickerLabel_WhenSetToNull_ReturnsNull()
    {
        // Arrange
        _viewModel.PickerLabel = "Some Label";

        // Act
        _viewModel.PickerLabel = null;

        // Assert
        Assert.IsNull(_viewModel.PickerLabel);
    }

    [TestMethod]
    public void PickerLabel_WhenSetToEmpty_ReturnsEmpty()
    {
        // Act
        _viewModel.PickerLabel = string.Empty;

        // Assert
        Assert.AreEqual(string.Empty, _viewModel.PickerLabel);
    }

    [TestMethod]
    public void PickerLabel_TypicalLabels_CanBeSet()
    {
        var labels = new[]
        {
            "Protagonist",
            "Antagonist",
            "Story Problem",
            "Scene Location"
        };

        foreach (var label in labels)
        {
            // Act
            _viewModel.PickerLabel = label;

            // Assert
            Assert.AreEqual(label, _viewModel.PickerLabel);
        }
    }

    #endregion

    #region ShowPicker Method Tests

    [TestMethod]
    [Ignore("ShowPicker requires XamlRoot and ContentDialog which are UI-dependent")]
    public async Task ShowPicker_WhenCalled_ResetsViewModelState()
    {
        // This test is ignored because ShowPicker requires UI thread and XamlRoot
        await Task.CompletedTask;
    }

    [TestMethod]
    [Ignore("ShowPicker requires XamlRoot and ContentDialog which are UI-dependent")]
    public async Task ShowPicker_WithForcedType_SetsForcedTypeProperty()
    {
        await Task.CompletedTask;
    }

    [TestMethod]
    [Ignore("ShowPicker requires XamlRoot and ContentDialog which are UI-dependent")]
    public async Task ShowPicker_WithLabel_SetsPickerLabelProperty()
    {
        await Task.CompletedTask;
    }

    #endregion

    #region CreateNode Method Tests

    [TestMethod]
    public void CreateNode_WhenCalled_DoesNotThrow()
    {
        // Arrange - CreateNode has commented-out implementation
        _viewModel.StoryModel = _storyModel;
        _viewModel.ForcedType = StoryItemType.Character;
        _viewModel.NewNodeName = "Test Node";

        // Act & Assert - should not throw
        _viewModel.CreateNode();
    }

    [TestMethod]
    public void CreateNode_WithoutForcedType_DoesNotThrow()
    {
        // Arrange
        _viewModel.StoryModel = _storyModel;
        _viewModel.ForcedType = null;
        _viewModel.NewNodeName = "Test Node";

        // Act & Assert
        _viewModel.CreateNode();
    }

    [TestMethod]
    public void CreateNode_WithoutStoryModel_DoesNotThrow()
    {
        // Arrange
        _viewModel.StoryModel = null;
        _viewModel.ForcedType = StoryItemType.Scene;
        _viewModel.NewNodeName = "Test Scene";

        // Act & Assert
        _viewModel.CreateNode();
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void ViewModelState_AfterMultiplePropertySets_MaintainsAllValues()
    {
        // Arrange
        var storyModel = new StoryModel();
        var character = new CharacterModel("Test Hero", storyModel, null);

        // Act
        _viewModel.StoryModel = storyModel;
        _viewModel.SelectedType = StoryItemType.Character;
        _viewModel.SelectedElement = character;
        _viewModel.NewNodeName = "New Node";
        _viewModel.ForcedType = StoryItemType.Character;
        _viewModel.PickerLabel = "Protagonist";

        // Assert
        Assert.AreSame(storyModel, _viewModel.StoryModel);
        Assert.AreEqual(StoryItemType.Character, _viewModel.SelectedType);
        Assert.AreSame(character, _viewModel.SelectedElement);
        Assert.AreEqual("New Node", _viewModel.NewNodeName);
        Assert.AreEqual(StoryItemType.Character, _viewModel.ForcedType);
        Assert.AreEqual("Protagonist", _viewModel.PickerLabel);
    }

    [TestMethod]
    public void ViewModelState_CanBeReset()
    {
        // Arrange
        _viewModel.StoryModel = new StoryModel();
        _viewModel.SelectedType = StoryItemType.Scene;
        _viewModel.SelectedElement = new SceneModel("Test", _storyModel, null);
        _viewModel.NewNodeName = "Test";
        _viewModel.ForcedType = StoryItemType.Scene;
        _viewModel.PickerLabel = "Test Label";

        // Act - reset (simulating ShowPicker behavior)
        _viewModel.SelectedType = null;
        _viewModel.SelectedElement = null;
        _viewModel.NewNodeName = "";
        _viewModel.ForcedType = null;
        _viewModel.PickerLabel = null;

        // Assert
        Assert.IsNull(_viewModel.SelectedType);
        Assert.IsNull(_viewModel.SelectedElement);
        Assert.AreEqual("", _viewModel.NewNodeName);
        Assert.IsNull(_viewModel.ForcedType);
        Assert.IsNull(_viewModel.PickerLabel);
    }

    [TestMethod]
    public void SelectedElement_WhenCastToStoryElement_HasValidUuid()
    {
        // Arrange
        var character = new CharacterModel("Test Character", _storyModel, null);

        // Act
        _viewModel.SelectedElement = character;
        var storyElement = _viewModel.SelectedElement as StoryElement;

        // Assert
        Assert.IsNotNull(storyElement);
        Assert.AreNotEqual(Guid.Empty, storyElement.Uuid);
    }

    [TestMethod]
    public void MultipleElementTypes_CanBeSelectedSequentially()
    {
        // Arrange
        var character = new CharacterModel("Hero", _storyModel, null);
        var scene = new SceneModel("Opening", _storyModel, null);
        var problem = new ProblemModel("Conflict", _storyModel, null);

        // Act & Assert - Character
        _viewModel.SelectedElement = character;
        Assert.IsInstanceOfType(_viewModel.SelectedElement, typeof(CharacterModel));

        // Act & Assert - Scene
        _viewModel.SelectedElement = scene;
        Assert.IsInstanceOfType(_viewModel.SelectedElement, typeof(SceneModel));

        // Act & Assert - Problem
        _viewModel.SelectedElement = problem;
        Assert.IsInstanceOfType(_viewModel.SelectedElement, typeof(ProblemModel));
    }

    #endregion
}
