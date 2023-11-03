using CommunityToolkit.Mvvm.DependencyInjection;
using dotenv.net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Backend;
using StoryCAD.Services.IoC;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;
using System.IO;
using Windows.ApplicationModel;

namespace StoryCADTests
{
    [TestClass]
    public class IocLoaderTests
    {
        /// <summary>
        /// Stops initalise from running multiple times
        /// as it seems to be called more than once some
        /// by the test manager, thus causing all tests
        /// to fail.
        /// </summary>
        private static bool IocSetupComplete = false;

        /// <summary>
        /// Don't modify this unless you know what you are doing
        /// This MUST be public static and have a Test Context
        /// if you remove this, you will break automated test
        /// </summary>
        /// <param name="ctx"></param>
        [AssemblyInitialize]
        public static void Initalise(TestContext ctx) 
        {
            if (!IocSetupComplete)
            {
                Ioc.Default.ConfigureServices(ServiceConfigurator.Configure());
                IocSetupComplete = true;
                AppState State = Ioc.Default.GetService<AppState>();
                State.Preferences = new();
                State.Preferences.Name = "StoryCADTestUser";
                State.Preferences.Email = "sysadmin@storybuilder.org";
                //return;
                try
                {

                    string path = Path.Combine(Package.Current.InstalledLocation.Path, ".env");
                    DotEnvOptions options = new(false, new[] { path });
                    DotEnv.Load(options);
                }
                catch { }
            }
        }

        [TestMethod]
        public void TestIOCLoad()
        {
            //IOC is initalised in App.xaml.cs
            Assert.IsNotNull(Ioc.Default.GetService<CharacterViewModel>());
            Assert.IsNotNull(Ioc.Default.GetService<ProblemViewModel>());
            Assert.IsNotNull(Ioc.Default.GetService<WebViewModel>());
            Assert.IsNotNull(Ioc.Default.GetService<FolderViewModel>());
            Assert.IsNotNull(Ioc.Default.GetService<BackendService>());
            Assert.IsNotNull(Ioc.Default.GetService<TopicsViewModel>());
            Assert.IsNotNull(Ioc.Default.GetService<TraitsViewModel>());
        }
    }
}
