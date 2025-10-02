using System;
using dotenv.net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using StoryCAD.Services.Json;
using System.Threading.Tasks;

namespace StoryCADTests.Services.Json;

/// <summary>
/// All the tests here require a .ENV File.If you are unaffiliated 
/// with Storybuilder-org i.e you are a contributor. You will
/// not have this file in your copy of StoryCAD and these tests
/// as a result will always fail.
/// </summary>
[TestClass]
public class EnvTests
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
