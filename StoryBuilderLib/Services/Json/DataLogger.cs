using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;  
using System.Text.Json;
using StoryBuilder.Services.Logging;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace StoryBuilder.Services.Json
{
    public class DataLogger
    {

        private static readonly HttpClient client = new HttpClient();
        private static async Task PostPreferences(PreferencesData preferences)
        {
            var log = Ioc.Default.GetService<LogService>();
            
            try
            {
                client.DefaultRequestHeaders.Accept.Clear();
                // Identify who we are (maybe use a secrets token?)
                //    client.DefaultRequestHeaders.Add("User-Agent", "StoryBuilder");

                //TODO: Add try/catch logic

                Uri server = new Uri("localhost:3000");
                string jsonString = JsonSerializer.Serialize(preferences);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(server, content);
                response.EnsureSuccessStatusCode();
                //TODO: Log success
            }
            catch (Exception ex)
            {
                log.LogException(LogLevel.Warn, ex, ex.Message);
            }           
            return;
        }
    }
}