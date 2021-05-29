using Microsoft.Toolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OverviewPage : BindablePage
    {

        public OverviewViewModel OverviewVm => Ioc.Default.GetService<OverviewViewModel>();
        public OverviewPage()
        {
            InitializeComponent();
            this.DataContext = OverviewVm;
        }

        private void ComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs e)
        {
            sender.Items.Add(e.Text);
        }
    }
}
