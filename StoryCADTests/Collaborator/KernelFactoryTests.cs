using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCollaborator.Models;

#nullable disable

namespace StoryCADTests.Collaborator;

/// <summary>
///     Covers the Collaborator proxy credential resolution added for the PR #1470 review item
///     (issue #89 omission): the store-activation JWT replaces the COLLAB_PROXY_TOKEN shared
///     secret on Collaborator calls, and no credential at all refuses the call client-side.
///     Env and JWT sources are injected, so nothing here reads real environment variables or IoC.
/// </summary>
[TestClass]
public class KernelFactoryTests
{
    private static string NoEnv(string _) => null;

    private static Func<string, string> Env(string name, string value) =>
        key => key == name ? value : null;

    // ── ResolveWorkflowCredential ────────────────────────────────────────────

    [TestMethod]
    public void ResolveWorkflowCredential_JwtHeld_ReturnsJwtNotSharedSecret()
    {
        var credential = KernelFactory.ResolveWorkflowCredential(
            Env("COLLAB_PROXY_TOKEN", "shared-secret"), () => "activation-jwt");

        Assert.AreEqual("activation-jwt", credential,
            "an activated subscriber's JWT must replace the shared secret");
    }

    [TestMethod]
    public void ResolveWorkflowCredential_NoJwt_FallsBackToSharedSecret()
    {
        var credential = KernelFactory.ResolveWorkflowCredential(
            Env("COLLAB_PROXY_TOKEN", "shared-secret"), () => null);

        Assert.AreEqual("shared-secret", credential);
    }

    [TestMethod]
    public void ResolveWorkflowCredential_NoCredentials_ReturnsNull()
    {
        Assert.IsNull(KernelFactory.ResolveWorkflowCredential(NoEnv, () => null),
            "no JWT and no shared secret must yield no credential, never an empty one");
    }

    // ── ResolveConfig ────────────────────────────────────────────────────────

    [TestMethod]
    public void ResolveConfig_JwtOnly_ProxyPathWithJwtCredential()
    {
        var config = KernelFactory.ResolveConfig(NoEnv, () => "activation-jwt");

        Assert.AreEqual(KernelFactory.KernelPath.Proxy, config.Path);
        Assert.AreEqual("activation-jwt", config.Credential);
        Assert.AreEqual(KernelFactory.DefaultProxyBaseUrl, config.Endpoint);
    }

    [TestMethod]
    public void ResolveConfig_JwtAndSharedToken_JwtWins()
    {
        var config = KernelFactory.ResolveConfig(
            Env("COLLAB_PROXY_TOKEN", "shared-secret"), () => "activation-jwt");

        Assert.AreEqual("activation-jwt", config.Credential);
    }

    [TestMethod]
    public void ResolveConfig_SharedTokenOnly_ProxyPathUnchanged()
    {
        var config = KernelFactory.ResolveConfig(
            Env("COLLAB_PROXY_TOKEN", "shared-secret"), () => null);

        Assert.AreEqual(KernelFactory.KernelPath.Proxy, config.Path);
        Assert.AreEqual("shared-secret", config.Credential);
    }

    [TestMethod]
    public void ResolveConfig_OpenAiKeyOnly_DirectPathUnchanged()
    {
        var config = KernelFactory.ResolveConfig(
            Env("OPENAI_API_KEY", "sk-key"), () => null);

        Assert.AreEqual(KernelFactory.KernelPath.Direct, config.Path);
        Assert.AreEqual("sk-key", config.Credential);
    }

    [TestMethod]
    public void ResolveConfig_NoCredentialsAnywhere_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(
            () => KernelFactory.ResolveConfig(NoEnv, () => null));
    }
}
