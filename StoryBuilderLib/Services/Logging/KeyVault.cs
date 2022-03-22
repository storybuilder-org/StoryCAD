using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace StoryBuilder.Services.Logging;

private class KeyVault
{
    private static Tuple<string, string> GetSecrets()
    {
        KeyVaultSecret apiSecret = null;
        KeyVaultSecret logSecret = null;

        // try to obtain developer tokens
        string apiKey = Environment.GetEnvironmentVariable("API-KEY");
        string logID = Environment.GetEnvironmentVariable("Log-ID");
        if ((apiKey != null) 
        && (logID != null))
            return Tuple.Create(apiKey, logID);

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
