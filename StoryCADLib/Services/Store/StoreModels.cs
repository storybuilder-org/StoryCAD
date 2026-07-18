namespace StoryCADLib.Services.Store;

/// <summary>
///     Lifecycle state of a store entitlement, as reported by the platform store.
///     Gate access on this, not on the presence of a transaction: an entitlement in
///     <see cref="GracePeriod" /> or <see cref="BillingRetry" /> still grants access
///     while the store chases payment.
/// </summary>
public enum EntitlementState
{
    Active,
    GracePeriod,
    BillingRetry,
    Expired,
    Revoked
}

/// <summary>
///     Outcome of a purchase attempt. <see cref="Pending" /> is Ask to Buy on macOS
///     (parental approval) and is not an error; <see cref="UserCancelled" /> is the
///     user dismissing the sheet.
/// </summary>
public enum PurchaseStatus
{
    Success,
    UserCancelled,
    Pending,
    Failed
}

/// <summary>
///     A purchasable product from the platform store. Prices are formatted by the store
///     per storefront; display <see cref="DisplayPrice" /> as-is.
/// </summary>
public record StoreProduct(
    string Id,
    string DisplayName,
    string Description,
    string DisplayPrice,
    string SubscriptionPeriod,
    bool HasIntroOffer);

/// <summary>
///     A verified entitlement to a product. <see cref="Jws" /> is the platform-signed
///     proof (Apple's <c>Transaction.jwsRepresentation</c> on macOS) the Worker verifies;
///     <see cref="OriginalTransactionId" /> is stable across renewals.
/// </summary>
public record StoreEntitlement(
    string ProductId,
    string TransactionId,
    string OriginalTransactionId,
    DateTime PurchaseDateUtc,
    DateTime? ExpirationDateUtc,
    EntitlementState State,
    bool WillAutoRenew,
    string Jws);

/// <summary>
///     Result of <see cref="IStoreService.PurchaseAsync" />. <see cref="Error" /> is set
///     only when <see cref="Status" /> is <see cref="PurchaseStatus.Failed" />.
/// </summary>
public record PurchaseResult(PurchaseStatus Status, string Error = null);

/// <summary>
///     Result of <see cref="IStoreService.PurchaseConsumableAsync" /> (issue #90 design section 10
///     "Credit packs", step 10). Unlike a subscription, a consumable's proof comes directly from the
///     purchase call itself: StoreKit's <c>Transaction.currentEntitlements</c> excludes consumables,
///     so there is no ongoing entitlement <see cref="IStoreService.GetPurchaseProofAsync" /> could
///     re-query afterward. <see cref="Proof" /> and <see cref="TransactionId" /> are set only when
///     <see cref="Status" /> is <see cref="PurchaseStatus.Success" />; <see cref="TransactionId" /> is
///     the identifier <see cref="IStoreService.FinishConsumableAsync" /> needs (Apple only —
///     Windows has nothing to finish client-side, per the design's step 10 correction).
/// </summary>
public record ConsumablePurchaseResult(PurchaseStatus Status, PurchaseProof Proof = null, string TransactionId = null, string Error = null);

/// <summary>
///     Store-signed proof of purchase sent to the Worker <c>/activate</c> endpoint.
///     <see cref="Platform" /> is "apple" or "microsoft"; <see cref="Payload" /> is the
///     JWS on macOS or the Microsoft purchase-ID key on Windows. <see cref="UserGuid" />
///     is embedded in the signed proof at purchase time so the purchase-to-user link is
///     vouched for by the vendor, not asserted by the client.
/// </summary>
public record PurchaseProof(string Platform, string Payload, string ProductId, string UserGuid);
