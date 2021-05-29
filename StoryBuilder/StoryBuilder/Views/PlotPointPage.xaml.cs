using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlotPointPage : BindablePage
    {
        public PlotPointViewModel PlotpointVm => Ioc.Default.GetService<PlotPointViewModel>();

        public PlotPointPage()
        {
            this.InitializeComponent();
            this.DataContext = PlotpointVm;
        }
    }
}
