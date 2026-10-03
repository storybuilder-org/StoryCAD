using System.Threading;

namespace StoryCADLib.Services.Store;

/// <summary>
///     Activation state the UI gates on. The store's local entitlement drives this;
///     the Worker-issued JWT is the actual enforcement boundary on every Collaborator call.
/// </summary>
public enum ActivationState
{
    /// <summary>No verified purchase; show the purchase entry point.</summary>
    NotPurchased,

    /// <summary>Purchase is awaiting approval (Ask to Buy); completion arrives out of band.</summary>
    PendingStore,

    /// <summary>A valid Worker JWT is held; Collaborator is unlocked.</summary>
    Active,

    /// <summary>The Worker refused the proof (invalid or revoked); access is denied.</summary>
    Refused
}

/// <summary>
///     Orchestrates activation: takes proof from <see cref="IStoreService" />, exchanges it
///     with the Worker for a short-lived JWT, exposes the <see cref="ActivationState" /> the
///     UI gates on, and refreshes the JWT as it expires. One shared implementation across
///     platforms; only <see cref="IStoreService" /> differs per platform.
/// </summary>
public interface IStoreActivationService
{
    ActivationState State { get; }

    event EventHandler<ActivationState> StateChanged;

    /// <summary>Startup path: use a cached unexpired JWT, else re-present proof to the Worker.</summary>
    Task InitializeAsync(CancellationToken ct = default);

    Task<PurchaseResult> PurchaseAndActivateAsync(string productId, CancellationToken ct = default);

    Task RestoreAsync(CancellationToken ct = default);

    /// <summary>
    ///     Join: POST <c>platform: "dev"</c> with <c>enroll: true</c>. Returns the Worker
    ///     response. Caches the JWT only when <see cref="ActivationResponse.Ok" /> and a JWT
    ///     are present. Transport failure throws <see cref="StoreActivationUnreachableException" />.
    /// </summary>
    Task<ActivationResponse> EnrollBetaAsync(CancellationToken ct = default);

    /// <summary>
    ///     Refresh an approved allowlist row: POST <c>platform: "dev"</c> with enroll false.
    /// </summary>
    Task RefreshAllowlistAsync(CancellationToken ct = default);

    /// <summary>True when the held JWT <c>pur</c> claim starts with <c>allowlist:</c>.</summary>
    bool IsAllowlistActivation { get; }

    /// <summary>The current Worker JWT, attached to Collaborator calls; null when not active.</summary>
    string CurrentJwt { get; }

    /// <summary>
    ///     Re-presents proof to the Worker on demand (issue #90 step 8 item 7). Used by a workflow
    ///     caller that gets a 401 to refresh once before retrying, covering a session that outlives
    ///     the ~12h JWT between the launch/purchase/restore/entitlement-change refresh points above.
    /// </summary>
    Task ReactivateAsync(CancellationToken ct = default);
}
