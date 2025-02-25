﻿using Microsoft.Extensions.DependencyInjection;
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

namespace StoryCAD.Services.IoC
{
    public static class ServiceLocator
    {
        public static ServiceCollection Services { get; private set; }

        static ServiceLocator()
        {
            Services = new ServiceCollection();
        }

        public static void Initialize()
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
            Services.AddSingleton<UnifiedVM>();
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
}