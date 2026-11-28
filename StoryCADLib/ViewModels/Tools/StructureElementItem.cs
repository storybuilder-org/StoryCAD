using CommunityToolkit.Mvvm.ComponentModel;

namespace StoryCADLib.ViewModels.Tools;

/// <summary>
///     One row of the element list of the Structure tab and of the beat tree window:
///     a Scene or a Problem, with how the structure of the outline uses it (issue #1546).
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public class StructureElementItem : ObservableObject
{
    public StructureElementItem(StoryElement element, StructureUse use = StructureUse.NotUsed)
    {
        Element = element;
        _use = use;
    }

    public StoryElement Element { get; }

    public string Name => Element.Name;

    private StructureUse _use;

    public StructureUse Use
    {
        get => _use;
        set
        {
            if (SetProperty(ref _use, value))
            {
                OnPropertyChanged(nameof(UseText));
            }
        }
    }

    /// <summary>
    ///     The words that mark a used element. The mark is text, so it does not depend on color.
    /// </summary>
    public string UseText => Use switch
    {
        StructureUse.OnBeat => "on a beat",
        StructureUse.OnNoBeat => "on no beat",
        _ => string.Empty
    };
}
