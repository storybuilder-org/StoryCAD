using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
using StoryBuilder.Services.Logging;
using System;
using System.Threading.Tasks;

namespace StoryBuilder.Services.Preferences
{
    public class PreferencesService
    {
        public readonly LogService Logger = Ioc.Default.GetService<LogService>();

        public async Task LoadPreferences(string path)
        {
            try
            {
                Logger.Log(LogLevel.Info, "Loading Preferences");
                PreferencesModel model = new();
                PreferencesIO loader = new(model, path);
                await loader.UpdateModel();
                
                GlobalData.Preferences = model;
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error loading Preferences");
                Application.Current.Exit();  // Win32;  // Win32
            }
        }
    }
}
