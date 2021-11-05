using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.Controllers;
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

        public async Task LoadPreferences(string path, StoryController story)
        {
            try
            {
                Logger.Log(LogLevel.Info, "Loading Preferences");
                PreferencesModel model = new PreferencesModel();
                PreferencesIO loader = new PreferencesIO(model, path);
                await loader.UpdateModel();
                // When ran from the app, the app's local folder is the 
                // installation directory
                if (model.InstallationDirectory.CompareTo(path) != 0)
                {
                    model.InstallationDirectory = path;
                }
                
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
