using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using dotenv.net.Utilities;


namespace StoryBuilder.Services.Json    
{

    public class Doppler
    {
        [JsonPropertyName("APIKEY")]
        public string APIKEY { get; set; }

        [JsonPropertyName("LOGID")]
        public string LOGID { get; set; }

        private static HttpClient client = new HttpClient();

        /// <summary>
        /// Obtain tokens for elmah.io logging, if they exist.
        /// Based on https://docs.doppler.com/docs/asp-net-core-csharp
        /// </summary>
        /// <returns>elmah.io tokens, or empty strings</returns>
        public async Task<Doppler> FetchSecretsAsync()
        {
            try
            {
                var token = EnvReader.GetStringValue("DOPPLER_TOKEN");
                var basicAuthHeaderValue = Convert.ToBase64String(Encoding.Default.GetBytes(token + ":"));

                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuthHeaderValue);
                var streamTask = client.GetStreamAsync("https://api.doppler.com/v3/configs/config/secrets/download?format=json");
                var secrets = await JsonSerializer.DeserializeAsync<Doppler>(await streamTask);
                GlobalData.DopplerConnection = true;
                return secrets;
            }
            catch (Exception ex)
            {
                var log = Ioc.Default.GetService<LogService>();
                log.LogException(LogLevel.Warn, ex, ex.Message);
                return this;
            }
        }

    }
}
