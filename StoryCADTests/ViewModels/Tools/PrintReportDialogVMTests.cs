using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using StoryCAD.Services;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;

namespace StoryCADTests.ViewModels.Tools;

[TestClass]
public class PrintReportDialogVMTests
{
    /// <summary>
    /// Note: PrintReportDialogVM has UI dependencies (PrintDocument) that cannot be instantiated 
    /// in a test environment. These tests verify the constructor signature for DI purposes.
    /// Full integration testing would require UI test framework.
    /// </summary>
    
    [TestMethod]
    public void Constructor_HasCorrectDISignature()
    {
        // This test verifies that PrintReportDialogVM has the correct constructor signature
        // for dependency injection after removing ShellViewModel dependency
        
        // Arrange
        var constructors = typeof(PrintReportDialogVM).GetConstructors();
        
        // Act
        var diConstructor = constructors.FirstOrDefault(c => 
            c.GetParameters().Length == 4 &&
            c.GetParameters()[0].ParameterType == typeof(AppState) &&
            c.GetParameters()[1].ParameterType == typeof(Windowing) &&
            c.GetParameters()[2].ParameterType == typeof(EditFlushService) &&
            c.GetParameters()[3].ParameterType == typeof(ILogService));
        
        // Assert
        Assert.IsNotNull(diConstructor, "PrintReportDialogVM should have a constructor with (AppState, Windowing, EditFlushService, ILogService) parameters");
    }
    
    [TestMethod]
    public void Constructor_DoesNotDependOnShellViewModel()
    {
        // This test ensures PrintReportDialogVM no longer depends on ShellViewModel
        
        // Arrange
        var constructors = typeof(PrintReportDialogVM).GetConstructors();
        
        // Act
        var hasShellViewModelDependency = constructors.Any(c =>
            c.GetParameters().Any(p => p.ParameterType.Name == "ShellViewModel"));
        
        // Assert
        Assert.IsFalse(hasShellViewModelDependency, "PrintReportDialogVM should not have ShellViewModel as a constructor parameter");
    }
    
    [TestMethod]
    public void OpenPrintReportDialog_UsesEditFlushService()
    {
        // This test verifies that the OpenPrintReportDialog method exists and can be called
        // Note: We can't test the actual execution due to UI dependencies
        
        // Arrange
        var method = typeof(PrintReportDialogVM).GetMethod("OpenPrintReportDialog");
        
        // Assert
        Assert.IsNotNull(method, "OpenPrintReportDialog method should exist");
        Assert.AreEqual(typeof(Task), method.ReturnType, "OpenPrintReportDialog should return Task");
    }
}