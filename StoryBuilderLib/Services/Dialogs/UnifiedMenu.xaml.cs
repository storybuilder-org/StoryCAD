
using StoryBuilder.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using System.ComponentModel;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Services.Dialogs
{
    public sealed partial class UnifiedMenu
    {
        public UnifiedMenu()
        {
            InitializeComponent();
            UnifiedMenuVM.HideOpen = HideDialog;
        }

        //public IAsyncInfo AsyncInfo;

        public delegate void HideDelegate();

        public void HideDialog() 
        {
            this.Hide();
            //AsyncInfo.Cancel();
        }

        //private PropertyChangedEventHandler CheckForClose()
        //{
        //    if (UnifiedMenuVM.Closing) { this.Hide(); }
        //    return null;
        //}

        public UnifiedVM UnifiedMenuVM
        {
            get { return Ioc.Default.GetService<UnifiedVM>(); }
        }


    }
}