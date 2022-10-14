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
            PreferencesModel _Model = new();
            PreferencesIo _Loader = new(_Model, path);
            await _Loader.UpdateModel();
                
            GlobalData.Preferences = _Model;
        }
        catch (Exception _Ex)
        {
            Logger.LogException(LogLevel.Error, _Ex, "Error loading Preferences");
            Application.Current.Exit();  // Win32;  // Win32
        }
    }
}