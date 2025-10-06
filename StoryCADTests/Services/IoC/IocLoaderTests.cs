using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Services.Backend;
using StoryCADLib.ViewModels;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADTests.Services.IoC;

[TestClass]
public class IocLoaderTests
{
    /// <summary>
    ///     Tests loading of IOC
    /// </summary>
    [TestMethod]
    public void TestIOCLoad()
    {
        Assert.IsNotNull(Ioc.Default.GetService<CharacterViewModel>());
        Assert.IsNotNull(Ioc.Default.GetService<ProblemViewModel>());
        Assert.IsNotNull(Ioc.Default.GetService<WebViewModel>());
        Assert.IsNotNull(Ioc.Default.GetService<FolderViewModel>());
        Assert.IsNotNull(Ioc.Default.GetService<BackendService>());
        Assert.IsNotNull(Ioc.Default.GetService<TopicsViewModel>());
        Assert.IsNotNull(Ioc.Default.GetService<TraitsViewModel>());
    }
}
