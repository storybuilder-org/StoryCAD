using System;
using System.Text.Json.Serialization;
using StoryBuilder.Models;
using StoryBuilder.Models.Tools;
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

        public PreferencesData()
        {
            var preferences = GlobalData.Preferences;
            Email = preferences.Email;
            Name = preferences.Name;
            ErrorCollectionConsent = preferences.ErrorCollectionConsent;
            Newsletter = preferences.Newsletter;
            Version = preferences.Version;
            Date = DateTime.Now.ToShortDateString();
        }

        public PreferencesData(PreferencesModel model) 
        {
            Email = model.Email;
            Name = model.Name;
            ErrorCollectionConsent = model.ErrorCollectionConsent;
            Newsletter = model.Newsletter;
            Version = model.Version;
            Date = DateTime.Now.ToShortDateString();
        }
    }
}
