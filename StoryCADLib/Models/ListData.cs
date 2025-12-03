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
                    ListControlSource[kvp.Key] = new ObservableCollection<string>(kvp.Value);
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
