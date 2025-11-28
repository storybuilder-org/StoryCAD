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
            using StreamReader reader = new(internalResourceStream!);
            var json = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _controlsData = JsonSerializer.Deserialize<ControlsJsonData>(json, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        // Populate UserControl data source collections
        List<object> controls =
        [
            LoadConflictTypes(),
            LoadRelationTypes()
        ];
        Clear();
        return controls;
    }

    private SortedDictionary<string, ConflictCategoryModel> LoadConflictTypes()
    {
        SortedDictionary<string, ConflictCategoryModel> conflictTypes = new();

        if (_controlsData?.ConflictTypes != null)
        {
            foreach (var conflictType in _controlsData.ConflictTypes)
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

    private List<string> LoadRelationTypes()
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
