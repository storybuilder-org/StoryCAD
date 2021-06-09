using Microsoft.UI.Xaml.Controls;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProblemPage : BindablePage
    {
        public ProblemViewModel ProblemVm => Ioc.Default.GetService<ProblemViewModel>();

        public ProblemPage()
        {
            this.InitializeComponent();
            this.DataContext = ProblemVm;
        }
    }
}
