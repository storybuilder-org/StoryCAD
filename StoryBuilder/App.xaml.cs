using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ABI.Microsoft.UI.Windowing;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using WinUIEx;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PInvoke;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services;
using StoryBuilder.Services.Installation;
using StoryBuilder.Services.Json;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Navigation;
using StoryBuilder.Services.Preferences;
using StoryBuilder.Services.Search;
using StoryBuilder.Services.Parse;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;
using StoryBuilder.Views;
using dotenv.net;
using dotenv.net.Utilities;
using Syncfusion.Licensing;
using AppWindow = Microsoft.UI.Windowing.AppWindow;
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
    private const string SectionPage = "SectionPage";
    private const string SettingPage = "SettingPage";
    private const string TrashCanPage = "TrashCanPage";

    private LogService _log;

    private IntPtr m_windowHandle;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        ConfigureIoc();

        GlobalData.Version = "Version: " + Windows.ApplicationModel.Package.Current.Id.Version.Major + "." +
            Windows.ApplicationModel.Package.Current.Id.Version.Minor + "." + Windows.ApplicationModel.Package.Current.Id.Version.Build +
            "." + Windows.ApplicationModel.Package.Current.Id.Version.Revision;



        var path = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, ".env");
        var options = new DotEnvOptions(false, new[] { path });
        DotEnv.Load(options);
        
        //Register Syncfusion license
        var token = EnvReader.GetStringValue("SYNCFUSION_TOKEN");
        SyncfusionLicenseProvider.RegisterLicense(token);

        InitializeComponent();

        _log = Ioc.Default.GetService<LogService>();
        Current.UnhandledException += OnUnhandledException;
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
                .AddSingleton<BackupService>()
                .AddSingleton<DeletionService>()
                .AddSingleton<ParseService>()
                // Register ViewModels 
                .AddSingleton<ShellViewModel>()
                .AddSingleton<OverviewViewModel>()
                .AddSingleton<CharacterViewModel>()
                .AddSingleton<ProblemViewModel>()
                .AddSingleton<SettingViewModel>()
                .AddSingleton<SceneViewModel>()
                .AddSingleton<FolderViewModel>()
                .AddSingleton<SectionViewModel>()
                .AddSingleton<TrashCanViewModel>()
                .AddSingleton<UnifiedVM>()
                .AddSingleton<InitVM>()
                .AddSingleton<TreeViewSelection>()
                // Register ContentDialog ViewModels
                .AddSingleton<NewProjectViewModel>()
                .AddSingleton<NewRelationshipViewModel>()
                .AddSingleton<PrintReportDialogVM>()
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

        if (Debugger.IsAttached)
            _log.Log(LogLevel.Info, "Bypassing elmah.io");
        else
        {
            await _log.AddElmahTarget();
            if (GlobalData.ElmahLogging)
                _log.Log(LogLevel.Info, "elmah.io log target added");
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
        ParseService parse = Ioc.Default.GetService<ParseService>();    
        await pref.LoadPreferences(GlobalData.RootDirectory);
        // If the previous attempt to communicate to the back-end server failed, retry
        if (!GlobalData.Preferences.ParsePreferencesStatus)
            await parse.PostPreferences(GlobalData.Preferences);
        if (!GlobalData.Preferences.ParseVersionStatus)
            await parse.PostVersion();
        
        if (!GlobalData.Version.Equals(GlobalData.Preferences.Version))
        {
            // Process a version change (usually a new release)
            _log.Log(LogLevel.Info, "Version mismatch: " + GlobalData.Version + " != " + GlobalData.Preferences.Version);
            var preferences = GlobalData.Preferences;
            // Update Preferences
            preferences.Version = GlobalData.Version;
            PreferencesIO prefIO = new(preferences, System.IO.Path.Combine(ApplicationData.Current.RoamingFolder.Path, "Storybuilder"));
            await prefIO.UpdateFile();
            // Post deployment to backend server
            await parse.PostVersion();
        }

        await ProcessInstallationFiles();

        await LoadControls(GlobalData.RootDirectory);

        await LoadLists(GlobalData.RootDirectory);
            
        await LoadTools(GlobalData.RootDirectory);

        ConfigureNavigation();

        WindowEx mainWindow = new MainWindow();
        int dpi = User32.GetDpiForWindow(mainWindow.GetWindowHandle());
        float scalingFactor = (float)dpi / 96;

        mainWindow.MinHeight = 675 * scalingFactor;
        mainWindow.MinWidth = 900 * scalingFactor;
        mainWindow.Width = 1050 * scalingFactor;
        mainWindow.Height = 750 * scalingFactor;
        mainWindow.Title = "StoryBuilder";

        // Create a Frame to act as the navigation context and navigate to the first page (Shell)
        Frame rootFrame = new();
        // Place the frame in the current Window
        mainWindow.Content = rootFrame;
        mainWindow.CenterOnScreen(); //Centers the window on the monitor

        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(GlobalData.Preferences.PreferencesInitialised ? typeof(Shell) : typeof(PreferencesInitialization));
        }
        mainWindow.Activate();
        GlobalData.MainWindow = (MainWindow) mainWindow;
        //Get the Window's HWND
        m_windowHandle = User32.GetActiveWindow();
        GlobalData.WindowHandle = m_windowHandle;


        // The Window object doesn't (yet) have Width and Height properties in WInUI 3 Desktop yet.
        // To set the Width and Height, you can use the Win32 API SetWindowPos.
        // Note, you should apply the DPI scale factor if you are thinking of dpi instead of pixels.
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
            _log.Log(LogLevel.Info,
                $"{GlobalData.KeyQuestionsSource.Keys.Count} Key Questions created");
            _log.Log(LogLevel.Info,
                $"{GlobalData.StockScenesSource.Keys.Count} Stock Scenes created");
            _log.Log(LogLevel.Info,
                $"{GlobalData.TopicsSource.Count} Topics created");
            _log.Log(LogLevel.Info,
                $"{GlobalData.MasterPlotsSource.Count} Master Plots created");
            _log.Log(LogLevel.Info,
                $"{GlobalData.DramaticSituationsSource.Count} Dramatic Situations created");
            _log.Log(LogLevel.Info,
                $"{GlobalData.QuotesSource.Count} Quotes created");

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
            nav.Configure(SectionPage, typeof(SectionPage));
            nav.Configure(SettingPage, typeof(SettingPage));
            nav.Configure(ScenePage, typeof(ScenePage));
            nav.Configure(TrashCanPage, typeof(TrashCanPage));
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