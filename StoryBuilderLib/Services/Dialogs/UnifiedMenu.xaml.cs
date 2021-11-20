using StoryBuilder.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.ComponentModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs
{
    public sealed partial class UnifiedMenu
    {
        public UnifiedMenu()
        {
            InitializeComponent();
            UnifiedMenuVM.PropertyChanged += CheckForClose();
        }

        private PropertyChangedEventHandler CheckForClose()
        {
            if (UnifiedMenuVM.Closing) { this.Hide(); }
            return null;
        }

        public UnifiedVM UnifiedMenuVM
        {
            get { return Ioc.Default.GetService<UnifiedVM>(); }
        }


    }
}