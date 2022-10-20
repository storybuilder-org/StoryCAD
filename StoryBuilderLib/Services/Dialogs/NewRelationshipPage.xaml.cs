using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.ViewModels;

namespace StoryBuilder.Services.Dialogs;

public sealed partial class NewRelationshipPage
{
    public CharacterViewModel CharVM = Ioc.Default.GetService<CharacterViewModel>();
    public NewRelationshipViewModel NewRelVM;
    #region public Properties

    public StoryElementCollection StoryElements;
    public StoryElement Member { get; set; }

    public StoryElement SelectedPartner { get; set; }

    public ObservableCollection<RelationType> RelationTypes;

    #endregion  
    public NewRelationshipPage(NewRelationshipViewModel vm)
    {
        InitializeComponent();
        RelationTypes = new ObservableCollection<RelationType>();
        NewRelVM = vm;
    }
}