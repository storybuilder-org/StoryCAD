using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace StoryBuilder.DAL
{
    internal class SecretReader
    {
        SecretClient secretClient = null;
        Uri uri = new Uri("https://storybuilder-secrets.vault.azure.net/");
        ClientSecretCredential credential = new ClientSecretCredential(
            tenantId: "65929ef0 - a65f - 479b - a6ba - 6083a7163b1c",
            clientId: "",
            clientSecret: "");
              
    }
}
