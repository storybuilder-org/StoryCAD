﻿using System;
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
using StoryCAD.DAL;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Json;
using StoryCAD.Services.Logging;
using StoryCAD.Services.Navigation;
using StoryCAD.Services.Search;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;
using StoryCAD.Views;
using Syncfusion.Licensing;
using WinUIEx;
using AppInstance = Microsoft.Windows.AppLifecycle.AppInstance;
using UnhandledExceptionEventArgs = Microsoft.UI.Xaml.UnhandledExceptionEventArgs;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Microsoft.UI.Dispatching;
using StoryCAD.Services.Backup;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
using System.Globalization;
using System.Reflection;
using StoryCAD.Services.IoC;

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

    private IntPtr m_windowHandle;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        CheckForOtherInstances(); //Check other instances aren't already open.

        //Loads all Singletons/VMs
        Ioc.Default.ConfigureServices(ServiceConfigurator.Configure());

        string path = Path.Combine(Package.Current.InstalledLocation.Path, ".env");
        DotEnvOptions options = new(false, new[] { path });
        
        try
        {
            DotEnv.Load(options);

            //Register Syncfusion license
            string token = EnvReader.GetStringValue("SYNCFUSION_TOKEN");
            SyncfusionLicenseProvider.RegisterLicense(token);
            Ioc.Default.GetRequiredService<AppState>().EnvPresent = true;
        }
        catch { }

        InitializeComponent();

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
            _MainInstance.Activated += new Windowing().ActivateMainInstance;

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
                        //This will be launched when ShellVM has finished initalising
                        Ioc.Default.GetRequiredService<ShellViewModel>().FilePathToLaunch = fileArgs.Files.FirstOrDefault().Path; 
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
    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        _log.Log(LogLevel.Info, "StoryCAD.App launched");

        // Note: Shell_Loaded in Shell.xaml.cs will display a
        // connection status message as soon as it's displayable.

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

        if (Debugger.IsAttached) {_log.Log(LogLevel.Info, "Bypassing elmah.io as debugger is attached.");}
        else
        {
            if (Ioc.Default.GetRequiredService<AppState>().Preferences.ErrorCollectionConsent)
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
        
        Ioc.Default.GetService<BackendService>()!.StartupRecording();
        await Ioc.Default.GetService<AppState>().LoadPreferences();
        ConfigureNavigation();

        // Construct a Window to hold our Pages
        WindowEx mainWindow = new MainWindow() { MinHeight = 675, MinWidth = 900, Width = 1050,
            Height=750, Title="StoryCAD" };

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
        if (!Ioc.Default.GetRequiredService<AppState>().Preferences.PreferencesInitialized) {rootFrame.Navigate(typeof(PreferencesInitialization));}
        else {rootFrame.Navigate(typeof(Shell));}

        // Preserve both the Window and its Handle for future use
        Ioc.Default.GetRequiredService<Windowing>().MainWindow = (MainWindow) mainWindow;
        //Get the Window's HWND
        m_windowHandle = User32.GetActiveWindow();
        Ioc.Default.GetRequiredService<Windowing>().WindowHandle = m_windowHandle;

        _log.Log(LogLevel.Debug, $"Layout: Window size width={mainWindow.Width} height={mainWindow.Height}");
        _log.Log(LogLevel.Info, "StoryCAD App loaded and launched");

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