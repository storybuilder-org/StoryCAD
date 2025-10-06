using StoryCADLib.Models;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Logging;
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
}
