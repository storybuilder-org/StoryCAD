using StoryBuilder.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs
{
    public sealed partial class UnifiedMenu
    {
        public UnifiedMenu()
        {
            InitializeComponent();
            UnifiedMenuVM = new UnifiedVM();
            UnifiedMenuVM.HideOpen = HideDialog;  // Connect the VM's delegate to HideDialog
            UnifiedMenuVM.CurrentTab = new ListBoxItem() { Name="Recents"}; //Makes unified VM load recents by default
            UnifiedMenuVM.SidebarChange(null,null);
        }

        public delegate void HideDelegate();

        public void HideDialog() 
        {
            Hide();
        }

        public UnifiedVM UnifiedMenuVM;

    }
}