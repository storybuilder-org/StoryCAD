using System.Collections.ObjectModel;
using StoryCAD.DAL;

namespace StoryCAD.Models;

/// <summary>
///     This stores the lists for StoryCAD's Lists.ini.
///     Previously lists were stored in GlobalData.
/// </summary>
public class ListData
{
    private readonly ListLoader _listLoader;
    private readonly ILogService _log;

    /// The ComboBox and ListBox source bindings in viewmodels point to lists in this Dictionary. 
    /// Each list has a unique key related to the ComboBox or ListBox use.
    public Dictionary<string, ObservableCollection<string>> ListControlSource;

    public ListData(ILogService log, ListLoader listLoader)
    {
        _log = log;
        _listLoader = listLoader;
        try
        {
            _log.Log(LogLevel.Info, "Loading Lists.ini data");
            Task.Run(async () => { ListControlSource = await _listLoader.Init(); }).Wait();

            _log.Log(LogLevel.Info, $"{ListControlSource.Keys.Count} ListLoader.Init keys created");
        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error loading Lists.ini");
            Application.Current.Exit();
        }
    }
}
