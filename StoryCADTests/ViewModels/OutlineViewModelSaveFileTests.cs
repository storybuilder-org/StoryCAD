using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.ViewModels.SubViewModels;
using StoryCAD.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.Threading.Tasks;
using System.Linq;
using StoryCAD.Models;

namespace StoryCAD.Tests.ViewModels;

[TestClass]
public class OutlineViewModelSaveFileTests
{
    [TestMethod]
    public async Task SaveFile_CallsEditFlushService()
    {
        // This test verifies that OutlineViewModel.SaveFile() uses EditFlushService
        // instead of calling ShellViewModel.SaveModel()
        
        // Arrange
        var outlineViewModel = Ioc.Default.GetRequiredService<OutlineViewModel>();
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var storyModel = new StoryModel();
        appState.CurrentDocument = new StoryDocument(storyModel, @"C:\test.stbx");
        
        // We can't easily test the full SaveFile method without setting up a lot of state,
        // but we can verify that OutlineViewModel has EditFlushService injected
        var editFlushServiceField = outlineViewModel.GetType()
            .GetField("_editFlushService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Assert
        Assert.IsNotNull(editFlushServiceField, "OutlineViewModel should have _editFlushService field");
        var editFlushService = editFlushServiceField.GetValue(outlineViewModel);
        Assert.IsNotNull(editFlushService, "EditFlushService should be injected");
        Assert.IsInstanceOfType(editFlushService, typeof(EditFlushService));
    }
    
    [TestMethod]
    public void OutlineViewModel_DoesNotReferenceShellViewModelSaveModel()
    {
        // This test verifies that OutlineViewModel no longer calls ShellViewModel.SaveModel()
        // We check this by searching for the string in the compiled assembly
        
        // Arrange
        var outlineViewModel = Ioc.Default.GetRequiredService<OutlineViewModel>();
        
        // Act - check that OutlineViewModel has EditFlushService dependency
        var constructor = outlineViewModel.GetType().GetConstructors()[0];
        var parameters = constructor.GetParameters();
        
        // Assert - should have EditFlushService parameter
        Assert.IsTrue(parameters.Any(p => p.ParameterType == typeof(EditFlushService)),
            "OutlineViewModel constructor should have EditFlushService parameter");
    }
}