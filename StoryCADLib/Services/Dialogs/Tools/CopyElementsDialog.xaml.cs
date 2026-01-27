using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.ViewModels.Tools;

namespace StoryCADLib.Services.Dialogs.Tools;

/// <summary>
/// Code-behind for CopyElementsDialog.
/// Minimal code - all logic is in CopyElementsDialogVM.
/// </summary>
public sealed partial class CopyElementsDialog : Page
{
    public CopyElementsDialogVM VM { get; }

    public CopyElementsDialog()
    {
        InitializeComponent();
        VM = Ioc.Default.GetRequiredService<CopyElementsDialogVM>();
    }

    /// <summary>
    /// Handles filter ComboBox selection change.
    /// Delegates to ViewModel for list refresh.
    /// </summary>
    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        VM.RefreshSourceElements();
        // Target list accumulates copied elements - doesn't refresh on filter change
    }
}
