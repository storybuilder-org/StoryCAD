using StoryCADLib.DAL;
using StoryCADLib.Models.Resources;
using StoryCADLib.Models.Tools;

namespace StoryCADLib.ViewModels;

/// <summary>
///     This contains controls data loaded from Controls.json.
///     Previously stored in GlobalData.cs
/// </summary>
public class ControlData
{
    private readonly ILogService _log;

    //Character conflicts
    public SortedDictionary<string, ConflictCategoryModel> ConflictTypes;

    /// <summary>
    ///     Possible relations
    /// </summary>
    public List<string> RelationTypes;

    public ControlData(ILogService log, JSONResourceLoader resourceLoader)
    {
        _log = log;
        var subTypeCount = 0;
        var exampleCount = 0;
        try
        {
            _log.Log(LogLevel.Info, "Loading Controls.json data");
            Task.Run(async () =>
            {
                var controlsData = await resourceLoader.LoadResource<ControlsJson>("Controls.json");
                ConflictTypes = LoadConflictTypes(controlsData);
                RelationTypes = LoadRelationTypes(controlsData);
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
                    $"{exampleCount} Total ConflictSubType examples created");
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

    private static SortedDictionary<string, ConflictCategoryModel> LoadConflictTypes(ControlsJson controlsData)
    {
        SortedDictionary<string, ConflictCategoryModel> conflictTypes = new();

        if (controlsData?.ConflictTypes != null)
        {
            foreach (var conflictType in controlsData.ConflictTypes)
            {
                var model = new ConflictCategoryModel(conflictType.Category);

                foreach (var subCategory in conflictType.SubCategories)
                {
                    model.SubCategories.Add(subCategory.Name);
                    model.Examples.Add(subCategory.Name, [..subCategory.Examples]);
                }

                conflictTypes.Add(conflictType.Category, model);
            }
        }

        return conflictTypes;
    }

    private static List<string> LoadRelationTypes(ControlsJson controlsData)
    {
        return controlsData?.RelationTypes != null
            ? new List<string>(controlsData.RelationTypes)
            : new List<string>();
    }
}
