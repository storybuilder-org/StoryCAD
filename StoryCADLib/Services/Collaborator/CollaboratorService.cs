using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using StoryCAD.Collaborator;
using StoryCAD.Collaborator.Models;
using StoryCAD.Collaborator.ViewModels;
using StoryCAD.Models;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Logging;
using StoryCAD.ViewModels;
using WinUIEx;

namespace StoryCAD.Services.Collaborator;

public class CollaboratorService
{
    private bool dllExists;
    private string dllPath; 
    private AppState State = Ioc.Default.GetRequiredService<AppState>();
    private LogService logger = Ioc.Default.GetRequiredService<LogService>();
    public  object CollaboratorProxy;
    public WizardStepViewModel stepWizard;
    private Assembly CollabAssembly;
    public WindowEx CollaboratorWindow;  // The secondary window for Collaborator
    private Type collaboratorType;
    private object collaborator;

    #region Collaborator calls
    public void LoadWizardModel(CollaboratorArgs args)
    {
        var wizard = Ioc.Default.GetService<WizardViewModel>();
        wizard!.Model = args.SelectedElement;
        // Get the 'GetMenuItems' method that expects a parameter of type 'StoryItemType'
        MethodInfo  menuCall = collaboratorType.GetMethod("GetMenuItems", new[] { typeof(StoryItemType) });
        object[] methodArgs = { args.SelectedElement.Type };
        // ...and invoke it
        var menuItems = (List<MenuItem>) menuCall!.Invoke(collaborator, methodArgs);
        // Load the model
        wizard.LoadModel(menuItems);
    }

    /// <summary>
    /// Request the WizardStepModel which contains the data for the selected
    /// step, in order to populate and run WizardStepViewModel.
    /// </summary>
    /// <param name="model">The StoryElement to process</param>
    /// <param name="stepName">The name (Title) of the StoryElement property to load</param>
    public WizardStepArgs LoadWizardStepModel(StoryElement model, string stepName)
    {
        stepWizard = Ioc.Default.GetService<WizardStepViewModel>();
        stepWizard!.Model = model;
        // Get the 'GetWizardStepModel' method that expects a parameter of type string (the name of the step)
        MethodInfo loadStep = collaboratorType.GetMethod("GetWizardStepModel", new[] { typeof(StoryElement), typeof(string) });
        object[] methodArgs = { model, stepName  };
        // ...and invoke it
        var stepModel  = (WizardStepArgs) loadStep!.Invoke(collaborator, methodArgs);
        return stepModel;
    }

    #endregion

    #region Collaboratorlib connection
    /// <summary>
    /// If the plugin is active, connect CollaboratorLib and create an instance
    /// of Collaborator. 
    /// 
    /// 
    /// </summary>
    public void ConnectCollaborator()
    {
        // Use the custom context to load the assembly
        CollabAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
        logger.Log(LogLevel.Info, "Loaded CollaboratorLib.dll");
        //Create a new WindowEx for collaborator to prevent access errors.
        CollaboratorWindow = new WindowEx();
        CollaboratorWindow.AppWindow.Closing += HideCollaborator;
        // Create a Window for StoryBuilder Collaborator
        Frame rootFrame = new();
        //CollaboratorWindow = args.window;
        CollaboratorWindow.MinWidth = Convert.ToDouble("500");
        CollaboratorWindow.MinHeight = Convert.ToDouble("500");
        CollaboratorWindow.Closed += (sender, args) => CollaboratorClosed();
        CollaboratorWindow.Title = "StoryCAD Collaborator";
        CollaboratorWindow.Content = rootFrame;
        logger.Log(LogLevel.Info, "Collaborator window created and configured.");

        rootFrame.Content = new WizardShell();
        logger.Log(LogLevel.Info, "Set collaborator window content to WizardShell.");

        // Get the type of the Collaborator class
        collaboratorType = CollabAssembly.GetType("StoryCollaborator.Collaborator");
        // Create an instance of the Collaborator class
        logger.Log(LogLevel.Info, "Calling Collaborator constructor.");
        collaborator = collaboratorType!.GetConstructors()[0].Invoke(new object[0]);
        logger.Log(LogLevel.Info, "Collaborator Constructor finished.");
    }
    public bool CollaboratorEnabled()
    {
        return State.DeveloperBuild
               && Debugger.IsAttached
               && FindDll();
    }

    /// <summary>
    /// Checks if CollaboratorLib.dll exists.
    /// </summary>
    /// <returns>True if CollaboratorLib.dll exists, false otherwise.</returns>
    private bool FindDll()
    {
        // Get the path to the Documents folder
        //string documentsPath = "C:\\Users\\RARI\\Documents\\Repos\\CADCorp\\CollabApp\\CollaboratorLib\\bin\\x64\\Debug\\net8.0-windows10.0.22621.0";
        string documentsPath = "C:\\dev\\src\\StoryBuilderCollaborator\\CollaboratorLib\\bin\\x64\\Debug\\net8.0-windows10.0.22621.0";
        dllPath = Path.Combine(documentsPath, "CollaboratorLib.dll");

        // Verify that the DLL is present
        dllExists = File.Exists(dllPath);
        logger.Log(LogLevel.Info, $"Collaborator.dll exists {dllExists}");
        return dllExists;
    }
    #endregion

    #region Show/Hide window
    //TODO: Use Show and hide properly
    //CollaboratorWindow.Show();
    //CollaboratorWindow.Activate();
    //Logger.Log(LogLevel.Debug, "Collaborator window opened and focused");
    /// <summary>
    /// This closes, disposes and full removes collaborator from memory.
    /// </summary>
    public void DestroyCollaborator(AppWindow sender, AppWindowClosingEventArgs args)
    {
        //TODO: Absolutely make sure Collaborator is not left in memory after this.
        logger.Log(LogLevel.Warn, "Destroying collaborator object.");
        if (CollaboratorProxy != null)
        {
            CollaboratorWindow.Close(); // Destroy window object
            logger.Log(LogLevel.Info, "Closed collaborator window");

            //Null objects to deallocate them
            CollabAssembly = null;
            CollaboratorProxy = null;
            logger.Log(LogLevel.Info, "Nulled collaborator objects");

            //Run garbage collection to clean up any remnants.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            logger.Log(LogLevel.Info, "Garbage collection finished.");

        }
    }

    /// <summary>
    /// This will hide the collaborator window.
    /// </summary>
    private void HideCollaborator(AppWindow appWindow, AppWindowClosingEventArgs e)
    {
        logger.Log(LogLevel.Debug, "Hiding collaborator window.");
        e.Cancel = true; // Cancel stops the window from being disposed.
        appWindow.Hide(); // Hide the window instead.
        logger.Log(LogLevel.Debug, "Successfully hid collaborator window.");

        //Call collaborator callback since we need to reenable async to prevent a locked state.
        FinishedCallback();
    }

    /// <summary>
    /// This is called when collaborator has finished doing stuff.
    /// Note: This is invoked from the Collaborator side via a Delegate named onDoneCallback
    /// </summary>
    private void FinishedCallback()
    {
        logger.Log(LogLevel.Info, "Collaborator Callback, re-enabling async");
        //Reenable Timed Backup if needed.
        if (Ioc.Default.GetRequiredService<PreferenceService>().Model.TimedBackup)
        {
            Ioc.Default.GetRequiredService<BackupService>().StartTimedBackup();
        }
        //Reenable auto save if needed.
        if (Ioc.Default.GetRequiredService<PreferenceService>().Model.AutoSave)
        {
            Ioc.Default.GetRequiredService<AutoSaveService>().StartAutoSave();
        }
        //Reenable StoryCAD buttons
        Ioc.Default.GetRequiredService<ShellViewModel>()._canExecuteCommands = true;
        logger.Log(LogLevel.Info, "Async re-enabled.");
    }
    public void CollaboratorClosed()
    {
        logger.Log(LogLevel.Debug, "Closing Collaborator.");
        //TODO: Add FTP upload code here.

    }
    #endregion

}

//TODO: On calls, set callback delegate
//args.onDoneCallback = FinishedCallback;
// Logging
//Logger.Log(LogLevel.Debug,
//    $"""
//     Collaborator Args Information
//     StoryModel FilePath -  {args.StoryModel.ProjectFile.Path}
//     StoryModel Elements - {args.StoryModel.StoryElements.Count}
//     Story Element Name  - {args.SelectedElement.Name}
//     Story Element GUID  - {args.SelectedElement.Uuid}
//     Story Element Type  - {args.SelectedElement.Type}
//     """);
//TODO: On calls, set model etc.


//CollaboratorWindow.Content = page;  // was WizardShell