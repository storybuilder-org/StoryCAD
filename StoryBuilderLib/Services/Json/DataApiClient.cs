using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;  
using System.Text.Json;

namespace StoryBuilder.Services.Json
{
    public class DataApiClient
    {

        private static readonly HttpClient client = new HttpClient();

        //public async Task<bool> PostData()
        //{
        //    try
        //    {
        //        client.DefaultRequestHeaders.Authorization = new HttpCredentialsHeaderValue("Bearer", access_token);
        //        client.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue("application/json"));

        //        var httpResponseMessage = await client.PostAsync(new Uri(href), new HttpStringContent(response));
        //        string resp = await httpResponseMessage.Content.ReadAsStringAsync();
        //        Debug.WriteLine(resp);
        //        ApplicationData.Current.LocalSettings.Values["POSTCallMade"] = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        var log = Ioc.Default.GetService<LogService>();
        //        log.LogException(LogLevel.Warn, ex, ex.Message);
        //        return false;
        //    }
        //    return true;
        //}

        public DataApiClient()
        {
        }

        private static async Task<bool> PostPreferences(PreferencesData preferences)
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
            return true;
        }
    }
}