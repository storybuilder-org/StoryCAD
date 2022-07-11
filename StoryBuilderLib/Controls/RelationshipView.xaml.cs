using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;
using Syncfusion.UI.Xaml.Core;

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
    }

    private void UIElement_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        RelationshipModel characterToDelete = null;
        foreach (var character in CharVm.CharacterRelationships)
        {
            if (character.PartnerUuid == (sender as SymbolIcon).Tag) { characterToDelete = character;  }
        }
        Ioc.Default.GetService<CharacterViewModel>().CharacterRelationships.Remove(characterToDelete);
        CharVm.SaveRelationships();

    }
}