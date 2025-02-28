﻿using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace StoryCAD.Services.Dialogs;

public sealed partial class UnifiedMenuPage
{
    readonly Windowing Windowing = Ioc.Default.GetRequiredService<Windowing>();
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
                UnifiedMenuVM.SelectedTemplateIndex = Ioc.Default.GetRequiredService<PreferenceService>().Model.LastSelectedTemplate;
                MenuContent.Children.Add(new NewProjectPage(UnifiedMenuVM));
                break;
            case "Sample":
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
        UnifiedMenuVM.CurrentTab = new ListBoxItem { Name = "Recent" }; //Makes unified VM load recents by default
        UnifiedMenuVM.SidebarChange(null, null);
        if (Windowing.RequestedTheme == ElementTheme.Light) {UnifiedMenuVM.AdjustmentColor = new SolidColorBrush(Colors.White);}
    }
}