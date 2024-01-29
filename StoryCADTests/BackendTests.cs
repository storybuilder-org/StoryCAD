using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace StoryCADTests;

[TestClass]
public class BackendTests
{

    /// <summary>
    /// This checks that the Backend Server connection
    /// works correctly. (Requires .ENV)
    /// </summary>
    [TestMethod]
    public void CheckConnection()
    {
        AppState State = Ioc.Default.GetService<AppState>();
        
        //Load keys.
        Doppler doppler = new();
        Doppler keys = new();
        Task.Run(async () =>
        {
            keys = await doppler.FetchSecretsAsync();
            await Ioc.Default.GetRequiredService<BackendService>().SetConnectionString(keys);
        }).Wait();

        //Make sure app logic thinks versions need syncing
        State.Preferences.RecordVersionStatus = false;
        State.Preferences.FirstName = "StoryCAD";
        State.Preferences.LastName = "Tests";
        State.Preferences.Email = "sysadmin@storybuilder.org";
        
        //Call backend service to check connection
        Task.Run(async () =>
        {
            await Ioc.Default.GetRequiredService<BackendService>().PostVersion();
            await Ioc.Default.GetRequiredService<BackendService>().PostPreferences(State.Preferences);
        }).Wait();

        //Check if test passed (RecordVersionStatus should be true now)
        Assert.IsTrue(State.Preferences.RecordVersionStatus);
    }

}
