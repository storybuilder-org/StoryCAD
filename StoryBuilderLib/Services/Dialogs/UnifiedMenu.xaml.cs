using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services.Dialogs;

public sealed partial class UnifiedMenuPage
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
                UnifiedMenuVM.SelectedTemplateIndex = GlobalData.Preferences.LastSelectedTemplate;
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
        UnifiedMenuVM = new()
        {
            UpdateContent = UpdateContent, // Connect the VM's delegate to HideDialog
            CurrentTab = new ListBoxItem { Name = "Recent" } //Makes unified VM load recent by default
        };
        UnifiedMenuVM.SidebarChange(null, null);
        if (ActualTheme == ElementTheme.Light) {UnifiedMenuVM.AdjustmentColor = new SolidColorBrush(Colors.White);}
    }
}