using Microsoft.Extensions.DependencyInjection;
using StoryCADLib.DAL;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.API;
using StoryCADLib.Services.Backend;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Collaborator.Contracts;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.MacMenuBar;
using StoryCADLib.Services.Navigation;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Ratings;
using StoryCADLib.Services.Search;
using StoryCADLib.Services.Store;
using StoryCADLib.ViewModels.SubViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.IoC;

/// <summary>
///     Handles initialisation of StoryCADLib.
/// </summary>
public static class BootStrapper
{
    /// <summary>
    ///     Tracks if we already initialised StoryCADLib
    /// </summary>
    private static bool Initalised;

    static BootStrapper()
    {
        Services = new ServiceCollection();
        Initalised = false;
    }

    public static ServiceCollection Services { get; private set; }

    public static void Initialise(bool headless = true, ServiceCollection additionalServices = null,
        Func<ICollaborator> collaboratorFactory = null)
    {
        //Prevent running this twice.
        if (Initalised)
        {
            return;
        }

        //Add any additional services
        if (additionalServices != null)
        {
            Services = additionalServices;
        }

        //Register the Collaborator factory supplied by the head (CollaboratorLib compiled in).
        //Null when Collaborator is absent (public/free build) - CollaboratorService then reports
        //HasCollaborator == false and StoryCAD runs Collaborator-free.
        if (collaboratorFactory != null)
        {
            Services.AddSingleton<Func<ICollaborator>>(collaboratorFactory);
        }

        //Add StoryCADLib Services
        SetupIOC();
        Ioc.Default.ConfigureServices(Services.BuildServiceProvider());

        // Set headless mode
        var state = Ioc.Default.GetRequiredService<AppState>();
        state.Headless = headless;

        //Setup prefs.
        var prefs = Ioc.Default.GetRequiredService<PreferenceService>();
        prefs.Model = new PreferencesModel();

        //Set default preferences for headless mode
        if (headless)
        {
            prefs.Model.FirstName = "Headless";
            prefs.Model.LastName = "User";
            prefs.Model.Email = "sysadmin@storybuilder.org";
            prefs.Model.BackupOnOpen = false;
            prefs.Model.AutoSave = false;
            prefs.Model.TimedBackup = false;
        }

        //Mark as initalised
        Initalised = true;
    }

    private static void SetupIOC()
    {
        Services.AddSingleton<NavigationService>();
        Services.AddSingleton<ILogService, LogService>();
        Services.AddSingleton<LogService>();
        Services.AddSingleton<SearchService>();
        Services.AddSingleton<JSONResourceLoader>();
        Services.AddSingleton<ScrivenerIo>();
        Services.AddSingleton<StoryIO>();
        Services.AddSingleton<IMySqlIo, MySqlIo>();
        Services.AddSingleton<OutlineService>();
        Services.AddSingleton<StoryCADApi>();
        Services.AddSingleton<BackupService>();
        Services.AddSingleton<AutoSaveService>();
        Services.AddSingleton<BackendService>();
        Services.AddSingleton<IUsageTrackingService, UsageTrackingService>();
        Services.AddSingleton<CollaboratorService>();
        Services.AddSingleton<ListData>();
        Services.AddSingleton<ToolsData>();
        Services.AddSingleton<ControlData>();
        Services.AddSingleton<AppState>();
        Services.AddSingleton<EditFlushService>();
        Services.AddSingleton<Windowing>();
        Services.AddSingleton<FileOpenService>();
        Services.AddSingleton<FileCreateService>();
        Services.AddSingleton<ToolValidationService>();
        Services.AddSingleton<RatingService>();
        // Store billing (issue #30). NullStoreService is the default so a binary runs unchanged
        // outside a store bundle; platform heads override with a real store when one is available.
#if HAS_UNO
        // Factory so the dylib probe (a synchronous dlopen) runs lazily at first resolution —
        // App.xaml.cs's fire-and-forget activation init — instead of during DI setup on the
        // startup path. A failed macOS dylib load falls back to Null, never crashing startup.
        Services.AddSingleton<IStoreService>(sp =>
            OperatingSystem.IsMacOS() && StoreKitInterop.IsAvailable()
                ? new MacStoreService(sp.GetRequiredService<Windowing>(), sp.GetRequiredService<ILogService>())
                : new NullStoreService());
#elif WINDOWS && !HAS_UNO
        // Real WinAppSDK head only (same guard as WindowsStoreContextAdapter; HAS_UNO_WINUI is also
        // defined on the desktop head, which is handled above). This TFM only runs on Windows, so
        // register unconditionally; WindowsStoreService is plain C#, the WinRT calls live in the adapter.
        Services.AddSingleton<IStoreContextAdapter, WindowsStoreContextAdapter>();
        Services.AddSingleton<IStoreService, WindowsStoreService>();
#else
        Services.AddSingleton<IStoreService, NullStoreService>();
#endif
        Services.AddSingleton<IActivationClient, ProxyActivationClient>();
        Services.AddSingleton<IStoreActivationService, StoreActivationService>();
        Services.AddSingleton<StoryCADLib.ViewModels.Store.SubscribeDialogViewModel>();
        Services.AddSingleton<OutlineViewModel>();
        Services.AddSingleton<ShellViewModel>();
        Services.AddSingleton<PreferenceService>();
        Services.AddSingleton<OverviewViewModel>();
        Services.AddSingleton<CharacterViewModel>();
        Services.AddSingleton<ProblemViewModel>();
        Services.AddSingleton<SettingViewModel>();
        Services.AddSingleton<SceneViewModel>();
        Services.AddSingleton<FolderViewModel>();
        Services.AddSingleton<WebViewModel>();
        Services.AddSingleton<TrashCanViewModel>();
        Services.AddSingleton<StoryWorldViewModel>();
        Services.AddSingleton<FileOpenVM>();
        Services.AddSingleton<InitVM>();
        Services.AddSingleton<BackupNowVM>();
        Services.AddSingleton<FeedbackViewModel>();
        Services.AddSingleton<TreeViewSelection>();
        // Register ContentDialog ViewModels
        Services.AddSingleton<NewProjectViewModel>();
        Services.AddSingleton<NewRelationshipViewModel>();
        Services.AddSingleton<StoryIO>();
        Services.AddSingleton<PrintReportDialogVM>();
        Services.AddSingleton<NarrativeToolVM>();
        Services.AddSingleton<ElementPickerVM>();
        Services.AddSingleton<CopyElementsDialogVM>();
        // Register Tools ViewModels
        Services.AddSingleton<KeyQuestionsViewModel>();
        Services.AddSingleton<TopicsViewModel>();
        Services.AddSingleton<MasterPlotsViewModel>();

        Services.AddSingleton<StockScenesViewModel>();
        Services.AddSingleton<DramaticSituationsViewModel>();
        Services.AddSingleton<SaveAsViewModel>();
        Services.AddSingleton<PreferencesViewModel>();
        Services.AddSingleton<FlawViewModel>();
        Services.AddSingleton<TraitsViewModel>();
        Services.AddSingleton<OutlineService>();
        Services.AddSingleton<MacMenuBarService>();
    }
}
