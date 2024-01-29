using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Models;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using StoryCAD.Collaborator;
using WinUIEx;

namespace StoryCAD.Services.Collaborator;

public class CollaboratorService
{
    private bool dllExists = false;
    private string dllPath; 
    private AppState State = Ioc.Default.GetRequiredService<AppState>();
    public object Collaborator;
    public IWizardViewModel WizardVM;
    public Assembly CollabAssembly;
    private WindowEx window; // Do not make public, control from the collaborator side.
    public bool CollaboratorEnabled()
    {
        return State.DeveloperBuild
               && Debugger.IsAttached
               && FindDll();
    }

    private bool FindDll()
    {
        // Get the path to the Documents folder
        string documentsPath = "C:\\Users\\RARI\\Desktop\\cadappcollab\\Collaborator\\CollaboratorLib\\bin\\x64\\Debug\\net8.0-windows10.0.22621.0";
        dllPath = Path.Combine(documentsPath, "CollaboratorLib.dll");

        // Verify that the DLL is present
        dllExists = File.Exists(dllPath);
        return dllExists;
    }

    /// <summary>
    /// Destroys collaborator window.
    /// </summary>
    public void DestroyWindow()
    {
        window.Close();
    }

    public void RunCollaborator(CollaboratorArgs args)
    {
        // Check collaborator hasn't already been initalised.
        if (Collaborator == null)
        {
            // Create custom AssemblyLoadContext for StoryCAD's location
            var assemblyPath = Assembly.GetExecutingAssembly().Location;
            var executingDirectory = Path.GetDirectoryName(assemblyPath);
            var loadContext = AssemblyLoadContext.Default;

            // Use the custom context to load the assembly
            CollabAssembly = loadContext.LoadFromAssemblyPath(dllPath);
            //Create a new WindowEx for collaborator to prevent access errors.
            window = new WindowEx();
            window.AppWindow.Closing += hideCollaborator;
            args.window = window;

            // Get the type of the Collaborator class
            Type collaboratorType = CollabAssembly.GetType("StoryCollaborator.Collaborator");

            // Create an instance of the Collaborator class
            Collaborator = collaboratorType.GetConstructors()[0].Invoke(new object[] { args });

            // Get WizardVM
            WizardVM = (IWizardViewModel)collaboratorType.GetField("wizard").GetValue(Collaborator);
            WizardVM.Model = args.SelectedElement;
            WizardVM.LoadModel();

            // Set Collaborator Window to CollaboratorShell
            collaboratorType.GetMethod("SetPage").Invoke(Collaborator, new object[] { new CollaboratorShell() });
        }
        else { args.window.Show(); }
    }

    private void hideCollaborator(AppWindow appWindow, AppWindowClosingEventArgs e)
    {
        e.Cancel = true;
        appWindow.Hide();
    }
}