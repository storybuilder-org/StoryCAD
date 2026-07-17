using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.DAL;
using StoryCADLib.Services.Locking;
using StoryCADLib.Services.Messages;

namespace StoryCADLib.Services.Store;

/// <summary>
///     Shared activation orchestrator across platforms. Takes store proof, exchanges it with
///     the Worker for a short-lived JWT, and exposes the <see cref="ActivationState" /> the UI
///     gates on. Only <see cref="IStoreService" /> differs per platform.
/// </summary>
public sealed class StoreActivationService : IStoreActivationService
{
    // Clock-skew allowance when treating a cached JWT as still valid.
    private static readonly TimeSpan ExpiryLeeway = TimeSpan.FromMinutes(1);

    // Used when the Worker omits expiresAt (contract violation; the contract says ~12h).
    // Short and conservative: the JWT stays usable for the session without trusting an
    // unstated lifetime, and the next launch re-verifies.
    private static readonly TimeSpan FallbackJwtLifetime = TimeSpan.FromHours(1);

    private readonly IStoreService _store;
    private readonly IActivationClient _client;
    private readonly PreferenceService _preferenceService;
    private readonly Windowing _windowing;
    private readonly ILogService _logService;

    // Serialises activation so a startup Initialize and an out-of-band EntitlementChanged
    // can't interleave and clobber state.
    private readonly SemaphoreSlim _gate = new(1, 1);

    private ActivationState _state = ActivationState.NotPurchased;
    private string _jwt = string.Empty;
    private DateTime _expiryUtc = DateTime.MinValue;

    public StoreActivationService(IStoreService store, IActivationClient client,
        PreferenceService preferenceService, Windowing windowing, ILogService logService)
    {
        _store = store;
        _client = client;
        _preferenceService = preferenceService;
        _windowing = windowing;
        _logService = logService;
        _store.EntitlementChanged += OnStoreEntitlementChanged;
    }

    public ActivationState State => _state;

    public event EventHandler<ActivationState> StateChanged;

    public string CurrentJwt => string.IsNullOrEmpty(_jwt) ? null : _jwt;

    // The stable per-user GUID, generated on this machine at startup (issue #90 D8) and embedded
    // in the signed proof at purchase time. Provisioned by
    // PreferenceService.EnsureUserGuidProvisionedAsync before this service's InitializeAsync runs,
    // so it is never empty in practice; defensively empty-coalesced below.
    private string UserGuid => _preferenceService.Model.StoreUserGuid ?? string.Empty;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        // A cached, unexpired JWT activates without a Worker round-trip (survives offline launch
        // after one online success).
        _jwt = _preferenceService.Model.StoreActivationJwt ?? string.Empty;
        _expiryUtc = _preferenceService.Model.StoreActivationJwtExpiry;

        if (HasValidCachedJwt())
        {
            SetState(ActivationState.Active);
            return;
        }

        // Startup path: activation must never crash or fault the launch, so the guarantee lives
        // here rather than at every call site.
        try
        {
            await RefreshActivationAsync(ct);
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Store activation initialization failed: {ex.Message}");
        }
    }

    public async Task<PurchaseResult> PurchaseAndActivateAsync(string productId, CancellationToken ct = default)
    {
        var result = await _store.PurchaseAsync(productId, UserGuid, ct);
        switch (result.Status)
        {
            case PurchaseStatus.Success:
                await RefreshActivationAsync(ct);
                break;
            case PurchaseStatus.Pending:
                // Ask to Buy: completion arrives later through EntitlementChanged.
                SetState(ActivationState.PendingStore);
                break;
            case PurchaseStatus.UserCancelled:
            case PurchaseStatus.Failed:
                // Leave state unchanged; the caller surfaces the outcome to the user.
                break;
        }

        return result;
    }

    public async Task RestoreAsync(CancellationToken ct = default)
    {
        await _store.RestoreAsync(ct);
        await RefreshActivationAsync(ct);
    }

    // Proof -> Worker -> JWT. Shared by startup, purchase, restore, and entitlement changes.
    private async Task RefreshActivationAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            if (string.IsNullOrEmpty(UserGuid))
            {
                // Defensive: the startup provisioning step (issue #90 D8) should have populated
                // StoreUserGuid before this runs. Log so an unexpected empty value -- proofs
                // carrying it cannot be bound to a user -- is diagnosable in the field.
                _logService.Log(LogLevel.Warn,
                    "StoreUserGuid is empty; purchase proof cannot be bound to a user.");
            }

            // Dev/tester allowlist activation (issue #90 D7/D8): COLLAB_DEV_ACTIVATION routes around
            // the platform store entirely -- there is no store proof to present, only the GUID a
            // human approved on the Worker's allowlist. payload/productId are ignored by the
            // Worker for platform "dev" (design section 6).
            var proof = IsDevActivationEnabled()
                ? new PurchaseProof("dev", string.Empty, string.Empty, UserGuid)
                : await _store.GetPurchaseProofAsync(UserGuid, ct);
            if (proof is null)
            {
                // Authoritative "no purchase" from the store: drop any cached JWT.
                SetJwt(string.Empty, DateTime.MinValue);
                SetState(ActivationState.NotPurchased);
                return;
            }

            ActivationResponse response;
            try
            {
                response = await _client.ActivateAsync(proof, ct);
            }
            catch (OperationCanceledException)
            {
                // App shutting down or the caller cancelled: not a network failure. Leave state
                // untouched and return quietly rather than flashing an "unreachable" warning.
                return;
            }
            catch (Exception ex)
            {
                // No offline mode: Collaborator needs the Worker to function at all. Surface the
                // condition. Keep a still-valid cached JWT through a transient blip (don't deny a
                // paying user); otherwise fall back to NotPurchased so the UI shows the retry path.
                _logService.Log(LogLevel.Warn, $"Store activation unreachable: {ex.Message}");
                WeakReferenceMessenger.Default.Send(new StatusChangedMessage(
                    new StatusMessage("Store activation unreachable. Check your connection and try again.",
                        LogLevel.Warn, true)));

                FallBackToCachedJwtOrNotPurchased();
                return;
            }

            if (response.Ok)
            {
                if (!string.IsNullOrEmpty(response.Jwt))
                {
                    SetJwt(response.Jwt, response.ExpiresAtUtc ?? DateTime.UtcNow.Add(FallbackJwtLifetime));
                    SetState(ActivationState.Active);
                    return;
                }

                // ok:true but no token is a malformed success, not an authenticated refusal.
                _logService.Log(LogLevel.Warn, "Activation response was ok but carried no JWT; treating as transient.");
                FallBackToCachedJwtOrNotPurchased();
                return;
            }

            // Refusal. Revoked/invalid -> access denied; expired -> lapsed, offer resubscribe.
            SetJwt(string.Empty, DateTime.MinValue);
            SetState(response.Reason == "expired" ? ActivationState.NotPurchased : ActivationState.Refused);
        }
        finally
        {
            _gate.Release();
        }
    }

    // COLLAB_DEV_ACTIVATION is a routing hint only (design section 12): it says "activate through the
    // dev/tester allowlist instead of the platform store"; it grants nothing by itself -- the
    // Worker's allowlist row decides dev entitlement.
    private static bool IsDevActivationEnabled() =>
        Environment.GetEnvironmentVariable("COLLAB_DEV_ACTIVATION") == "1";

    /// <summary>
    ///     Re-presents proof to the Worker on demand (issue #90 step 8 item 7): used by a workflow
    ///     caller that gets a 401 to refresh once before retrying, covering a session that outlives
    ///     the ~12h JWT between the launch/purchase/restore/entitlement-change refresh points above.
    ///     Shares <see cref="RefreshActivationAsync" /> with those callers, including its gate.
    /// </summary>
    public Task ReactivateAsync(CancellationToken ct = default) => RefreshActivationAsync(ct);

    private async void OnStoreEntitlementChanged(object sender, StoreEntitlement e)
    {
        // Renewals, completed Ask-to-Buy purchases, and revocations all re-evaluate centrally.
        try
        {
            await RefreshActivationAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Entitlement-change refresh failed: {ex.Message}");
        }
    }

    // Additive comparison: `_expiryUtc - ExpiryLeeway` would throw on DateTime.MinValue
    // (e.g. a hand-edited or truncated preferences file); `UtcNow + leeway` cannot underflow.
    private bool HasValidCachedJwt() =>
        !string.IsNullOrEmpty(_jwt) && _expiryUtc > DateTime.UtcNow + ExpiryLeeway;

    // Transient-failure policy shared by the unreachable and malformed-success paths: a
    // still-valid cached JWT keeps a paying user Active through a blip; otherwise clear
    // and surface the retry path.
    private void FallBackToCachedJwtOrNotPurchased()
    {
        if (HasValidCachedJwt())
        {
            return;
        }

        SetJwt(string.Empty, DateTime.MinValue);
        SetState(ActivationState.NotPurchased);
    }

    // Single writer for the JWT cache. Persists only on change, so a clear that clears
    // nothing (the common startup path for non-purchasers) never touches the disk.
    private void SetJwt(string jwt, DateTime expiryUtc)
    {
        _jwt = jwt ?? string.Empty;
        _expiryUtc = expiryUtc;

        var model = _preferenceService.Model;
        if ((model.StoreActivationJwt ?? string.Empty) == _jwt && model.StoreActivationJwtExpiry == expiryUtc)
        {
            return;
        }

        model.StoreActivationJwt = _jwt;
        model.StoreActivationJwtExpiry = expiryUtc;
        PersistPreferences();
    }

    // Best-effort flush so the JWT survives relaunch; matches the repo's `new PreferencesIo()`
    // usage (Windowing, FileOpenVM). Fire-and-forget with faults observed so a disk failure can
    // never break activation or crash on finalization. Treat the JWT as a cache, not a secret.
    private void PersistPreferences()
    {
        try
        {
            var writeTask = new PreferencesIo().WritePreferences(_preferenceService.Model);
            writeTask.ContinueWith(
                t => _logService.Log(LogLevel.Warn,
                    $"Failed to persist store activation token: {t.Exception?.GetBaseException().Message}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Failed to persist store activation token: {ex.Message}");
        }
    }

    private void SetState(ActivationState newState)
    {
        if (_state == newState)
        {
            return;
        }

        _state = newState;
        var handler = StateChanged;
        if (handler is null)
        {
            return;
        }

        // Raise on the UI thread; EnqueueAsync runs inline when there is no dispatcher (headless/tests).
        _ = _windowing.GlobalDispatcher.EnqueueAsync(() => handler(this, newState));
    }
}
