using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;

namespace StoryBuilder.Services.Preferences;

public class PreferencesService
{
    public readonly LogService Logger = Ioc.Default.GetService<LogService>();

    public async Task LoadPreferences(string path)
    {
        try
        {
            Logger.Log(LogLevel.Info, "Loading Preferences");
            PreferencesModel model = new();
            PreferencesIo loader = new(model, path);
            await loader.LoadModel();
                
            GlobalData.Preferences = model;
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error loading Preferences");
            Application.Current.Exit();  // Win32; 
        }
    }
}