using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SectionPage : BindablePage
    {

        public SectionViewModel SectionVm => Ioc.Default.GetService<SectionViewModel>();

        public SectionPage()
        {
            InitializeComponent();
            this.DataContext = SectionVm;
        }
    }
}
