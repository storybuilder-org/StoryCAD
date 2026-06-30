using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

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
        /// Pure config resolution — no I/O, no side effects, injectable env source for tests.
        /// Resolution order:
        ///   1. COLLAB_PROXY_TOKEN set → Proxy path (default); URL from COLLAB_PROXY_URL or compiled default.
        ///   2. OPENAI_API_KEY set → Direct path (developer fallback only; reached when proxy unreachable).
        ///   3. Neither → InvalidOperationException with a message naming the missing config.
        /// </summary>
        public static KernelConfig ResolveConfig(Func<string, string?>? getEnv = null)
        {
            getEnv ??= Environment.GetEnvironmentVariable;

            var proxyUrl = getEnv("COLLAB_PROXY_URL") ?? DefaultProxyBaseUrl;
            var proxyToken = getEnv("COLLAB_PROXY_TOKEN");

            if (!string.IsNullOrWhiteSpace(proxyToken))
                return new KernelConfig(KernelPath.Proxy, ModelId, proxyUrl, proxyToken);

            var openAiKey = getEnv("OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(openAiKey))
                return new KernelConfig(KernelPath.Direct, ModelId, "https://api.openai.com/v1", openAiKey);

            throw new InvalidOperationException(
                "AI features are unavailable: set COLLAB_PROXY_TOKEN for proxy access " +
                "or OPENAI_API_KEY for direct OpenAI access.");
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
