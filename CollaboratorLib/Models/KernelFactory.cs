using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using StoryCADLib.Services.Store;

namespace StoryCollaborator.Models
{
    /// <summary>
    /// Single construction point for Semantic Kernel. Resolves the proxy path from the
    /// activation JWT alone (issue #90 step 8 items 5 and 6 retire the direct-OpenAI path and the
    /// COLLAB_PROXY_TOKEN shared-secret fallback) and logs it on every build, per D6.
    /// </summary>
    public static class KernelFactory
    {
        // The proxy base URL SK receives. SK appends /chat/completions.
        internal const string DefaultProxyBaseUrl =
            "https://storycad-collaborator-proxy-production.storybuilder-foundation.workers.dev/v1";

        private const string ModelId = "gpt-5.4-nano";

        public enum KernelPath { Proxy }

        public record KernelConfig(KernelPath Path, string ModelId, string Endpoint, string Credential);

        /// <summary>
        /// Pure config resolution — no I/O, no side effects, injectable env/JWT sources for tests.
        /// Resolution: activation JWT held (subscriber, or dev/tester on the allowlist) → Proxy
        /// path; the JWT is the sole credential (issue #90 retires the shared secret on /workflow
        /// and the direct-OpenAI path in favor of this token). No JWT → InvalidOperationException.
        /// </summary>
        public static KernelConfig ResolveConfig(Func<string, string?>? getEnv = null,
            Func<string?>? getActivationJwt = null)
        {
            getEnv ??= Environment.GetEnvironmentVariable;

            var proxyUrl = getEnv("COLLAB_PROXY_URL") ?? DefaultProxyBaseUrl;
            var credential = ResolveWorkflowCredential(getActivationJwt);

            if (!string.IsNullOrWhiteSpace(credential))
                return new KernelConfig(KernelPath.Proxy, ModelId, proxyUrl, credential);

            throw new InvalidOperationException(
                "AI features are unavailable: subscribe to Collaborator, or (for dev builds) enroll " +
                "on the allowlist and set COLLAB_DEV_ENABLED=1.");
        }

        /// <summary>
        /// The Bearer credential for Collaborator proxy calls: the Worker-issued activation JWT
        /// when the user is activated, else null (callers must refuse to call the proxy rather
        /// than send an unauthenticated request). The activation contract requires the JWT on
        /// every Collaborator call (StoryCADWiki: wiki/repos/Collaborator/sources/iap-billing-docs.md).
        /// </summary>
        internal static string? ResolveWorkflowCredential(Func<string?>? getActivationJwt = null)
        {
            getActivationJwt ??= GetActivationJwt;
            return getActivationJwt();
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

            builder.AddOpenAIChatCompletion(config.ModelId, new Uri(config.Endpoint), config.Credential);

            return builder.Build();
        }
    }
}
