using Microsoft.UI.Xaml.Input;

namespace StoryCADLib.Controls;

public sealed partial class RelationshipView : UserControl
{
    public RelationshipView()
    {
        InitializeComponent();
    }

    public CharacterViewModel CharVm => Ioc.Default.GetService<CharacterViewModel>();
    private LogService Logger => Ioc.Default.GetService<LogService>();

    /// <summary>
    /// This removes a relationship from the 'master' character.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void RemoveRelationship(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            Logger.Log(LogLevel.Info, "Starting to remove relationship");

            // Get the RelationshipModel directly from DataContext
            var characterToDelete = (sender as FrameworkElement)?.DataContext as RelationshipModel;
            if (characterToDelete == null)
            {
                Logger.Log(LogLevel.Warn, "Could not get RelationshipModel from DataContext");
                return;
            }

            Logger.Log(LogLevel.Info,
                $"Character to delete: {characterToDelete.Partner.Name}({characterToDelete.Partner.Uuid})");

            //Show confirmation dialog and gets result.
            ContentDialog cd = new()
            {
                Title = "Are you sure?",
                Content =
                    $"Are you sure you want to delete the relationship between {CharVm.Name} and {characterToDelete.Partner.Name}?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No"
            };
            var result = await Ioc.Default.GetRequiredService<Windowing>().ShowContentDialog(cd);
            Logger.Log(LogLevel.Info, $"Dialog Result: {result}");

            if (result == ContentDialogResult.Primary) //If positive, then delete.
            {
                Logger.Log(LogLevel.Info, $"Deleting Relationship to {characterToDelete.Partner.Name}");
                Ioc.Default.GetRequiredService<CharacterViewModel>().CharacterRelationships.Remove(characterToDelete);
                Logger.Log(LogLevel.Info, "Deleted");
                CharVm.SaveRelationships();
            }

            Logger.Log(LogLevel.Info, "Remove relationship complete!");
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error, ex, "Error removing relationship");
        }
    }

    //When focus is lost, we save the relationship to the disk. (this is different from saving the story)
    private void OnLostFocus(UIElement sender, LosingFocusEventArgs args)
    {
        CharVm.SaveRelationships();
    }
}
