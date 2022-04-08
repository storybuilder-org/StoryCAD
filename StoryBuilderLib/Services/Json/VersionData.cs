using System;
using System.Text.Json.Serialization;
using StoryBuilder.Models;

namespace StoryBuilder.Services.Json
{
    public class VersionData
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("currentversion")]
        public string CurrentVersion { get; set; }

        [JsonPropertyName("previousversion")]
        public string PreviousVersion { get; set; }

        [JsonPropertyName("date")]
        public string Date { get; set; }

        public VersionData()
        {
            var preferences = GlobalData.Preferences;
            Email = preferences.Email;
            Name = preferences.Name;
            PreviousVersion = preferences.Version;
            CurrentVersion = GlobalData.Version; ;
            Date = DateTime.Now.ToShortDateString();
        }

    }
}
