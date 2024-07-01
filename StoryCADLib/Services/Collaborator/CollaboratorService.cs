using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Windows.ApplicationModel;
using Microsoft.UI.Windowing;
using StoryCAD.Collaborator;
using StoryCAD.Collaborator.ViewModels;
using StoryCAD.Services.Backup;
using WinUIEx;

namespace StoryCAD.Services.Collaborator;

public class CollaboratorService
{
    private bool dllExists;
    private string dllPath; 
    private AppState State = Ioc.Default.GetRequiredService<AppState>();
    private LogService logger = Ioc.Default.GetRequiredService<LogService>();
    private WizardStepViewModel stepWizard;
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
        MethodInfo  menuCall = collaboratorType.GetMethod("LoadWizardModel", new[] { typeof(StoryItemType) });
        object[] methodArgs = { args.SelectedElement.Type };
        // ...and invoke it
        menuCall!.Invoke(collaborator, methodArgs);
    }

    /// <summary>
    /// Load the WizardViewModel (WizardShell's VM) with the high level
    /// NavigationView menu.
    ///
    /// This is a proxy for Collaborator's LoadWizardViewModel.
    /// </summary>
    /// <param name="args"></param>
    public void LoadWizardViewModel()
    {
        // Get the 'LoadWizardViewModel' method. The method has no parameters.
        MethodInfo wizardCall = collaboratorType.GetMethod("LoadWizardViewModel");
        // ...and invoke it
        wizardCall!.Invoke(collaborator, null);
    }

    /// <summary>
    /// Load the current WizardStep from the collection of step models
    /// for this StoryElement type.
    ///
    /// This is a proxy for Collaborator's LoadWizardStep method.
    /// </summary>
    /// <param name="element">The StoryElement to process</param>
    /// <param name="stepName">The name (Title) of the StoryElement property to load</param>
    public void LoadWizardStep(StoryElement element, string stepName)
    {
        stepWizard = Ioc.Default.GetService<WizardStepViewModel>();
        stepWizard!.Model = element;
        // Get the 'LoadWizardStep' method that expects a parameter of type string (the name of the step)
        MethodInfo loadStep = collaboratorType.GetMethod("LoadWizardStep", new[] { typeof(StoryElement), typeof(string) });
        object[] methodArgs = { element, stepName  };
        // ...and invoke it
        loadStep!.Invoke(collaborator, methodArgs);
    }

    /// <summary>
    /// Load the WizardStepViewModel with the currentStep
    /// WizardStepModel.
    ///
    /// This is a proxy for Collaborator's LoadWizardStepViewModel method.
    /// </summary>
    public void LoadWizardStepViewModel()
    {
        // Get the 'LoadWizardStepViewModel' method. The method has no parameters.
        MethodInfo wizardCall = collaboratorType.GetMethod("LoadWizardStepViewModel");
        // ...and invoke it
        wizardCall!.Invoke(collaborator, null);
    }

    /// <summary>
    /// Process the WizardStepModel prompt.
    /// 
    /// This is a proxy for Collaborator's ProcessWizardStep method.
    /// </summary>
    public void ProcessWizardStep()
    {
        // Get the 'ProcessWizardStep' method. The method has no parameters.
        MethodInfo wizardCall = collaboratorType.GetMethod("ProcessWizardStep");
        // ...and invoke it
        wizardCall!.Invoke(collaborator, null);
    }

    /// <summary>
    /// Save any unchanged OutputProperty values to their StoryElement.
    ///
    /// This is a proxy for Collaborator's SaveOutputs() method.
    /// </summary>
    public void SaveOutputs()
    {
    }

    #endregion

    #region Collaboratorlib connection

    /// <summary>
    /// If the plugin is active, connect to CollaboratorLib and create an instance
    /// of Collaborator. 
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
        var collabArgs = Ioc.Default.GetService<ShellViewModel>()!.CollabArgs = new();
        collabArgs.WizardVm = Ioc.Default.GetService<WizardViewModel>();
        collabArgs.WizardStepVM = Ioc.Default.GetService<WizardStepViewModel>();
        object[] methodArgs = { collabArgs };

        collaborator = collaboratorType!.GetConstructors()[0].Invoke(methodArgs);
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
	    logger.Log(LogLevel.Info, "Locating Collaborator Package...");
	    Package CollaboratorPkg = Package.Current.Dependencies.FirstOrDefault(pkg => pkg.DisplayName == "CollaboratorPackage");
	    if (CollaboratorPkg != null)
	    {
		    logger.Log(LogLevel.Info, $"Found Collaborator Package, {CollaboratorPkg.DisplayName}" +
		                              $" version {CollaboratorPkg.Id.Version.Major}.{CollaboratorPkg.Id.Version.Minor}" +
		                              $".{CollaboratorPkg.Id.Version.Build} Located at {CollaboratorPkg.InstalledLocation}");

		    // Get the path to the DLL
		    dllPath = Path.Combine(CollaboratorPkg.InstalledPath, "CollaboratorLib.dll");
		    dllExists = File.Exists(dllPath); // Verify that the DLL is present

	    }
	    else
	    {
		    logger.Log(LogLevel.Info, "Failed to find Collaborator Package");
		    dllExists = false;
	    }


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

    public void DestroyCollaborator()
    {
        //TODO: Absolutely make sure Collaborator is not left in memory after this.
        logger.Log(LogLevel.Warn, "Destroying collaborator object.");
        if (CollaboratorWindow != null)
        {
            CollaboratorWindow.Close(); // Destroy window object
            logger.Log(LogLevel.Info, "Closed collaborator window");

            //Null objects to deallocate them
            CollabAssembly = null;
            logger.Log(LogLevel.Info, "Nulled collaborator objects");

            //Run garbage collection to clean up any remnants.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            logger.Log(LogLevel.Info, "Garbage collection finished.");

        }
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