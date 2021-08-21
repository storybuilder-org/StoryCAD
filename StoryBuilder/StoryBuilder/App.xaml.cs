using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Help;
using StoryBuilder.Services.Installation;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Preferences;
using StoryBuilder.Services.Search;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;
using StoryBuilder.Views;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using NavigationService = StoryBuilder.Services.Navigation.NavigationService;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private const int width = 1050;
        private const int height = 700;
        public static string HomePage = "HomePage";
        public static string OverviewPage = "OverviewPage";
        public static string ProblemPage = "ProblemPage";
        public static string CharacterPage = "CharacterPage";
        public static string PlotPointPage = "PlotPointPage";
        public static string FolderPage = "FolderPage";
        public static string SectionPage = "SectionPage";
        public static string SettingPage = "SettingPage";
        public static string TrashCanPage = "TrashCanPage";

        private LogService _log;

        private Window m_window;
        private IntPtr m_windowHandle;
        public IntPtr WindowHandle { get { return m_windowHandle; } }

        private void SetWindowSize(IntPtr hwnd, int width, int height)
        {
            var dpi = PInvoke.User32.GetDpiForWindow(hwnd);
            float scalingFactor = (float)dpi / 96;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);

            PInvoke.User32.SetWindowPos(hwnd, PInvoke.User32.SpecialWindowHandles.HWND_TOP,
                                        0, 0, width, height,
                                        PInvoke.User32.SetWindowPosFlags.SWP_SHOWWINDOW);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("EECDBF0E-BAE9-4CB6-A68E-9598E1CB57BB")]
        internal interface IWindowNative
        {
            IntPtr WindowHandle { get; }
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            ConfigureIoc();
            InitializeComponent();
            Current.UnhandledException += OnUnhandledException;
        }

        private void ConfigureIoc()
        {
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    // Register services
                    .AddSingleton<PreferencesService>()
                    .AddSingleton<NavigationService>()
                    .AddSingleton<LogService>()
                    .AddSingleton<HelpService>()
                    .AddSingleton<SearchService>()
                    .AddSingleton<InstallationService>()
                    .AddSingleton<ControlLoader>()
                    .AddSingleton<ListLoader>()
                    .AddSingleton<ToolLoader>()
                    .AddSingleton<ScrivenerIo>()
                    .AddSingleton<StoryController>()
                    .AddSingleton<StoryReader>()
                    .AddSingleton<StoryWriter>()
                    // Register ViewModels 
                    .AddSingleton<ShellViewModel>()
                    .AddSingleton<OverviewViewModel>()
                    .AddSingleton<CharacterViewModel>()
                    .AddSingleton<ProblemViewModel>()
                    .AddSingleton<SettingViewModel>()
                    .AddSingleton<PlotPointViewModel>()
                    .AddSingleton<FolderViewModel>()
                    .AddSingleton<SectionViewModel>()
                    .AddSingleton<TrashCanViewModel>()
                    .AddSingleton<TreeViewSelection>()
                    // Register ContentDialog ViewModels
                    .AddSingleton<NewProjectViewModel>()
                    .AddSingleton<NewRelationshipViewModel>()
                    // Register Tools ViewModels  
                    .AddSingleton<KeyQuestionsViewModel>()
                    .AddSingleton<TopicsViewModel>()
                    .AddSingleton<MasterPlotsViewModel>()
                    .AddSingleton<StockScenesViewModel>()
                    .AddSingleton<DramaticSituationsViewModel>()
                    .AddSingleton<SaveAsViewModel>()
                    .AddSingleton<PreferencesViewModel>()
                    // Complete 
                    .BuildServiceProvider());
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            //Current.Resources.Add("Locator", new ViewModelLocator());
            _log = Ioc.Default.GetService<LogService>();
            _log.Log(LogLevel.Info, "StoryBuilder.App launched");

            await ProcessInstallationFiles();


            StoryController story = Ioc.Default.GetService<StoryController>();
            string localPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}";
            localPath = System.IO.Path.Combine(localPath, "StoryBuilder");
            StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(localPath);
            _log.Log(LogLevel.Info, "Configuration data location = " + localFolder.Path);

            PreferencesService pref = Ioc.Default.GetService<PreferencesService>();
            await pref.LoadPreferences(localFolder.Path, story);

            await LoadControls(localFolder.Path, story);

            await LoadLists(localFolder.Path, story);

            await LoadTools(localFolder.Path, story);

            ConfigureNavigation();

            m_window = new MainWindow();
            // Create a Frame to act as the navigation context and navigate to the first page (Shell)
            Frame rootFrame = new Frame();
            if (rootFrame.Content == null)
                rootFrame.Navigate(typeof(Shell));
            // Place the frame in the current Window
            m_window.Content = rootFrame;
            m_window.Activate();

            //Get the Window's HWND
            m_windowHandle = PInvoke.User32.GetActiveWindow();
            m_window.Title = "StoryBuilder";
            // The Window object doesn't (yet) have Width and Height properties in WInUI 3 Desktop yet.
            // To set the Width and Height, you can use the Win32 API SetWindowPos.
            // Note, you should apply the DPI scale factor if you are thinking of dpi instead of pixels.
            SetWindowSize(m_windowHandle, width, height);   // was 800, 600
            _log.Log(LogLevel.Debug, string.Format("Layout: Window size width={0} height={1}", width, height));
            _log.Log(LogLevel.Info, "StoryBuilder App loaded and launched");
        }

        private async Task ProcessInstallationFiles()
        {
            try
            {
                _log.Log(LogLevel.Info, "Processing Installation files");
                var install = Ioc.Default.GetService<InstallationService>();
                await install.InstallFiles();
            }
            catch (Exception ex)
            {
                _log.LogException(LogLevel.Error, ex, "Error loading Installation files");
                AbortApp();
            }
        }

        private async Task LoadControls(string path, StoryController story)
        {
            int subTypeCount = 0;
            int exampleCount = 0;
            try
            {
                _log.Log(LogLevel.Info, "Loading Controls.ini data");
                ControlLoader loader = Ioc.Default.GetService<ControlLoader>();
                await loader.Init(path, story);
                _log.Log(LogLevel.Info, "ConflictType Counts");
                _log.Log(LogLevel.Info,
                    $"{GlobalData.ConflictTypes.Keys.Count} ConflictType keys created");
                foreach (ConflictCategoryModel type in GlobalData.ConflictTypes.Values)
                {
                    subTypeCount += type.SubCategories.Count;
                    foreach (string subType in type.SubCategories)
                    {
                        exampleCount += type.Examples[subType].Count;
                    }
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
        private async Task LoadLists(string path, StoryController story)
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

        private async Task LoadTools(string path, StoryController story)
        {
            try
            {
                _log.Log(LogLevel.Info, "Loading Tools.ini data");
                ToolLoader loader = Ioc.Default.GetService<ToolLoader>();
                await loader.Init(path, story);
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
                nav.Configure(App.HomePage, typeof(HomePage));
                nav.Configure(App.OverviewPage, typeof(OverviewPage));
                nav.Configure(App.ProblemPage, typeof(ProblemPage));
                nav.Configure(App.CharacterPage, typeof(CharacterPage));
                nav.Configure(App.FolderPage, typeof(FolderPage));
                nav.Configure(App.SectionPage, typeof(SectionPage));
                nav.Configure(App.SettingPage, typeof(SettingPage));
                nav.Configure(App.PlotPointPage, typeof(PlotPointPage));
                nav.Configure(App.TrashCanPage, typeof(TrashCanPage));
            }
            catch (Exception ex)
            {
                _log.LogException(LogLevel.Error, ex, "Error configuring page navigation");
                AbortApp();
            }
        }

        private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            _log.LogException(LogLevel.Error, e.Exception, e.Message);
            _log.Flush();
            AbortApp();
        }

        private void AbortApp()
        {
            Application.Current.Exit();  // Win32
        }
    }
}
