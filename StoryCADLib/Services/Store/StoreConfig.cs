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
}
