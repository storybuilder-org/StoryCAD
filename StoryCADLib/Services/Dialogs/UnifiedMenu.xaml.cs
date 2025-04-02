using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace StoryCAD.Services.Dialogs;

public sealed partial class UnifiedMenuPage
{
    readonly Windowing Windowing = Ioc.Default.GetRequiredService<Windowing>();
    public delegate void UpdateContentDelegate();

    public FileOpenVM UnifiedMenuVM = Ioc.Default.GetRequiredService<FileOpenVM>();


    public UnifiedMenuPage()
    {
        InitializeComponent();
        UnifiedMenuVM.RecentsTabContentVisibilty = Visibility.Collapsed;
        UnifiedMenuVM.SamplesTabContentVisibilty = Visibility.Collapsed;
        UnifiedMenuVM.NewTabContentVisibilty = Visibility.Visible;
    }
}