using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCAD.Models;
using StoryCAD.Services.Backend;
using StoryCAD.Services.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using StoryCAD.Services;

namespace StoryCADTests.Services.Backend;

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
        PreferenceService Prefs = Ioc.Default.GetService<PreferenceService>();
        
        //Load keys.
        Doppler doppler = new();
        Doppler keys = new();
        Task.Run(async () =>
        {
            keys = await doppler.FetchSecretsAsync();
            await Ioc.Default.GetRequiredService<BackendService>().SetConnectionString(keys);
        }).Wait();

        //Make sure app logic thinks versions need syncing
        Prefs.Model.RecordVersionStatus = false;
        Prefs.Model.FirstName = "StoryCAD";
        Prefs.Model.LastName = "Tests";
        Prefs.Model.Email = "sysadmin@storybuilder.org";
        
        //Call backend service to check connection
        Task.Run(async () =>
        {
            await Ioc.Default.GetRequiredService<BackendService>().PostVersion();
            await Ioc.Default.GetRequiredService<BackendService>().PostPreferences(Prefs.Model);
        }).Wait();

        //Check if test passed (RecordVersionStatus should be true now)
        Assert.IsTrue(Prefs.Model.RecordVersionStatus);
    }

}
