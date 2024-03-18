using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using System.IO;
using StoryCAD.Services;

namespace StoryCADTests;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private LogService _log;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        //Loads Singletons/VMs
        IocLoaderTests.Initialise(null);//Runs the initialization code
        Directory.CreateDirectory(Ioc.Default.GetRequiredService<AppState>().RootDirectory);
        InitializeComponent();

        _log = Ioc.Default.GetService<LogService>();
        Current.UnhandledException += OnUnhandledException;   //TODO: Does the unhandled exception handler belong in the test project?
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _log.Log(LogLevel.Info, "StoryCADTests.App launched");
        AppState State = Ioc.Default.GetRequiredService<AppState>();
        await Ioc.Default.GetRequiredService<StoryCAD.Services.PreferenceService>().LoadPreferences();

        string pathMsg = string.Format("Configuration data location = " + State.RootDirectory);
        _log.Log(LogLevel.Info, pathMsg);

        Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();
        Window = new MainWindow();
        // Ensure the current window is active
        Window.Activate();
        UITestMethodAttribute.DispatcherQueue = Window.DispatcherQueue;


        // Replace back with e.Arguments when https://github.com/microsoft/microsoft-ui-xaml/issues/3368 is fixed
        Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _log.LogException(LogLevel.Fatal, e.Exception, e.Message);
        _log.Flush();
        AbortApp();
    }

    /// <summary>
    /// Closes the app
    /// </summary>
    private static void AbortApp() { Current.Exit(); }

    public static Window Window;
}
