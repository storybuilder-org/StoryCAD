using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using StoryCADLib.Services;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Store;

namespace StoryCADLib.ViewModels.Store;

/// <summary>
///     Backs <c>BuyCreditsDialog</c> (issue #90 design section 10 "Credit packs", step 10). Loads
///     the configured credit packs from the platform store, lets the user pick one, purchases it,
///     and posts the resulting proof straight to the Worker's <c>/v1/activate</c> via
///     <see cref="IActivationClient" /> — unlike <see cref="SubscribeDialogViewModel" />, this does
///     not go through <see cref="IStoreActivationService" />: a pack purchase tops up the balance
///     behind the caller's *existing* JWT, so there is no cached-JWT state for this view model to
///     update (the Worker's 200 for a pack activation is discarded once the credit is confirmed).
/// </summary>
public sealed class BuyCreditsDialogViewModel : ObservableObject
{
    private readonly IStoreService _store;
    private readonly IActivationClient _client;
    private readonly PreferenceService _preferenceService;
    private readonly ILogService _logService;

    public BuyCreditsDialogViewModel(IStoreService store, IActivationClient client,
        PreferenceService preferenceService, ILogService logService)
    {
        _store = store;
        _client = client;
        _preferenceService = preferenceService;
        _logService = logService;
    }

    public ObservableCollection<StoreProduct> Packs { get; } = new();

    private StoreProduct _selectedPack;
    public StoreProduct SelectedPack { get => _selectedPack; set => SetProperty(ref _selectedPack, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    // The stored status state is just message + severity; everything else is derived (same shape
    // as SubscribeDialogViewModel's status handling).
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Error;
    public InfoBarSeverity StatusSeverity { get => _statusSeverity; private set => SetProperty(ref _statusSeverity, value); }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

    /// <summary>True while any status message (error or informational) should be visible.</summary>
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public bool HasError => HasStatus && StatusSeverity == InfoBarSeverity.Error;

    public bool HasPacks => Packs.Count > 0;

    // Set by BuyAsync; ShowAsync reports this once the dialog closes rather than re-deriving it
    // from a persistent state property (unlike SubscribeDialogViewModel, a pack purchase has no
    // ongoing ActivationState to check afterward -- it either credited the account or it didn't).
    private bool _lastPurchaseSucceeded;

    private string UserGuid => _preferenceService.Model.StoreUserGuid ?? string.Empty;

    /// <summary>Loads the configured packs from the store. Call when the dialog opens.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        ClearStatus();

        // Packs are static for the session, same reasoning as SubscribeDialogViewModel.LoadAsync:
        // don't re-query the store every time the dialog reopens. A failed load leaves Packs empty,
        // so the retry path still fetches.
        if (Packs.Count > 0)
        {
            SelectedPack ??= Packs.FirstOrDefault();
            return;
        }

        try
        {
            var products = await _store.GetProductsAsync(_store.CreditPackProductIds, ct);
            Packs.Clear();
            foreach (var product in products)
            {
                Packs.Add(product);
            }

            SelectedPack = Packs.FirstOrDefault();
            OnPropertyChanged(nameof(HasPacks));
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Failed to load credit packs: {ex.Message}");
            SetStatus("Couldn't reach the store. Check your connection and try again.");
        }
    }

    /// <summary>
    ///     Shows the buy-credits dialog (same panel on macOS and Windows) and drives the purchase.
    ///     Returns true when a pack was successfully purchased and credited.
    /// </summary>
    public async Task<bool> ShowAsync(Windowing windowing)
    {
        await LoadAsync();
        _lastPurchaseSucceeded = false;

        var dialog = new ContentDialog
        {
            Title = "Buy Credits",
            Content = new BuyCreditsDialog(this),
            PrimaryButtonText = "Buy",
            CloseButtonText = "Not now",
            DefaultButton = ContentDialogButton.Primary
        };

        dialog.PrimaryButtonClick += async (_, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                // Keep the dialog open unless the purchase actually credited the account.
                args.Cancel = !await BuyAsync();
            }
            finally
            {
                deferral.Complete();
            }
        };

        await windowing.ShowContentDialog(dialog);
        return _lastPurchaseSucceeded;
    }

    /// <summary>Purchases the selected pack and posts its proof to the Worker. Returns true on success.</summary>
    public async Task<bool> BuyAsync(CancellationToken ct = default)
    {
        if (SelectedPack is null)
        {
            return false;
        }

        IsBusy = true;
        ClearStatus();
        try
        {
            var purchase = await _store.PurchaseConsumableAsync(SelectedPack.Id, UserGuid, ct);
            switch (purchase.Status)
            {
                case PurchaseStatus.Success:
                    _lastPurchaseSucceeded = await ActivatePurchaseAsync(purchase, ct);
                    return _lastPurchaseSucceeded;
                case PurchaseStatus.Pending:
                    // Ask to Buy: not a failure. Tell the user what happens next; the dialog
                    // stays open so this message is visible until they dismiss it.
                    SetStatus("Your purchase is waiting for approval. Your credits will be added " +
                              "automatically once it's approved.", InfoBarSeverity.Informational);
                    return false;
                case PurchaseStatus.UserCancelled:
                    return false; // the user backed out of the system sheet; no message needed
                default:
                    SetStatus(purchase.Error ?? "That didn't go through. You haven't been charged. Try again.");
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Buy credits failed: {ex.Message}");
            SetStatus("Couldn't reach the store. Check your connection and try again.");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Design section 10 "Credit packs": the Worker records the purchase (journals and credits the
    // ledger) before the client tells the store it is consumed, so FinishConsumableAsync (Apple:
    // Transaction.finish(); Windows: a no-op, the Worker reports fulfillment itself) runs only
    // after ActivateAsync's 200, never before.
    private async Task<bool> ActivatePurchaseAsync(ConsumablePurchaseResult purchase, CancellationToken ct)
    {
        if (purchase.Proof is null)
        {
            SetStatus("Your purchase went through, but the store returned no proof. " +
                      "Check your connection and try again.");
            return false;
        }

        ActivationResponse response;
        try
        {
            response = await _client.ActivateAsync(purchase.Proof, ct);
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Credit-pack activation unreachable: {ex.Message}");
            SetStatus("Your purchase went through, but we couldn't add the credits yet. " +
                      "Check your connection; try again from this screen.");
            return false;
        }

        if (!response.Ok)
        {
            // The wire contract's refusal reasons (invalid/revoked/expired) don't map to a
            // meaningful distinction for a just-completed consumable purchase; any refusal here
            // means the Worker did not credit the account.
            SetStatus("The store confirmed your purchase, but the credits couldn't be applied. " +
                      "Contact support.");
            return false;
        }

        await _store.FinishConsumableAsync(purchase.TransactionId, ct);
        return true;
    }

    private void SetStatus(string message, InfoBarSeverity severity = InfoBarSeverity.Error)
    {
        StatusSeverity = severity;
        StatusMessage = message ?? string.Empty;
        OnPropertyChanged(nameof(HasStatus));
        OnPropertyChanged(nameof(HasError));
    }

    private void ClearStatus() => SetStatus(null);
}
