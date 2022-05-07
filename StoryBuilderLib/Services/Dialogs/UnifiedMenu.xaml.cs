using Windows.UI;
using ABI.Microsoft.UI.Xaml;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StoryBuilder.ViewModels;
using Application = Microsoft.UI.Xaml.Application;

namespace StoryBuilder.Services.Dialogs;

public sealed partial class UnifiedMenuPage : Page
{
    public delegate void UpdateContentDelegate();


    public void UpdateContent()
    {
        MenuContent.Children.Clear();
        switch (UnifiedMenuVM.CurrentTab.Name)
        {
            case "Recent":
                MenuContent.Children.Add(new RecentFiles(UnifiedMenuVM));
                break;
            case "New":
                UnifiedMenuVM.SelectedTemplateIndex = Models.GlobalData.Preferences.LastSelectedTemplate;
                MenuContent.Children.Add(new NewProjectPage(UnifiedMenuVM));
                break;
            case "Example":
                MenuContent.Children.Add(new SamplePage(UnifiedMenuVM));
                break;
        }
    }

    public UnifiedVM UnifiedMenuVM;


    public UnifiedMenuPage()
    {
        InitializeComponent();
        UnifiedMenuVM = new();
        UnifiedMenuVM.UpdateContent = UpdateContent;  // Connect the VM's delegate to HideDialog
        UnifiedMenuVM.CurrentTab = new ListBoxItem() { Name = "Recent" }; //Makes unified VM load recents by default
        UnifiedMenuVM.SidebarChange(null, null);
        if (ActualTheme == ElementTheme.Light) {UnifiedMenuVM.AdjustmentColor = new SolidColorBrush(Colors.White);}
    }
}