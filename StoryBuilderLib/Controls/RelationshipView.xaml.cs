using System;
<<<<<<< Updated upstream
using System.Runtime.InteropServices;
=======
using System.Linq;
using System.Threading.Tasks;
>>>>>>> Stashed changes
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;
using Syncfusion.UI.Xaml.Editors;

namespace StoryBuilder.Controls;

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
    private void RelationshipChanged(object sender, SelectionChangedEventArgs e)
    {
        CharVm.SaveRelationship(CharVm.CurrentRelationship);
        CharVm.LoadRelationship(CharVm.SelectedRelationship);
        CharVm.CurrentRelationship = CharVm.SelectedRelationship;
<<<<<<< Updated upstream
        if ((sender as SfComboBox).SelectedValue == null) {  CharVm.IsLoaded = false; }
        else { CharVm.IsLoaded = true;}
=======
       // if (RelationshipPickerBox.SelectedValue == null) {  CharVm.IsLoaded = false; }
    //    else { CharVm.IsLoaded = true;}
>>>>>>> Stashed changes
    }

    private async void UIElement_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
<<<<<<< Updated upstream
        ContentDialogResult result = await new ContentDialog()
        {
            XamlRoot = GlobalData.XamlRoot,
            Content = "Are you sure you want to delete the relationship between PLACEHOLDER and PLACEHOLDER",
=======
       await CharVm.AddRelationship();
       //RelationshipPickerBox.SelectedItem = CharVm.CharacterRelationships.Last();
    }

    private async void UIElement_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ContentDialogResult result = await new ContentDialog()
        {
            XamlRoot = GlobalData.XamlRoot,
            Content = $"Are you sure you want to delete the relationship between {CharVm.Name} and {(sender as SymbolIcon).Tag}",
>>>>>>> Stashed changes
            Title = "Are you sure?",
            PrimaryButtonText = "Yes",
            SecondaryButtonText = "No"
        }.ShowAsync();
<<<<<<< Updated upstream

=======
        
>>>>>>> Stashed changes
        if (result == ContentDialogResult.Primary)
        {
            foreach (var relationship in CharVm.CharacterRelationships)
            {
                if (relationship.Partner.Uuid.Equals(CharVm.CurrentRelationship.Partner.Uuid))
                {
                    CharVm.CharacterRelationships.Remove(relationship);
                    break;
                }
            }
        }
    }
}