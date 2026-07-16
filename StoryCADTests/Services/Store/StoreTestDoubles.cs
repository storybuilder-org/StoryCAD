using System.Threading;
using StoryCADLib.Services.Store;

#nullable disable

namespace StoryCADTests.Services.Store;

// Shared configurable fakes for the store seams. One fake per interface, with the union of the
// knobs the store test classes need, so an IStoreService/IActivationClient change lands here once.

internal sealed class FakeStoreService : IStoreService
{
    public IReadOnlyList<StoreProduct> Products = Array.Empty<StoreProduct>();
    public bool ThrowOnGetProducts;
    public int GetProductsCallCount;
    public PurchaseProof Proof;
    public int ProofCallCount;
    public PurchaseResult PurchaseResult = new(PurchaseStatus.Success);
    public string LastPurchaseUserGuid;
    public bool RestoreCalled;
    public IReadOnlyList<StoreEntitlement> Entitlements = Array.Empty<StoreEntitlement>();

    // issue #90 design section 10 "Credit packs" (step 10).
    public ConsumablePurchaseResult ConsumableResult = new(PurchaseStatus.Success,
        new PurchaseProof("apple", "pack-jws", "CollabCreditPack500", "user-guid"), "pack-transaction-1");
    public string LastConsumablePurchaseUserGuid;
    public string LastFinishedTransactionId;
    public int FinishConsumableCallCount;
    public int PurchaseConsumableCallCount;

    public bool IsSupported => true;

    public IReadOnlyList<string> ProductIds { get; set; } = new[] { "org.storycad.collaborator.monthly" };

    public IReadOnlyList<string> CreditPackProductIds { get; set; } = new[] { "CollabCreditPack500" };

    public event EventHandler<StoreEntitlement> EntitlementChanged;

    public Task<IReadOnlyList<StoreProduct>> GetProductsAsync(IReadOnlyList<string> productIds,
        CancellationToken ct = default)
    {
        GetProductsCallCount++;
        return ThrowOnGetProducts
            ? Task.FromException<IReadOnlyList<StoreProduct>>(new Exception("store unreachable"))
            : Task.FromResult(Products);
    }

    public Task<PurchaseResult> PurchaseAsync(string productId, string userGuid, CancellationToken ct = default)
    {
        LastPurchaseUserGuid = userGuid;
        return Task.FromResult(PurchaseResult);
    }

    public Task RestoreAsync(CancellationToken ct = default)
    {
        RestoreCalled = true;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<StoreEntitlement>> GetCurrentEntitlementsAsync(CancellationToken ct = default) =>
        Task.FromResult(Entitlements);

    public Task<PurchaseProof> GetPurchaseProofAsync(string userGuid, CancellationToken ct = default)
    {
        ProofCallCount++;
        return Task.FromResult(Proof);
    }

    public Task<ConsumablePurchaseResult> PurchaseConsumableAsync(string productId, string userGuid,
        CancellationToken ct = default)
    {
        PurchaseConsumableCallCount++;
        LastConsumablePurchaseUserGuid = userGuid;
        return Task.FromResult(ConsumableResult);
    }

    public Task FinishConsumableAsync(string transactionId, CancellationToken ct = default)
    {
        FinishConsumableCallCount++;
        LastFinishedTransactionId = transactionId;
        return Task.CompletedTask;
    }

    public void RaiseEntitlementChanged() =>
        EntitlementChanged?.Invoke(this, new StoreEntitlement(
            "org.storycad.collaborator.monthly", "t1", "o1", DateTime.UtcNow, null,
            EntitlementState.Active, true, "sample-jws"));
}

internal sealed class FakeActivationClient : IActivationClient
{
    public ActivationResponse Response = new(true, "jwt-token", DateTime.UtcNow.AddHours(12), null);
    public Exception ThrowOnActivate;
    public int ActivateCallCount;
    public PurchaseProof LastProof;
    public string Ticket = "aad-service-ticket";
    public int TicketCallCount;
    public string LastTicketPurpose;

    public Task<ActivationResponse> ActivateAsync(PurchaseProof proof, CancellationToken ct = default)
    {
        ActivateCallCount++;
        LastProof = proof;
        return ThrowOnActivate != null
            ? Task.FromException<ActivationResponse>(ThrowOnActivate)
            : Task.FromResult(Response);
    }

    public Task<string> GetStoreTicketAsync(string purpose = "purchase", CancellationToken ct = default)
    {
        TicketCallCount++;
        LastTicketPurpose = purpose;
        return Task.FromResult(Ticket);
    }
}
