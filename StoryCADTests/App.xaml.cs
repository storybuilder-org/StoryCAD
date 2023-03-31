using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Installation;
using StoryCAD.Services.Json;
using StoryCAD.Services.Logging;
using StoryCAD.Services.Navigation;
using StoryCAD.Services.Preferences;
using StoryCAD.Services.Search;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;
using dotenv.net.Utilities;
using dotenv.net;
using Microsoft.Extensions.Options;
using Syncfusion.Licensing;
using Path = System.IO.Path;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryCADTests
{
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
            ConfigureIoc();

            string path = Path.Combine(Package.Current.InstalledLocation.Path, ".env");
            DotEnvOptions options = new(false, new[] { path });
            try
            {
                DotEnv.Load(options);

                //Register Syncfusion license
                string token = EnvReader.GetStringValue("SYNCFUSION_TOKEN");
                SyncfusionLicenseProvider.RegisterLicense(token);
            }
            catch { GlobalData.ShowDotEnvWarning = true; }

            this.InitializeComponent();

            _log = Ioc.Default.GetService<LogService>();
            //TODO: Does the unhandled exception handler belong in the test project?
            Current.UnhandledException += OnUnhandledException;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _log.Log(LogLevel.Info, "StoryCADTests.App launched");

            string pathMsg = string.Format("Configuration data location = " + GlobalData.RootDirectory);
            _log.Log(LogLevel.Info, pathMsg);

            // Load Preferences
            PreferencesService pref = Ioc.Default.GetService<PreferencesService>();
            await pref.LoadPreferences(GlobalData.RootDirectory);

            await ProcessInstallationFiles();

            await LoadControls(GlobalData.RootDirectory);
            await LoadLists(GlobalData.RootDirectory);
            await LoadTools(GlobalData.RootDirectory);

            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();

            Window = new MainWindow();

            // Ensure the current window is active
            Window.Activate();

            UITestMethodAttribute.DispatcherQueue = Window.DispatcherQueue;

            // Replace back with e.Arguments when https://github.com/microsoft/microsoft-ui-xaml/issues/3368 is fixed
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
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
}
