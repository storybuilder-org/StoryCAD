using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StoryCAD.Models
{
    /// <summary>
    /// This stores the lists for StoryCAD's Lists.ini.
    /// Previously lists were stored in GlobalData.
    /// </summary>
    public class ListData
    {
        /// The ComboBox and ListBox source bindings in viewmodels point to lists in this Dictionary. 
        /// Each list has a unique key related to the ComboBox or ListBox use.
        public Dictionary<string, ObservableCollection<string>> ListControlSource;
    }
}