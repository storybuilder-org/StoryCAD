using StoryCADLib.ViewModels.Store;

namespace StoryCADLib.Services.Dialogs;

/// <summary>
///     Content body for the "Unlock StoryCAD Collaborator" ContentDialog. The wrapper dialog
///     (title, Subscribe / Restore purchases / Not now buttons) is assembled by
///     <see cref="SubscribeDialogViewModel.ShowAsync" />, which passes itself in here so the page
///     binds the same instance that drives the buttons.
/// </summary>
public sealed partial class SubscribeDialog : Page
{
    public SubscribeDialogViewModel Vm { get; }

    public SubscribeDialog(SubscribeDialogViewModel vm)
    {
        Vm = vm;
        InitializeComponent();
    }
}
