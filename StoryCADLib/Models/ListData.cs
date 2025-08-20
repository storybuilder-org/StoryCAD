using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using StoryCAD.DAL;

namespace StoryCAD.Models
{
    /// <summary>
    /// This stores the lists for StoryCAD's Lists.ini.
    /// Previously lists were stored in GlobalData.
    /// </summary>
    public class ListData
    {
        private readonly ILogService _log;

        /// The ComboBox and ListBox source bindings in viewmodels point to lists in this Dictionary. 
        /// Each list has a unique key related to the ComboBox or ListBox use.
        public Dictionary<string, ObservableCollection<string>> ListControlSource;
        
        public ListData(ILogService log) 
        {
            _log = log;
            try
            {
                _log.Log(LogLevel.Info, "Loading Lists.ini data");
                ListLoader loader = Ioc.Default.GetService<ListLoader>();
                Task.Run(async () => { ListControlSource = await loader.Init(); }).Wait();

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