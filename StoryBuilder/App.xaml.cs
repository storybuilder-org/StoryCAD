  using System;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Threading.Tasks;
  using Windows.ApplicationModel;
  using CommunityToolkit.Mvvm.DependencyInjection;
  using dotenv.net;
  using dotenv.net.Utilities;
  using Microsoft.Extensions.DependencyInjection;
  using Microsoft.UI.Xaml;
  using Microsoft.UI.Xaml.Controls;
  using Microsoft.Windows.AppLifecycle;
  using PInvoke;
  using StoryBuilder.DAL;
  using StoryBuilder.Models;
  using StoryBuilder.Models.Tools;
  using StoryBuilder.Services;
  using StoryBuilder.Services.Backend;
  using StoryBuilder.Services.Installation;
  using StoryBuilder.Services.Json;
  using StoryBuilder.Services.Logging;
  using StoryBuilder.Services.Navigation;
  using StoryBuilder.Services.Preferences;
  using StoryBuilder.Services.Search;
  using StoryBuilder.ViewModels;
  using StoryBuilder.ViewModels.Tools;
  using StoryBuilder.Views;
  using WinUIEx;
  using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;
  using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

  namespace StoryBuilder;

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


    private LogService _log;

    private IntPtr m_windowHandle;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        CheckForOtherInstances(); //Check other instances aren't already open.
        
        ConfigureIoc();

        GlobalData.Version = "Version: " + Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + "." + Package.Current.Id.Version.Build + "." + Package.Current.Id.Version.Revision;
        string path = Path.Combine(Package.Current.InstalledLocation.Path, ".env");
        DotEnvOptions options = new(false, new[] { path });
        
        try
        {
            DotEnv.Load(options);

        }
        catch { GlobalData.ShowDotEnvWarning = true; }
        
        InitializeComponent();

        _log = Ioc.Default.GetService<LogService>();
        Current.UnhandledException += OnUnhandledException;
    }

    /// <summary>
    /// This checks for other already open storybuilder instances
    /// If one is open, pull it up and kill this instance.
    /// </summary>
    private async void CheckForOtherInstances()
    {
        //If this instance is the first, then we will register it, otherwise we will get info about the other instance.
        AppInstance _MainInstance = AppInstance.FindOrRegisterForKey("main"); //Get main instance
        _MainInstance.Activated += ActivateMainInstance;

        //Redirect to other instance if one exists, otherwise continue initializing this instance.
        if (!_MainInstance.IsCurrent)
        {
            //Bring up the 'main' instance 
            AppActivationArguments _ActivatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            await _MainInstance.RedirectActivationToAsync(_ActivatedEventArgs);
            Process.GetCurrentProcess().Kill();
        }
    }

    /// <summary>
    /// When a second instance is opened, this code will be ran on the main (first) instance
    /// It will bring up the main window.
    /// </summary>
    private void ActivateMainInstance(object sender, AppActivationArguments e)
    {
        GlobalData.MainWindow.Restore(); //Resize window and unminimize window
        GlobalData.MainWindow.BringToFront(); //Bring window to front
    }

    private static void ConfigureIoc()
    {
        Ioc.Default.ConfigureServices(
            new ServiceCollection()
                // Register services
                .AddSingleton<PreferencesService>()
                .AddSingleton<NavigationService>()
                .AddSingleton<LogService>()
                .AddSingleton<SearchService>()
                .AddSingleton<InstallationService>()
                .AddSingleton<ControlLoader>()
                .AddSingleton<ListLoader>()
                .AddSingleton<ToolLoader>()
                .AddSingleton<ScrivenerIo>()
                .AddSingleton<StoryReader>()
                .AddSingleton<StoryWriter>()
                .AddSingleton<MySqlIo>()
                .AddSingleton<BackupService>()
                .AddSingleton<AutoSaveService>()
                .AddSingleton<DeletionService>()
                .AddSingleton<BackendService>()
                // Register ViewModels 
                .AddSingleton<ShellViewModel>()
                .AddSingleton<OverviewViewModel>()
                .AddSingleton<CharacterViewModel>()
                .AddSingleton<ProblemViewModel>()
                .AddSingleton<SettingViewModel>()
                .AddSingleton<SceneViewModel>()
                .AddSingleton<FolderViewModel>()
                .AddSingleton<WebViewModel>()
                .AddSingleton<TrashCanViewModel>()
                .AddSingleton<UnifiedVM>()
                .AddSingleton<InitVM>()
                .AddSingleton<TreeViewSelection>()
                // Register ContentDialog ViewModels
                .AddSingleton<NewProjectViewModel>()
                .AddSingleton<NewRelationshipViewModel>()
                .AddSingleton<PrintReportDialogVM>()
                .AddSingleton<NarrativeToolVM>()
                // Register Tools ViewModels  
                .AddSingleton<KeyQuestionsViewModel>()
                .AddSingleton<TopicsViewModel>()
                .AddSingleton<MasterPlotsViewModel>()
                .AddSingleton<StockScenesViewModel>()
                .AddSingleton<DramaticSituationsViewModel>()
                .AddSingleton<SaveAsViewModel>()
                .AddSingleton<PreferencesViewModel>()
                .AddSingleton<FlawViewModel>()
                .AddSingleton<TraitsViewModel>()
                // Complete 
                .BuildServiceProvider());
    }

    /// <summary>
    /// Invoked when the application is launched normally by the end user.  Other entry points
    /// will be used such as when the application is launched to open a specific file.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _log.Log(LogLevel.Info, "StoryBuilder.App launched");

        // Note: Shell_Loaded in Shell.xaml.cs will display a
        // connection status message as soon as it's displayable.

        // Obtain keys if defined
        try
        {
            Doppler doppler = new Doppler();
            Doppler keys = await doppler.FetchSecretsAsync();
            BackendService backend = Ioc.Default.GetService<BackendService>();
            await backend.SetConnectionString(keys);
            _log.SetElmahTokens(keys);

        }
        catch (Exception ex) { _log.LogException(LogLevel.Error, ex, ex.Message); }

        if (Debugger.IsAttached) {_log.Log(LogLevel.Info, "Bypassing elmah.io as debugger is attached.");}
        else
        {
            //TODO: check elmah is bypassed when logging is disabled by user.
            await _log.AddElmahTarget();
            if (GlobalData.ElmahLogging) {_log.Log(LogLevel.Info, "elmah.io log target added");}
            else  // can have several reasons (no doppler, or an error adding the target)
                _log.Log(LogLevel.Info, "elmah.io log target bypassed");
        }

        string pathMsg = string.Format("Configuration data location = " + GlobalData.RootDirectory);
        _log.Log(LogLevel.Info, pathMsg);
        Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        Trace.AutoFlush = true;
        Trace.Indent();
        Trace.WriteLine(pathMsg);
        
        // Load Preferences
        PreferencesService pref = Ioc.Default.GetService<PreferencesService>();
        await pref.LoadPreferences(GlobalData.RootDirectory);
        
        Ioc.Default.GetService<BackendService>()!.StartupRecording();

        await ProcessInstallationFiles();

        await LoadControls(GlobalData.RootDirectory);
        await LoadLists(GlobalData.RootDirectory);
        await LoadTools(GlobalData.RootDirectory);

        ConfigureNavigation();

        // Construct a Window to hold our Pages
        WindowEx mainWindow = new MainWindow();
        mainWindow.MinHeight = 675;
        mainWindow.MinWidth = 900;
        mainWindow.Width = 1050;
        mainWindow.Height = 750;
        mainWindow.Title = "StoryBuilder";

        // Create a Frame to act as the navigation context 
        Frame rootFrame = new();
        // Place the frame in the current Window
        mainWindow.Content = rootFrame;
        mainWindow.CenterOnScreen(); // Centers the window on the monitor
        mainWindow.Activate();

        // Navigate to the first page:
        //   If we've not yet initialized Preferences, it's PreferencesInitialization.
        //   If we have initialized Preferences, it Shell.
        // PreferencesInitialization will Navigate to Shell after it's done its business.
        if (!GlobalData.Preferences.PreferencesInitialized) {rootFrame.Navigate(typeof(PreferencesInitialization));}
        else {rootFrame.Navigate(typeof(Shell));}

        // Preserve both the Window and its Handle for future use
        GlobalData.MainWindow = (MainWindow) mainWindow;
        //Get the Window's HWND
        m_windowHandle = User32.GetActiveWindow();
        GlobalData.WindowHandle = m_windowHandle;

        _log.Log(LogLevel.Debug, $"Layout: Window size width={mainWindow.Width} height={mainWindow.Height}");
        _log.Log(LogLevel.Info, "StoryBuilder App loaded and launched");

    }

    private async Task ProcessInstallationFiles()
    {
        try
        {
            _log.Log(LogLevel.Info, "Processing Installation files");
            await Ioc.Default.GetService<InstallationService>().InstallFiles(); //Runs InstallationService.InstallFiles()
        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error loading Installation files");
            AbortApp();
        }
    }

    private async Task LoadControls(string path)
    {
        int subTypeCount = 0;
        int exampleCount = 0;
        try
        {
            _log.Log(LogLevel.Info, "Loading Controls.ini data");
            ControlLoader loader = Ioc.Default.GetService<ControlLoader>();
            await loader.Init(path);
            _log.Log(LogLevel.Info, "ConflictType Counts");
            _log.Log(LogLevel.Info,
                $"{GlobalData.ConflictTypes.Keys.Count} ConflictType keys created");
            foreach (ConflictCategoryModel type in GlobalData.ConflictTypes.Values)
            {
                subTypeCount += type.SubCategories.Count;
                exampleCount += type.SubCategories.Sum(subType => type.Examples[subType].Count);
            }
            _log.Log(LogLevel.Info,
                $"{subTypeCount} Total ConflictSubType keys created");
            _log.Log(LogLevel.Info,
                $"{exampleCount} Total ConflictSubType keys created");
        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error loading Controls.ini");
            AbortApp();
        }
    }
    private async Task LoadLists(string path)
    {
        try
        {
            _log.Log(LogLevel.Info, "Loading Lists.ini data");
            ListLoader loader = Ioc.Default.GetService<ListLoader>();
            GlobalData.ListControlSource = await loader.Init(path);
            _log.Log(LogLevel.Info,
                $"{GlobalData.ListControlSource.Keys.Count} ListLoader.Init keys created");
        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error loading Lists.ini");
            AbortApp();
        }
    }

    private async Task LoadTools(string path)
    {
        try
        {
            _log.Log(LogLevel.Info, "Loading Tools.ini data");
            ToolLoader loader = Ioc.Default.GetService<ToolLoader>();
            await loader.Init(path);
            _log.Log(LogLevel.Info, $"{GlobalData.KeyQuestionsSource.Keys.Count} Key Questions created");
            _log.Log(LogLevel.Info, $"{GlobalData.StockScenesSource.Keys.Count} Stock Scenes created");
            _log.Log(LogLevel.Info, $"{GlobalData.TopicsSource.Count} Topics created");
            _log.Log(LogLevel.Info, $"{GlobalData.MasterPlotsSource.Count} Master Plots created");
            _log.Log(LogLevel.Info, $"{GlobalData.DramaticSituationsSource.Count} Dramatic Situations created");

        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error loading Tools.ini");
            AbortApp();
        }
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

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
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