using System.Diagnostics;
using Windows.ApplicationModel;
using dotenv.net;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Json;
using StoryCAD.Services.Logging;
using StoryCAD.Services.Navigation;
using StoryCAD.Views;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;
using Windows.ApplicationModel.Activation;
using Microsoft.UI.Xaml;
using StoryCAD.DAL;
using StoryCAD.Services.IoC;
using StoryCAD.Services;
using StoryCAD.Services.Collaborator;
using Microsoft.Extensions.DependencyInjection;
using StoryCAD.Models.Tools;

namespace StoryCAD;

public partial class App
{
    private const string HomePage = "HomePage";
    private const string OverviewPage = "OverviewPage";
    private const string ProblemPage = "ProblemPage";
    private const string CharacterPage = "CharacterPage";
    private const string ScenePage = "ScenePage";
    private const string FolderPage = "FolderPage";
    private const string SettingPage = "SettingPage";
    private const string TrashCanPage = "TrashCanPage";
    private const string WebPage = "WebPage";

    private LogService _log;

	/// <summary>
	/// This is the path to the STBX file that StoryCAD was launched with,
	/// if StoryCAD wasn't launched with a file this will be null.
	/// </summary>
	string LaunchPath = null;

	/// <summary>
	/// Used to track uptime of app
	/// </summary>
	DateTime StartTime = DateTime.Now;

	/// <summary>
	/// Initializes the singleton application object. This is the first line of authored code
	/// executed, and as such is the logical equivalent of main() or WinMain().
	/// </summary>
	public App()
    {
        CheckForOtherInstances(); //Check other instances aren't already open.
 
        //Set up IOC and the services.
        BootStrapper.Initialise(false);

        string path = Path.Combine(Package.Current.InstalledLocation.Path, ".env");
        DotEnvOptions options = new(false, new[] { path });
        
        try
        {
            DotEnv.Load(options);
            Ioc.Default.GetRequiredService<AppState>().EnvPresent = true;
        }
        catch { }

        InitializeComponent();

        // Make sure ToolsData is loaded by forcing instantiation
        Ioc.Default.GetRequiredService<ToolsData>();

        _log = Ioc.Default.GetService<LogService>();
        Current.UnhandledException += OnUnhandledException;
    }

    /// <summary>
    /// This checks for other already open StoryCAD instances
    /// If one is open, pull it up and kill this instance.
    /// </summary>
    private void CheckForOtherInstances()
    {
        Task.Run( async () =>
        {
            //If this instance is the first, then we will register it, otherwise we will get info about the other instance.
            AppInstance _MainInstance = AppInstance.FindOrRegisterForKey("main"); //Get main instance
            _MainInstance.Activated += (((sender, e) => new Windowing().ActivateMainInstance()));

            AppActivationArguments activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            //Redirect to other instance if one exists, otherwise continue initializing this instance.
            if (!_MainInstance.IsCurrent)
            {
                //Bring up the 'main' instance 
                await _MainInstance.RedirectActivationToAsync(activatedEventArgs);
                Process.GetCurrentProcess().Kill();
            }
            else
            {
                if (activatedEventArgs.Kind == ExtendedActivationKind.File)
                {
                    if (activatedEventArgs.Data is IFileActivatedEventArgs fileArgs)
                    {
                        //This will be launched when ShellVM has finished initialising
                        LaunchPath = fileArgs.Files.FirstOrDefault().Path; 
                    }
                }
            }
        });
    }


    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _log.Log(LogLevel.Info, "StoryCAD.App launched");
		// Note: Shell_Loaded in Shell.xaml.cs will display a
		// connection status message as soon as it's displayable.

		PreferenceService Preferences = Ioc.Default.GetRequiredService<PreferenceService>();

		// Obtain keys if defined
		try
        {
            Doppler doppler = new();
            Doppler keys = await doppler.FetchSecretsAsync();
            BackendService backend = Ioc.Default.GetService<BackendService>();
            await backend.SetConnectionString(keys);
            _log.SetElmahTokens(keys);
        }
        catch (Exception ex) { _log.LogException(LogLevel.Error, ex, ex.Message); }

        AppState AppDat = Ioc.Default.GetRequiredService<AppState>();
        string pathMsg = string.Format("Configuration data location = " + AppDat.RootDirectory);
        _log.Log(LogLevel.Info, pathMsg);
        
        Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        Trace.AutoFlush = true;
        Trace.Indent();
        Trace.WriteLine(pathMsg);

        //Load user preferences or initialise them.
        await new PreferencesIo().ReadPreferences();

		//Set the launch path in ShellVM so if a File was used to open StoryCAD,
		//so that the file will be opened when shell has finished loading.
		Ioc.Default.GetRequiredService<ShellViewModel>().FilePathToLaunch = LaunchPath;

        if (Debugger.IsAttached) {_log.Log(LogLevel.Info, "Bypassing elmah.io as debugger is attached.");}
        else
        {
            if (Preferences.Model.ErrorCollectionConsent)
            {
                _log.ElmahLogging = _log.AddElmahTarget();
                if (_log.ElmahLogging) { _log.Log(LogLevel.Info, "elmah successfully added."); }
                else { _log.Log(LogLevel.Info, "Couldn't add elmah."); }
            }
            else  // can have several reasons (no doppler, or an error adding the target){
            {
                _log.Log(LogLevel.Info, "elmah.io log target bypassed");
            }
        }

        await Ioc.Default.GetService<BackendService>()!.StartupRecording();
        ConfigureNavigation();

        // Construct a Window to hold our Pages
        Window mainWindow = new MainWindow() {/* MinHeight = 675, MinWidth = 900, Width = 1050, Height=750,*/ Title="StoryCAD"};

        // Create a Frame to act as the navigation context 
        Frame rootFrame = new();
        // Place the frame in the current Window
        mainWindow.Content = rootFrame;
        mainWindow.Activate();

        // Navigate to the first page:
        //   If we've not yet initialized Preferences, it's PreferencesInitialization.
        //   If we have initialized Preferences, it Shell.
        // PreferencesInitialization will Navigate to Shell after it's done its business.
        if (!Preferences.Model.PreferencesInitialized)
        {
	        rootFrame.Navigate(typeof(PreferencesInitialization));
        }
        else {rootFrame.Navigate(typeof(Shell));}

        Windowing window = Ioc.Default.GetRequiredService<Windowing>();

        // Preserve both the Window and its Handle for future use
        window.MainWindow = (MainWindow) mainWindow;

        //Get the Window's HWND
        window.WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window.MainWindow);

        //_log.Log(LogLevel.Debug, $"Layout: Window size width={mainWindow.Width} height={mainWindow.Height}");
        _log.Log(LogLevel.Info, "StoryCAD App loaded and launched");
    }

	private void MainWindow_Closed(object sender, WindowEventArgs args)
	{
        ///TODO: MainWindow_Closed is never called. Call when appropriate (from exit/shutdown)
		//Update used time
		PreferenceService Prefs = Ioc.Default.GetService<PreferenceService>();
		AppState State = Ioc.Default.GetService<AppState>();
		Prefs.Model.CumulativeTimeUsed += Convert.ToInt64((DateTime.Now - StartTime).TotalSeconds);

		//Save prefs
		PreferencesIo prefIO = new();
		Task.Run(async () => { await prefIO.WritePreferences(Prefs.Model); });

		//Purge Collaborator from memory
		Ioc.Default.GetRequiredService<CollaboratorService>().DestroyCollaborator();
	}

	private void ConfigureNavigation()
    {
        try
        {
            _log.Log(LogLevel.Info, "Configuring page navigation");
            NavigationService nav = Ioc.Default.GetService<NavigationService>();
            nav.Configure(HomePage, typeof(HomePage));
            nav.Configure(OverviewPage, typeof(OverviewPage));
            nav.Configure(ProblemPage, typeof(ProblemPage));
            nav.Configure(CharacterPage, typeof(CharacterPage));
            nav.Configure(FolderPage, typeof(FolderPage));
            nav.Configure(SettingPage, typeof(SettingPage));
            nav.Configure(ScenePage, typeof(ScenePage));
            nav.Configure(TrashCanPage, typeof(TrashCanPage));
            nav.Configure(WebPage, typeof(WebPage));
        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error configuring page navigation");
            AbortApp();
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        _log.LogException(LogLevel.Fatal, e.Exception, e.Message);
        _log.Flush();
        AbortApp();
    }

    /// <summary>
    /// Closes the app
    /// </summary>
    private static void AbortApp() { Current.Exit();  }
}