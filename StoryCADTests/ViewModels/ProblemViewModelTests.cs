using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;
using System;
using System.Linq;

namespace StoryCADTests.ViewModels;

[TestClass]
public class ProblemViewModelTests
{
    private ProblemViewModel _viewModel;
    private ProblemModel _problemModel;
    private StoryModel _storyModel;

    [TestInitialize]
    public void TestInitialize()
    {
        // Create a test story model and problem model
        _storyModel = new StoryModel();
        _problemModel = new ProblemModel("Test Problem", _storyModel, null);

        // Initialize the view model
        _viewModel = Ioc.Default.GetService<ProblemViewModel>();
        _viewModel.Model = _problemModel;
        _viewModel.StructureBeats = new();
        _viewModel.StructureModelTitle = "Custom Beat Sheet"; // Enable editing
    }

    [TestMethod]
    public void CreateBeat_ShouldAddNewBeatToCollection()
    {
        // Arrange
        int initialCount = _viewModel.StructureBeats.Count;

        // Act
        _viewModel.CreateBeat(null, null);

        // Assert
        Assert.AreEqual(initialCount + 1, _viewModel.StructureBeats.Count);
        var newBeat = _viewModel.StructureBeats.Last();
        Assert.AreEqual("New Beat", newBeat.Title);
        Assert.AreEqual("Describe your beat here", newBeat.Description);
    }

    [TestMethod]
    public void CreateBeat_MultipleCalls_ShouldAddMultipleBeats()
    {
        // Arrange
        int initialCount = _viewModel.StructureBeats.Count;

        // Act
        _viewModel.CreateBeat(null, null);
        _viewModel.CreateBeat(null, null);
        _viewModel.CreateBeat(null, null);

        // Assert
        Assert.AreEqual(initialCount + 3, _viewModel.StructureBeats.Count);
    }

    [TestMethod]
    public void DeleteBeat_WithNullSelectedBeat_ShouldNotRemoveAnyBeat()
    {
        // Arrange
        _viewModel.StructureBeats.Add(CreateTestBeat("Test Beat"));
        _viewModel.SelectedBeat = null;
        int initialCount = _viewModel.StructureBeats.Count;

        // Act
        _viewModel.DeleteBeat(null, null);

        // Assert
        Assert.AreEqual(initialCount, _viewModel.StructureBeats.Count);
    }

    [TestMethod]
    public void MoveUp_WithSelectedBeatAtIndexZero_ShouldNotMove()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = beat1;
        _viewModel.SelectedBeatIndex = 0;

        // Act
        _viewModel.MoveUp(null, null);

        // Assert
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveUp_WithSelectedBeatAtIndexOne_ShouldMoveToIndexZero()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = beat2;
        _viewModel.SelectedBeatIndex = 1;

        // Act
        _viewModel.MoveUp(null, null);

        // Assert
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveUp_WithNullSelectedBeat_ShouldNotMoveAnyBeat()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = null;

        // Act
        _viewModel.MoveUp(null, null);

        // Assert
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveDown_WithSelectedBeatAtLastIndex_ShouldNotMove()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = beat2;
        _viewModel.SelectedBeatIndex = 1;

        // Act
        _viewModel.MoveDown(null, null);

        // Assert
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveDown_WithSelectedBeatAtIndexZero_ShouldMoveToIndexOne()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = beat1;
        _viewModel.SelectedBeatIndex = 0;

        // Act
        _viewModel.MoveDown(null, null);

        // Assert
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveDown_WithNullSelectedBeat_ShouldNotMoveAnyBeat()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.SelectedBeat = null;

        // Act
        _viewModel.MoveDown(null, null);

        // Assert
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat2));
    }

    [TestMethod]
    public void MoveDown_WithMultipleBeats_ShouldMoveCorrectly()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        var beat3 = CreateTestBeat("Beat 3");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.StructureBeats.Add(beat3);
        _viewModel.SelectedBeat = beat2;
        _viewModel.SelectedBeatIndex = 1;

        // Act
        _viewModel.MoveDown(null, null);

        // Assert
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(2, _viewModel.StructureBeats.IndexOf(beat2));
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat3));
    }

    [TestMethod]
    public void MoveUp_WithMultipleBeats_ShouldMoveCorrectly()
    {
        // Arrange
        var beat1 = CreateTestBeat("Beat 1");
        var beat2 = CreateTestBeat("Beat 2");
        var beat3 = CreateTestBeat("Beat 3");
        _viewModel.StructureBeats.Add(beat1);
        _viewModel.StructureBeats.Add(beat2);
        _viewModel.StructureBeats.Add(beat3);
        _viewModel.SelectedBeat = beat2;
        _viewModel.SelectedBeatIndex = 1;

        // Act
        _viewModel.MoveUp(null, null);

        // Assert
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(beat1));
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(beat2));
        Assert.AreEqual(2, _viewModel.StructureBeats.IndexOf(beat3));
    }

    [TestMethod]
    public void BeatOperations_IntegrationTest_ShouldWorkTogether()
    {
        // Arrange - Start with empty collection
        Assert.AreEqual(0, _viewModel.StructureBeats.Count);

        // Act & Assert - Create beats
        _viewModel.CreateBeat(null, null);
        _viewModel.CreateBeat(null, null);
        _viewModel.CreateBeat(null, null);
        Assert.AreEqual(3, _viewModel.StructureBeats.Count);

        // Act & Assert - Move middle beat up
        _viewModel.SelectedBeat = _viewModel.StructureBeats[1];
        _viewModel.SelectedBeatIndex = 1;
        _viewModel.MoveUp(null, null);
        Assert.AreEqual(0, _viewModel.StructureBeats.IndexOf(_viewModel.SelectedBeat));

        // Act & Assert - Move it down twice
        _viewModel.SelectedBeatIndex = 0;
        _viewModel.MoveDown(null, null);
        Assert.AreEqual(1, _viewModel.StructureBeats.IndexOf(_viewModel.SelectedBeat));

        _viewModel.SelectedBeatIndex = 1;
        _viewModel.MoveDown(null, null);
        Assert.AreEqual(2, _viewModel.StructureBeats.IndexOf(_viewModel.SelectedBeat));
    }

    [TestMethod]
    public void StructureBeats_PropertyChanges_ShouldNotifyPropertyChanged()
    {
        // Arrange
        bool propertyChangedFired = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.StructureBeats))
                propertyChangedFired = true;
        };

        // Act
        _viewModel.StructureBeats = new();

        // Assert
        Assert.IsTrue(propertyChangedFired);
    }

    /// <summary>
    /// Helper method to create test beats without relying on IoC container
    /// </summary>
    private StructureBeatViewModel CreateTestBeat(string title, string description = "Test Description")
    {
        // Create a beat with the required constructor parameters
        StructureBeatViewModel beat = new(title, description);

        return beat;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= _viewModel.OnPropertyChanged;
        }
        _viewModel = null;
        _problemModel = null;
        _storyModel = null;
    }
}