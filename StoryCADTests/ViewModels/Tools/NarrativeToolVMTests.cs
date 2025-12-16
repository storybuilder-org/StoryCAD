using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Outline;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADTests.ViewModels.Tools;

[TestClass]
public class NarrativeToolVMTests
{
    /// <summary>
    ///     Note: NarrativeToolVM still has a ShellViewModel dependency due to VerifyToolUse.
    ///     This needs to be refactored to a service in a future task.
    ///     These tests verify the constructor signature for DI purposes.
    /// </summary>
    [TestMethod]
    public void Constructor_HasCorrectDISignature()
    {
        // This test verifies that NarrativeToolVM has the correct constructor signature
        // for dependency injection after adding Windowing

        // Arrange
        var constructors = typeof(NarrativeToolVM).GetConstructors();

        // Act
        var diConstructor = constructors.FirstOrDefault(c =>
            c.GetParameters().Length == 5 &&
            c.GetParameters()[0].ParameterType.Name == "ShellViewModel" &&
            c.GetParameters()[1].ParameterType == typeof(AppState) &&
            c.GetParameters()[2].ParameterType == typeof(Windowing) &&
            c.GetParameters()[3].ParameterType == typeof(ToolValidationService) &&
            c.GetParameters()[4].ParameterType == typeof(ILogService));

        // Assert
        Assert.IsNotNull(diConstructor,
            "NarrativeToolVM should have a constructor with (ShellViewModel, AppState, Windowing, ToolValidationService, ILogService) parameters");
    }

    [TestMethod]
    public void Constructor_HasWindowing()
    {
        // This test ensures NarrativeToolVM has Windowing as a constructor parameter

        // Arrange
        var constructors = typeof(NarrativeToolVM).GetConstructors();

        // Act
        var hasWindowing = constructors.Any(c =>
            c.GetParameters().Any(p => p.ParameterType == typeof(Windowing)));

        // Assert
        Assert.IsTrue(hasWindowing, "NarrativeToolVM should have Windowing as a constructor parameter");
    }

    [TestMethod]
    public void OpenNarrativeTool_MethodExists()
    {
        // This test verifies that the OpenNarrativeTool method exists
        // Note: We can't test the actual execution due to UI dependencies and ShellViewModel dependency

        // Arrange
        var method = typeof(NarrativeToolVM).GetMethod("OpenNarrativeTool");

        // Assert
        Assert.IsNotNull(method, "OpenNarrativeTool method should exist");
        Assert.AreEqual(typeof(Task), method.ReturnType, "OpenNarrativeTool should return Task");
    }

    [TestMethod]
    public void Constructor_StillHasShellViewModelDependency()
    {
        // This test documents that NarrativeToolVM still depends on ShellViewModel
        // This is a known issue that needs to be refactored in a separate task

        // Arrange
        var constructors = typeof(NarrativeToolVM).GetConstructors();

        // Act
        var hasShellViewModelDependency = constructors.Any(c =>
            c.GetParameters().Any(p => p.ParameterType.Name == "ShellViewModel"));

        // Assert
        Assert.IsTrue(hasShellViewModelDependency,
            "NarrativeToolVM still has ShellViewModel dependency (needs refactoring to extract VerifyToolUse to a service)");
    }

    #region Delete Tests

    [TestMethod]
    public async Task Delete_WhenNodeDeleted_ClearsSelectedNode()
    {
        // Arrange - This tests the fix for deleted elements remaining highlighted
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        var toolValidation = Ioc.Default.GetRequiredService<ToolValidationService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Create test model with narrator view
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");

        // Create a scene and add it to narrator view via its node
        var scene = new SceneModel("Test Scene", model, model.ExplorerView[0]);
        scene.Node.CopyToNarratorView(model);
        var narratorScene = model.NarratorView[0].Children.FirstOrDefault(c => c.Name == "Test Scene");

        var vm = new NarrativeToolVM(shellVM, appState, windowing, toolValidation, logger);
        vm.SelectedNode = narratorScene;
        vm.IsNarratorSelected = true;

        // Act
        vm.Delete();

        // Assert
        Assert.IsNull(vm.SelectedNode, "SelectedNode should be null after deletion");
        Assert.IsFalse(vm.IsNarratorSelected, "IsNarratorSelected should be false after deletion");
    }

    [TestMethod]
    public async Task Delete_WhenNotNarratorSelected_DoesNotClearSelection()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        var toolValidation = Ioc.Default.GetRequiredService<ToolValidationService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Create model with a scene
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");
        var scene = new SceneModel("Test Scene", model, model.ExplorerView[0]);

        var vm = new NarrativeToolVM(shellVM, appState, windowing, toolValidation, logger);
        vm.SelectedNode = scene.Node;
        vm.IsNarratorSelected = false; // Explorer view selected, not narrator

        // Act
        vm.Delete();

        // Assert - Selection should NOT be cleared since we can't delete from explorer in this context
        Assert.AreEqual(scene.Node, vm.SelectedNode, "SelectedNode should remain unchanged when not deleting from narrator");
        Assert.AreEqual("You can't delete from here!", vm.Message, "Should show appropriate message");
    }

    [TestMethod]
    public async Task Delete_WhenTrashCanSelected_ReturnsEarlyWithMessage()
    {
        // Arrange
        var appState = Ioc.Default.GetRequiredService<AppState>();
        var shellVM = Ioc.Default.GetRequiredService<ShellViewModel>();
        var windowing = Ioc.Default.GetRequiredService<Windowing>();
        var toolValidation = Ioc.Default.GetRequiredService<ToolValidationService>();
        var logger = Ioc.Default.GetRequiredService<ILogService>();
        var outlineService = Ioc.Default.GetRequiredService<OutlineService>();

        // Create model to get access to trash node
        var model = await outlineService.CreateModel("Test Story", "Test Author", 0);
        appState.CurrentDocument = new StoryDocument(model, "test.stbx");
        var trashNode = model.TrashView.FirstOrDefault();

        var vm = new NarrativeToolVM(shellVM, appState, windowing, toolValidation, logger);
        vm.SelectedNode = trashNode;
        vm.IsNarratorSelected = true;

        // Act
        vm.Delete();

        // Assert - Should return early without attempting deletion
        Assert.AreEqual("You can't delete this node!", vm.Message, "Should show cannot delete message");
        Assert.AreEqual(trashNode, vm.SelectedNode, "SelectedNode should remain unchanged for protected nodes");
    }

    #endregion
}
