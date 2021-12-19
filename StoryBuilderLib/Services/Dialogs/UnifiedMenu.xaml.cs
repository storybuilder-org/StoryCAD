using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
            this.InitializeComponent();
            UnifiedMenuVM = new();
            UnifiedMenuVM.UpdateContent = UpdateContent;  // Connect the VM's delegate to HideDialog
            UnifiedMenuVM.CurrentTab = new ListBoxItem() { Name = "Recent" }; //Makes unified VM load recents by default
            UnifiedMenuVM.SidebarChange(null, null);
        }
    }
}
