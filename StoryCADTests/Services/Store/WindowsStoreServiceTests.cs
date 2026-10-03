using System.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Models;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Store;

#nullable disable

namespace StoryCADTests.Services.Store;

/// <summary>
///     Covers <see cref="WindowsStoreService" />'s mapping and orchestration with a fake
///     <see cref="IStoreContextAdapter" /> and a fake ticket client. This is the part of the Windows
///     store path that is plain C#, so it compiles and runs on every head (including this macOS
///     desktop head). The WinRT half (<c>WindowsStoreContextAdapter</c>) is verified on Windows.
/// </summary>
[TestClass]
public class WindowsStoreServiceTests
{
    // On Windows the identifier the pipeline keys on is the add-on's Store ID (StoreConfig.WindowsStoreIds).
    private const string ProductId = "9PF1LZ07MSH6";
    private const string UserGuid = "11111111-1111-1111-1111-111111111111";

    private static WindowsStoreService CreateService(FakeStoreContextAdapter adapter, FakeActivationClient client) =>
        new(adapter, client,
            Ioc.Default.GetRequiredService<Windowing>(),
            Ioc.Default.GetRequiredService<ILogService>());

    [TestMethod]
    public void IsSupported_OnWindowsHead_IsTrue()
    {
        var service = CreateService(new FakeStoreContextAdapter(), new FakeActivationClient());
        Assert.IsTrue(service.IsSupported);
    }

    [TestMethod]
    public async Task GetProductsAsync_WithAdapterProduct_MapsAllFields()
    {
        var adapter = new FakeStoreContextAdapter
        {
            Products = new[]
            {
                new StoreProductInfo(ProductId, "Collaborator Monthly",
                    "AI writing assistant", "$4.99", "P1M", true)
            }
        };
        var service = CreateService(adapter, new FakeActivationClient());

        var products = await service.GetProductsAsync(new[] { ProductId });

        Assert.AreEqual(1, products.Count);
        var p = products[0];
        Assert.AreEqual(ProductId, p.Id, "product id is the Store ID on Windows");
        Assert.AreEqual("Collaborator Monthly", p.DisplayName);
        Assert.AreEqual("AI writing assistant", p.Description);
        Assert.AreEqual("$4.99", p.DisplayPrice);
        Assert.AreEqual("P1M", p.SubscriptionPeriod);
        Assert.IsTrue(p.HasIntroOffer);
    }

    [TestMethod]
    public async Task PurchaseAsync_Succeeded_ReturnsSuccess()
    {
        var adapter = new FakeStoreContextAdapter { PurchaseResult = new StorePurchaseResult(StorePurchaseOutcome.Succeeded) };
        var service = CreateService(adapter, new FakeActivationClient());

        var result = await service.PurchaseAsync(ProductId, UserGuid);

        Assert.AreEqual(PurchaseStatus.Success, result.Status);
        Assert.AreEqual(ProductId, adapter.LastPurchaseProductId);
    }

    [TestMethod]
    public async Task PurchaseAsync_AlreadyPurchased_ReturnsSuccess()
    {
        var adapter = new FakeStoreContextAdapter { PurchaseResult = new StorePurchaseResult(StorePurchaseOutcome.AlreadyPurchased) };
        var service = CreateService(adapter, new FakeActivationClient());

        var result = await service.PurchaseAsync(ProductId, UserGuid);

        Assert.AreEqual(PurchaseStatus.Success, result.Status);
    }

    [TestMethod]
    public async Task PurchaseAsync_NotPurchased_ReturnsUserCancelled()
    {
        var adapter = new FakeStoreContextAdapter { PurchaseResult = new StorePurchaseResult(StorePurchaseOutcome.NotPurchased) };
        var service = CreateService(adapter, new FakeActivationClient());

        var result = await service.PurchaseAsync(ProductId, UserGuid);

        Assert.AreEqual(PurchaseStatus.UserCancelled, result.Status);
    }

    [TestMethod]
    public async Task PurchaseAsync_ServerError_ReturnsFailedWithExtendedError()
    {
        var adapter = new FakeStoreContextAdapter
        {
            PurchaseResult = new StorePurchaseResult(StorePurchaseOutcome.ServerError, "0x803F8001")
        };
        var service = CreateService(adapter, new FakeActivationClient());

        var result = await service.PurchaseAsync(ProductId, UserGuid);

        Assert.AreEqual(PurchaseStatus.Failed, result.Status);
        Assert.AreEqual("0x803F8001", result.Error);
    }

    [TestMethod]
    public async Task GetCurrentEntitlementsAsync_ActiveLicenseInFuture_MapsActive()
    {
        var adapter = new FakeStoreContextAdapter
        {
            Licenses = new[] { new StoreLicenseInfo(ProductId, DateTime.UtcNow.AddDays(20), true) }
        };
        var service = CreateService(adapter, new FakeActivationClient());

        var entitlements = await service.GetCurrentEntitlementsAsync();

        Assert.AreEqual(1, entitlements.Count);
        Assert.AreEqual(ProductId, entitlements[0].ProductId);
        Assert.AreEqual(EntitlementState.Active, entitlements[0].State);
    }

    [TestMethod]
    public async Task GetCurrentEntitlementsAsync_ExpiredOrInactive_MapsExpired()
    {
        var adapter = new FakeStoreContextAdapter
        {
            Licenses = new[]
            {
                new StoreLicenseInfo("past", DateTime.UtcNow.AddDays(-1), true),   // active flag but expired by date
                new StoreLicenseInfo("inactive", DateTime.UtcNow.AddDays(30), false) // future date but not active
            }
        };
        var service = CreateService(adapter, new FakeActivationClient());

        var entitlements = await service.GetCurrentEntitlementsAsync();

        Assert.IsTrue(entitlements.All(e => e.State == EntitlementState.Expired),
            "Windows exposes only active/expired client-side; neither of these is active");
    }

    [TestMethod]
    public async Task GetPurchaseProofAsync_ActiveEntitlement_ReturnsMicrosoftProof()
    {
        var adapter = new FakeStoreContextAdapter
        {
            Licenses = new[] { new StoreLicenseInfo(ProductId, DateTime.UtcNow.AddDays(20), true) },
            PurchaseId = "ms-purchase-key"
        };
        var client = new FakeActivationClient { Ticket = "aad-service-ticket" };
        var service = CreateService(adapter, client);

        var proof = await service.GetPurchaseProofAsync(UserGuid);

        Assert.IsNotNull(proof);
        Assert.AreEqual("microsoft", proof.Platform);
        Assert.AreEqual("ms-purchase-key", proof.Payload);
        Assert.AreEqual(ProductId, proof.ProductId);
        Assert.AreEqual(UserGuid, proof.UserGuid);
        Assert.AreEqual("aad-service-ticket", adapter.LastServiceTicket, "the Worker's ticket must reach the Store");
        Assert.AreEqual(UserGuid, adapter.LastPublisherUserId, "our GUID is the publisherUserId");
    }

    [TestMethod]
    public async Task GetPurchaseProofAsync_NoTicket_ReturnsNull()
    {
        var adapter = new FakeStoreContextAdapter
        {
            Licenses = new[] { new StoreLicenseInfo(ProductId, DateTime.UtcNow.AddDays(20), true) }
        };
        var client = new FakeActivationClient { Ticket = null };
        var service = CreateService(adapter, client);

        var proof = await service.GetPurchaseProofAsync(UserGuid);

        Assert.IsNull(proof, "no ticket -> no proof (activation reports NotPurchased and offers retry)");
    }

    [TestMethod]
    public async Task GetPurchaseProofAsync_NoActiveEntitlement_ReturnsNullWithoutFetchingTicket()
    {
        var adapter = new FakeStoreContextAdapter
        {
            Licenses = new[] { new StoreLicenseInfo(ProductId, DateTime.UtcNow.AddDays(-1), true) } // expired
        };
        var client = new FakeActivationClient();
        var service = CreateService(adapter, client);

        var proof = await service.GetPurchaseProofAsync(UserGuid);

        Assert.IsNull(proof);
        Assert.AreEqual(0, client.TicketCallCount, "no entitlement means no ticket round-trip");
    }

    [TestMethod]
    public async Task LicensesChanged_WhenRaised_SignalsEntitlementChanged()
    {
        // The payload is a sentinel by contract: the shared activation service ignores it and
        // re-runs proof->JWT itself, so the event must fire (with any non-null value) on every
        // license change — including a revocation that leaves no license behind.
        var adapter = new FakeStoreContextAdapter { Licenses = Array.Empty<StoreLicenseInfo>() };
        var service = CreateService(adapter, new FakeActivationClient());
        var tcs = new TaskCompletionSource<StoreEntitlement>(TaskCreationOptions.RunContinuationsAsynchronously);
        service.EntitlementChanged += (_, e) => tcs.TrySetResult(e);

        adapter.RaiseLicensesChanged();

        var raised = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsNotNull(raised);
    }

    // ----- Credit packs (issue #90 design section 10, step 10) -----

    private const string PackProductId = "9PCREDITPAK1";

    [TestMethod]
    public async Task PurchaseConsumableAsync_Succeeded_ReturnsSuccessWithCollectionsProof()
    {
        var adapter = new FakeStoreContextAdapter
        {
            PurchaseResult = new StorePurchaseResult(StorePurchaseOutcome.Succeeded),
            CollectionsId = "collections-key-1"
        };
        var client = new FakeActivationClient { Ticket = "aad-collections-ticket" };
        var service = CreateService(adapter, client);

        var result = await service.PurchaseConsumableAsync(PackProductId, UserGuid);

        Assert.AreEqual(PurchaseStatus.Success, result.Status);
        Assert.IsNotNull(result.Proof);
        Assert.AreEqual("microsoft", result.Proof.Platform);
        Assert.AreEqual("collections-key-1", result.Proof.Payload);
        Assert.AreEqual(PackProductId, result.Proof.ProductId);
        Assert.AreEqual(UserGuid, result.Proof.UserGuid);
        // The collections ticket is a *different* Microsoft Store ID key from the subscription
        // purchase-audience one (design section 10 step 10 correction).
        Assert.AreEqual("collections", client.LastTicketPurpose);
        Assert.AreEqual("aad-collections-ticket", adapter.LastCollectionsServiceTicket);
        Assert.AreEqual(UserGuid, adapter.LastCollectionsPublisherUserId);
    }

    [TestMethod]
    public async Task PurchaseConsumableAsync_NoTicket_ReturnsFailed()
    {
        var adapter = new FakeStoreContextAdapter { PurchaseResult = new StorePurchaseResult(StorePurchaseOutcome.Succeeded) };
        var client = new FakeActivationClient { Ticket = null };
        var service = CreateService(adapter, client);

        var result = await service.PurchaseConsumableAsync(PackProductId, UserGuid);

        Assert.AreEqual(PurchaseStatus.Failed, result.Status);
        Assert.IsNull(result.Proof);
    }

    [TestMethod]
    public async Task PurchaseConsumableAsync_NotPurchased_ReturnsUserCancelled()
    {
        var adapter = new FakeStoreContextAdapter { PurchaseResult = new StorePurchaseResult(StorePurchaseOutcome.NotPurchased) };
        var service = CreateService(adapter, new FakeActivationClient());

        var result = await service.PurchaseConsumableAsync(PackProductId, UserGuid);

        Assert.AreEqual(PurchaseStatus.UserCancelled, result.Status);
    }

    [TestMethod]
    public async Task FinishConsumableAsync_OnWindows_IsNoOp()
    {
        // The Worker reports fulfillment itself via the Microsoft collection API (design section
        // 10 step 10 correction); the Windows client has nothing to call.
        var service = CreateService(new FakeStoreContextAdapter(), new FakeActivationClient());

        await service.FinishConsumableAsync("some-transaction-id");
        // No exception, no adapter call to assert on -- the point of this test is that it's a no-op.
    }

    // ----- Fakes -----

    private sealed class FakeStoreContextAdapter : IStoreContextAdapter
    {
        public IReadOnlyList<StoreProductInfo> Products = Array.Empty<StoreProductInfo>();
        public StorePurchaseResult PurchaseResult = new(StorePurchaseOutcome.Succeeded);
        public IReadOnlyList<StoreLicenseInfo> Licenses = Array.Empty<StoreLicenseInfo>();
        public string PurchaseId = "purchase-id-key";
        public string CollectionsId = "collections-id-key";
        public string LastServiceTicket;
        public string LastPublisherUserId;
        public string LastPurchaseProductId;
        public string LastCollectionsServiceTicket;
        public string LastCollectionsPublisherUserId;

        public event Action LicensesChanged;

        public Task<IReadOnlyList<StoreProductInfo>> GetAssociatedProductsAsync(IReadOnlyList<string> productIds,
            CancellationToken ct = default) => Task.FromResult(Products);

        public Task<StorePurchaseResult> RequestPurchaseAsync(string productId, CancellationToken ct = default)
        {
            LastPurchaseProductId = productId;
            return Task.FromResult(PurchaseResult);
        }

        public Task<IReadOnlyList<StoreLicenseInfo>> GetAddOnLicensesAsync(CancellationToken ct = default) =>
            Task.FromResult(Licenses);

        public Task<string> GetCustomerPurchaseIdAsync(string serviceTicket, string publisherUserId,
            CancellationToken ct = default)
        {
            LastServiceTicket = serviceTicket;
            LastPublisherUserId = publisherUserId;
            return Task.FromResult(PurchaseId);
        }

        public Task<string> GetCustomerCollectionsIdAsync(string serviceTicket, string publisherUserId,
            CancellationToken ct = default)
        {
            LastCollectionsServiceTicket = serviceTicket;
            LastCollectionsPublisherUserId = publisherUserId;
            return Task.FromResult(CollectionsId);
        }

        public void RaiseLicensesChanged() => LicensesChanged?.Invoke();
    }

    // The IActivationClient fake is shared: StoreTestDoubles.cs.
}
