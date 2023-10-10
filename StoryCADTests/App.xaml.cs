using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using Windows.ApplicationModel;
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Installation;
using StoryCAD.Services.Logging;
using StoryCAD.Services.Preferences;
using dotenv.net.Utilities;
using dotenv.net;
using Syncfusion.Licensing;
using Path = System.IO.Path;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using StoryCAD.Services.IoC;
using StoryCAD.ViewModels;

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
            //Loads Singletons/VMs
            Ioc.Default.ConfigureServices(ServiceConfigurator.Configure());

            string path = Path.Combine(Package.Current.InstalledLocation.Path, ".env");
            DotEnvOptions options = new(false, new[] { path });
            try
            {
                DotEnv.Load(options);

                //Register Syncfusion license
                string token = EnvReader.GetStringValue("SYNCFUSION_TOKEN");
                SyncfusionLicenseProvider.RegisterLicense(token);

                Ioc.Default.GetRequiredService<Developer>().EnvPresent = true;
            }
            catch {  }

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
            Developer AppDat = Ioc.Default.GetRequiredService<Developer>();
            string pathMsg = string.Format("Configuration data location = " + AppDat.RootDirectory);
            _log.Log(LogLevel.Info, pathMsg);

            // Load Preferences
            PreferencesService pref = Ioc.Default.GetService<PreferencesService>();
            await pref.LoadPreferences(AppDat.RootDirectory);

            await ProcessInstallationFiles();

            await LoadTools(AppDat.RootDirectory);

            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.CreateDefaultUI();

            Window = new MainWindow();

            // Ensure the current window is active
            Window.Activate();

            UITestMethodAttribute.DispatcherQueue = Window.DispatcherQueue;

            // Replace back with e.Arguments when https://github.com/microsoft/microsoft-ui-xaml/issues/3368 is fixed
            Microsoft.VisualStudio.TestPlatform.TestExecutor.UnitTestClient.Run(Environment.CommandLine);
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

        private async Task LoadTools(string path)
        {
            try
            {
                ToolsData toolsdata = Ioc.Default.GetService<ToolsData>();
                _log.Log(LogLevel.Info, "Loading Tools.ini data");
                ToolLoader loader = Ioc.Default.GetService<ToolLoader>();
                await loader.Init(path);
                _log.Log(LogLevel.Info, $"{toolsdata.KeyQuestionsSource.Keys.Count} Key Questions created");
                _log.Log(LogLevel.Info, $"{toolsdata.StockScenesSource.Keys.Count} Stock Scenes created");
                _log.Log(LogLevel.Info, $"{toolsdata.TopicsSource.Count} Topics created");
                _log.Log(LogLevel.Info, $"{toolsdata.MasterPlotsSource.Count} Master Plots created");
                _log.Log(LogLevel.Info, $"{toolsdata.DramaticSituationsSource.Count} Dramatic Situations created");

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
