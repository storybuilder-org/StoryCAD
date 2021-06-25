using Microsoft.UI.Xaml.Controls;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;
using StoryBuilder.Models;

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

        private void NewCastMember_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlotpointVm.AddCastSelectionChanged((StoryElement) e.AddedItems[0]);
        }
    }
}
