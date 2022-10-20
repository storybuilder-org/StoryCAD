using System;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Controls;

public sealed partial class RelationshipView
{
    public CharacterViewModel CharVm => Ioc.Default.GetService<CharacterViewModel>();
    public LogService Logger => Ioc.Default.GetService<LogService>();
    public RelationshipView() { InitializeComponent(); }
    
    /// <summary>
    /// This removes a relationship from the 'master' character.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void RemoveRelationship(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            //First identify the relationship.
            Logger.Log(LogLevel.Info, "Starting to remove relationship");
            RelationshipModel _CharacterToDelete = null;
            foreach (RelationshipModel _Character in CharVm.CharacterRelationships)
            {   //UUID is stored in tag as a cheeky hack to identify the relationship.
                if (_Character.PartnerUuid.Equals((sender as SymbolIcon)?.Tag)) //Identify via tag.
                {
                    _CharacterToDelete = _Character;
                }
            }
            Logger.Log(LogLevel.Info, $"Character to delete: {_CharacterToDelete!.Partner.Name}({_CharacterToDelete.Partner.Uuid})");

            //Show confirmation dialog and gets result.
            ContentDialog _Cd = new()
            {
                Title = "Are you sure?",
                Content = $"Are you sure you want to delete the relationship between {CharVm.Name} and {_CharacterToDelete.Partner.Name}?",
                XamlRoot = GlobalData.XamlRoot,
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            };
            ContentDialogResult _Result = await _Cd.ShowAsync();
            Logger.Log(LogLevel.Info, $"Dialog Result: {_Result}");

            if (_Result == ContentDialogResult.Primary) //If positive, then delete.
            {
                Logger.Log(LogLevel.Info, $"Deleting Relationship to {_CharacterToDelete.Partner.Name}");
                Ioc.Default.GetService<CharacterViewModel>()!.CharacterRelationships.Remove(_CharacterToDelete);
                Logger.Log(LogLevel.Info, "Deleted");
                CharVm.SaveRelationships();
            }
            Logger.Log(LogLevel.Info, "Remove relationship complete!");
        }
        catch (Exception _Ex)
        {
            Logger.LogException(LogLevel.Error, _Ex, "Error removing relationship");
        }
    }

    //When focus is lost, we save the relationship to the disk. (this is different from saving the story)
    private void OnLostFocus(UIElement sender, LosingFocusEventArgs args) { CharVm.SaveRelationships(); }
}