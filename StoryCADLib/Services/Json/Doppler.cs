using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using dotenv.net.Utilities;

namespace StoryCAD.Services.Json    
{

    public class Doppler
    {
        /// <summary>
        /// Set to true if doppler connection is successful
        /// </summary>
        public static bool DopplerConnection;

        [JsonPropertyName("APIKEY")]
        public string APIKEY { get; set; }

        [JsonPropertyName("CAFILE")]
        public string CAFILE { get; set; }

        [JsonPropertyName("CONNECTION")]
        public string CONNECTION { get; set; }

        [JsonPropertyName("LOGID")]
        public string LOGID  { get; set; }

        [JsonPropertyName("SSLCA")]
        public string SSLCA { get; set; }

        [JsonPropertyName("GITHUB_TOKEN")]
        public string GITHUB_TOKEN { get; set; }

		private static HttpClient client = new();

        /// <summary>
        /// Obtain tokens for elmah.io and and MySQL connection to the backend server.
        /// Based on https://docs.doppler.com/docs/asp-net-core-csharp
        /// </summary>
        /// <returns>Doppler tokens, or empty strings</returns>
        public async Task<Doppler> FetchSecretsAsync()
        {
            try
            {
                string token = EnvReader.GetStringValue("DOPPLER_TOKEN");
                string basicAuthHeaderValue = Convert.ToBase64String(Encoding.Default.GetBytes(token + ":"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthHeaderValue);
                Task<Stream> streamTask = client.GetStreamAsync("https://api.doppler.com/v3/configs/config/secrets/download?format=json");
                Doppler secrets = await JsonSerializer.DeserializeAsync<Doppler>(await streamTask);
                DopplerConnection = true;
                return secrets;
            }
            catch (Exception ex)
            {
                ILogService log = Ioc.Default.GetService<ILogService>();
                log?.LogException(LogLevel.Warn, ex, ex.Message);
                return this;
            }
        }

    }
}
