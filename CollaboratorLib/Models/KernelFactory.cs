using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using StoryCADLib.Services.Store;

namespace StoryCollaborator.Models
{
    /// <summary>
    /// Single construction point for Semantic Kernel. Resolves the active path
    /// (direct OpenAI or proxy) and logs it on every build, per D6.
    /// </summary>
    public static class KernelFactory
    {
        // The proxy base URL SK receives. SK appends /chat/completions.
        internal const string DefaultProxyBaseUrl =
            "https://storycad-collaborator-proxy-production.storybuilder-foundation.workers.dev/v1";

        private const string ModelId = "gpt-5.4-nano";

        public enum KernelPath { Direct, Proxy }

        public record KernelConfig(KernelPath Path, string ModelId, string Endpoint, string Credential);

        /// <summary>
        /// Pure config resolution — no I/O, no side effects, injectable env/JWT sources for tests.
        /// Resolution order:
        ///   1. Activation JWT held (subscriber) → Proxy path; the JWT is the credential
        ///      (issue #90 retires the shared secret on /workflow in favor of this token).
        ///   2. COLLAB_PROXY_TOKEN set → Proxy path (developer shared secret).
        ///   3. OPENAI_API_KEY set → Direct path (developer fallback only; reached when proxy unreachable).
        ///   4. None → InvalidOperationException with a message naming the missing config.
        /// </summary>
        public static KernelConfig ResolveConfig(Func<string, string?>? getEnv = null,
            Func<string?>? getActivationJwt = null)
        {
            getEnv ??= Environment.GetEnvironmentVariable;

            var proxyUrl = getEnv("COLLAB_PROXY_URL") ?? DefaultProxyBaseUrl;
            var credential = ResolveWorkflowCredential(getEnv, getActivationJwt);

            if (!string.IsNullOrWhiteSpace(credential))
                return new KernelConfig(KernelPath.Proxy, ModelId, proxyUrl, credential);

            var openAiKey = getEnv("OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(openAiKey))
                return new KernelConfig(KernelPath.Direct, ModelId, "https://api.openai.com/v1", openAiKey);

            throw new InvalidOperationException(
                "AI features are unavailable: subscribe to Collaborator, or set COLLAB_PROXY_TOKEN " +
                "for proxy access or OPENAI_API_KEY for direct OpenAI access.");
        }

        /// <summary>
        /// The Bearer credential for Collaborator proxy calls: the Worker-issued activation JWT
        /// when the user is activated, else the COLLAB_PROXY_TOKEN shared secret, else null
        /// (callers must refuse to call the proxy rather than send an unauthenticated request).
        /// The activation contract requires the JWT on every Collaborator call
        /// (StoryCADWiki: wiki/repos/Collaborator/sources/iap-billing-docs.md).
        /// </summary>
        internal static string? ResolveWorkflowCredential(Func<string, string?>? getEnv = null,
            Func<string?>? getActivationJwt = null)
        {
            getEnv ??= Environment.GetEnvironmentVariable;
            getActivationJwt ??= GetActivationJwt;

            var jwt = getActivationJwt();
            return !string.IsNullOrWhiteSpace(jwt) ? jwt : getEnv("COLLAB_PROXY_TOKEN");
        }

        /// <summary>
        /// The activation JWT held by StoryCAD's store-activation service, or null when the user
        /// is not activated or IoC is not configured (PromptTestRunner, CollaboratorTests).
        /// Read per call, never cached here: the service refreshes the JWT as it expires.
        /// </summary>
        internal static string? GetActivationJwt()
        {
            try
            {
                return Ioc.Default.GetService<IStoreActivationService>()?.CurrentJwt;
            }
            catch (InvalidOperationException)
            {
                return null; // Ioc.Default not configured in this host
            }
        }

        /// <summary>
        /// Builds and returns a configured Kernel. Always logs the active path and endpoint
        /// host to both the supplied loggerFactory and Debug output (D6 always-on requirement).
        /// </summary>
        public static Kernel Build(ILoggerFactory? loggerFactory = null)
        {
            var config = ResolveConfig();

            // D6: log the active path on every build, always, not behind a verbose flag.
            var host = new Uri(config.Endpoint).Host;
            var message = $"[KernelFactory] path={config.Path} endpoint={host}";
            loggerFactory?.CreateLogger("KernelFactory").LogInformation(message);
            System.Diagnostics.Debug.WriteLine(message);

            var builder = Kernel.CreateBuilder();
            if (loggerFactory is not null)
                builder.Services.AddSingleton(loggerFactory);

            if (config.Path == KernelPath.Direct)
                builder.AddOpenAIChatCompletion(config.ModelId, config.Credential);
            else
                builder.AddOpenAIChatCompletion(config.ModelId, new Uri(config.Endpoint), config.Credential);

            return builder.Build();
        }
    }
}
