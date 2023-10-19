using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Services.Backend;
using StoryCAD.Services.IoC;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;

namespace StoryCADTests
{
    [TestClass]
    public class IocLoaderTests
    {
        /// <summary>
        /// Don't modify this unless you know what you are doing
        /// This MUST be public static and have a Test Context
        /// if you remove this, you will break automated test
        /// </summary>
        /// <param name="ctx"></param>
        [AssemblyInitialize]
        public static void Initalise(TestContext ctx) 
        {
            Ioc.Default.ConfigureServices(ServiceConfigurator.Configure());
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
