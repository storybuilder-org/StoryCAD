using System.Diagnostics;
using Windows.UI.ViewManagement;
using dotenv.net;
using StoryCAD.DAL;
using StoryCAD.Models.Tools;
using StoryCAD.Services;
using StoryCAD.Services.Backend;
using StoryCAD.Services.IoC;
using StoryCAD.Services.Json;
using StoryCAD.Services.Logging;
using StoryCAD.Services.Navigation;
using StoryCAD.Views;

namespace StoryCAD;
public partial class App : Application
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

    private ILogService _log;

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

        _log = Ioc.Default.GetService<ILogService>();
        Current.UnhandledException += OnUnhandledException;
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
    private static void AbortApp() { Current.Exit(); }
    protected Window? MainWindow { get; private set; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
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

        MainWindow = new Window();
#if DEBUG
        MainWindow.UseStudio();
#endif

        if (Debugger.IsAttached) {_log.Log(LogLevel.Info, "Bypassing elmah.io as debugger is attached.");}
        else
        {
            if (Preferences.Model.ErrorCollectionConsent)
            {
                _log.AddElmahTarget();
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

        // Do not repeat app initialization when the Window already has content,
        // just ensure that the window is active
        if (MainWindow.Content is not Frame rootFrame)
        {
            // Create a Frame to act as the navigation context and navigate to the first page
            rootFrame = new Frame();

            // Place the frame in the current Window
            MainWindow.Content = rootFrame;

            rootFrame.NavigationFailed += OnNavigationFailed;
        }

        if (rootFrame.Content == null)
        {
            // When the navigation stack isn't restored navigate to the first page,
            // configuring the new page by passing required information as a navigation
            // parameter
            // Navigate to the first page:
            //   If we've not yet initialized Preferences, it's PreferencesInitialization.
            //   If we have initialized Preferences, it Shell.
            // PreferencesInitialization will Navigate to Shell after it's done its business.
            if (!Preferences.Model.PreferencesInitialized)
            {
                rootFrame.Navigate(typeof(PreferencesInitialization));
            }
            else { rootFrame.Navigate(typeof(Shell)); }
        }

        Windowing window = Ioc.Default.GetRequiredService<Windowing>();
        // Preserve both the Window and its Handle for future use
        window.MainWindow = MainWindow;

        //Get the Window's HWND
        window.WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window.MainWindow);

        ApplicationView.GetForCurrentView().SetPreferredMinSize(new Windows.Foundation.Size(1000, 700));
        ApplicationView.GetForCurrentView().TryResizeView(new Windows.Foundation.Size(1200, 800));
        //_log.Log(LogLevel.Debug, $"Layout: Window size width={mainWindow.Width} height={mainWindow.Height}");
        _log.Log(LogLevel.Info, "StoryCAD App loaded and launched");
            

        MainWindow.SetWindowIcon();
        // Ensure the current window is active
        MainWindow.Activate();
    }

    /// <summary>
    /// Invoked when Navigation to a certain page fails
    /// </summary>
    /// <param name="sender">The Frame which failed navigation</param>
    /// <param name="e">Details about the navigation failure</param>
    void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
    }

    /// <summary>
    /// Configures global Uno Platform logging
    /// </summary>
    public static void InitializeLogging()
    {
#if DEBUG
        // Logging is disabled by default for release builds, as it incurs a significant
        // initialization cost from Microsoft.Extensions.Logging setup. If startup performance
        // is a concern for your application, keep this disabled. If you're running on the web or
        // desktop targets, you can use URL or command line parameters to enable it.
        //
        // For more performance documentation: https://platform.uno/docs/articles/Uno-UI-Performance.html

        var factory = LoggerFactory.Create(builder =>
        {
#if __WASM__
            builder.AddProvider(new global::Uno.Extensions.Logging.WebAssembly.WebAssemblyConsoleLoggerProvider());
#elif __IOS__ || __MACCATALYST__
            builder.AddProvider(new global::Uno.Extensions.Logging.OSLogLoggerProvider());
#else
            builder.AddConsole();
#endif

            // Exclude logs below this level
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);

            // Default filters for Uno Platform namespaces
            builder.AddFilter("Uno", Microsoft.Extensions.Logging.LogLevel.Information);
            builder.AddFilter("Windows", Microsoft.Extensions.Logging.LogLevel.Information);
            builder.AddFilter("Microsoft", Microsoft.Extensions.Logging.LogLevel.Information);

            // Generic Xaml events
            // builder.AddFilter("Microsoft.UI.Xaml", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.VisualStateGroup", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.StateTriggerBase", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.UIElement", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.FrameworkElement", LogLevel.Trace );

            // Layouter specific messages
            // builder.AddFilter("Microsoft.UI.Xaml.Controls", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Layouter", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Controls.Panel", LogLevel.Debug );

            // builder.AddFilter("Windows.Storage", LogLevel.Debug );

            // Binding related messages
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );
            // builder.AddFilter("Microsoft.UI.Xaml.Data", LogLevel.Debug );

            // Binder memory references tracking
            // builder.AddFilter("Uno.UI.DataBinding.BinderReferenceHolder", LogLevel.Debug );

            // DevServer and HotReload related
            // builder.AddFilter("Uno.UI.RemoteControl", LogLevel.Information);

            // Debug JS interop
            // builder.AddFilter("Uno.Foundation.WebAssemblyRuntime", LogLevel.Debug );
        });

        global::Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory = factory;

#if HAS_UNO
        global::Uno.UI.Adapter.Microsoft.Extensions.Logging.LoggingAdapter.Initialize();
#endif
#endif
    }
}
