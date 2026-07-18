namespace StoryCADLib.Services.Store;

/// <summary>
///     Static configuration for the Collaborator subscription (issue #30). The store page is
///     data-driven, so it renders whatever <see cref="ProductIds" /> resolve to at runtime; the legal
///     URLs are still placeholders until the real pages exist and Terry signs off on the copy.
/// </summary>
public static class StoreConfig
{
    // Apple App Store Connect product ID for the monthly Collaborator subscription (subscription group
    // "Collaborator", 22215401). StoreKit looks products up by this ID. Immutable once created.
    public static readonly IReadOnlyList<string> AppleProductIds = new[] { "Collaborator1m" };

    // Microsoft Store ID(s) for the Collaborator subscription add-on (Partner Center). The Windows
    // adapter matches Durable add-ons by StoreProduct.StoreId against these — the add-on's custom
    // Product ID (InAppOfferToken) differs from Apple's, so we key off the canonical Store ID.
    public static readonly IReadOnlyList<string> WindowsStoreIds = new[] { "9PF1LZ07MSH6" };

    // The active platform's identifier scheme is exposed by IStoreService.ProductIds (each
    // implementation returns the list it understands), so the platform switch lives only in the
    // DI registration.

    // TODO #30: replace with the real hosted pages before store submission.
    public const string TermsOfUseUrl = "https://storybuilder.org/terms";
    public const string PrivacyPolicyUrl = "https://storybuilder.org/privacy";

    // Credit-pack (consumable) product IDs (issue #90 design section 10 "Credit packs", step 10).
    // Placeholders: pack sizes, prices, and the credit exchange rate are gate 3 decisions, not yet
    // made (devdocs/store_submissions.md). Must match the Worker's PRODUCT_MAP entries verbatim
    // (proxy/src/index.js, Collaborator repo) or a genuine purchase will refuse with "invalid" —
    // the Worker only recognizes a signed proof for a product it has in its own table.
    public static readonly IReadOnlyList<string> AppleCreditPackProductIds = new[] { "CollabCreditPack500" };
    public static readonly IReadOnlyList<string> WindowsCreditPackStoreIds = new[] { "9PCREDITPAK1" };

    // Shown by the workflow and chat callers when the Worker refuses an LLM call with 429 because
    // the caller's balance is at or below zero (issue #90 design section 10 "The cutoff"; ruling of
    // 2026-07-15, step 10). The Worker's 429 shape is identical for this case and the unrelated
    // daily-rate-limit case (design section 10: "the existing 429 quota response"), so this message
    // is shown for either — a deliberate simplification the wire contract does not distinguish.
    public const string OutOfCreditsMessage =
        "You've used all your credits for this period. Buy more from Collaborator's Buy Credits " +
        "screen, or wait for your next monthly renewal.";
}
