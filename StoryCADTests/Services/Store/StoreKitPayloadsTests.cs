using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Services.Store;

namespace StoryCADTests.Services.Store;

/// <summary>
///     Parses each StoreKit shim contract payload (devdocs/iap/shim-contract.md) into the public store
///     models, plus the error and malformed-body paths. Pure JSON mapping, so these run on any TFM.
/// </summary>
[TestClass]
public class StoreKitPayloadsTests
{
    private const string ProductsPayload =
        """{"ok":true,"products":[{"id":"org.storycad.collaborator.monthly","displayName":"Collaborator","description":"AI writing assistant","displayPrice":"£4.99","rawPrice":"4.99","currency":"GBP","subscriptionPeriod":"P1M","hasIntroOffer":false}]}""";

    private const string PurchaseSuccessPayload =
        """{"ok":true,"status":"success","transactionId":"2000000123","originalTransactionId":"2000000123","productId":"org.storycad.collaborator.monthly","jws":"header.body.sig"}""";

    private const string PurchaseCancelledPayload = """{"ok":true,"status":"userCancelled"}""";

    private const string EntitlementsPayload =
        """{"ok":true,"entitlements":[{"productId":"org.storycad.collaborator.monthly","transactionId":"2000000123","originalTransactionId":"2000000100","purchaseDate":"2026-07-01T12:00:00Z","expirationDate":"2026-08-01T12:00:00Z","state":"active","willAutoRenew":true,"jws":"header.body.sig"}]}""";

    private const string ErrorPayload =
        """{"ok":false,"error":"Cannot connect to the App Store","code":"ASDErrorDomain-509"}""";

    private const string MalformedPayload = "{ this is not json";

    [TestMethod]
    public void ParseProducts_ValidPayload_MapsAllFields()
    {
        var products = StoreKitPayloads.ParseProducts(ProductsPayload);

        Assert.AreEqual(1, products.Count);
        var p = products[0];
        Assert.AreEqual("org.storycad.collaborator.monthly", p.Id);
        Assert.AreEqual("Collaborator", p.DisplayName);
        Assert.AreEqual("AI writing assistant", p.Description);
        Assert.AreEqual("£4.99", p.DisplayPrice);
        Assert.AreEqual("P1M", p.SubscriptionPeriod);
        Assert.IsFalse(p.HasIntroOffer);
    }

    [TestMethod]
    public void ParseProducts_ErrorPayload_ReturnsEmpty()
    {
        Assert.AreEqual(0, StoreKitPayloads.ParseProducts(ErrorPayload).Count);
    }

    [TestMethod]
    public void ParsePurchase_Success_ReturnsSuccess()
    {
        var result = StoreKitPayloads.ParsePurchase(PurchaseSuccessPayload);

        Assert.AreEqual(PurchaseStatus.Success, result.Status);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void ParsePurchase_UserCancelled_ReturnsUserCancelled()
    {
        Assert.AreEqual(PurchaseStatus.UserCancelled, StoreKitPayloads.ParsePurchase(PurchaseCancelledPayload).Status);
    }

    [TestMethod]
    public void ParsePurchase_ErrorPayload_ReturnsFailedWithMessage()
    {
        var result = StoreKitPayloads.ParsePurchase(ErrorPayload);

        Assert.AreEqual(PurchaseStatus.Failed, result.Status);
        Assert.AreEqual("Cannot connect to the App Store", result.Error);
    }

    [TestMethod]
    public void ParsePurchase_MalformedPayload_ReturnsFailed()
    {
        Assert.AreEqual(PurchaseStatus.Failed, StoreKitPayloads.ParsePurchase(MalformedPayload).Status);
    }

    [TestMethod]
    public void ParseEntitlements_ActiveSubscription_MapsAllFields()
    {
        var entitlements = StoreKitPayloads.ParseEntitlements(EntitlementsPayload);

        Assert.AreEqual(1, entitlements.Count);
        var e = entitlements[0];
        Assert.AreEqual("org.storycad.collaborator.monthly", e.ProductId);
        Assert.AreEqual("2000000123", e.TransactionId);
        Assert.AreEqual("2000000100", e.OriginalTransactionId, "originalTransactionId is the renewal-stable key");
        Assert.AreEqual(EntitlementState.Active, e.State);
        Assert.IsTrue(e.WillAutoRenew);
        Assert.AreEqual("header.body.sig", e.Jws);
        Assert.AreEqual(new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc), e.PurchaseDateUtc);
        Assert.IsTrue(e.ExpirationDateUtc.HasValue);
        Assert.AreEqual(new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc), e.ExpirationDateUtc.Value);
    }

    [TestMethod]
    public void ParseEntitlements_ErrorPayload_ReturnsEmpty()
    {
        Assert.AreEqual(0, StoreKitPayloads.ParseEntitlements(ErrorPayload).Count);
    }

    [TestMethod]
    public void ParseEntitlements_GracePeriodState_MapsToGracePeriod()
    {
        const string payload =
            """{"ok":true,"entitlements":[{"productId":"p","state":"gracePeriod","willAutoRenew":false}]}""";

        Assert.AreEqual(EntitlementState.GracePeriod, StoreKitPayloads.ParseEntitlements(payload)[0].State);
    }

    [TestMethod]
    public void ParseEntitlements_UnknownState_MapsToExpired()
    {
        // Fail closed: a state this client doesn't recognise must never grant access.
        const string payload =
            """{"ok":true,"entitlements":[{"productId":"p","state":"someFutureState"}]}""";

        Assert.AreEqual(EntitlementState.Expired, StoreKitPayloads.ParseEntitlements(payload)[0].State);
    }

    [TestMethod]
    public void SerializeProductIds_TwoIds_ProducesJsonArray()
    {
        var json = StoreKitPayloads.SerializeProductIds(new[] { "a.monthly", "b.annual" });

        Assert.AreEqual("[\"a.monthly\",\"b.annual\"]", json);
        // Round-trips back through the products path shape.
        CollectionAssert.AreEqual(new[] { "a.monthly", "b.annual" },
            System.Text.Json.JsonSerializer.Deserialize<string[]>(json));
    }
}
