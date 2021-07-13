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
            story.ListControlSource = await loader.Init(localPath);
            Assert.AreEqual(11, story.ListControlSource["Adventurous"].Count);
            Assert.AreEqual(12, story.ListControlSource["Aggressiveness"].Count);
            Assert.AreEqual(9, story.ListControlSource["Archetype"].Count);
            Assert.AreEqual(13, story.ListControlSource["Assurance"].Count);
            Assert.AreEqual(7, story.ListControlSource["Build"].Count);
            Assert.AreEqual(7, story.ListControlSource["Complexion"].Count);
            Assert.AreEqual(17, story.ListControlSource["Confidence"].Count);
            Assert.AreEqual(14, story.ListControlSource["Conflict"].Count);
            Assert.AreEqual(7, story.ListControlSource["ConflictSource"].Count);
            Assert.AreEqual(7, story.ListControlSource["ConflictType"].Count);
            Assert.AreEqual(11, story.ListControlSource["Conscientiousness"].Count);
            Assert.AreEqual(11, story.ListControlSource["Creativeness"].Count);
            Assert.AreEqual(10, story.ListControlSource["Dominance"].Count);
            Assert.AreEqual(45, story.ListControlSource["Emotion"].Count);
            Assert.AreEqual(9, story.ListControlSource["Enneagram"].Count);
            Assert.AreEqual(10, story.ListControlSource["Enthusiasm"].Count);
            Assert.AreEqual(5, story.ListControlSource["EyeColor"].Count);
            Assert.AreEqual(9, story.ListControlSource["Focus"].Count);
            Assert.AreEqual(4, story.ListControlSource["Font"].Count);
            Assert.AreEqual(18, story.ListControlSource["Genre"].Count);
            Assert.AreEqual(70, story.ListControlSource["Goal"].Count);
            Assert.AreEqual(11, story.ListControlSource["HairColor"].Count);
            Assert.AreEqual(9, story.ListControlSource["Intelligence"].Count);
            Assert.AreEqual(90, story.ListControlSource["Locale"].Count);
            Assert.AreEqual(9, story.ListControlSource["MentalIllness"].Count);
            Assert.AreEqual(27, story.ListControlSource["Method"].Count);
            Assert.AreEqual(95, story.ListControlSource["Motive"].Count);
            Assert.AreEqual(74, story.ListControlSource["Country"].Count);
            Assert.AreEqual(9, story.ListControlSource["LanguageStyle"].Count);
            Assert.AreEqual(23, story.ListControlSource["LiteraryDevice"].Count);
            Assert.AreEqual(104, story.ListControlSource["LiteraryStyle"].Count);
            Assert.AreEqual(8, story.ListControlSource["Opposition"].Count);
            Assert.AreEqual(11, story.ListControlSource["Outcome"].Count);
            Assert.AreEqual(7, story.ListControlSource["ProblemSource"].Count);
            Assert.AreEqual(3, story.ListControlSource["ProblemType"].Count);
            Assert.AreEqual(5, story.ListControlSource["Race"].Count);
            Assert.AreEqual(216, story.ListControlSource["Role"].Count);
            Assert.AreEqual(12, story.ListControlSource["SceneOutcome"].Count);
            Assert.AreEqual(9, story.ListControlSource["ScenePurpose"].Count);
            Assert.AreEqual(15, story.ListControlSource["SceneType"].Count);
            Assert.AreEqual(7, story.ListControlSource["Season"].Count);
            Assert.AreEqual(9, story.ListControlSource["Sensitivity"].Count);
            Assert.AreEqual(16, story.ListControlSource["Shrewdness"].Count);
            Assert.AreEqual(14, story.ListControlSource["Sociability"].Count);
            Assert.AreEqual(8, story.ListControlSource["Stability"].Count);
            Assert.AreEqual(11, story.ListControlSource["Title"].Count);
            Assert.AreEqual(6, story.ListControlSource["StoryRole"].Count);
            Assert.AreEqual(8, story.ListControlSource["StoryType"].Count);
            Assert.AreEqual(212, story.ListControlSource["ProblemSubject"].Count);
            Assert.AreEqual(5, story.ListControlSource["Tense"].Count);
            Assert.AreEqual(33, story.ListControlSource["Theme"].Count);
            Assert.AreEqual(220, story.ListControlSource["Tone"].Count);
            Assert.AreEqual(9, story.ListControlSource["Topic"].Count);
            Assert.AreEqual(20, story.ListControlSource["Value"].Count);
            Assert.AreEqual(163, story.ListControlSource["ValueExchange"].Count);
            Assert.AreEqual(5, story.ListControlSource["Viewpoint"].Count);
            Assert.AreEqual(7, story.ListControlSource["Voice"].Count);
            Assert.AreEqual(8, story.ListControlSource["WoundCategory"].Count);
            Assert.AreEqual(121, story.ListControlSource["Wound"].Count);
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
