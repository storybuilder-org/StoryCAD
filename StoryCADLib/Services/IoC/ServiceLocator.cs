using Microsoft.Extensions.DependencyInjection;
using StoryCADLib.Collaborator.ViewModels;
using StoryCADLib.DAL;
using StoryCADLib.Models.Tools;
using StoryCADLib.Services.Backend;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Navigation;
using StoryCADLib.Services.Outline;
using StoryCADLib.Services.Ratings;
using StoryCADLib.Services.Search;
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

    public static void Initialise(bool headless = true, ServiceCollection additionalServices = null)
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
        Services.AddSingleton<ControlLoader>();
        Services.AddSingleton<ListLoader>();
        Services.AddSingleton<ToolLoader>();
        Services.AddSingleton<ScrivenerIo>();
        Services.AddSingleton<StoryIO>();
        Services.AddSingleton<MySqlIo>();
        Services.AddSingleton<OutlineService>();
        Services.AddSingleton<BackupService>();
        Services.AddSingleton<AutoSaveService>();
        Services.AddSingleton<BackendService>();
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
        Services.AddSingleton<WorkflowViewModel>();
        Services.AddSingleton<TrashCanViewModel>();
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
        // Register Tools ViewModels
        Services.AddSingleton<KeyQuestionsViewModel>();
        Services.AddSingleton<TopicsViewModel>();
        Services.AddSingleton<MasterPlotsViewModel>();
        Services.AddSingleton<BeatSheetsViewModel>();
        Services.AddSingleton<StockScenesViewModel>();
        Services.AddSingleton<DramaticSituationsViewModel>();
        Services.AddSingleton<SaveAsViewModel>();
        Services.AddSingleton<PreferencesViewModel>();
        Services.AddSingleton<FlawViewModel>();
        Services.AddSingleton<TraitsViewModel>();
        Services.AddSingleton<OutlineService>();
    }
}
