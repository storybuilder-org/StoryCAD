using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json;

namespace StoryCAD.DAL;

public class ListLoader
{
    #region Public Methods

    public async Task<Dictionary<string, ObservableCollection<string>>> Init()
    {
        Dictionary<string, ObservableCollection<string>> _lists = new();

        await using Stream internalResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("StoryCAD.Assets.Install.Lists.json");
        using StreamReader reader = new(internalResourceStream);

        // Read the JSON file and deserialize it
        string json = await reader.ReadToEndAsync();
        var jsonLists = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
        
        // Convert to ObservableCollection format
        if (jsonLists != null)
        {
            foreach (var kvp in jsonLists)
            {
                _lists[kvp.Key] = new ObservableCollection<string>(kvp.Value);
            }
        }
        
        return _lists;
    }
    #endregion
}