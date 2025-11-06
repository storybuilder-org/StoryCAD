using StoryCADLib.DAL;
using StoryCADLib.Models.Tools;

namespace StoryCADLib.ViewModels;

/// <summary>
///     This contains controls data
///     previously stored in GlobalData.cs
/// </summary>
public class ControlData
{
    private readonly ControlLoader _controlLoader;
    private readonly ILogService _log;

    //Character conflics
    public SortedDictionary<string, ConflictCategoryModel> ConflictTypes;

    /// <summary>
    ///     Possible relations
    /// </summary>
    public List<string> RelationTypes;

    public ControlData(ILogService log, ControlLoader controlLoader)
    {
        _log = log;
        _controlLoader = controlLoader;
        var subTypeCount = 0;
        var exampleCount = 0;
        try
        {
            _log.Log(LogLevel.Info, "Loading Controls.ini data");
            Task.Run(async () =>
            {
                var Controls = await _controlLoader.Init();
                ConflictTypes = (SortedDictionary<string, ConflictCategoryModel>)Controls[0];
                RelationTypes = (List<string>)Controls[1];
            }).Wait();

            _log.Log(LogLevel.Info, "ConflictType Counts");
            if (ConflictTypes != null)
            {
                _log.Log(LogLevel.Info,
                    $"{ConflictTypes.Keys.Count} ConflictType keys created");
            }

            if (RelationTypes != null)
            {
                _log.Log(LogLevel.Info,
                    $"{RelationTypes.Count} RelationTypes loaded");
            }

            if (ConflictTypes != null)
            {
                foreach (var type in ConflictTypes.Values)
                {
                    subTypeCount += type.SubCategories.Count;
                    exampleCount += type.SubCategories.Sum(subType => type.Examples[subType].Count);
                }

                _log.Log(LogLevel.Info,
                    $"{subTypeCount} Total ConflictSubType keys created");
                _log.Log(LogLevel.Info,
                    $"{exampleCount} Total ConflictSubType keys created");
            }
        }
        catch (Exception ex)
        {
            _log.LogException(LogLevel.Error, ex, "Error loading controls data");
            if (Application.Current != null)
            {
                Application.Current.Exit();
            }
        }
    }
}
