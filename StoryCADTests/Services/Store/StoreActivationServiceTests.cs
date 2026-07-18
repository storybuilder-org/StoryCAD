using System.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Services;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Store;

#nullable disable

namespace StoryCADTests.Services.Store;

/// <summary>
///     Covers every <see cref="ActivationState" /> transition in <see cref="StoreActivationService" />
///     with a hand-rolled fake store and fake Worker client, including the plan's named scenarios:
///     refresh-while-unreachable, revoked-at-refresh, and pending-then-completed.
/// </summary>
[TestClass]
public class StoreActivationServiceTests
{
    private static readonly PurchaseProof SampleProof =
        new("apple", "sample-jws", "org.storycad.collaborator.monthly", "11111111-1111-1111-1111-111111111111");

    [TestInitialize]
    public void Reset()
    {
        // PreferenceService is a shared singleton; clear the store fields so a cached JWT from a
        // prior test can't leak into this one.
        var prefs = Ioc.Default.GetRequiredService<PreferenceService>();
        prefs.Model.StoreActivationJwt = string.Empty;
        prefs.Model.StoreActivationJwtExpiry = DateTime.MinValue;
        prefs.Model.StoreUserGuid = "11111111-1111-1111-1111-111111111111";

        // Defensive reset: guards against a leaked COLLAB_DEV_ACTIVATION from a prior test or the
        // outer shell environment leaking into tests that don't expect dev-platform routing.
        Environment.SetEnvironmentVariable("COLLAB_DEV_ACTIVATION", null);
    }

    private static StoreActivationService CreateService(FakeStoreService store, FakeActivationClient client) =>
        new(store, client,
            Ioc.Default.GetRequiredService<PreferenceService>(),
            Ioc.Default.GetRequiredService<Windowing>(),
            Ioc.Default.GetRequiredService<ILogService>());

    private static void SetCachedJwt(string jwt, DateTime expiryUtc)
    {
        var prefs = Ioc.Default.GetRequiredService<PreferenceService>();
        prefs.Model.StoreActivationJwt = jwt;
        prefs.Model.StoreActivationJwtExpiry = expiryUtc;
    }

    [TestMethod]
    public async Task InitializeAsync_NoEntitlement_NotPurchased()
    {
        var store = new FakeStoreService { Proof = null };
        var service = CreateService(store, new FakeActivationClient());

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.NotPurchased, service.State);
        Assert.IsNull(service.CurrentJwt);
    }

    [TestMethod]
    public async Task InitializeAsync_CachedUnexpiredJwt_ActiveWithoutStoreCall()
    {
        SetCachedJwt("cached-jwt", DateTime.UtcNow.AddHours(6));
        var store = new FakeStoreService { Proof = SampleProof };
        var client = new FakeActivationClient();
        var service = CreateService(store, client);

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.Active, service.State);
        Assert.AreEqual("cached-jwt", service.CurrentJwt);
        Assert.AreEqual(0, store.ProofCallCount, "an unexpired cached JWT must not hit the store or Worker");
        Assert.AreEqual(0, client.ActivateCallCount);
    }

    [TestMethod]
    public async Task InitializeAsync_ExpiredCachedJwt_RefreshesAndActivates()
    {
        SetCachedJwt("stale-jwt", DateTime.UtcNow.AddHours(-1));
        var store = new FakeStoreService { Proof = SampleProof };
        var client = new FakeActivationClient
        {
            Response = new ActivationResponse(true, "fresh-jwt", DateTime.UtcNow.AddHours(12), null)
        };
        var service = CreateService(store, client);

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.Active, service.State);
        Assert.AreEqual("fresh-jwt", service.CurrentJwt);
        Assert.AreEqual(1, client.ActivateCallCount);
    }

    [TestMethod]
    public async Task InitializeAsync_WorkerUnreachableOnRefresh_NotPurchased()
    {
        // refresh-while-unreachable: expired cache forces a refresh, the Worker call throws.
        SetCachedJwt("stale-jwt", DateTime.UtcNow.AddHours(-1));
        var store = new FakeStoreService { Proof = SampleProof };
        var client = new FakeActivationClient
        {
            ThrowOnActivate = new StoreActivationUnreachableException("network down")
        };
        var service = CreateService(store, client);

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.NotPurchased, service.State,
            "an unreachable Worker must not deny a paying user; stay NotPurchased and let them retry");
        Assert.IsNull(service.CurrentJwt);
    }

    [TestMethod]
    public async Task InitializeAsync_WorkerRevokedOnRefresh_Refused()
    {
        // revoked-at-refresh: proof still present, Worker reports the purchase revoked.
        var store = new FakeStoreService { Proof = SampleProof };
        var client = new FakeActivationClient
        {
            Response = new ActivationResponse(false, null, null, "revoked")
        };
        var service = CreateService(store, client);

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.Refused, service.State);
        Assert.IsNull(service.CurrentJwt);
    }

    [TestMethod]
    public async Task InitializeAsync_WorkerExpiredOnRefresh_NotPurchased()
    {
        var store = new FakeStoreService { Proof = SampleProof };
        var client = new FakeActivationClient
        {
            Response = new ActivationResponse(false, null, null, "expired")
        };
        var service = CreateService(store, client);

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.NotPurchased, service.State,
            "an expired subscription is lapsed, not refused; show the resubscribe path");
    }

    [TestMethod]
    public async Task PurchaseAndActivateAsync_Success_Active()
    {
        var store = new FakeStoreService { Proof = SampleProof, PurchaseResult = new PurchaseResult(PurchaseStatus.Success) };
        var client = new FakeActivationClient();
        var service = CreateService(store, client);

        var result = await service.PurchaseAndActivateAsync(SampleProof.ProductId);

        Assert.AreEqual(PurchaseStatus.Success, result.Status);
        Assert.AreEqual(ActivationState.Active, service.State);
        Assert.AreEqual(SampleProof.UserGuid, store.LastPurchaseUserGuid, "the user GUID must reach the store");
    }

    [TestMethod]
    public async Task PurchaseAndActivateAsync_UserCancelled_StateUnchanged()
    {
        var store = new FakeStoreService { PurchaseResult = new PurchaseResult(PurchaseStatus.UserCancelled) };
        var service = CreateService(store, new FakeActivationClient());

        var result = await service.PurchaseAndActivateAsync(SampleProof.ProductId);

        Assert.AreEqual(PurchaseStatus.UserCancelled, result.Status);
        Assert.AreEqual(ActivationState.NotPurchased, service.State);
    }

    [TestMethod]
    public async Task PurchaseAndActivateAsync_Pending_PendingStore()
    {
        var store = new FakeStoreService { PurchaseResult = new PurchaseResult(PurchaseStatus.Pending) };
        var service = CreateService(store, new FakeActivationClient());

        var result = await service.PurchaseAndActivateAsync(SampleProof.ProductId);

        Assert.AreEqual(PurchaseStatus.Pending, result.Status);
        Assert.AreEqual(ActivationState.PendingStore, service.State);
    }

    [TestMethod]
    public async Task PurchaseAndActivateAsync_Failed_StateUnchanged()
    {
        var store = new FakeStoreService { PurchaseResult = new PurchaseResult(PurchaseStatus.Failed, "declined") };
        var service = CreateService(store, new FakeActivationClient());

        var result = await service.PurchaseAndActivateAsync(SampleProof.ProductId);

        Assert.AreEqual(PurchaseStatus.Failed, result.Status);
        Assert.AreEqual("declined", result.Error);
        Assert.AreEqual(ActivationState.NotPurchased, service.State);
    }

    [TestMethod]
    public async Task EntitlementChanged_AfterPending_CompletesActivation()
    {
        // pending-then-completed: Ask to Buy starts Pending, later approval arrives out of band.
        var store = new FakeStoreService { PurchaseResult = new PurchaseResult(PurchaseStatus.Pending) };
        var client = new FakeActivationClient();
        var service = CreateService(store, client);

        await service.PurchaseAndActivateAsync(SampleProof.ProductId);
        Assert.AreEqual(ActivationState.PendingStore, service.State);

        // Approval: the entitlement now exists and the store pushes a change.
        store.Proof = SampleProof;
        store.RaiseEntitlementChanged();

        Assert.AreEqual(ActivationState.Active, service.State);
        Assert.AreEqual("jwt-token", service.CurrentJwt);
    }

    [TestMethod]
    public async Task EntitlementChanged_Revocation_DropsToRefused()
    {
        var store = new FakeStoreService { Proof = SampleProof };
        var client = new FakeActivationClient();
        var service = CreateService(store, client);

        await service.InitializeAsync();
        Assert.AreEqual(ActivationState.Active, service.State);

        // The subscription is revoked (refund); the next verification refuses it.
        client.Response = new ActivationResponse(false, null, null, "revoked");
        store.RaiseEntitlementChanged();

        Assert.AreEqual(ActivationState.Refused, service.State);
        Assert.IsNull(service.CurrentJwt);
    }

    [TestMethod]
    public async Task RestoreAsync_WithEntitlement_Active()
    {
        var store = new FakeStoreService { Proof = SampleProof };
        var service = CreateService(store, new FakeActivationClient());

        await service.RestoreAsync();

        Assert.IsTrue(store.RestoreCalled);
        Assert.AreEqual(ActivationState.Active, service.State);
    }

    [TestMethod]
    public async Task InitializeAsync_OkResponseWithoutJwt_NotPurchased()
    {
        // ok:true with no token is a malformed success; it must be retryable, never Refused.
        var store = new FakeStoreService { Proof = SampleProof };
        var client = new FakeActivationClient { Response = new ActivationResponse(true, null, null, null) };
        var service = CreateService(store, client);

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.NotPurchased, service.State,
            "a success response with no JWT is transient, not an authenticated refusal");
        Assert.IsNull(service.CurrentJwt);
    }

    [TestMethod]
    public async Task InitializeAsync_OkResponseWithoutExpiry_PersistsFutureExpiry()
    {
        // A missing expiresAt must not cache an already-expired JWT.
        var store = new FakeStoreService { Proof = SampleProof };
        var client = new FakeActivationClient { Response = new ActivationResponse(true, "jwt-x", null, null) };
        var service = CreateService(store, client);

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.Active, service.State);
        var prefs = Ioc.Default.GetRequiredService<PreferenceService>();
        Assert.IsTrue(prefs.Model.StoreActivationJwtExpiry > DateTime.UtcNow,
            "the fallback expiry must be in the future so the cache is usable");
    }

    [TestMethod]
    public async Task InitializeAsync_CachedJwtWithMinValueExpiry_RefreshesWithoutThrowing()
    {
        // A corrupt prefs file can pair a JWT with a MinValue expiry; the leeway
        // comparison must not underflow.
        SetCachedJwt("orphaned-jwt", DateTime.MinValue);
        var store = new FakeStoreService { Proof = SampleProof };
        var service = CreateService(store, new FakeActivationClient());

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.Active, service.State);
    }

    [TestMethod]
    public async Task StateChanged_WhenInitializeActivates_RaisesActive()
    {
        var store = new FakeStoreService { Proof = SampleProof };
        var service = CreateService(store, new FakeActivationClient());
        ActivationState? observed = null;
        service.StateChanged += (_, s) => observed = s;

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.Active, observed);
    }

    // ── Dev/tester allowlist activation (issue #90 D7/D8, step 8 item 3) ───────

    [TestMethod]
    public async Task StoreActivationService_DevEnabled_PostsDevPlatformWithGuid()
    {
        Environment.SetEnvironmentVariable("COLLAB_DEV_ACTIVATION", "1");
        try
        {
            var store = new FakeStoreService { Proof = SampleProof };
            var client = new FakeActivationClient();
            var service = CreateService(store, client);

            await service.InitializeAsync();

            Assert.AreEqual(ActivationState.Active, service.State);
            Assert.AreEqual(1, client.ActivateCallCount);
            Assert.IsNotNull(client.LastProof);
            Assert.AreEqual("dev", client.LastProof.Platform);
            Assert.AreEqual("11111111-1111-1111-1111-111111111111", client.LastProof.UserGuid);
            Assert.AreEqual(0, store.ProofCallCount,
                "dev activation must not ask the platform store for proof");
        }
        finally
        {
            Environment.SetEnvironmentVariable("COLLAB_DEV_ACTIVATION", null);
        }
    }

    [TestMethod]
    public async Task StoreActivationService_DevDisabled_UsesStoreProof()
    {
        // COLLAB_DEV_ACTIVATION left unset: the default, store-driven path is unchanged.
        Environment.SetEnvironmentVariable("COLLAB_DEV_ACTIVATION", null);
        var store = new FakeStoreService { Proof = SampleProof };
        var client = new FakeActivationClient();
        var service = CreateService(store, client);

        await service.InitializeAsync();

        Assert.AreEqual(ActivationState.Active, service.State);
        Assert.AreEqual(1, store.ProofCallCount, "the platform store must be asked for proof");
        Assert.IsNotNull(client.LastProof);
        Assert.AreEqual(SampleProof.Platform, client.LastProof.Platform);
        Assert.AreNotEqual("dev", client.LastProof.Platform);
    }

    // Fakes shared with the other store test classes live in StoreTestDoubles.cs.
}
