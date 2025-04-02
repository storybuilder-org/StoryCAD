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

namespace StoryCADTests;

[TestClass]
public class IocLoaderTests
{
	/// <summary>
	/// Don't modify this unless you know what you are doing
	/// This MUST be public static and have a Test Context
	/// if you remove this, you will break automated test
	/// </summary>
	/// <param name="ctx">Required by MSTest, useless to us</param>
	[AssemblyInitialize]
	public static void Initialise(TestContext ctx) 
	{
        BootStrapper.Initialise(true, null);
        PreferenceService prefs = Ioc.Default.GetRequiredService<PreferenceService>();
        prefs.Model.ProjectDirectory = App.InputDir;
        prefs.Model.BackupDirectory = App.ResultsDir;

        try
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            DotEnvOptions options = new(false, [path]);
            DotEnv.Load(options);
        }
        catch { }
    }

	/// <summary>
	/// Tests loading of IOC
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