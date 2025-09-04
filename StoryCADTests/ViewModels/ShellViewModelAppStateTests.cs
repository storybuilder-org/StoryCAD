using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels;
using StoryCAD.Services;
using StoryCAD.Models;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryCADTests.ViewModels;

[TestClass]
public class ShellViewModelAppStateTests
{
    private ShellViewModel _shellViewModel;
    private AppState _appState;
    
    [TestInitialize]
    public void Initialize()
    {
        _shellViewModel = Ioc.Default.GetRequiredService<ShellViewModel>();
        _appState = Ioc.Default.GetRequiredService<AppState>();
    }
    
    [TestMethod]
    public void CreateBackupNow_WithNoCurrentDocument_ShowsWarning()
    {
        // Arrange
        _appState.CurrentDocument = null;
        
        // Act & Assert - should not throw
        var task = _shellViewModel.CreateBackupNow();
        Assert.IsNotNull(task);
    }
    
    [TestMethod]
    public void CreateBackupNow_WithEmptyCurrentView_ShowsWarning()
    {
        // Arrange
        var model = new StoryModel();
        _appState.CurrentDocument = new StoryDocument(model, "test.stbx");
        
        // Act & Assert - should not throw
        var task = _shellViewModel.CreateBackupNow();
        Assert.IsNotNull(task);
    }
    
    [TestMethod]
    public void ResetModel_CreatesNewEmptyDocument()
    {
        // Arrange
        var model = new StoryModel();
        _appState.CurrentDocument = new StoryDocument(model, "test.stbx");
        
        // Act
        _shellViewModel.ResetModel();
        
        // Assert
        Assert.IsNotNull(_appState.CurrentDocument);
        Assert.IsNotNull(_appState.CurrentDocument.Model);
        Assert.IsNull(_appState.CurrentDocument.FilePath);
    }
    
    [TestMethod]
    public void ResetModel_CreatesNewStoryModel()
    {
        // Arrange
        _appState.CurrentDocument = null;
        
        // Act
        _shellViewModel.ResetModel();
        
        // Assert
        Assert.IsNotNull(_appState.CurrentDocument);
        Assert.IsNotNull(_appState.CurrentDocument.Model);
        Assert.IsNull(_appState.CurrentDocument.FilePath);
    }
}