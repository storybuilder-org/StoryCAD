using Microsoft.UI.Xaml.Controls;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CharacterPage : BindablePage
    {
        public CharacterViewModel CharVm => Ioc.Default.GetService<CharacterViewModel>();

        private void ComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        {
            sender.SelectedItem = sender.Text;
            args.Handled = true;
        }

        public CharacterPage()
        {
            InitializeComponent();
            DataContext = CharVm;
        }
    }
}
