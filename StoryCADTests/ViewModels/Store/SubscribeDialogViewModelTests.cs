using System.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.Store;
using StoryCADLib.ViewModels.Store;
using StoryCADTests.Services.Store;

#nullable disable

namespace StoryCADTests.ViewModels.Store;

/// <summary>
///     Headless logic tests for <see cref="SubscribeDialogViewModel" /> with a fake store and fake
///     activation service: plan loading, price/trial formatting, and the subscribe/restore paths. The
///     dialog XAML itself is not exercised here (no UI testing).
/// </summary>
[TestClass]
public class SubscribeDialogViewModelTests
{
    private static readonly StoreProduct Monthly =
        new("org.storycad.collaborator.monthly", "Monthly", "Billed monthly", "$4.99", "P1M", true);

    private static readonly StoreProduct Annual =
        new("org.storycad.collaborator.annual", "Annual", "Billed yearly", "$49.99", "P1Y", false);

    private static SubscribeDialogViewModel Create(FakeStoreService store, FakeActivation activation) =>
        new(store, activation, Ioc.Default.GetRequiredService<ILogService>());

    [TestMethod]
    public async Task LoadAsync_WithProducts_PopulatesPlansAndSelectsFirst()
    {
        var vm = Create(new FakeStoreService { Products = new[] { Monthly, Annual } }, new FakeActivation());

        await vm.LoadAsync();

        Assert.AreEqual(2, vm.Plans.Count);
        Assert.AreEqual(Monthly, vm.SelectedPlan);
        Assert.IsTrue(vm.HasPlans);
        Assert.IsFalse(vm.HasError);
    }

    [TestMethod]
    public async Task LoadAsync_StoreThrows_SetsError()
    {
        var vm = Create(new FakeStoreService { ThrowOnGetProducts = true }, new FakeActivation());

        await vm.LoadAsync();

        Assert.IsTrue(vm.HasError);
        Assert.IsFalse(string.IsNullOrEmpty(vm.StatusMessage));
        Assert.IsFalse(vm.HasPlans);
    }

    [TestMethod]
    public void PriceSummary_MonthlyWithTrial_MentionsTrialWithoutLength()
    {
        var vm = Create(new FakeStoreService(), new FakeActivation());
        vm.SelectedPlan = Monthly;

        // The store payload carries no offer length, so the summary must not name one.
        Assert.AreEqual("Includes a free trial, then $4.99/month.", vm.PriceSummary);
    }

    [TestMethod]
    public void PriceSummary_NoStorePrice_ShowsPlaceholder()
    {
        var vm = Create(new FakeStoreService(), new FakeActivation());
        vm.SelectedPlan = new StoreProduct("id", "Monthly", "", "", "P1M", true);

        Assert.AreEqual("Includes a free trial, then PLACEHOLDER/month.", vm.PriceSummary);
    }

    [TestMethod]
    public void PriceSummary_NoTrial_IsPriceAndPeriodOnly()
    {
        var vm = Create(new FakeStoreService(), new FakeActivation());
        vm.SelectedPlan = Annual;

        Assert.AreEqual("$49.99/year.", vm.PriceSummary);
    }

    [TestMethod]
    public void SelectedPlan_Change_RaisesPriceSummary()
    {
        var vm = Create(new FakeStoreService(), new FakeActivation());
        var raised = false;
        vm.PropertyChanged += (_, e) => raised |= e.PropertyName == nameof(vm.PriceSummary);

        vm.SelectedPlan = Monthly;

        Assert.IsTrue(raised, "changing the plan must refresh the price summary");
    }

    [TestMethod]
    public async Task SubscribeAsync_SuccessActivates_ReturnsTrue()
    {
        var activation = new FakeActivation { PurchaseResult = new PurchaseResult(PurchaseStatus.Success), StateAfterPurchase = ActivationState.Active };
        var vm = Create(new FakeStoreService(), activation);
        vm.SelectedPlan = Monthly;

        var ok = await vm.SubscribeAsync();

        Assert.IsTrue(ok);
        Assert.AreEqual(Monthly.Id, activation.LastProductId);
        Assert.IsFalse(vm.HasError);
        Assert.IsFalse(vm.IsBusy);
    }

    [TestMethod]
    public async Task SubscribeAsync_Pending_ShowsInfoAndReturnsFalse()
    {
        // Ask to Buy: not a failure and not silence — the user must see what happens next.
        var activation = new FakeActivation { PurchaseResult = new PurchaseResult(PurchaseStatus.Pending) };
        var vm = Create(new FakeStoreService(), activation);
        vm.SelectedPlan = Monthly;

        var ok = await vm.SubscribeAsync();

        Assert.IsFalse(ok);
        Assert.IsFalse(vm.HasError, "pending approval is not an error");
        Assert.IsTrue(vm.HasStatus, "the user must be told the purchase awaits approval");
        Assert.AreEqual(InfoBarSeverity.Informational, vm.StatusSeverity);
    }

    [TestMethod]
    public async Task SubscribeAsync_SuccessButNotActivated_SetsError()
    {
        // The store charged but the Worker handshake didn't finish; silence here reads as a
        // charge with nothing delivered.
        var activation = new FakeActivation
        {
            PurchaseResult = new PurchaseResult(PurchaseStatus.Success),
            StateAfterPurchase = ActivationState.NotPurchased
        };
        var vm = Create(new FakeStoreService(), activation);
        vm.SelectedPlan = Monthly;

        var ok = await vm.SubscribeAsync();

        Assert.IsFalse(ok);
        Assert.IsTrue(vm.HasError);
    }

    [TestMethod]
    public async Task SubscribeAsync_UserCancelled_NoErrorReturnsFalse()
    {
        var activation = new FakeActivation { PurchaseResult = new PurchaseResult(PurchaseStatus.UserCancelled) };
        var vm = Create(new FakeStoreService(), activation);
        vm.SelectedPlan = Monthly;

        var ok = await vm.SubscribeAsync();

        Assert.IsFalse(ok);
        Assert.IsFalse(vm.HasError, "cancelling is not an error");
    }

    [TestMethod]
    public async Task SubscribeAsync_Failed_SetsErrorReturnsFalse()
    {
        var activation = new FakeActivation { PurchaseResult = new PurchaseResult(PurchaseStatus.Failed, "declined") };
        var vm = Create(new FakeStoreService(), activation);
        vm.SelectedPlan = Monthly;

        var ok = await vm.SubscribeAsync();

        Assert.IsFalse(ok);
        Assert.IsTrue(vm.HasError);
    }

    [TestMethod]
    public async Task RestoreAsync_WhenInvoked_CallsActivationRestore()
    {
        var activation = new FakeActivation();
        var vm = Create(new FakeStoreService(), activation);

        await vm.RestoreAsync();

        Assert.IsTrue(activation.RestoreCalled);
        Assert.IsFalse(vm.IsBusy);
    }

    [TestMethod]
    public async Task RestoreAsync_NothingToRestore_ShowsInfoNotError()
    {
        // A restore that completes but finds no purchase must say so, not stop silently.
        var activation = new FakeActivation { StateAfterPurchase = ActivationState.NotPurchased };
        var vm = Create(new FakeStoreService(), activation);

        await vm.RestoreAsync();

        Assert.IsFalse(vm.HasError, "finding nothing to restore is not an error");
        Assert.IsTrue(vm.HasStatus);
        Assert.AreEqual(InfoBarSeverity.Informational, vm.StatusSeverity);
    }

    [TestMethod]
    public async Task LoadAsync_PlansAlreadyLoaded_SkipsStoreQuery()
    {
        var store = new FakeStoreService { Products = new[] { Monthly } };
        var vm = Create(store, new FakeActivation());

        await vm.LoadAsync();
        await vm.LoadAsync();

        Assert.AreEqual(1, store.GetProductsCallCount, "products are static per session; reopen must not refetch");
        Assert.AreEqual(1, vm.Plans.Count);
        Assert.AreEqual(Monthly, vm.SelectedPlan);
    }

    // ----- Fakes ----- (the IStoreService fake is shared: StoreTestDoubles.cs)

    private sealed class FakeActivation : IStoreActivationService
    {
        public PurchaseResult PurchaseResult = new(PurchaseStatus.Success);
        public ActivationState StateAfterPurchase = ActivationState.Active;
        public bool RestoreCalled;
        public string LastProductId;

        public ActivationState State { get; private set; } = ActivationState.NotPurchased;
        public string CurrentJwt => null;
        public event EventHandler<ActivationState> StateChanged { add { } remove { } }

        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task<PurchaseResult> PurchaseAndActivateAsync(string productId, CancellationToken ct = default)
        {
            LastProductId = productId;
            if (PurchaseResult.Status == PurchaseStatus.Success)
            {
                State = StateAfterPurchase;
            }

            return Task.FromResult(PurchaseResult);
        }

        public Task RestoreAsync(CancellationToken ct = default)
        {
            RestoreCalled = true;
            State = StateAfterPurchase;
            return Task.CompletedTask;
        }

        public Task ReactivateAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
