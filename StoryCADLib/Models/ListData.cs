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
        LogService _log = Ioc.Default.GetService<LogService>();
        private readonly Lazy<Task> _initializationTask;

        /// The ComboBox and ListBox source bindings in viewmodels point to lists in this Dictionary. 
        /// Each list has a unique key related to the ComboBox or ListBox use.
        public Dictionary<string, ObservableCollection<string>> ListControlSource { get; private set; }
        
        public ListData() 
        {
            // Initialize with empty dictionary to prevent null reference exceptions
            ListControlSource = new Dictionary<string, ObservableCollection<string>>();
            
            // Use lazy initialization to avoid blocking constructor
            _initializationTask = new Lazy<Task>(InitializeAsync);
        }

        /// <summary>
        /// Ensures data is loaded before accessing properties. Call this method before using any list data.
        /// </summary>
        public async Task EnsureInitializedAsync()
        {
            await _initializationTask.Value.ConfigureAwait(false);
        }

        private async Task InitializeAsync()
        {
            try
            {
                _log.Log(LogLevel.Info, "Loading Lists.ini data");
                ListLoader loader = Ioc.Default.GetService<ListLoader>();
                ListControlSource = await loader.Init().ConfigureAwait(false);

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