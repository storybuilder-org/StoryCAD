using Microsoft.Extensions.DependencyInjection;
using StoryCAD.DAL;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Navigation;
using StoryCAD.Services.Search;
using StoryCAD.ViewModels.Tools;
using StoryCAD.Collaborator.ViewModels;
using StoryCAD.Models.Tools;
using StoryCAD.Services.API;
using StoryCAD.Services.Collaborator;
using StoryCAD.Services.Outline;
using StoryCAD.Services.Ratings;
using StoryCAD.ViewModels.SubViewModels;
using System;
using System.Drawing;

namespace StoryCAD.Services.IoC;
/// <summary>
/// Handles initialisation of StoryCADLib.
/// </summary>
public static class BootStrapper
{

    /// <summary>
    /// Tracks if we already initialised StoryCADLib 
    /// </summary>
    private static bool Initalised;

    public static ServiceCollection Services { get; private set; }

    static BootStrapper()
    {
        Services = new();
        Initalised = false;
    }

    public static void Initialise(bool headless = true, ServiceCollection additionalServices = null)
    {
        //Prevent running this twice.
        if (Initalised) { return; }

        //TODO: Support replacing the base services.
        //Add any additional services
        if (additionalServices != null)
        {
            Services = additionalServices;
        }

        //Add StoryCADLib Services
        SetupIOC();
        Ioc.Default.ConfigureServices(Services.BuildServiceProvider());

        // Set headless mode
        AppState state = Ioc.Default.GetRequiredService<AppState>();
        state.Headless = headless;

        //Setup prefs.
        PreferenceService prefs = Ioc.Default.GetRequiredService<PreferenceService>();
        prefs.Model = new();

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
        Services.AddSingleton<DeletionService>();
        Services.AddSingleton<BackendService>();
        Services.AddSingleton<CollaboratorService>();
        Services.AddSingleton<ListData>();
        Services.AddSingleton<ToolsData>();
        Services.AddSingleton<ControlData>();
        Services.AddSingleton<AppState>();
        Services.AddSingleton<Windowing>();
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
        Services.AddSingleton<SemanticKernelApi>();
        Services.AddSingleton<OutlineService>();
    }
}