using Microsoft.Extensions.DependencyInjection;
using StoryCAD.DAL;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Backup;
using StoryCAD.Services.Logging;
using StoryCAD.Services.Navigation;
using StoryCAD.Services.Search;
using StoryCAD.ViewModels.Tools;
using StoryCAD.ViewModels;
using StoryCAD.Collaborator.ViewModels;
using StoryCAD.Models;
using StoryCAD.Models.Tools;
using StoryCAD.Services.Collaborator;
using StoryCAD.Services.Ratings;

namespace StoryCAD.Services.IoC;

public class ServiceConfigurator
{
    public static ServiceProvider Configure()
    {
        return new ServiceCollection()
             .AddSingleton<NavigationService>()
             .AddSingleton<LogService>()
             .AddSingleton<SearchService>()
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
             .AddSingleton<CollaboratorService>()
             .AddSingleton<ListData>()
             .AddSingleton<ToolsData>()
             .AddSingleton<ControlData>()
             .AddSingleton<AppState>()
             .AddSingleton<Windowing>()
             .AddSingleton<RatingService>()
             .AddSingleton<ShellViewModel>()
             .AddSingleton<PreferenceService>()
             .AddSingleton<OverviewViewModel>()
             .AddSingleton<CharacterViewModel>()
             .AddSingleton<ProblemViewModel>()
             .AddSingleton<SettingViewModel>()
             .AddSingleton<SceneViewModel>()
             .AddSingleton<FolderViewModel>()
             .AddSingleton<WebViewModel>()
             .AddSingleton<WizardViewModel>()
             .AddSingleton<WizardStepViewModel>()
             .AddSingleton<TrashCanViewModel>()
             .AddSingleton<UnifiedVM>()
             .AddSingleton<InitVM>()
             .AddSingleton<FeedbackViewModel>()
             .AddSingleton<TreeViewSelection>()
             // Register ContentDialog ViewModels
             .AddSingleton<NewProjectViewModel>()
             .AddSingleton<NewRelationshipViewModel>()
             .AddSingleton<PrintReportDialogVM>()
             .AddSingleton<NarrativeToolVM>()
             .AddSingleton<ElementPickerVM>()
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
             .BuildServiceProvider();
    }
}
