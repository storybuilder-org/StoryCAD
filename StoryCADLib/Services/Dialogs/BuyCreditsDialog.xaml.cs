using StoryCADLib.ViewModels.Store;

namespace StoryCADLib.Services.Dialogs;

/// <summary>
///     Content body for the "Buy Credits" ContentDialog. The wrapper dialog (title, Buy / Not now
///     buttons) is assembled by <see cref="BuyCreditsDialogViewModel.ShowAsync" />, which passes
///     itself in here so the page binds the same instance that drives the buttons.
/// </summary>
public sealed partial class BuyCreditsDialog : Page
{
    public BuyCreditsDialogViewModel Vm { get; }

    public BuyCreditsDialog(BuyCreditsDialogViewModel vm)
    {
        Vm = vm;
        InitializeComponent();
    }
}
