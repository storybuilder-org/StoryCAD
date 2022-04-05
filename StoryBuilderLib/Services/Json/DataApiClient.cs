using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
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
        //private static async Task<List<Repository>> ProcessRepositories()
        //{
        //    client.DefaultRequestHeaders.Accept.Clear();
        //    client.DefaultRequestHeaders.Accept.Add(
        //        new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            
        //    // Identify who we are (use a token?)
        //    client.DefaultRequestHeaders.Add("User-Agent", "StoryBuilder");

        //    var streamTask = client.GetStreamAsync("https://api.github.com/orgs/dotnet/repos");
        //    var repositories = await JsonSerializer.DeserializeAsync<List<Repository>>(await streamTask);
        //    return repositories;
        //}
    }
}