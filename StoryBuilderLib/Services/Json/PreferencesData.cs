using System.Text.Json.Serialization;

namespace StoryBuilder.Services.Json
{
    public class PreferencesData
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("elmahconsent")]
        public bool ErrorCollectionConsent { get; set; }
        
        [JsonPropertyName("newsletterconsent")]
        public bool Newsletter { get; set; }
        
        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        //string jsonString = JsonSerializer.Serialize(PreferencesData instance);

    }
    
    
}
