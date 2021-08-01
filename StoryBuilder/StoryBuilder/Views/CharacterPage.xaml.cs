using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.ViewModels;
using StoryBuilder.ViewModels.Tools;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace StoryBuilder.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CharacterPage : BindablePage
    {
        public CharacterViewModel CharVm => Ioc.Default.GetService<CharacterViewModel>();
 
        public CharacterPage()
        {
            InitializeComponent();
            DataContext = CharVm;
        }
    }
}
