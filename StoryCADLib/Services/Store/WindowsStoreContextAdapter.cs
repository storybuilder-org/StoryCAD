// Real WinAppSDK head only. HAS_UNO_WINUI is also defined on the Uno desktop/Skia head (where the
// Windows.Services.Store members below are Uno "not implemented" stubs), so guard on WINDOWS && !HAS_UNO
// like Windowing.cs does; this file is empty on every other head.
#if WINDOWS && !HAS_UNO
using System.Threading;
using Windows.Services.Store;
using WinRT.Interop;
// Windows.Services.Store.StoreProduct collides with our StoreProduct record in this namespace; alias
// the WinRT type so the mapping helper can name it unambiguously.
using WinRtStoreProduct = Windows.Services.Store.StoreProduct;

namespace StoryCADLib.Services.Store;

/// <summary>
///     The only WinRT-touching part of the Windows store path. Wraps <see cref="StoreContext" /> and
///     projects its results into the plain-C# DTOs on <see cref="IStoreContextAdapter" /> so
///     <see cref="WindowsStoreService" /> stays testable off-Windows. Compiled only on the Windows
///     head (<c>HAS_UNO_WINUI</c>); on the desktop/macOS head this file is empty and the service is
///     never registered.
/// </summary>
/// <remarks>
///     NOTE (issue #30): this file cannot be compiled or run on macOS. It is written against the
///     documented <c>Windows.Services.Store</c> API and must be built + exercised on Windows
///     (the manual purchase/relaunch/restore loop in the IAP testing checklist,
///     <c>StoryCADWiki: wiki/repos/Collaborator/sources/iap-billing-docs.md</c>).
/// </remarks>
public sealed class WindowsStoreContextAdapter : IStoreContextAdapter
{
    private readonly Windowing _windowing;
    private readonly ILogService _logService;
    private readonly StoreContext _context;

    // InitializeWithWindow must run once before any UI-showing call (the purchase dialog); the window
    // may not exist at construction time, so associate it lazily on first purchase.
    private bool _windowInitialised;

    public WindowsStoreContextAdapter(Windowing windowing, ILogService logService)
    {
        _windowing = windowing;
        _logService = logService;
        _context = StoreContext.GetDefault();
        _context.OfflineLicensesChanged += OnOfflineLicensesChanged;
    }

    public event Action LicensesChanged;

    public async Task<IReadOnlyList<StoreProductInfo>> GetAssociatedProductsAsync(
        IReadOnlyList<string> productIds, CancellationToken ct = default)
    {
        var wanted = new HashSet<string>(productIds, StringComparer.OrdinalIgnoreCase);
        var result = new List<StoreProductInfo>();

        var query = await _context.GetAssociatedStoreProductsAsync(new[] { "Durable" }).AsTask(ct);
        if (query.ExtendedError != null)
        {
            _logService.Log(LogLevel.Warn,
                $"GetAssociatedStoreProductsAsync failed: {query.ExtendedError.Message}");
            return result;
        }

        foreach (var product in query.Products.Values)
        {
            if (!wanted.Contains(product.StoreId))
            {
                continue;
            }

            result.Add(MapProduct(product));
        }

        return result;
    }

    public async Task<StorePurchaseResult> RequestPurchaseAsync(string productId, CancellationToken ct = default)
    {
        EnsureWindowInitialised();

        // productId is the WinRT StoreId (StoreConfig.WindowsStoreIds); RequestPurchaseAsync takes it directly.
        var purchase = await _context.RequestPurchaseAsync(productId).AsTask(ct);
        var outcome = purchase.Status switch
        {
            StorePurchaseStatus.Succeeded => StorePurchaseOutcome.Succeeded,
            StorePurchaseStatus.AlreadyPurchased => StorePurchaseOutcome.AlreadyPurchased,
            StorePurchaseStatus.NotPurchased => StorePurchaseOutcome.NotPurchased,
            StorePurchaseStatus.NetworkError => StorePurchaseOutcome.NetworkError,
            _ => StorePurchaseOutcome.ServerError
        };
        return new StorePurchaseResult(outcome, purchase.ExtendedError?.Message);
    }

    public async Task<IReadOnlyList<StoreLicenseInfo>> GetAddOnLicensesAsync(CancellationToken ct = default)
    {
        var appLicense = await _context.GetAppLicenseAsync().AsTask(ct);
        var result = new List<StoreLicenseInfo>();
        // The AddOnLicenses dictionary is keyed by the add-on's Store ID, which is the identifier the
        // rest of the pipeline keys on (StoreConfig.WindowsStoreIds); use the key, not InAppOfferToken.
        foreach (var (storeId, license) in appLicense.AddOnLicenses)
        {
            result.Add(new StoreLicenseInfo(storeId, ToExpiryUtc(license.ExpirationDate), license.IsActive));
        }

        return result;
    }

    public async Task<string> GetCustomerPurchaseIdAsync(string serviceTicket, string publisherUserId,
        CancellationToken ct = default)
    {
        // Never call with a cached/expired AAD ticket; the caller fetches a fresh ticket per activation.
        return await _context.GetCustomerPurchaseIdAsync(serviceTicket, publisherUserId).AsTask(ct);
    }

    private void EnsureWindowInitialised()
    {
        if (_windowInitialised)
        {
            return;
        }

        // Required before the purchase dialog in a WinAppSDK desktop app, or it fails to show.
        InitializeWithWindow.Initialize(_context, _windowing.WindowHandle);
        _windowInitialised = true;
    }

    private void OnOfflineLicensesChanged(StoreContext sender, object args) => LicensesChanged?.Invoke();

    private static StoreProductInfo MapProduct(WinRtStoreProduct product)
    {
        var subscription = product.Skus?
            .Select(sku => sku.SubscriptionInfo)
            .FirstOrDefault(info => info != null);

        var period = subscription is null
            ? string.Empty
            : ToIso8601Duration(subscription.BillingPeriod, subscription.BillingPeriodUnit);

        return new StoreProductInfo(
            product.StoreId,
            product.Title,
            product.Description,
            product.Price?.FormattedPrice ?? string.Empty,
            period,
            subscription?.HasTrialPeriod ?? false);
    }

    private static DateTime? ToExpiryUtc(DateTimeOffset expiration)
    {
        // Durable (non-subscription) licenses report a sentinel far-future/zero date; treat as no expiry.
        if (expiration == default || expiration.UtcDateTime.Year >= 9000)
        {
            return null;
        }

        return expiration.UtcDateTime;
    }

    private static string ToIso8601Duration(uint period, StoreDurationUnit unit) => unit switch
    {
        StoreDurationUnit.Day => $"P{period}D",
        StoreDurationUnit.Week => $"P{period}W",
        StoreDurationUnit.Month => $"P{period}M",
        StoreDurationUnit.Year => $"P{period}Y",
        _ => string.Empty
    };
}
#endif
