using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FolderPage : BindablePage
    {
        public FolderViewModel FolderVm => Ioc.Default.GetService<FolderViewModel>();

        public FolderPage()
        {
            InitializeComponent();
            this.DataContext = FolderVm;
        }
    }
}
