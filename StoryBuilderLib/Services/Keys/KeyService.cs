using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace StoryBuilder.Services.Keys
{
    public class KeyService
    {
        /// <summary>
        /// Process
        /// </summary>
        /// <returns>Elmah</returns>
        public Tuple<string, string> ElmahTokens()
        {
            string apiKey = null;
            string logID = null;

            // Try to obtain elmah.io tokens from developer-set
            // environment variables, if present. 
            // If so, there's no need to query Key Vault.
            apiKey = Environment.GetEnvironmentVariable("API-KEY");
            logID = Environment.GetEnvironmentVariable("Log-ID");
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
                Console.WriteLine(ex.Message);
                return Tuple.Create(string.Empty, string.Empty);
            }
            return Tuple.Create(apiSecret.Value.ToString(), logSecret.Value.ToString());
        }
    }
}
