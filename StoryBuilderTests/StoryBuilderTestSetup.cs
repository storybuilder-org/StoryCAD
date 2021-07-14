using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NavigationService = StoryBuilder.Services.Navigation.NavigationService;
using StoryBuilder.Controllers;
using StoryBuilder.DAL;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Installation;
using StoryBuilder.Services.Logging;
using StoryBuilder.Services.Preferences;
using StoryBuilder.Services.Help;
using StoryBuilder.Services.Search;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;
using Windows.Storage;
using Windows.UI.Text.Core;

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
            Assert.IsNotNull(story.Preferences);
            Assert.AreEqual(localPath, story.Preferences.InstallationDirectory);

            ListLoader loader = Ioc.Default.GetService<ListLoader>();
            StaticData.ListControlSource = await loader.Init(localPath);
            Assert.AreEqual(11, StaticData.ListControlSource["Adventurous"].Count);
            Assert.AreEqual(12, StaticData.ListControlSource["Aggressiveness"].Count);
            Assert.AreEqual(9, StaticData.ListControlSource["Archetype"].Count);
            Assert.AreEqual(13, StaticData.ListControlSource["Assurance"].Count);
            Assert.AreEqual(7, StaticData.ListControlSource["Build"].Count);
            Assert.AreEqual(7, StaticData.ListControlSource["Complexion"].Count);
            Assert.AreEqual(17, StaticData.ListControlSource["Confidence"].Count);
            Assert.AreEqual(14, StaticData.ListControlSource["Conflict"].Count);
            Assert.AreEqual(7, StaticData.ListControlSource["ConflictSource"].Count);
            Assert.AreEqual(7, StaticData.ListControlSource["ConflictType"].Count);
            Assert.AreEqual(11, StaticData.ListControlSource["Conscientiousness"].Count);
            Assert.AreEqual(11, StaticData.ListControlSource["Creativeness"].Count);
            Assert.AreEqual(10, StaticData.ListControlSource["Dominance"].Count);
            Assert.AreEqual(45, StaticData.ListControlSource["Emotion"].Count);
            Assert.AreEqual(9, StaticData.ListControlSource["Enneagram"].Count);
            Assert.AreEqual(10, StaticData.ListControlSource["Enthusiasm"].Count);
            Assert.AreEqual(5, StaticData.ListControlSource["EyeColor"].Count);
            Assert.AreEqual(9, StaticData.ListControlSource["Focus"].Count);
            Assert.AreEqual(4, StaticData.ListControlSource["Font"].Count);
            Assert.AreEqual(18, StaticData.ListControlSource["Genre"].Count);
            Assert.AreEqual(70, StaticData.ListControlSource["Goal"].Count);
            Assert.AreEqual(11, StaticData.ListControlSource["HairColor"].Count);
            Assert.AreEqual(9, StaticData.ListControlSource["Intelligence"].Count);
            Assert.AreEqual(90, StaticData.ListControlSource["Locale"].Count);
            Assert.AreEqual(9, StaticData.ListControlSource["MentalIllness"].Count);
            Assert.AreEqual(27, StaticData.ListControlSource["Method"].Count);
            Assert.AreEqual(95, StaticData.ListControlSource["Motive"].Count);
            Assert.AreEqual(74, StaticData.ListControlSource["Country"].Count);
            Assert.AreEqual(9, StaticData.ListControlSource["LanguageStyle"].Count);
            Assert.AreEqual(23, StaticData.ListControlSource["LiteraryDevice"].Count);
            Assert.AreEqual(104, StaticData.ListControlSource["LiteraryStyle"].Count);
            Assert.AreEqual(8, StaticData.ListControlSource["Opposition"].Count);
            Assert.AreEqual(11, StaticData.ListControlSource["Outcome"].Count);
            Assert.AreEqual(7, StaticData.ListControlSource["ProblemSource"].Count);
            Assert.AreEqual(3, StaticData.ListControlSource["ProblemType"].Count);
            Assert.AreEqual(5, StaticData.ListControlSource["Race"].Count);
            Assert.AreEqual(216, StaticData.ListControlSource["Role"].Count);
            Assert.AreEqual(12, StaticData.ListControlSource["SceneOutcome"].Count);
            Assert.AreEqual(9, StaticData.ListControlSource["ScenePurpose"].Count);
            Assert.AreEqual(15, StaticData.ListControlSource["SceneType"].Count);
            Assert.AreEqual(7, StaticData.ListControlSource["Season"].Count);
            Assert.AreEqual(9, StaticData.ListControlSource["Sensitivity"].Count);
            Assert.AreEqual(16, StaticData.ListControlSource["Shrewdness"].Count);
            Assert.AreEqual(14, StaticData.ListControlSource["Sociability"].Count);
            Assert.AreEqual(8, StaticData.ListControlSource["Stability"].Count);
            Assert.AreEqual(11, StaticData.ListControlSource["Title"].Count);
            Assert.AreEqual(6, StaticData.ListControlSource["StoryRole"].Count);
            Assert.AreEqual(8, StaticData.ListControlSource["StoryType"].Count);
            Assert.AreEqual(212, StaticData.ListControlSource["ProblemSubject"].Count);
            Assert.AreEqual(5, StaticData.ListControlSource["Tense"].Count);
            Assert.AreEqual(33, StaticData.ListControlSource["Theme"].Count);
            Assert.AreEqual(220, StaticData.ListControlSource["Tone"].Count);
            Assert.AreEqual(9, StaticData.ListControlSource["Topic"].Count);
            Assert.AreEqual(20, StaticData.ListControlSource["Value"].Count);
            Assert.AreEqual(163, StaticData.ListControlSource["ValueExchange"].Count);
            Assert.AreEqual(5, StaticData.ListControlSource["Viewpoint"].Count);
            Assert.AreEqual(7, StaticData.ListControlSource["Voice"].Count);
            Assert.AreEqual(8, StaticData.ListControlSource["WoundCategory"].Count);
            Assert.AreEqual(121, StaticData.ListControlSource["Wound"].Count);
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
