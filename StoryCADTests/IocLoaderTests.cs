using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Services.Backend;
using StoryCAD.ViewModels;
using StoryCAD.ViewModels.Tools;

namespace StoryCADTests
{
    [TestClass]
    public class IocLoaderTests
    {
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
