using System.Threading;

namespace StoryCADLib.Services.Store;

/// <summary>
///     Wraps the platform store (StoreKit on macOS, the Microsoft Store on Windows):
///     list products, purchase, read entitlements, produce purchase proof. Platform heads
///     register the concrete implementation; <see cref="NullStoreService" /> is the default
///     so the same desktop binary runs unchanged outside a store bundle.
/// </summary>
public interface IStoreService
{
    /// <summary>
    ///     True when a real platform store is reachable. False for <see cref="NullStoreService" />;
    ///     callers gate store UI on this.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    ///     The product identifiers this store understands (StoreKit product IDs on Apple, Store IDs
    ///     on Windows, empty for <see cref="NullStoreService" />). Keeping the scheme with the store
    ///     that consumes it means the platform switch exists exactly once, in the DI registration.
    /// </summary>
    IReadOnlyList<string> ProductIds { get; }

    /// <summary>
    ///     Raised when the store reports an entitlement change out of band: a renewal or a
    ///     completed Ask-to-Buy purchase (arriving through the platform's transaction listener)
    ///     or a revocation. Marshalled to the UI thread by the raising implementation.
    /// </summary>
    event EventHandler<StoreEntitlement> EntitlementChanged;

    Task<IReadOnlyList<StoreProduct>> GetProductsAsync(IReadOnlyList<string> productIds, CancellationToken ct = default);

    /// <summary>
    ///     Starts a purchase. <paramref name="userGuid" /> is passed to the store so it is
    ///     embedded in the signed proof (Apple <c>appAccountToken</c> / Microsoft
    ///     <c>publisherUserId</c>) and returns inside the JWS.
    /// </summary>
    Task<PurchaseResult> PurchaseAsync(string productId, string userGuid, CancellationToken ct = default);

    /// <summary>
    ///     Restores purchases. On macOS this shows a system sign-in prompt, so only call it
    ///     from an explicit Restore Purchases action, never automatically.
    /// </summary>
    Task RestoreAsync(CancellationToken ct = default);

    /// <summary>
    ///     The store's current entitlements for this account (empty for
    ///     <see cref="NullStoreService" />). Callers gate on <see cref="StoreEntitlement.State" />,
    ///     not on presence; the raising implementations already exclude lapsed/revoked where the
    ///     platform does.
    /// </summary>
    Task<IReadOnlyList<StoreEntitlement>> GetCurrentEntitlementsAsync(CancellationToken ct = default);

    /// <summary>
    ///     Returns the current entitlement's signed proof for the Worker handshake, or null
    ///     when the store reports no entitlement.
    /// </summary>
    Task<PurchaseProof> GetPurchaseProofAsync(string userGuid, CancellationToken ct = default);

    /// <summary>
    ///     The credit-pack (consumable) product identifiers this store understands (issue #90
    ///     design section 10 "Credit packs", step 10); empty for <see cref="NullStoreService" />.
    ///     Parallel to <see cref="ProductIds" />, which is the subscription catalog only — packs are
    ///     a separate product family with no ongoing entitlement, so they get their own list rather
    ///     than joining it.
    /// </summary>
    IReadOnlyList<string> CreditPackProductIds { get; }

    /// <summary>
    ///     Purchases a consumable (credit pack) and returns its own proof directly from the purchase
    ///     call — unlike a subscription, a consumable has no ongoing entitlement
    ///     <see cref="GetPurchaseProofAsync" /> could re-derive one from afterward.
    ///     <paramref name="userGuid" /> is embedded in the proof the same way <see cref="PurchaseAsync" />
    ///     embeds it for a subscription.
    /// </summary>
    Task<ConsumablePurchaseResult> PurchaseConsumableAsync(string productId, string userGuid, CancellationToken ct = default);

    /// <summary>
    ///     Finishes a consumable transaction after the Worker has confirmed the credit (design
    ///     section 10: "the client finishes the transaction only after the Worker's 200"). Apple:
    ///     finishes the StoreKit transaction <see cref="PurchaseConsumableAsync" /> deliberately left
    ///     open. Windows: a no-op — the Worker itself reports consumable fulfillment to the Microsoft
    ///     collection API (design section 10's step 10 correction), so there is nothing for the
    ///     client to finish.
    /// </summary>
    Task FinishConsumableAsync(string transactionId, CancellationToken ct = default);
}
