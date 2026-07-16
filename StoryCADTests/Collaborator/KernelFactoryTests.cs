using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator.Models;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Covers the Collaborator proxy credential resolution after issue #90 step 8 items 5 and 6:
///     the direct-to-OpenAI path (OPENAI_API_KEY) and the COLLAB_PROXY_TOKEN shared-secret
///     fallback both retired, so the store-activation JWT is the sole credential and the only
///     resolution path is Proxy. No credential at all refuses the call client-side.
///     The JWT source is injected, so nothing here reads real environment variables or IoC.
/// </summary>
[TestClass]
public class KernelFactoryTests
{
    private static string NoEnv(string _) => null;

    private static Func<string, string> Env(string name, string value) =>
        key => key == name ? value : null;

    // ── ResolveWorkflowCredential ────────────────────────────────────────────

    [TestMethod]
    public void ResolveWorkflowCredential_JwtHeld_ReturnsJwt()
    {
        var credential = KernelFactory.ResolveWorkflowCredential(() => "activation-jwt");

        Assert.AreEqual("activation-jwt", credential);
    }

    [TestMethod]
    public void ResolveWorkflowCredential_NoJwt_ReturnsNull()
    {
        Assert.IsNull(KernelFactory.ResolveWorkflowCredential(() => null),
            "no JWT must yield no credential; the shared-secret fallback is retired");
    }

    // ── ResolveConfig ────────────────────────────────────────────────────────

    [TestMethod]
    public void ResolveConfig_JwtHeld_ProxyPathWithJwtCredential()
    {
        var config = KernelFactory.ResolveConfig(NoEnv, () => "activation-jwt");

        Assert.AreEqual(KernelFactory.KernelPath.Proxy, config.Path);
        Assert.AreEqual("activation-jwt", config.Credential);
        Assert.AreEqual(KernelFactory.DefaultProxyBaseUrl, config.Endpoint);
    }

    [TestMethod]
    public void ResolveConfig_CustomProxyUrl_UsesConfiguredEndpoint()
    {
        var config = KernelFactory.ResolveConfig(
            Env("COLLAB_PROXY_URL", "https://dev.example.com/v1"), () => "activation-jwt");

        Assert.AreEqual("https://dev.example.com/v1", config.Endpoint);
        Assert.AreEqual("activation-jwt", config.Credential);
    }

    [TestMethod]
    public void ResolveConfig_SharedTokenEnvVarSet_IgnoredNotUsedAsCredential()
    {
        // COLLAB_PROXY_TOKEN retired as a credential (issue #90 D6 as overruled): even if the env
        // var happens to be set (e.g. a developer's shell), it must not be read as a fallback.
        var config = KernelFactory.ResolveConfig(
            Env("COLLAB_PROXY_TOKEN", "shared-secret"), () => "activation-jwt");

        Assert.AreEqual("activation-jwt", config.Credential);
    }

    [TestMethod]
    public void ResolveConfig_NoJwt_ThrowsNamingSubscribeAndAllowlist()
    {
        var ex = Assert.ThrowsExactly<InvalidOperationException>(
            () => KernelFactory.ResolveConfig(NoEnv, () => null));

        StringAssert.Contains(ex.Message, "subscribe");
        StringAssert.Contains(ex.Message, "allowlist");
    }

    [TestMethod]
    public void ResolveConfig_OpenAiKeyEnvVarSet_StillThrows()
    {
        // OPENAI_API_KEY retired as a fallback (issue #90 step 8 item 5): no Direct path remains,
        // so its presence in the environment must not resolve a config.
        Assert.ThrowsExactly<InvalidOperationException>(
            () => KernelFactory.ResolveConfig(Env("OPENAI_API_KEY", "sk-key"), () => null));
    }
}
