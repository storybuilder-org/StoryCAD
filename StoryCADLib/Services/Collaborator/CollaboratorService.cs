using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using Windows.ApplicationModel;
using Microsoft.UI.Windowing;
using StoryCAD.Collaborator;
using StoryCAD.Collaborator.ViewModels;
using StoryCAD.Services.API;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Collaborator.Contracts;
using WinUIEx;
using Windows.ApplicationModel.AppExtensions;
using StoryCAD.ViewModels.SubViewModels;

namespace StoryCAD.Services.Collaborator;

public class CollaboratorService
{
    private bool dllExists;
    private string dllPath;
    private AppState State = Ioc.Default.GetRequiredService<AppState>();
    private LogService logger = Ioc.Default.GetRequiredService<LogService>();
    private Assembly CollabAssembly;
    public WindowEx CollaboratorWindow;  // The secondary window for Collaborator
    private ICollaborator _collaboratorInterface;  // Interface-based reference

    #region Collaborator calls
    
    /// <summary>
    /// Gets whether a collaborator is available
    /// </summary>
    public bool HasCollaborator => _collaboratorInterface != null;
    
    /// <summary>
    /// Sets the collaborator instance (for testing or direct injection)
    /// </summary>
    public void SetCollaborator(ICollaborator collaboratorInstance)
    {
        _collaboratorInterface = collaboratorInstance;
    }
    
    public void LoadWorkflows(CollaboratorArgs args)
    {
        var wizard = Ioc.Default.GetService<WorkflowViewModel>();
        wizard!.Model = args.SelectedElement;
        
        if (_collaboratorInterface != null)
        {
            LoadWorkflowViewModel(args.SelectedElement.ElementType);
        }
        else
        {
            logger.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }
    
    /// <summary>
    /// Load the workflow view model for a specific element type
    /// </summary>
    public void LoadWorkflowViewModel(StoryItemType elementType)
    {
        if (_collaboratorInterface != null)
        {
            _collaboratorInterface.LoadWorkflowViewModel(elementType);
        }
        else
        {
            logger.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    /// Load the WizardViewModel (WizardShell's VM) with the high level
    /// NavigationView menu.
    ///
    /// This is a proxy for Collaborator's LoadWizardViewModel.
    /// </summary>
    public void LoadWizardViewModel()
    {
        if (_collaboratorInterface != null)
        {
            _collaboratorInterface.LoadWizardViewModel();
        }
        else
        {
            logger.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    /// Load the WorkflowViewModel with the currently selected 
    /// Workflow Model.
    ///
    /// This is a proxy for Collaborator's LoadWorkflowModel method.
    /// </summary>
    public void LoadWorkflowModel(StoryElement element, string workflow)
    {
        if (_collaboratorInterface != null)
        {
            _collaboratorInterface.LoadWorkflowModel(element, workflow);
        }
        else
        {
            logger.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    /// Process the Workflow we've loaded.
    /// 
    /// This is a proxy for Collaborator's ProcessWorkflow method.
    /// </summary>
    public void ProcessWorkflow()
    {
        if (_collaboratorInterface != null)
        {
            // Call async version synchronously for backward compatibility
            Task.Run(async () => await ProcessWorkflowAsync()).Wait();
        }
        else
        {
            logger.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }
    
    /// <summary>
    /// Process the Workflow we've loaded asynchronously.
    /// </summary>
    public async Task ProcessWorkflowAsync()
    {
        if (_collaboratorInterface != null)
        {
            await _collaboratorInterface.ProcessWorkflowAsync();
        }
        else
        {
            logger.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    public void SendButtonClicked()
    {
        if (_collaboratorInterface != null)
        {
            // Call async version synchronously for backward compatibility
            Task.Run(async () => await SendButtonClickedAsync()).Wait();
        }
        else
        {
            logger.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }
    
    /// <summary>
    /// Handle send button click asynchronously.
    /// </summary>
    public async Task SendButtonClickedAsync()
    {
        if (_collaboratorInterface != null)
        {
            await _collaboratorInterface.SendButtonClickedAsync();
        }
        else
        {
            logger.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
    }

    /// <summary>
    /// Save any unchanged OutputProperty values to their StoryElement.
    ///
    /// This is a proxy for Collaborator's SaveOutputs() method.
    /// </summary>
    public void SaveOutputs()
    {
        if (_collaboratorInterface != null)
        {
            _collaboratorInterface.SaveOutputs();
        }
        else
        {
            logger.Log(LogLevel.Error, "Collaborator interface not initialized");
        }
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

        rootFrame.Content = new WorkflowShell();
        logger.Log(LogLevel.Info, "Set collaborator window content to WizardShell.");

        // Get the type of the Collaborator class
        var collaboratorType = CollabAssembly.GetType("StoryCollaborator.Collaborator");
        if (collaboratorType == null)
        {
            logger.Log(LogLevel.Error, "Could not find Collaborator type in assembly");
            return;
        }
        
        // Create an instance of the Collaborator class
        logger.Log(LogLevel.Info, "Calling Collaborator constructor.");
        var collabArgs = Ioc.Default.GetService<ShellViewModel>()!.CollabArgs = new();
        collabArgs.WorkflowVm = Ioc.Default.GetService<WorkflowViewModel>();
        collabArgs.CollaboratorWindow = CollaboratorWindow;
        object[] methodArgs = { collabArgs };

        // Find the constructor that takes CollaboratorArgs parameter
        var constructor = collaboratorType.GetConstructor(new[] { typeof(CollaboratorArgs) });
        if (constructor == null)
        {
            logger.Log(LogLevel.Error, "Could not find Collaborator constructor that takes CollaboratorArgs");
            throw new InvalidOperationException("Collaborator must have a constructor that takes CollaboratorArgs");
        }
        var collaborator = constructor.Invoke(methodArgs);
        logger.Log(LogLevel.Info, "Collaborator Constructor finished.");
        
        // Cast to interface - this is now required
        _collaboratorInterface = collaborator as ICollaborator;
        if (_collaboratorInterface != null)
        {
            logger.Log(LogLevel.Info, "Collaborator successfully loaded with ICollaborator interface.");
        }
        else
        {
            logger.Log(LogLevel.Error, "Collaborator does not implement ICollaborator interface. Cannot proceed.");
            throw new InvalidOperationException("CollaboratorLib must implement ICollaborator interface");
        }
    }
    public async Task<bool> CollaboratorEnabled()
    {
        return State.DeveloperBuild
               && Debugger.IsAttached
               && await FindDll();
    }

    /// <summary>
    /// Checks if CollaboratorLib.dll exists.
    /// </summary>
    /// <returns>True if CollaboratorLib.dll exists, false otherwise.</returns>
    private async Task<bool> FindDll()
    {
	    logger.Log(LogLevel.Info, "Locating CollaboratorLib...");

		// Development path - when debugging with StoryBuilderCollaborator source
		if (Debugger.IsAttached && State.DeveloperBuild)
		{
			// Get the current assembly's location (StoryCAD)
			var currentAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			var storyCADDir = Path.GetDirectoryName(currentAssemblyPath);
			
			// Navigate up to find the common parent directory containing both repos
			// Typically: .../StoryCAD/StoryCADLib/bin/x64/Debug/net8.0-windows10.0.xxxxx/StoryCADLib.dll
			// We need to go up 7 levels to reach the parent of StoryCAD (the repos directory)
			var reposDir = storyCADDir;
			for (int i = 0; i < 7; i++)
			{
				reposDir = Path.GetDirectoryName(reposDir);
				if (reposDir == null) break;
			}
			
			if (reposDir != null)
			{
				// Try multiple possible debug output paths (different .NET versions)
				string[] relativePaths = 
				{
					@"StoryBuilderCollaborator\CollaboratorLib\bin\x64\Debug\net8.0-windows10.0.22621.0\CollaboratorLib.dll",
					@"StoryBuilderCollaborator\CollaboratorLib\bin\x64\Debug\net8.0-windows10.0.19041.0\CollaboratorLib.dll",
					@"StoryBuilderCollaborator\CollaboratorPackage\bin\x64\Debug\net8.0-windows10.0.22621\CollaboratorLib.dll"
				};

				foreach (var relativePath in relativePaths)
				{
					var devPath = Path.Combine(reposDir, relativePath);
					if (File.Exists(devPath))
					{
						dllPath = devPath;
						dllExists = true;
						logger.Log(LogLevel.Info, $"Found CollaboratorLib at development path: {dllPath}");
						return true;
					}
				}
			}
			logger.Log(LogLevel.Warn, "CollaboratorLib not found in development paths, falling back to package lookup");
		}

		// Production path - look for installed package (existing code)
		//Find all installed extensions
	    AppExtensionCatalog _catalog = AppExtensionCatalog.Open("org.storybuilder");
	    var InstalledExtensions = await _catalog.FindAllAsync();
	    logger.Log(LogLevel.Info, $"Found {InstalledExtensions} installed extensions");

		//No point in continuing if we have no extensions.
		if (InstalledExtensions.Count == 0)
		{
			return false;
		}

		//Get package information for collaborator if installed.
		Package CollabPkg = InstalledExtensions.First(ext => 
			ext.Package.DisplayName == "StoryCAD Collaborator").Package;
	    if (CollabPkg != null)
	    {
		    logger.Log(LogLevel.Info, $"Found Collaborator Package, {CollabPkg.DisplayName}" +
		                              $" version {CollabPkg.Id.Version.Major}" +
		                              $".{CollabPkg.Id.Version.Minor}" +
		                              $".{CollabPkg.Id.Version.Build} " +
		                              $"Located at {CollabPkg.InstalledLocation}");

		    if (await CollabPkg.VerifyContentIntegrityAsync())
		    {
			    // Get the path to the DLL
			    dllPath = Path.Combine(CollabPkg.InstalledPath, "CollaboratorLib.dll");
			    dllExists = File.Exists(dllPath); // Verify that the DLL is present
			}
		    else
		    {
				logger.Log(LogLevel.Error, "Failed to verify CollabPackage, " +
				                           "not loading it (Not a StoryCAD issue)");
		    }


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
        //TODO: Use lock here.
        //Ioc.Default.GetRequiredService<OutlineViewModel>()._canExecuteCommands = true;
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