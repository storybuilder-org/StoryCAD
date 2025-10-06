using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestExecutor;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Services.Logging;

namespace StoryCADTests;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static string InputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestInputs");
    public static string ResultsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults");

    private readonly LogService _log;

    /// <summary>
    ///     Initializes the singleton application object.  This is the first line of authored code
    ///     executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        //Loads Singletons/VMs
        TestSetup.Initialize(null); //Runs the initialization code
        Directory.CreateDirectory(Ioc.Default.GetRequiredService<AppState>().RootDirectory);
        InitializeComponent();

        _log = Ioc.Default.GetService<LogService>();
    }

    /// <summary>
    ///     Invoked when the application is launched normally by the end user.  Other entry points
    ///     will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _log.Log(LogLevel.Info, "StoryCADTests.App launched");
        var State = Ioc.Default.GetRequiredService<AppState>();
        PreferencesIo preferencesIo = new();
        await preferencesIo.ReadPreferences();

        var pathMsg = string.Format("Configuration data location = " + State.RootDirectory);
        _log.Log(LogLevel.Info, pathMsg);

#if WINDOWS10_0_22621_0_OR_GREATER
        // Replace back with e.Arguments when https://github.com/microsoft/microsoft-ui-xaml/issues/3368 is fixed
        UnitTestClient.Run(Environment.CommandLine);
#endif
    }
}
