using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryCAD.DAL;
using StoryCAD.Services.Logging;

namespace StoryCAD.Models
{
    /// <summary>
    /// This stores the lists for StoryCAD's Lists.ini.
    /// Previously lists were stored in GlobalData.
    /// </summary>
    public class ListData
    {
        LogService _log = Ioc.Default.GetService<LogService>();

        /// The ComboBox and ListBox source bindings in viewmodels point to lists in this Dictionary. 
        /// Each list has a unique key related to the ComboBox or ListBox use.
        public Dictionary<string, ObservableCollection<string>> ListControlSource;
        public ListData() 
        {
            try
            {
                _log.Log(LogLevel.Info, "Loading Lists.ini data");
                ListLoader loader = Ioc.Default.GetService<ListLoader>();
                Task.Run(async () =>
                {
                    ListControlSource = await loader.Init(GlobalData.RootDirectory);
                }).Wait();

                _log.Log(LogLevel.Info, $"{ListControlSource.Keys.Count} ListLoader.Init keys created");
            }
            catch (Exception ex)
            {
                _log.LogException(LogLevel.Error, ex, "Error loading Lists.ini");
                Application.Current.Exit();
            }
        }

    }
}