using Microsoft.UI.Xaml;
using StoryCADLib.ViewModels.SubViewModels;

namespace StoryCADLib.Controls;

/// <summary>
///     The buttons for one Problem: the seven buttons of the Structure tab, and two for the
///     elements of the Problem (issue #1546). The Structure tab and the beat tree window use it.
/// </summary>
public sealed partial class StructureCommandsControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel), typeof(BeatSheetsViewModel), typeof(StructureCommandsControl),
        new PropertyMetadata(null));

    public StructureCommandsControl()
    {
        InitializeComponent();
    }

    /// <summary>The Problem that the buttons act on.</summary>
    public BeatSheetsViewModel ViewModel
    {
        get => (BeatSheetsViewModel)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
}
