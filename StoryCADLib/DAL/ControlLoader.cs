using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using StoryCADLib.Models.Tools;

namespace StoryCADLib.DAL;

public class ControlLoader
{
    private ControlsJsonData _controlsData;

    public async Task<List<object>> Init()
    {
        try
        {
            await using var internalResourceStream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("StoryCADLib.Assets.Install.Controls.json");
            using StreamReader reader = new(internalResourceStream);
            var json = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _controlsData = JsonSerializer.Deserialize<ControlsJsonData>(json, options);
        }
        catch (Exception _ex)
        {
            Console.WriteLine(_ex.Message);
        }

        // Populate UserControl data source collections
        List<object> Controls = new()
        {
            LoadConflictTypes(),
            LoadRelationTypes()
        };
        Clear();
        return Controls;
    }

    public SortedDictionary<string, ConflictCategoryModel> LoadConflictTypes()
    {
        SortedDictionary<string, ConflictCategoryModel> _conflictTypes = new();

        if (_controlsData?.ConflictTypes != null)
        {
            foreach (var conflictType in _controlsData.ConflictTypes)
            {
                var model = new ConflictCategoryModel(conflictType.Category);

                foreach (var subCategory in conflictType.SubCategories)
                {
                    model.SubCategories.Add(subCategory.Name);
                    model.Examples.Add(subCategory.Name, new List<string>(subCategory.Examples));
                }

                _conflictTypes.Add(conflictType.Category, model);
            }
        }

        return _conflictTypes;
    }

    public List<string> LoadRelationTypes()
    {
        return _controlsData?.RelationTypes != null
            ? new List<string>(_controlsData.RelationTypes)
            : new List<string>();
    }

    public void Clear()
    {
        _controlsData = null;
    }

    // JSON data classes
    private class ControlsJsonData
    {
        [JsonPropertyName("conflictTypes")] public List<ConflictTypeData> ConflictTypes { get; set; }

        [JsonPropertyName("relationTypes")] public List<string> RelationTypes { get; set; }
    }

    private class ConflictTypeData
    {
        public string Category { get; set; }
        public List<SubCategoryData> SubCategories { get; set; }
    }

    private class SubCategoryData
    {
        public string Name { get; set; }
        public List<string> Examples { get; set; }
    }
}
