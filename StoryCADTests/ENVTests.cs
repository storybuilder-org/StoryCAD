using System;
using dotenv.net.Utilities;
using dotenv.net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Syncfusion.Licensing;
using System.IO;
using Windows.ApplicationModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Json;
using System.Threading.Tasks;

namespace StoryCADTests;

/// <summary>
/// All the tests here require a .ENV File.If you are unaffilliated 
/// with Storybuilder-org i.e you are a contributor. You will
/// not have this file in your copy of StoryCAD and these tests
/// as a result will always fail.
/// </summary>
[TestClass]
public class ENVTests
{
    /// <summary>
    /// Attempts to load the .ENV File to check its valid.
    /// </summary>
    [TestMethod]
    public void CheckDotEnvFile()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
        DotEnvOptions options = new(false, new[] { path });
        DotEnv.Load(options);
    }

    /// <summary>
    /// Ensures the SyncFusion license is there,
    /// This does not check the license is valid as SF
    /// offers no way to check that, besides the annoying popup it gives in the app.
    /// </summary>
    [TestMethod]
    public void CheckSFLicense() => Assert.IsNotNull(EnvReader.GetStringValue("SYNCFUSION_TOKEN"));


    [TestMethod]
    public void CheckDoppler()
    {
        Doppler doppler = new();
        Doppler keys = new();
        Task.Run(async () =>
        {
            keys = await doppler.FetchSecretsAsync();
        }).Wait();

        Assert.IsNotNull(keys.CONNECTION,"Test Failed, The Doppler Value for connection is null");
    }
}
