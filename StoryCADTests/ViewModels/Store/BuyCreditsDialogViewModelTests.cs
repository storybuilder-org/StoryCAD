using System.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Services;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Store;
using StoryCADLib.ViewModels.Store;
using StoryCADTests.Services.Store;

#nullable disable

namespace StoryCADTests.ViewModels.Store;

/// <summary>
///     Headless logic tests for <see cref="BuyCreditsDialogViewModel" /> (issue #90 design section
///     10 "Credit packs", step 10) with a fake store and a fake activation client: pack loading,
///     the buy path (purchase -> activate -> finish, in that order), and the refusal/error paths.
///     The dialog XAML itself is not exercised here (no UI testing), mirroring
///     SubscribeDialogViewModelTests.
/// </summary>
[TestClass]
public class BuyCreditsDialogViewModelTests
{
    private const string PackId = "CollabCreditPack500";
    private static readonly StoreProduct Pack500 = new(PackId, "500 Credits", "One-time credit pack", "$5.00", "", false);

    private static BuyCreditsDialogViewModel Create(FakeStoreService store, FakeActivationClient client) =>
        new(store, client, Ioc.Default.GetRequiredService<PreferenceService>(), Ioc.Default.GetRequiredService<ILogService>());

    [TestMethod]
    public async Task LoadAsync_WithPacks_PopulatesPacksAndSelectsFirst()
    {
        // FakeStoreService.GetProductsAsync (StoreTestDoubles.cs) returns Products regardless of
        // which id list LoadAsync passes it, so setting Products is what drives this test.
        var store = new FakeStoreService { Products = new[] { Pack500 } };
        var vm = Create(store, new FakeActivationClient());

        await vm.LoadAsync();

        Assert.AreEqual(1, vm.Packs.Count);
        Assert.AreEqual(Pack500, vm.SelectedPack);
        Assert.IsTrue(vm.HasPacks);
        Assert.IsFalse(vm.HasError);
    }

    [TestMethod]
    public async Task LoadAsync_StoreThrows_SetsError()
    {
        var vm = Create(new FakeStoreService { ThrowOnGetProducts = true }, new FakeActivationClient());

        await vm.LoadAsync();

        Assert.IsTrue(vm.HasError);
        Assert.IsFalse(string.IsNullOrEmpty(vm.StatusMessage));
        Assert.IsFalse(vm.HasPacks);
    }

    [TestMethod]
    public async Task LoadAsync_PacksAlreadyLoaded_SkipsStoreQuery()
    {
        var store = new FakeStoreService { Products = new[] { Pack500 } };
        var vm = Create(store, new FakeActivationClient());

        await vm.LoadAsync();
        await vm.LoadAsync();

        Assert.AreEqual(1, store.GetProductsCallCount, "packs are static per session; reopen must not refetch");
        Assert.AreEqual(1, vm.Packs.Count);
    }

    [TestMethod]
    public async Task BuyAsync_NoSelectedPack_ReturnsFalse()
    {
        var vm = Create(new FakeStoreService(), new FakeActivationClient());

        var ok = await vm.BuyAsync();

        Assert.IsFalse(ok);
    }

    [TestMethod]
    public async Task BuyAsync_SuccessAndActivated_ReturnsTrueAndFinishes()
    {
        var proof = new PurchaseProof("apple", "pack-jws", PackId, "user-guid-1");
        var store = new FakeStoreService
        {
            Products = new[] { Pack500 },
            ConsumableResult = new ConsumablePurchaseResult(PurchaseStatus.Success, proof, "transaction-1")
        };
        var client = new FakeActivationClient { Response = new ActivationResponse(true, "jwt", DateTime.UtcNow.AddHours(12), null) };
        var vm = Create(store, client);
        await vm.LoadAsync();

        var ok = await vm.BuyAsync();

        Assert.IsTrue(ok);
        Assert.IsFalse(vm.HasError);
        Assert.IsFalse(vm.IsBusy);
        Assert.AreEqual(1, client.ActivateCallCount);
        // The Worker's 200 must be seen (ActivateAsync called) before the transaction is finished
        // (design section 10: "the client finishes the transaction only after the Worker's 200").
        Assert.AreEqual(1, store.FinishConsumableCallCount);
        Assert.AreEqual("transaction-1", store.LastFinishedTransactionId);
    }

    [TestMethod]
    public async Task BuyAsync_ActivationRefused_ReturnsFalseAndDoesNotFinish()
    {
        var proof = new PurchaseProof("apple", "pack-jws", PackId, "user-guid-1");
        var store = new FakeStoreService
        {
            Products = new[] { Pack500 },
            ConsumableResult = new ConsumablePurchaseResult(PurchaseStatus.Success, proof, "transaction-1")
        };
        var client = new FakeActivationClient { Response = new ActivationResponse(false, null, null, "invalid") };
        var vm = Create(store, client);
        await vm.LoadAsync();

        var ok = await vm.BuyAsync();

        Assert.IsFalse(ok);
        Assert.IsTrue(vm.HasError);
        // A refused activation must not finish the transaction -- the Worker never credited it, so
        // there is nothing to consume yet, and finishing early is exactly what design section 10
        // forbids (a StoreKit-finished transaction cannot be retried).
        Assert.AreEqual(0, store.FinishConsumableCallCount);
    }

    [TestMethod]
    public async Task BuyAsync_ActivationUnreachable_ReturnsFalseAndDoesNotFinish()
    {
        var proof = new PurchaseProof("apple", "pack-jws", PackId, "user-guid-1");
        var store = new FakeStoreService
        {
            Products = new[] { Pack500 },
            ConsumableResult = new ConsumablePurchaseResult(PurchaseStatus.Success, proof, "transaction-1")
        };
        var client = new FakeActivationClient { ThrowOnActivate = new Exception("network down") };
        var vm = Create(store, client);
        await vm.LoadAsync();

        var ok = await vm.BuyAsync();

        Assert.IsFalse(ok);
        Assert.IsTrue(vm.HasError);
        Assert.AreEqual(0, store.FinishConsumableCallCount);
    }

    [TestMethod]
    public async Task BuyAsync_RetryAfterActivationUnreachable_ReactivatesHeldProofWithoutRepurchase()
    {
        // 2026-07-16 review finding 3: BuyAsync always started with a fresh
        // PurchaseConsumableAsync, so a retry after the Worker was unreachable purchased AGAIN --
        // on Apple, a second Product.purchase() on a consumable double-charges or errors on the
        // unfinished transaction. The money from the first purchase is already spent and the
        // proof it produced is still good, so a retry must re-activate that held proof instead.
        var proof = new PurchaseProof("apple", "pack-jws", PackId, "user-guid-1");
        var store = new FakeStoreService
        {
            Products = new[] { Pack500 },
            ConsumableResult = new ConsumablePurchaseResult(PurchaseStatus.Success, proof, "transaction-1")
        };
        var client = new FakeActivationClient { ThrowOnActivate = new Exception("network down") };
        var vm = Create(store, client);
        await vm.LoadAsync();

        var firstAttempt = await vm.BuyAsync();
        Assert.IsFalse(firstAttempt);
        Assert.AreEqual(1, store.PurchaseConsumableCallCount);
        Assert.AreEqual(1, client.ActivateCallCount);
        Assert.AreEqual(0, store.FinishConsumableCallCount);

        // The Worker becomes reachable again; the user clicks Buy a second time.
        client.ThrowOnActivate = null;
        client.Response = new ActivationResponse(true, "jwt-token", DateTime.UtcNow.AddHours(12), null);

        var secondAttempt = await vm.BuyAsync();

        Assert.IsTrue(secondAttempt);
        Assert.AreEqual(1, store.PurchaseConsumableCallCount, "the retry must not purchase a second time");
        Assert.AreEqual(2, client.ActivateCallCount);
        Assert.AreEqual(1, store.FinishConsumableCallCount);
        Assert.AreEqual("transaction-1", store.LastFinishedTransactionId);
    }

    [TestMethod]
    public async Task BuyAsync_RetryAfterActivationRefused_ReactivatesHeldProof()
    {
        // Same shape as the unreachable-retry test above, for the other retryable reason: a
        // refusal (the Worker's own 403/503) rather than a transport failure.
        var proof = new PurchaseProof("apple", "pack-jws", PackId, "user-guid-1");
        var store = new FakeStoreService
        {
            Products = new[] { Pack500 },
            ConsumableResult = new ConsumablePurchaseResult(PurchaseStatus.Success, proof, "transaction-1")
        };
        var client = new FakeActivationClient { Response = new ActivationResponse(false, null, null, "invalid") };
        var vm = Create(store, client);
        await vm.LoadAsync();

        var firstAttempt = await vm.BuyAsync();
        Assert.IsFalse(firstAttempt);
        Assert.AreEqual(1, store.PurchaseConsumableCallCount);
        Assert.AreEqual(1, client.ActivateCallCount);
        Assert.AreEqual(0, store.FinishConsumableCallCount);

        client.Response = new ActivationResponse(true, "jwt-token", DateTime.UtcNow.AddHours(12), null);

        var secondAttempt = await vm.BuyAsync();

        Assert.IsTrue(secondAttempt);
        Assert.AreEqual(1, store.PurchaseConsumableCallCount, "the retry must not purchase a second time");
        Assert.AreEqual(2, client.ActivateCallCount);
        Assert.AreEqual(1, store.FinishConsumableCallCount);
        Assert.AreEqual("transaction-1", store.LastFinishedTransactionId);
    }

    [TestMethod]
    public async Task BuyAsync_PurchaseSucceededNoProof_ReturnsFalseWithoutActivating()
    {
        var store = new FakeStoreService
        {
            Products = new[] { Pack500 },
            ConsumableResult = new ConsumablePurchaseResult(PurchaseStatus.Success, null, null)
        };
        var client = new FakeActivationClient();
        var vm = Create(store, client);
        await vm.LoadAsync();

        var ok = await vm.BuyAsync();

        Assert.IsFalse(ok);
        Assert.IsTrue(vm.HasError);
        Assert.AreEqual(0, client.ActivateCallCount, "no proof means nothing to send the Worker");
    }

    [TestMethod]
    public async Task BuyAsync_Pending_ShowsInfoAndReturnsFalse()
    {
        var store = new FakeStoreService
        {
            Products = new[] { Pack500 },
            ConsumableResult = new ConsumablePurchaseResult(PurchaseStatus.Pending)
        };
        var vm = Create(store, new FakeActivationClient());
        await vm.LoadAsync();

        var ok = await vm.BuyAsync();

        Assert.IsFalse(ok);
        Assert.IsFalse(vm.HasError, "pending approval is not an error");
        Assert.IsTrue(vm.HasStatus);
        Assert.AreEqual(InfoBarSeverity.Informational, vm.StatusSeverity);
    }

    [TestMethod]
    public async Task BuyAsync_UserCancelled_NoErrorReturnsFalse()
    {
        var store = new FakeStoreService
        {
            Products = new[] { Pack500 },
            ConsumableResult = new ConsumablePurchaseResult(PurchaseStatus.UserCancelled)
        };
        var vm = Create(store, new FakeActivationClient());
        await vm.LoadAsync();

        var ok = await vm.BuyAsync();

        Assert.IsFalse(ok);
        Assert.IsFalse(vm.HasError, "cancelling is not an error");
    }

    [TestMethod]
    public async Task BuyAsync_Failed_SetsErrorReturnsFalse()
    {
        var store = new FakeStoreService
        {
            Products = new[] { Pack500 },
            ConsumableResult = new ConsumablePurchaseResult(PurchaseStatus.Failed, Error: "declined")
        };
        var vm = Create(store, new FakeActivationClient());
        await vm.LoadAsync();

        var ok = await vm.BuyAsync();

        Assert.IsFalse(ok);
        Assert.IsTrue(vm.HasError);
        Assert.AreEqual("declined", vm.StatusMessage);
    }
}
