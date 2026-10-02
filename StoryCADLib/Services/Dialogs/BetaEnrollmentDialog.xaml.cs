using StoryCADLib.ViewModels.Store;

namespace StoryCADLib.Services.Dialogs;

/// <summary>
///     Body of the Join / Not now dialog. Title and buttons live on the ContentDialog
///     wrapper in <see cref="BetaEnrollmentDialogViewModel.ShowAsync" />. The Getting Started
///     link is on this page so it is visible before Join.
/// </summary>
public sealed partial class BetaEnrollmentDialog : Page
{
    public BetaEnrollmentDialogViewModel Vm { get; }

    public BetaEnrollmentDialog(BetaEnrollmentDialogViewModel vm)
    {
        Vm = vm;
        InitializeComponent();
    }
}
