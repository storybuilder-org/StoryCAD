using System.Collections.ObjectModel;
using StoryCADLib.DAL;

namespace StoryCADLib.Models;

/// <summary>
///     This stores the lists for StoryCAD's Lists.json.
///     Previously lists were stored in GlobalData.
/// </summary>
public class ListData
{
    private readonly ILogService _log;

    /// The ComboBox and ListBox source bindings in viewmodels point to lists in this Dictionary.
    /// Each list has a unique key related to the ComboBox or ListBox use.
    public Dictionary<string, ObservableCollection<string>> ListControlSource = new();

    public ListData(ILogService log, JSONResourceLoader resourceLoader)
    {
        _log = log;
        try
        {
            _log.Log(LogLevel.Info, "Loading Lists.json data");
            Task.Run(async () =>
            {
                var jsonLists = await resourceLoader.LoadResource<Dictionary<string, List<string>>>("Lists.json");
                // Convert to ObservableCollection format
                foreach (var kvp in jsonLists)
                {
                    var list = new ObservableCollection<string>(kvp.Value);
                    // Insert a space at index 0 for proper SelectedItem binding on non-editable ComboBoxes.
                    // This allows the ComboBox to display a blank selection when the bound value is empty.
                    // NOTE: We use " " (space) instead of "" (empty string) because UNO Platform has a bug
                    // where empty string is treated as null, causing x:Bind to fall back to the DataContext
                    // and display the ViewModel's ToString(). See Issue #1267.
                    list.Insert(0, " ");
                    ListControlSource[kvp.Key] = list;
                }
            }).Wait();

            _log.Log(LogLevel.Info, $"{ListControlSource.Keys.Count} ListLoader.Init keys created");
        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error loading Lists.json");
            Application.Current.Exit();
        }
    }
}
