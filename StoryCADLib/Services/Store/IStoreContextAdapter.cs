using System.Threading;

namespace StoryCADLib.Services.Store;

/// <summary>
///     Platform-neutral projection of a Microsoft Store product, produced by
///     <see cref="IStoreContextAdapter" />. The WinRT <c>StoreProduct</c> stays behind the adapter
///     so the DTO-to-<see cref="StoreProduct" /> mapping in <see cref="WindowsStoreService" /> is
///     plain C# and testable off-Windows.
/// </summary>
/// <remarks>
///     <c>ProductId</c> is the identifier callers use to select, purchase, and prove the product — the
///     WinRT <c>StoreId</c> on Windows (the same value configured in <c>StoreConfig.WindowsStoreIds</c>),
///     which is also what <c>StoreContext.RequestPurchaseAsync</c> takes. <c>SubscriptionPeriodIso</c> is
///     an ISO-8601 duration, e.g. "P1M" / "P1Y", empty when the product is not a subscription.
/// </remarks>
public record StoreProductInfo(
    string ProductId,
    string Title,
    string Description,
    string FormattedPrice,
    string SubscriptionPeriodIso,
    bool HasTrial);

/// <summary>
///     Platform-neutral projection of a Microsoft Store add-on license
///     (<c>StoreAppLicense.AddOnLicenses</c>). Windows exposes no grace-period or billing-retry
///     detail client-side, so only active/expired can be derived from this.
/// </summary>
/// <param name="ProductId">The add-on's Store ID (the <c>AddOnLicenses</c> dictionary key).</param>
/// <param name="ExpirationDateUtc">Subscription expiry in UTC; null for a durable (non-expiring) license.</param>
/// <param name="IsActive">The OS licensing service's view of whether the license is currently valid.</param>
public record StoreLicenseInfo(
    string ProductId,
    DateTime? ExpirationDateUtc,
    bool IsActive);

/// <summary>
///     Coarse outcome of a Microsoft Store purchase, mapped from WinRT <c>StorePurchaseStatus</c>
///     so the translation to <see cref="PurchaseResult" /> lives in testable code.
/// </summary>
public enum StorePurchaseOutcome
{
    Succeeded,
    AlreadyPurchased,
    NotPurchased,
    NetworkError,
    ServerError
}

/// <summary>
///     Result of <see cref="IStoreContextAdapter.RequestPurchaseAsync" />.
///     <see cref="ExtendedError" /> carries the WinRT <c>ExtendedError</c> string when present.
/// </summary>
public record StorePurchaseResult(StorePurchaseOutcome Outcome, string ExtendedError = null);

/// <summary>
///     Thin, mockable seam over the WinRT <c>StoreContext</c>. The concrete
///     <c>WindowsStoreContextAdapter</c> exists only on the Windows head
///     (<c>#if HAS_UNO_WINUI</c>); <see cref="WindowsStoreService" /> depends on this interface
///     and its plain-C# DTOs so its mapping logic compiles and unit-tests on every head.
/// </summary>
public interface IStoreContextAdapter
{
    /// <summary>
    ///     Raised when the OS licensing service reports a license change out of band
    ///     (<c>StoreContext.OfflineLicensesChanged</c>): a cross-device renewal, a refund, or a
    ///     revocation. Windows has no dedicated renewal event, so this is the only push signal.
    /// </summary>
    event Action LicensesChanged;

    /// <summary>
    ///     Queries the app's associated durable add-ons and returns those whose WinRT <c>StoreId</c>
    ///     is in <paramref name="productIds" /> (the Store IDs from <c>StoreConfig.WindowsStoreIds</c>).
    /// </summary>
    Task<IReadOnlyList<StoreProductInfo>> GetAssociatedProductsAsync(IReadOnlyList<string> productIds,
        CancellationToken ct = default);

    /// <summary>Starts the Store purchase UI for the given Store ID.</summary>
    Task<StorePurchaseResult> RequestPurchaseAsync(string productId, CancellationToken ct = default);

    /// <summary>Reads current add-on licenses from <c>GetAppLicenseAsync().AddOnLicenses</c>.</summary>
    Task<IReadOnlyList<StoreLicenseInfo>> GetAddOnLicensesAsync(CancellationToken ct = default);

    /// <summary>
    ///     Mints the Microsoft purchase-ID key via <c>GetCustomerPurchaseIdAsync</c>. The
    ///     <paramref name="serviceTicket" /> is the short-lived Azure AD token from the Worker
    ///     <c>/store/ticket</c> endpoint; <paramref name="publisherUserId" /> is our stable user GUID.
    /// </summary>
    Task<string> GetCustomerPurchaseIdAsync(string serviceTicket, string publisherUserId,
        CancellationToken ct = default);
}
