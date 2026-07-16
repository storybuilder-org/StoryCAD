using System.Threading;
using StoryCADLib.Services.Locking;

namespace StoryCADLib.Services.Store;

/// <summary>
///     Windows <see cref="IStoreService" /> over the Microsoft Store, driven through the mockable
///     <see cref="IStoreContextAdapter" />. All WinRT calls live behind the adapter, so this class is
///     plain C#: it compiles on every head and its mapping is unit-tested off-Windows. Registered
///     only when running on Windows; elsewhere the desktop head uses <see cref="NullStoreService" />.
///     Gating reads entitlement <see cref="EntitlementState" />, never transaction presence.
/// </summary>
public sealed class WindowsStoreService : IStoreService
{
    // Placeholder raised on any license change: the shared activation service ignores the payload
    // and simply re-evaluates (proof->JWT), so any non-null value triggers it.
    private static readonly StoreEntitlement NoEntitlement = new(
        string.Empty, string.Empty, string.Empty, default, null, EntitlementState.Revoked, false, string.Empty);

    private readonly IStoreContextAdapter _adapter;
    private readonly IActivationClient _activationClient;
    private readonly Windowing _windowing;
    private readonly ILogService _logService;

    public WindowsStoreService(IStoreContextAdapter adapter, IActivationClient activationClient,
        Windowing windowing, ILogService logService)
    {
        _adapter = adapter;
        _activationClient = activationClient;
        _windowing = windowing;
        _logService = logService;
        // Windows has no dedicated renewal event; OfflineLicensesChanged (cross-device renewal, refund,
        // revocation) is the only out-of-band signal. InitializeAsync at launch covers the rest.
        _adapter.LicensesChanged += OnLicensesChanged;
    }

    // A store context is always obtainable on the Windows head; a sideloaded debug build has no license
    // context and purchases fail there. COLLAB_DEV_ENABLED routes StoreActivationService around this
    // store entirely (dev/tester allowlist activation, issue #90 D7/D8) rather than patching it here.
    public bool IsSupported => true;

    public IReadOnlyList<string> ProductIds => StoreConfig.WindowsStoreIds;

    public event EventHandler<StoreEntitlement> EntitlementChanged;

    public async Task<IReadOnlyList<StoreProduct>> GetProductsAsync(IReadOnlyList<string> productIds,
        CancellationToken ct = default)
    {
        var products = await _adapter.GetAssociatedProductsAsync(productIds, ct);
        return products.Select(MapProduct).ToList();
    }

    public async Task<PurchaseResult> PurchaseAsync(string productId, string userGuid, CancellationToken ct = default)
    {
        // userGuid is not bound at purchase time on Windows; it is supplied to the Store as the
        // publisherUserId when the purchase-ID key is minted in GetPurchaseProofAsync.
        var result = await _adapter.RequestPurchaseAsync(productId, ct);
        return result.Outcome switch
        {
            StorePurchaseOutcome.Succeeded or StorePurchaseOutcome.AlreadyPurchased =>
                new PurchaseResult(PurchaseStatus.Success),
            StorePurchaseOutcome.NotPurchased =>
                new PurchaseResult(PurchaseStatus.UserCancelled),
            _ => new PurchaseResult(PurchaseStatus.Failed,
                result.ExtendedError ?? "The Microsoft Store purchase could not be completed.")
        };
    }

    public async Task<IReadOnlyList<StoreEntitlement>> GetCurrentEntitlementsAsync(CancellationToken ct = default)
    {
        var licenses = await _adapter.GetAddOnLicensesAsync(ct);
        return licenses.Select(MapEntitlement).ToList();
    }

    public Task RestoreAsync(CancellationToken ct = default)
    {
        // Windows has no in-app restore call: add-on licenses are bound to the signed-in Microsoft
        // account and reappear automatically once the correct account is signed in. The caller's
        // follow-up refresh re-reads entitlements, so there is nothing to do here.
        _logService.Log(LogLevel.Info, "Store restore on Windows is a no-op; entitlements are account-bound.");
        return Task.CompletedTask;
    }

    public async Task<PurchaseProof> GetPurchaseProofAsync(string userGuid, CancellationToken ct = default)
    {
        var entitlements = await GetCurrentEntitlementsAsync(ct);
        var current = entitlements.FirstOrDefault(e => e.State == EntitlementState.Active);
        if (current is null)
        {
            return null;
        }

        // The Worker mints the AAD ticket (it holds the secret); the client only forwards it to the
        // Store. No ticket -> no proof -> activation reports NotPurchased and offers retry.
        var ticket = await _activationClient.GetStoreTicketAsync(ct: ct);
        if (string.IsNullOrEmpty(ticket))
        {
            _logService.Log(LogLevel.Warn, "No store ticket available; cannot produce Microsoft purchase proof.");
            return null;
        }

        var key = await _adapter.GetCustomerPurchaseIdAsync(ticket, userGuid, ct);
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        return new PurchaseProof("microsoft", key, current.ProductId, userGuid);
    }

    public IReadOnlyList<string> CreditPackProductIds => StoreConfig.WindowsCreditPackStoreIds;

    public async Task<ConsumablePurchaseResult> PurchaseConsumableAsync(string productId, string userGuid,
        CancellationToken ct = default)
    {
        var purchase = await _adapter.RequestPurchaseAsync(productId, ct);
        if (purchase.Outcome is not (StorePurchaseOutcome.Succeeded or StorePurchaseOutcome.AlreadyPurchased))
        {
            return purchase.Outcome == StorePurchaseOutcome.NotPurchased
                ? new ConsumablePurchaseResult(PurchaseStatus.UserCancelled)
                : new ConsumablePurchaseResult(PurchaseStatus.Failed,
                    Error: purchase.ExtendedError ?? "The Microsoft Store purchase could not be completed.");
        }

        // Design section 10 "Credit packs" (step 10 correction): a consumable proof is a
        // *collections*-audience Microsoft Store ID key (GetCustomerCollectionsIdAsync), not the
        // purchase-audience key GetPurchaseProofAsync above uses for subscriptions — a different
        // key kind entirely, minted against the Worker's /store/ticket?purpose=collections ticket.
        var ticket = await _activationClient.GetStoreTicketAsync("collections", ct);
        if (string.IsNullOrEmpty(ticket))
        {
            _logService.Log(LogLevel.Warn, "No collections store ticket available; cannot produce Microsoft consumable proof.");
            return new ConsumablePurchaseResult(PurchaseStatus.Failed,
                Error: "Couldn't reach the store. Check your connection and try again.");
        }

        var key = await _adapter.GetCustomerCollectionsIdAsync(ticket, userGuid, ct);
        if (string.IsNullOrEmpty(key))
        {
            return new ConsumablePurchaseResult(PurchaseStatus.Failed,
                Error: "Couldn't reach the store. Check your connection and try again.");
        }

        return new ConsumablePurchaseResult(PurchaseStatus.Success, new PurchaseProof("microsoft", key, productId, userGuid));
    }

    public Task FinishConsumableAsync(string transactionId, CancellationToken ct = default)
    {
        // The Worker itself reports the consumable fulfilled to the Microsoft collection API
        // (design section 10's step 10 correction: POST /collections/consume); there is nothing
        // for the Windows client to finish, unlike Apple's local StoreKit transaction.
        return Task.CompletedTask;
    }

    private static StoreProduct MapProduct(StoreProductInfo p) =>
        new(p.ProductId, p.Title, p.Description, p.FormattedPrice, p.SubscriptionPeriodIso, p.HasTrial);

    private static StoreEntitlement MapEntitlement(StoreLicenseInfo lic)
    {
        // Active only when the OS licensing service says so AND any expiry is still in the future.
        // Windows exposes no grace-period or billing-retry detail client-side, so it is Active or Expired.
        var active = lic.IsActive && (lic.ExpirationDateUtc is null || lic.ExpirationDateUtc > DateTime.UtcNow);
        return new StoreEntitlement(
            lic.ProductId,
            string.Empty,   // no client-side transaction id on Windows
            string.Empty,   // no original-transaction id on Windows
            default,        // purchase date is not surfaced by the licensing service
            lic.ExpirationDateUtc,
            active ? EntitlementState.Active : EntitlementState.Expired,
            false,          // auto-renew is not reliably available client-side on Windows
            string.Empty);  // Windows proof is the purchase-ID key, not a JWS carried on the entitlement
    }

    // Re-evaluate centrally on any license change. The consumer ignores the payload and re-runs
    // proof->JWT itself, so raise the sentinel directly rather than paying a licensing round trip
    // to build a payload nothing reads.
    private void OnLicensesChanged() =>
        _ = _windowing.GlobalDispatcher.EnqueueAsync(() => EntitlementChanged?.Invoke(this, NoEntitlement));
}
