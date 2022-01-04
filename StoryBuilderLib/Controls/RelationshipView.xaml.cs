using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using StoryBuilder.ViewModels;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace StoryBuilder.Controls
{
    public sealed partial class RelationshipView : UserControl
    {
        public CharacterViewModel CharVm => Ioc.Default.GetService<CharacterViewModel>();
        public RelationshipView()
        {
            InitializeComponent();
        }

        /// Instead of loading a Character's RelationshipModels directly into
        /// the ViewModel and binding them, the models themselves are loaded 
        /// into the VM's CharacterRelationships ObservableCollection, but
        /// its properties are bound only when one of of the ComboBox items
        /// CharacterRelationships is bound to is selected.
        /// However, one property need modified during LoadModel: the Partner  
        /// StoryElement in the RelationshipModel needs loaded from its Uuid.
        public async void RelationshipChanged(object sender, SelectionChangedEventArgs args)
        {
            await CharVm.SaveRelationship(CharVm.CurrentRelationship);
            await CharVm.LoadRelationship(CharVm.SelectedRelationship);
            CharVm.CurrentRelationship = CharVm.SelectedRelationship;
        }
    }
}
