using StoryBuilder.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;

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
        }

        public delegate void HideDelegate();

        public void HideDialog() 
        {
            Hide();
        }

        public UnifiedVM UnifiedMenuVM;

    }
}