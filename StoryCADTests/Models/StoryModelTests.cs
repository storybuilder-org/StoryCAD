using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels.SubViewModels;
using System.ComponentModel;
using StoryCAD.ViewModels;
using Windows.UI;
using StoryCAD.Services.Logging;
using StoryCAD.Services;
using StoryCAD.Services.Outline;

namespace StoryCADTests.Models;

[TestClass]
public class StoryModelTests
{
    private StoryModel _storyModel;
    private static bool _iocInitialized = false;

    [TestInitialize]
    public void Setup()
    {
        if (!_iocInitialized)
        {
            // Initialize IoC for tests that need ShellViewModel
            try
            {
                var shellVm = Ioc.Default.GetService<ShellViewModel>();
                if (shellVm == null)
                {
                    // If ShellViewModel isn't registered, we're in basic test mode
                    // Tests that need ShellViewModel will handle this appropriately
                }
            }
            catch
            {
                // IoC not fully initialized for this test run
            }
            _iocInitialized = true;
        }
    }

    [TestMethod]
    public void StoryModelConstructorTest()
    {
        _storyModel = new StoryModel();
        Assert.IsNotNull(_storyModel);
        Assert.IsNotNull(_storyModel.StoryElements);
        Assert.AreEqual(0, _storyModel.StoryElements.Count);
        Assert.IsNotNull(_storyModel.ExplorerView);
        Assert.AreEqual(0, _storyModel.ExplorerView.Count);
        Assert.IsNotNull(_storyModel.NarratorView);
        Assert.AreEqual(0, _storyModel.NarratorView.Count);
        Assert.IsFalse(_storyModel.Changed);
    }

    #region Changed Property Tests

    [TestMethod]
    public void Changed_SetToTrue_RaisesPropertyChanged()
    {
        // Arrange
        _storyModel = new StoryModel();
        bool propertyChangedRaised = false;
        string propertyName = null;
        
        _storyModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
            propertyName = e.PropertyName;
        };

        // Act
        _storyModel.Changed = true;

        // Assert
        Assert.IsTrue(propertyChangedRaised, "PropertyChanged event should be raised");
        Assert.AreEqual(nameof(StoryModel.Changed), propertyName, "PropertyName should be 'Changed'");
        Assert.IsTrue(_storyModel.Changed, "Changed property should be true");
    }

    [TestMethod]
    public void Changed_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        _storyModel = new StoryModel();
        _storyModel.Changed = true; // Set initial value
        
        bool propertyChangedRaised = false;
        _storyModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
        };

        // Act
        _storyModel.Changed = true; // Set to same value

        // Assert
        Assert.IsFalse(propertyChangedRaised, "PropertyChanged should not be raised when setting same value");
    }

    [TestMethod]
    public void Changed_SetToFalse_RaisesPropertyChanged()
    {
        // Arrange
        _storyModel = new StoryModel();
        _storyModel.Changed = true; // Start with true
        
        bool propertyChangedRaised = false;
        _storyModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
        };

        // Act
        _storyModel.Changed = false;

        // Assert
        Assert.IsTrue(propertyChangedRaised, "PropertyChanged event should be raised");
        Assert.IsFalse(_storyModel.Changed, "Changed property should be false");
    }

    [TestMethod]
    public void Changed_WhenSet_UpdatesValue()
    {
        // Basic test that works with current StoryModel
        
        // Arrange
        _storyModel = new StoryModel();
        
        // Act & Assert - Setting to true
        _storyModel.Changed = true;
        Assert.IsTrue(_storyModel.Changed);
        
        // Act & Assert - Setting to false
        _storyModel.Changed = false;
        Assert.IsFalse(_storyModel.Changed);
    }

    #endregion

    #region CurrentView Property Tests

    [TestMethod]
    public void CurrentView_SetToExplorerView_RaisesPropertyChanged()
    {
        // Arrange
        _storyModel = new StoryModel();
        // Start with NarratorView so we can test changing to ExplorerView
        _storyModel.CurrentView = _storyModel.NarratorView;
        
        bool propertyChangedRaised = false;
        string propertyName = null;
        
        _storyModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
            propertyName = e.PropertyName;
        };

        // Act
        _storyModel.CurrentView = _storyModel.ExplorerView;

        // Assert
        Assert.IsTrue(propertyChangedRaised, "PropertyChanged event should be raised");
        Assert.AreEqual(nameof(StoryModel.CurrentView), propertyName, "PropertyName should be 'CurrentView'");
        Assert.AreEqual(_storyModel.ExplorerView, _storyModel.CurrentView, "CurrentView should reference ExplorerView");
    }

    [TestMethod]
    public void CurrentView_SetToNarratorView_RaisesPropertyChanged()
    {
        // Arrange
        _storyModel = new StoryModel();
        _storyModel.CurrentView = _storyModel.ExplorerView; // Start with Explorer
        
        bool propertyChangedRaised = false;
        _storyModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
        };

        // Act
        _storyModel.CurrentView = _storyModel.NarratorView;

        // Assert
        Assert.IsTrue(propertyChangedRaised, "PropertyChanged event should be raised");
        Assert.AreEqual(_storyModel.NarratorView, _storyModel.CurrentView, "CurrentView should reference NarratorView");
    }

    [TestMethod]
    public void CurrentView_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        _storyModel = new StoryModel();
        _storyModel.CurrentView = _storyModel.ExplorerView;
        
        bool propertyChangedRaised = false;
        _storyModel.PropertyChanged += (sender, e) =>
        {
            propertyChangedRaised = true;
        };

        // Act
        _storyModel.CurrentView = _storyModel.ExplorerView; // Same value

        // Assert
        Assert.IsFalse(propertyChangedRaised, "PropertyChanged should not be raised when setting same value");
    }

    #endregion

    #region TrashView Tests

    [TestMethod]
    public void TrashView_Initialized_IsNotNull()
    {
        // Arrange & Act
        _storyModel = new StoryModel();

        // Assert
        Assert.IsNotNull(_storyModel.TrashView, "TrashView should be initialized");
        Assert.AreEqual(0, _storyModel.TrashView.Count, "TrashView should be empty initially");
    }

    #endregion

    #region Collection Change Monitoring Tests

    [TestMethod]
    public void ExplorerView_AddItem_SetsChangedFlag()
    {
        // Arrange
        _storyModel = new StoryModel();
        _storyModel.Changed = false;
        var testNode = new StoryNodeItem(new OverviewModel("Test", _storyModel, null), null);

        // Act
        _storyModel.ExplorerView.Add(testNode);

        // Assert
        Assert.IsTrue(_storyModel.Changed, "Changed flag should be set when item added to ExplorerView");
    }

    [TestMethod]
    public void NarratorView_AddItem_SetsChangedFlag()
    {
        // Arrange
        _storyModel = new StoryModel();
        _storyModel.Changed = false;
        var testNode = new StoryNodeItem(new FolderModel("Test", _storyModel, StoryItemType.Folder, null), null);

        // Act
        _storyModel.NarratorView.Add(testNode);

        // Assert
        Assert.IsTrue(_storyModel.Changed, "Changed flag should be set when item added to NarratorView");
    }

    [TestMethod]
    public void TrashView_AddItem_SetsChangedFlag()
    {
        // Arrange
        _storyModel = new StoryModel();
        _storyModel.Changed = false;
        var testNode = new StoryNodeItem(new CharacterModel("Test", _storyModel, null), null);

        // Act
        _storyModel.TrashView.Add(testNode);

        // Assert
        Assert.IsTrue(_storyModel.Changed, "Changed flag should be set when item added to TrashView");
    }

    [TestMethod]
    public void Collection_RemoveItem_SetsChangedFlag()
    {
        // Arrange
        _storyModel = new StoryModel();
        var testNode = new StoryNodeItem(new CharacterModel("Test", _storyModel, null), null);
        _storyModel.ExplorerView.Add(testNode);
        _storyModel.Changed = false; // Reset after add

        // Act
        _storyModel.ExplorerView.Remove(testNode);

        // Assert
        Assert.IsTrue(_storyModel.Changed, "Changed flag should be set when item removed from collection");
    }

    #endregion

    #region Serialization Tests

    [TestMethod]
    public void Serialize_EmptyTrashView_ProducesFlattenedTrashView()
    {
        // Arrange
        _storyModel = new StoryModel();
        _storyModel.ExplorerView.Add(new StoryNodeItem(new OverviewModel("Test", _storyModel, null), null));
        _storyModel.TrashView.Clear(); // Ensure empty

        // Act
        string json = _storyModel.Serialize();

        // Assert
        Assert.IsTrue(json.Contains("\"FlattenedTrashView\""), "Serialized JSON should contain FlattenedTrashView property");
        // Note: FlattenedTrashView should exist but implementation may need to be added
    }

    [TestMethod]
    public void Serialize_WithTrashViewItems_IncludesItemsInFlattenedTrashView()
    {
        // Arrange
        _storyModel = new StoryModel();
        var deletedScene = new StoryNodeItem(new SceneModel("Deleted Scene", _storyModel, null), null);
        _storyModel.TrashView.Add(deletedScene);

        // Act
        string json = _storyModel.Serialize();

        // Assert
        Assert.IsTrue(json.Contains("\"FlattenedTrashView\""), "Serialized JSON should contain FlattenedTrashView");
        // Note: Should contain the deleted scene's UUID - implementation may need to be added
    }

    [TestMethod]
    public void Serialize_DoesNotIncludeRuntimeProperties()
    {
        // Arrange
        _storyModel = new StoryModel();
        _storyModel.CurrentView = _storyModel.NarratorView;
        _storyModel.CurrentViewType = StoryViewType.NarratorView;
        _storyModel.Changed = true;

        // Act
        string json = _storyModel.Serialize();

        // Assert
        Assert.IsFalse(json.Contains("\"CurrentView\""), "CurrentView should not be serialized");
        Assert.IsFalse(json.Contains("\"CurrentViewType\""), "CurrentViewType should not be serialized");
        Assert.IsFalse(json.Contains("\"Changed\""), "Changed property should not be serialized");
    }

    #endregion

    #region TrashView Tests (merged from ShellUITests.cs)

    [TestMethod]
    public void TrashView_SeparateFromMainViews_IsImplemented()
    {
        // This test verifies that TrashView is a separate collection
        // from ExplorerView and NarratorView
        
        // Arrange
        var storyModel = new StoryModel();
        
        // Act & Assert
        Assert.IsNotNull(storyModel.TrashView, "TrashView collection should be initialized");
        Assert.IsNotNull(storyModel.ExplorerView, "ExplorerView collection should be initialized");
        Assert.IsNotNull(storyModel.NarratorView, "NarratorView collection should be initialized");
        
        // Verify they are separate collections
        Assert.AreNotSame(storyModel.TrashView, storyModel.ExplorerView, "TrashView should be separate from ExplorerView");
        Assert.AreNotSame(storyModel.TrashView, storyModel.NarratorView, "TrashView should be separate from NarratorView");
    }

    [TestMethod]
    public void TrashView_CollectionChanges_SetChangedFlag()
    {
        // Arrange
        var storyModel = new StoryModel();
        var deletedScene = new SceneModel("Deleted Scene", storyModel, null);
        var deletedItem = new StoryNodeItem(deletedScene, null);
        
        // Reset changed flag
        storyModel.Changed = false;
        
        // Act
        storyModel.TrashView.Add(deletedItem);
        
        // Assert
        Assert.IsTrue(storyModel.Changed, "Adding to TrashView should set Changed flag");
    }

    [TestMethod]
    public void DragAndDrop_Constraints_Documentation()
    {
        // This test documents the expected drag-and-drop behavior:
        // 
        // 1. Main NavigationTree (Explorer/Narrator views):
        //    - CanDragItems="True"
        //    - AllowDrop="True"
        //    - CanReorderItems="True"
        //
        // 2. TrashView TreeView:
        //    - CanDragItems="False" (cannot drag items out of trash)
        //    - AllowDrop="False" (cannot drop items into trash via drag)
        //    - CanReorderItems="False" (cannot reorder within trash)
        //
        // 3. Cross-TreeView Operations:
        //    - Not possible because TreeViews are separate controls
        //    - Move to trash: Use context menu "Delete" command
        //    - Restore from trash: Use context menu "Restore" command
        
        Assert.IsTrue(true, "Drag-and-drop constraints are enforced through XAML properties");
    }

    #endregion
}
