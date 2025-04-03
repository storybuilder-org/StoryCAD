using Microsoft.UI.Xaml;

namespace StoryCAD.Services.Dialogs;

public sealed partial class FileOpenMenuPage
{
    public FileOpenVM UnifiedMenuVM = Ioc.Default.GetRequiredService<FileOpenVM>();

    public FileOpenMenuPage()
    {
        InitializeComponent();
        UnifiedMenuVM.RecentsTabContentVisibilty = Visibility.Collapsed;
        UnifiedMenuVM.SamplesTabContentVisibilty = Visibility.Collapsed;
        UnifiedMenuVM.NewTabContentVisibilty = Visibility.Collapsed;
        UnifiedMenuVM.CurrentTab = new NavigationViewItem() { Tag = "Recent" };
    }
}