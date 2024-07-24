using CommunityToolkit.Mvvm.DependencyInjection;
using dotenv.net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Backend;
using StoryCAD.Services.IoC;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;
using System;
using System.IO;
using StoryCAD.Services;
using Microsoft.Extensions.DependencyInjection;

namespace StoryCADTests
{
    [TestClass]
    public class IocLoaderTests
    {
        /// <summary>
        /// Stops initialise from running multiple times
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
        /// <param name="ctx">Required by MSTest, useless to us</param>
        [AssemblyInitialize]
        public static void Initialise(TestContext ctx) 
        {
            if (!IocSetupComplete)
            {
	            ServiceLocator.Initialize();

	            // Build the service provider
	            var serviceProvider = ServiceLocator.Services.BuildServiceProvider();
	            // Configure the default IOC container
	            Ioc.Default.ConfigureServices(serviceProvider);

				IocSetupComplete = true;
                AppState State = Ioc.Default.GetService<AppState>();
                PreferenceService Prefs = Ioc.Default.GetService<PreferenceService>();
                Prefs.Model = new();
                Prefs.Model.FirstName = "StoryCADTestUser";
                Prefs.Model.Email = "sysadmin@storybuilder.org";
                //return;
                try
                {

                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
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
