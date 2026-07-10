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
    ///     Returns the current entitlement's signed proof for the Worker handshake, or null
    ///     when the store reports no entitlement.
    /// </summary>
    Task<PurchaseProof> GetPurchaseProofAsync(string userGuid, CancellationToken ct = default);
}
