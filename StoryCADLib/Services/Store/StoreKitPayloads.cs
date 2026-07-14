using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace StoryCADLib.Services.Store;

// Wire DTOs and mapping for the StoreKit shim's JSON contracts (shim contract in
// StoryCADWiki: wiki/repos/Collaborator/sources/iap-billing-docs.md).
// Platform-agnostic on purpose: no P/Invoke here, so these map cleanly and the unit tests run on
// any TFM. The native calls live in StoreKitInterop (macOS/desktop only).

internal sealed class ShimProduct
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("displayName")] public string DisplayName { get; set; }
    [JsonPropertyName("description")] public string Description { get; set; }
    [JsonPropertyName("displayPrice")] public string DisplayPrice { get; set; }
    // The shim also emits rawPrice/currency (shim-contract.md); nothing consumes them, so they
    // are not mapped — System.Text.Json ignores unknown members.
    [JsonPropertyName("subscriptionPeriod")] public string SubscriptionPeriod { get; set; }
    [JsonPropertyName("hasIntroOffer")] public bool HasIntroOffer { get; set; }
}

internal sealed class ShimProductsResponse
{
    [JsonPropertyName("ok")] public bool Ok { get; set; }
    [JsonPropertyName("products")] public List<ShimProduct> Products { get; set; }
    [JsonPropertyName("error")] public string Error { get; set; }
    [JsonPropertyName("code")] public string Code { get; set; }
}

internal sealed class ShimEntitlement
{
    [JsonPropertyName("productId")] public string ProductId { get; set; }
    [JsonPropertyName("transactionId")] public string TransactionId { get; set; }
    [JsonPropertyName("originalTransactionId")] public string OriginalTransactionId { get; set; }
    [JsonPropertyName("purchaseDate")] public string PurchaseDate { get; set; }
    [JsonPropertyName("expirationDate")] public string ExpirationDate { get; set; }
    [JsonPropertyName("state")] public string State { get; set; }
    [JsonPropertyName("willAutoRenew")] public bool WillAutoRenew { get; set; }
    [JsonPropertyName("jws")] public string Jws { get; set; }
}

internal sealed class ShimEntitlementsResponse
{
    [JsonPropertyName("ok")] public bool Ok { get; set; }
    [JsonPropertyName("entitlements")] public List<ShimEntitlement> Entitlements { get; set; }
    [JsonPropertyName("error")] public string Error { get; set; }
    [JsonPropertyName("code")] public string Code { get; set; }
}

internal sealed class ShimPurchaseResponse
{
    [JsonPropertyName("ok")] public bool Ok { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; }
    [JsonPropertyName("transactionId")] public string TransactionId { get; set; }
    [JsonPropertyName("originalTransactionId")] public string OriginalTransactionId { get; set; }
    [JsonPropertyName("productId")] public string ProductId { get; set; }
    [JsonPropertyName("jws")] public string Jws { get; set; }
    [JsonPropertyName("error")] public string Error { get; set; }
    [JsonPropertyName("code")] public string Code { get; set; }
}

// Source-generated context: the desktop head publishes trimming-friendly, so no reflection-based
// serialisation for these types.
[JsonSerializable(typeof(ShimProductsResponse))]
[JsonSerializable(typeof(ShimEntitlementsResponse))]
[JsonSerializable(typeof(ShimPurchaseResponse))]
[JsonSerializable(typeof(string[]))]
internal partial class StoreKitJsonContext : JsonSerializerContext
{
}

internal static class StoreKitPayloads
{
    public static string SerializeProductIds(IReadOnlyList<string> ids) =>
        JsonSerializer.Serialize(ids as string[] ?? ids.ToArray(), StoreKitJsonContext.Default.StringArray);

    public static IReadOnlyList<StoreProduct> ParseProducts(string json)
    {
        var resp = Deserialize(json, StoreKitJsonContext.Default.ShimProductsResponse);
        if (resp?.Products is null)
        {
            return Array.Empty<StoreProduct>();
        }

        return resp.Products.Select(p => new StoreProduct(
            p.Id ?? string.Empty, p.DisplayName ?? string.Empty, p.Description ?? string.Empty,
            p.DisplayPrice ?? string.Empty, p.SubscriptionPeriod ?? string.Empty, p.HasIntroOffer)).ToList();
    }

    public static PurchaseResult ParsePurchase(string json)
    {
        var resp = Deserialize(json, StoreKitJsonContext.Default.ShimPurchaseResponse);
        if (resp is null)
        {
            return new PurchaseResult(PurchaseStatus.Failed, "Unparseable purchase response.");
        }

        if (!resp.Ok)
        {
            return new PurchaseResult(PurchaseStatus.Failed, resp.Error ?? "Purchase failed.");
        }

        return new PurchaseResult(MapPurchaseStatus(resp.Status));
    }

    public static IReadOnlyList<StoreEntitlement> ParseEntitlements(string json)
    {
        var resp = Deserialize(json, StoreKitJsonContext.Default.ShimEntitlementsResponse);
        if (resp?.Entitlements is null)
        {
            return Array.Empty<StoreEntitlement>();
        }

        return resp.Entitlements.Select(MapEntitlement).ToList();
    }

    private static T Deserialize<T>(string json, JsonTypeInfo<T> typeInfo) where T : class
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(json, typeInfo);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static StoreEntitlement MapEntitlement(ShimEntitlement e) => new(
        e.ProductId ?? string.Empty,
        e.TransactionId ?? string.Empty,
        e.OriginalTransactionId ?? string.Empty,
        ParseDate(e.PurchaseDate) ?? DateTime.MinValue,
        ParseDate(e.ExpirationDate),
        MapState(e.State),
        e.WillAutoRenew,
        e.Jws ?? string.Empty);

    private static PurchaseStatus MapPurchaseStatus(string status) => status switch
    {
        "success" => PurchaseStatus.Success,
        "userCancelled" => PurchaseStatus.UserCancelled,
        "pending" => PurchaseStatus.Pending,
        _ => PurchaseStatus.Failed
    };

    private static EntitlementState MapState(string state) => state switch
    {
        "active" => EntitlementState.Active,
        "gracePeriod" => EntitlementState.GracePeriod,
        "billingRetry" => EntitlementState.BillingRetry,
        "expired" => EntitlementState.Expired,
        "revoked" => EntitlementState.Revoked,
        // An unknown state is safest treated as no access rather than granting it.
        _ => EntitlementState.Expired
    };

    // Shared wire-date rule (ISO-8601 string -> UTC DateTime, null on absent/malformed); also
    // used by ProxyActivationClient for the Worker's expiresAt so the rule has one home.
    internal static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
            out var dto)
            ? dto.UtcDateTime
            : null;
    }
}
