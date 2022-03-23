using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using StoryBuilder.Models;

namespace StoryBuilder.Services.Keys
{
    public class KeyService
    {
        /// <summary>
        /// Obtain tokens for elmah.io logging, if they exist.
        /// </summary>
        /// <returns>elmah.io tokens, or empty strings</returns>
        public Tuple<string, string> ElmahTokens()
        {

            // Try to obtain elmah.io tokens from developer
            // environment variables, if present. 
            // If so, there's no need to query Key Vault.
            string apiKey = Environment.GetEnvironmentVariable("API-KEY");
            string logID = Environment.GetEnvironmentVariable("Log-ID");
            if ((apiKey != null)
            && (logID != null))
                return Tuple.Create(apiKey, logID);

            // Try to obtain elmah.io tokens from Azure Key Vault. 
            // If found, return them; otherwise, return empty strings.
            KeyVaultSecret apiSecret;
            KeyVaultSecret logSecret;
            try
            {
                string keyVaultUri = "https://storybuilder-secrets.vault.azure.net/";
                EnvironmentCredential credential = new EnvironmentCredential();
                SecretClient secretClient = new SecretClient(new Uri(keyVaultUri), credential);
                apiSecret = secretClient.GetSecret("Elmah-API-key");
                logSecret = secretClient.GetSecret("Elmah-Log-ID");
                if (apiSecret != null)
                    apiKey = apiSecret.Value.ToString();
                if (logSecret != null)
                    logID = logSecret.Value.ToString();
            }
            catch (Exception ex)
            {
                // Log exception
                Debug.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
            return Tuple.Create(apiSecret.Value.ToString(), logSecret.Value.ToString());
        }

        public string SyncfusionToken() 
        {
            string token = string.Empty;
            string path = Path.Combine(GlobalData.RootDirectory, "license.txt");
            return File.ReadAllText(path);
        }

        public KeyService() 
        { 
        }
    }
}
