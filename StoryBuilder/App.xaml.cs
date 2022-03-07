using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
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
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Navigation;
using StoryBuilder.Services.Preferences;
using StoryBuilder.Services.Search;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;
using StoryBuilder.Views;

using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private const int Width = 1050;
    private const int Height = 700;
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

    private static void SetWindowSize(IntPtr hwnd, int windowWidth, int windowHeight)
    {
        int dpi = User32.GetDpiForWindow(hwnd);
        float scalingFactor = (float)dpi / 96;
        windowWidth = (int)(windowWidth * scalingFactor);
        windowHeight = (int)(windowHeight * scalingFactor);

        User32.SetWindowPos(hwnd, User32.SpecialWindowHandles.HWND_TOP, 0, 0, windowWidth, windowHeight, User32.SetWindowPosFlags.SWP_SHOWWINDOW);
    }

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        ConfigureIoc();
        //Register Syncfusion license
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NTYzMTY1QDMxMzkyZTM0MmUzME9nM25VTkZZdjM1bDgxbHU3Y0pMTm9sTXJ5VDB4cTFvcmRKMEk0Ry8wUWM9");
        InitializeComponent();
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
        //Current.Resources.Add("Locator", new ViewModelLocator());
        _log = Ioc.Default.GetService<LogService>();
        _log.Log(LogLevel.Info, "StoryBuilder.App launched");

        string pathMsg = string.Format("Configuration data location = " + GlobalData.RootDirectory);
        _log.Log(LogLevel.Info, pathMsg);
        Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        Trace.AutoFlush = true;
        Trace.Indent();
        Trace.WriteLine(pathMsg);
        // Load Preferences
        PreferencesService pref = Ioc.Default.GetService<PreferencesService>();
        await pref.LoadPreferences(GlobalData.RootDirectory);

        await ProcessInstallationFiles();

        await LoadControls(GlobalData.RootDirectory);

        await LoadLists(GlobalData.RootDirectory);
            
        await LoadTools(GlobalData.RootDirectory);

        ConfigureNavigation();

        WindowEx mainWindow = new MainWindow();
        (mainWindow as WindowEx).MinHeight = Height;
        (mainWindow as WindowEx).MinWidth = Width;
        mainWindow.SetWindowSize(Width, Height);
        mainWindow.Title = "StoryBuilder";

        // Create a Frame to act as the navigation context and navigate to the first page (Shell)
        Frame rootFrame = new();
        // Place the frame in the current Window
        mainWindow.Content = rootFrame;
        mainWindow.Activate();
        mainWindow.CenterOnScreen(); //Centers the window on the monitor
    
        if (rootFrame.Content == null)
        {
            rootFrame.Navigate(GlobalData.Preferences.PreferencesInitialised ? typeof(Shell) : typeof(PreferencesInitialization));
        }
        GlobalData.MainWindow = (MainWindow) mainWindow;
        //Get the Window's HWND
        m_windowHandle = User32.GetActiveWindow();
        GlobalData.WindowHandle = m_windowHandle;



        // The Window object doesn't (yet) have Width and Height properties in WInUI 3 Desktop yet.
        // To set the Width and Height, you can use the Win32 API SetWindowPos.
        // Note, you should apply the DPI scale factor if you are thinking of dpi instead of pixels.
        //SetWindowSize(m_windowHandle, Width, Height);   // was 800, 600
        _log.Log(LogLevel.Debug, $"Layout: Window size width={Width} height={Height}");
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