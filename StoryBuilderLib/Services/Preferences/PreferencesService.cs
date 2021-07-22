using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryBuilder.Controllers;
using StoryBuilder.Services.Logging;
using StoryBuilder.DAL;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;

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
                await loader.UpdateFile();
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
