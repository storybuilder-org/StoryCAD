using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Services.Help;
using StoryBuilder.Services.Installation;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Preferences;
using StoryBuilder.Services.Search;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using NavigationService = StoryBuilder.Services.Navigation.NavigationService;

namespace StoryBuilderTests
{
    [TestClass]
    public sealed class StoryBuilderTestSetup
    {
        [AssemblyInitialize()]
        public async static Task AssemblyInit(TestContext context)
        {
            ConfigureIoc();
            // Validate service locator
            StoryController story = Ioc.Default.GetService<StoryController>();
            string localPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}";
            localPath = System.IO.Path.Combine(localPath, "StoryBuilder");
            StorageFolder localFolder = await StorageFolder.GetFolderFromPathAsync(localPath);
            var install = Ioc.Default.GetService<InstallationService>();
            await install.InstallFiles();
            // Validate InstallFiles

            PreferencesService pref = Ioc.Default.GetService<PreferencesService>();
            await pref.LoadPreferences(localFolder.Path, story);
            // Validate preferences
            Assert.IsNotNull(GlobalData.Preferences);
            Assert.AreEqual(localPath, GlobalData.Preferences.InstallationDirectory);

            ListLoader loader = Ioc.Default.GetService<ListLoader>();
            GlobalData.ListControlSource = await loader.Init(localPath);
            Assert.AreEqual(11, GlobalData.ListControlSource["Adventurous"].Count);
            Assert.AreEqual(12, GlobalData.ListControlSource["Aggressiveness"].Count);
            Assert.AreEqual(9, GlobalData.ListControlSource["Archetype"].Count);
            Assert.AreEqual(13, GlobalData.ListControlSource["Assurance"].Count);
            Assert.AreEqual(7, GlobalData.ListControlSource["Build"].Count);
            Assert.AreEqual(7, GlobalData.ListControlSource["Complexion"].Count);
            Assert.AreEqual(17, GlobalData.ListControlSource["Confidence"].Count);
            Assert.AreEqual(14, GlobalData.ListControlSource["Conflict"].Count);
            Assert.AreEqual(7, GlobalData.ListControlSource["ConflictSource"].Count);
            Assert.AreEqual(7, GlobalData.ListControlSource["ConflictType"].Count);
            Assert.AreEqual(11, GlobalData.ListControlSource["Conscientiousness"].Count);
            Assert.AreEqual(11, GlobalData.ListControlSource["Creativeness"].Count);
            Assert.AreEqual(10, GlobalData.ListControlSource["Dominance"].Count);
            Assert.AreEqual(45, GlobalData.ListControlSource["Emotion"].Count);
            Assert.AreEqual(9, GlobalData.ListControlSource["Enneagram"].Count);
            Assert.AreEqual(10, GlobalData.ListControlSource["Enthusiasm"].Count);
            Assert.AreEqual(5, GlobalData.ListControlSource["EyeColor"].Count);
            Assert.AreEqual(9, GlobalData.ListControlSource["Focus"].Count);
            Assert.AreEqual(4, GlobalData.ListControlSource["Font"].Count);
            Assert.AreEqual(18, GlobalData.ListControlSource["Genre"].Count);
            Assert.AreEqual(70, GlobalData.ListControlSource["Goal"].Count);
            Assert.AreEqual(11, GlobalData.ListControlSource["HairColor"].Count);
            Assert.AreEqual(9, GlobalData.ListControlSource["Intelligence"].Count);
            Assert.AreEqual(90, GlobalData.ListControlSource["Locale"].Count);
            Assert.AreEqual(9, GlobalData.ListControlSource["MentalIllness"].Count);
            Assert.AreEqual(27, GlobalData.ListControlSource["Method"].Count);
            Assert.AreEqual(95, GlobalData.ListControlSource["Motive"].Count);
            Assert.AreEqual(74, GlobalData.ListControlSource["Country"].Count);
            Assert.AreEqual(9, GlobalData.ListControlSource["LanguageStyle"].Count);
            Assert.AreEqual(23, GlobalData.ListControlSource["LiteraryDevice"].Count);
            Assert.AreEqual(104, GlobalData.ListControlSource["LiteraryStyle"].Count);
            Assert.AreEqual(8, GlobalData.ListControlSource["Opposition"].Count);
            Assert.AreEqual(11, GlobalData.ListControlSource["Outcome"].Count);
            Assert.AreEqual(7, GlobalData.ListControlSource["ProblemSource"].Count);
            Assert.AreEqual(3, GlobalData.ListControlSource["ProblemType"].Count);
            Assert.AreEqual(5, GlobalData.ListControlSource["Race"].Count);
            Assert.AreEqual(216, GlobalData.ListControlSource["Role"].Count);
            Assert.AreEqual(12, GlobalData.ListControlSource["SceneOutcome"].Count);
            Assert.AreEqual(9, GlobalData.ListControlSource["ScenePurpose"].Count);
            Assert.AreEqual(15, GlobalData.ListControlSource["SceneType"].Count);
            Assert.AreEqual(7, GlobalData.ListControlSource["Season"].Count);
            Assert.AreEqual(9, GlobalData.ListControlSource["Sensitivity"].Count);
            Assert.AreEqual(16, GlobalData.ListControlSource["Shrewdness"].Count);
            Assert.AreEqual(14, GlobalData.ListControlSource["Sociability"].Count);
            Assert.AreEqual(8, GlobalData.ListControlSource["Stability"].Count);
            Assert.AreEqual(11, GlobalData.ListControlSource["Title"].Count);
            Assert.AreEqual(6, GlobalData.ListControlSource["StoryRole"].Count);
            Assert.AreEqual(8, GlobalData.ListControlSource["StoryType"].Count);
            Assert.AreEqual(212, GlobalData.ListControlSource["ProblemSubject"].Count);
            Assert.AreEqual(5, GlobalData.ListControlSource["Tense"].Count);
            Assert.AreEqual(33, GlobalData.ListControlSource["Theme"].Count);
            Assert.AreEqual(220, GlobalData.ListControlSource["Tone"].Count);
            Assert.AreEqual(9, GlobalData.ListControlSource["Topic"].Count);
            Assert.AreEqual(38, GlobalData.ListControlSource["Value"].Count);
            Assert.AreEqual(163, GlobalData.ListControlSource["ValueExchange"].Count);
            Assert.AreEqual(5, GlobalData.ListControlSource["Viewpoint"].Count);
            Assert.AreEqual(7, GlobalData.ListControlSource["Voice"].Count);
            Assert.AreEqual(8, GlobalData.ListControlSource["WoundCategory"].Count);
            Assert.AreEqual(121, GlobalData.ListControlSource["Wound"].Count);
        }
        private static void ConfigureIoc()
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
                    .AddSingleton<ListLoader>()
                    .AddSingleton<ControlLoader>()
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
                    // Register Open/Save ViewModels
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
    }
}
