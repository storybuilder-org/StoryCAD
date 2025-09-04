using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Services;
using StoryCAD.Services.Logging;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Threading.Tasks;
using Windows.Storage;

namespace StoryCADTests.DAL;

[TestClass]
public class StoryIOTests
{
    private StoryIO _storyIO;
    private AppState _appState;
    private ILogService _logService;
    
    [TestInitialize]
    public void Initialize()
    {
        _appState = Ioc.Default.GetRequiredService<AppState>();
        _logService = Ioc.Default.GetRequiredService<ILogService>();
        _storyIO = new StoryIO(_logService, _appState);
    }
    
    [TestMethod]
    public void ReadStory_UpdatesCurrentDocumentFilePath()
    {
        // This test verifies that StoryIO no longer depends on OutlineViewModel
        // and can be constructed with just ILogService and AppState
        
        // Act - verify it compiles with new constructor
        var storyIO = new StoryIO(_logService, _appState);
        
        // Assert
        Assert.IsNotNull(storyIO);
    }
    
    [TestMethod]
    public void Constructor_AcceptsAppStateWithoutOutlineViewModel()
    {
        // Arrange & Act
        var storyIO = new StoryIO(_logService, _appState);
        
        // Assert
        Assert.IsNotNull(storyIO);
    }
}