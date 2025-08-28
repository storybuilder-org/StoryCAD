using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
//.AppContainer;
using StoryCAD.Models;
using StoryCAD.Services.Logging;
using System.IO;
using StoryCAD.DAL;

namespace StoryCADTests;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
	public static string InputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"TestInputs");
	public static string ResultsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"TestResults");

    private LogService _log;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        //Loads Singletons/VMs
        TestSetup.Initialize(null);//Runs the initialization code
        Directory.CreateDirectory(Ioc.Default.GetRequiredService<AppState>().RootDirectory);
        InitializeComponent();

        _log = Ioc.Default.GetService<LogService>();
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
        PreferencesIo preferencesIo = new();
        await preferencesIo.ReadPreferences();

        string pathMsg = string.Format("Configuration data location = " + State.RootDirectory);
        _log.Log(LogLevel.Info, pathMsg);

		// Replace back with e.Arguments when https://github.com/microsoft/microsoft-ui-xaml/issues/3368 is fixed
		Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
    }
}
