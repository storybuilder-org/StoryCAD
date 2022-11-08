using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using dotenv.net.Utilities;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;

namespace StoryBuilder.Services.Json;

public class Doppler
{
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
            var token = EnvReader.GetStringValue("DOPPLER_TOKEN");
            var basicAuthHeaderValue = Convert.ToBase64String(Encoding.Default.GetBytes(token + ":"));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuthHeaderValue);
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