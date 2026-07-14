using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoryCADLib.Services.Dialogs;
using StoryCADLib.Services.Store;
using Windows.System;

namespace StoryCADLib.ViewModels.Store;

/// <summary>
///     Backs <c>SubscribeDialog</c>. Loads the configured subscription plans from the platform store,
///     lets the user pick one, and drives purchase/restore through the shared
///     <see cref="IStoreActivationService" />. The actual charge is the platform's own payment sheet
///     (StoreKit on macOS, the Microsoft Store dialog on Windows); this view model never handles money.
/// </summary>
public sealed class SubscribeDialogViewModel : ObservableObject
{
    private readonly IStoreService _store;
    private readonly IStoreActivationService _activation;
    private readonly ILogService _logService;

    public SubscribeDialogViewModel(IStoreService store, IStoreActivationService activation, ILogService logService)
    {
        _store = store;
        _activation = activation;
        _logService = logService;
        OpenTermsCommand = new RelayCommand(() => Open(StoreConfig.TermsOfUseUrl));
        OpenPrivacyCommand = new RelayCommand(() => Open(StoreConfig.PrivacyPolicyUrl));
    }

    public ObservableCollection<StoreProduct> Plans { get; } = new();

    private StoreProduct _selectedPlan;
    public StoreProduct SelectedPlan
    {
        get => _selectedPlan;
        set
        {
            if (SetProperty(ref _selectedPlan, value))
            {
                OnPropertyChanged(nameof(PriceSummary));
            }
        }
    }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }

    // The stored status state is just message + severity; everything else is derived.
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Error;
    public InfoBarSeverity StatusSeverity { get => _statusSeverity; private set => SetProperty(ref _statusSeverity, value); }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; private set => SetProperty(ref _statusMessage, value); }

    /// <summary>True while any status message (error or informational) should be visible.</summary>
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public bool HasError => HasStatus && StatusSeverity == InfoBarSeverity.Error;

    public bool HasPlans => Plans.Count > 0;

    public RelayCommand OpenTermsCommand { get; }
    public RelayCommand OpenPrivacyCommand { get; }

    /// <summary>Price/free-trial line for the selected plan, e.g. "Includes a free trial, then $4.99/month."</summary>
    public string PriceSummary
    {
        get
        {
            if (SelectedPlan is null)
            {
                return string.Empty;
            }

            // Price is store-driven; until App Store Connect / Partner Center pricing is configured it
            // isn't known, so show a literal PLACEHOLDER rather than an invented number.
            var price = string.IsNullOrEmpty(SelectedPlan.DisplayPrice) ? "PLACEHOLDER" : SelectedPlan.DisplayPrice;
            var period = PeriodWord(SelectedPlan.SubscriptionPeriod);
            var priced = string.IsNullOrEmpty(period) ? price : $"{price}/{period}";
            // The store payload only reports whether an intro offer exists, not its length, so the
            // summary must not name a period (Apple guideline 3.1.2: purchase terms must be accurate).
            return SelectedPlan.HasIntroOffer ? $"Includes a free trial, then {priced}." : $"{priced}.";
        }
    }

    /// <summary>Loads the configured plans from the store. Call when the dialog opens.</summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        ClearStatus();

        // Products are static for the session; don't re-query the store every time the
        // dialog reopens. A failed load leaves Plans empty, so the retry path still fetches.
        if (Plans.Count > 0)
        {
            SelectedPlan ??= Plans.FirstOrDefault();
            return;
        }

        try
        {
            var products = await _store.GetProductsAsync(_store.ProductIds, ct);
            Plans.Clear();
            foreach (var product in products)
            {
                Plans.Add(product);
            }

            SelectedPlan = Plans.FirstOrDefault();
            OnPropertyChanged(nameof(HasPlans));
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Failed to load subscription plans: {ex.Message}");
            SetStatus("Couldn't reach the store. Check your connection and try again.");
        }
    }

    /// <summary>
    ///     Shows the subscribe dialog (same panel on macOS and Windows) and drives purchase/restore.
    ///     Returns true when the user is Active afterward. Buttons keep the dialog open on failure
    ///     (and on Ask-to-Buy pending, so its message stays visible); the actual charge is the
    ///     platform's own payment sheet.
    /// </summary>
    public async Task<bool> ShowAsync(Windowing windowing)
    {
        await LoadAsync();

        var dialog = new ContentDialog
        {
            Title = "StoryCAD Collaborator",
            Content = new SubscribeDialog(this),
            PrimaryButtonText = "Subscribe",
            SecondaryButtonText = "Restore purchases",
            CloseButtonText = "Not now",
            DefaultButton = ContentDialogButton.Primary
        };

        dialog.PrimaryButtonClick += async (_, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                // Keep the dialog open unless the purchase actually activated the user.
                args.Cancel = !await SubscribeAsync();
            }
            finally
            {
                deferral.Complete();
            }
        };

        dialog.SecondaryButtonClick += async (_, args) =>
        {
            var deferral = args.GetDeferral();
            try
            {
                await RestoreAsync();
                args.Cancel = _activation.State != ActivationState.Active;
            }
            finally
            {
                deferral.Complete();
            }
        };

        await windowing.ShowContentDialog(dialog);
        return _activation.State == ActivationState.Active;
    }

    /// <summary>Purchases the selected plan and activates it. Returns true when the user is now Active.</summary>
    public async Task<bool> SubscribeAsync()
    {
        if (SelectedPlan is null)
        {
            return false;
        }

        IsBusy = true;
        ClearStatus();
        try
        {
            var result = await _activation.PurchaseAndActivateAsync(SelectedPlan.Id);
            switch (result.Status)
            {
                case PurchaseStatus.Success:
                    if (_activation.State == ActivationState.Active)
                    {
                        return true;
                    }

                    // The store charged (or will), but the activation handshake didn't finish.
                    SetStatus("Your purchase went through, but activation didn't finish. " +
                              "Check your connection and use Restore purchases to try again.");
                    return false;
                case PurchaseStatus.Pending:
                    // Ask to Buy: not a failure. Tell the user what happens next; the dialog
                    // stays open so this message is visible until they dismiss it.
                    SetStatus("Your purchase is waiting for approval. Collaborator will unlock " +
                              "automatically once it's approved.", InfoBarSeverity.Informational);
                    return false;
                case PurchaseStatus.UserCancelled:
                    return false; // the user backed out of the system sheet; no message needed
                default:
                    SetStatus("That didn't go through. You haven't been charged. Try again.");
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Subscribe failed: {ex.Message}");
            SetStatus("Couldn't reach the store. Check your connection and try again.");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Restores an existing purchase (explicit user action only).</summary>
    public async Task RestoreAsync()
    {
        IsBusy = true;
        ClearStatus();
        try
        {
            await _activation.RestoreAsync();
            if (_activation.State != ActivationState.Active)
            {
                // Completed normally but found nothing: say so instead of leaving the
                // dialog open with a silently-stopped spinner.
                SetStatus("We couldn't find a purchase to restore for this account.",
                    InfoBarSeverity.Informational);
            }
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Restore failed: {ex.Message}");
            SetStatus("Couldn't restore your purchase. Check your connection and try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(string message, InfoBarSeverity severity = InfoBarSeverity.Error)
    {
        StatusSeverity = severity;
        StatusMessage = message ?? string.Empty;
        OnPropertyChanged(nameof(HasStatus));
        OnPropertyChanged(nameof(HasError));
    }

    private void ClearStatus() => SetStatus(null);

    private void Open(string url)
    {
        try
        {
            _ = Launcher.LaunchUriAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            _logService.Log(LogLevel.Warn, $"Failed to open {url}: {ex.Message}");
        }
    }

    private static string PeriodWord(string iso) => iso switch
    {
        "P1W" => "week",
        "P1M" => "month",
        "P3M" => "quarter",
        "P6M" => "6 months",
        "P1Y" => "year",
        _ => string.Empty
    };
}
