using System.Threading;

namespace StoryCADLib.Services.Store;

/// <summary>
///     Result of a Worker <c>/activate</c> call. On success <see cref="Ok" /> is true and
///     <see cref="Jwt" /> / <see cref="ExpiresAtUtc" /> are set; on refusal <see cref="Ok" />
///     is false and <see cref="Reason" /> is one of "invalid", "revoked", "expired",
///     "enrollment_closed", or "cap_reached".
/// </summary>
public record ActivationResponse(bool Ok, string Jwt, DateTime? ExpiresAtUtc, string Reason);

/// <summary>
///     Result of <c>POST /v1/beta-enrollment</c>. <see cref="Status" /> is the allowlist
///     row status for this GUID, or null when there is no row.
/// </summary>
public record BetaEnrollmentStatus(bool Open, bool UnderCap, string Status);

/// <summary>
///     Client seam for the Worker activation handshake. Implementations POST purchase proof
///     to the Worker <c>/activate</c> endpoint and return the parsed response. A transport
///     failure (Worker unreachable, timeout, unexpected status) is thrown, not returned, so
///     the caller can distinguish "unreachable" from an authenticated refusal.
/// </summary>
public interface IActivationClient
{
    Task<ActivationResponse> ActivateAsync(PurchaseProof proof, CancellationToken ct = default);

    /// <summary>
    ///     SELECT-only enrollment window check. Posts <c>{ "userGuid" }</c> to
    ///     <c>/beta-enrollment</c>. Does not insert an allowlist row. Transport failure throws
    ///     <see cref="StoreActivationUnreachableException" />.
    /// </summary>
    Task<BetaEnrollmentStatus> GetBetaEnrollmentAsync(string userGuid, CancellationToken ct = default);

    /// <summary>
    ///     Windows only: fetches the short-lived Azure AD service ticket the client passes to a
    ///     <c>StoreContext</c> key-minting call. The Worker holds the AAD client secret and mints
    ///     the ticket; the client never sees the secret. Returns null when the ticket cannot be
    ///     obtained (unauthenticated build, endpoint down) — the caller then produces no proof
    ///     rather than throwing. Never called on macOS.
    /// </summary>
    /// <param name="purpose">
    ///     "purchase" (default) mints the ticket for <c>StoreContext.GetCustomerPurchaseIdAsync</c>
    ///     (subscriptions); "collections" mints the *different* ticket audience
    ///     <c>StoreContext.GetCustomerCollectionsIdAsync</c> needs for a consumable purchase (issue
    ///     #90 design section 10 "Credit packs", step 10 correction). Maps to the Worker's
    ///     <c>/store/ticket?purpose=collections</c> query parameter.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> GetStoreTicketAsync(string purpose = "purchase", CancellationToken ct = default);
}
