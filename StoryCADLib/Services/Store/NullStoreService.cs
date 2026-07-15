using System.Threading;

namespace StoryCADLib.Services.Store;

/// <summary>
///     Default <see cref="IStoreService" /> used when no platform store is available:
///     non-store desktop builds, headless runs, and any TFM without a store implementation.
///     Everything is a no-op; <see cref="IsSupported" /> is false so store UI stays hidden
///     and activation never proceeds via a store.
/// </summary>
public sealed class NullStoreService : IStoreService
{
    public bool IsSupported => false;

    public IReadOnlyList<string> ProductIds => Array.Empty<string>();

    // Never raised; present so the interface contract holds.
    public event EventHandler<StoreEntitlement> EntitlementChanged
    {
        add { }
        remove { }
    }

    public Task<IReadOnlyList<StoreProduct>> GetProductsAsync(IReadOnlyList<string> productIds,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<StoreProduct>>(Array.Empty<StoreProduct>());

    public Task<PurchaseResult> PurchaseAsync(string productId, string userGuid, CancellationToken ct = default) =>
        Task.FromResult(new PurchaseResult(PurchaseStatus.Failed, "No store is available in this build."));

    public Task RestoreAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<StoreEntitlement>> GetCurrentEntitlementsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<StoreEntitlement>>(Array.Empty<StoreEntitlement>());

    public Task<PurchaseProof> GetPurchaseProofAsync(string userGuid, CancellationToken ct = default) =>
        Task.FromResult<PurchaseProof>(null);
}
