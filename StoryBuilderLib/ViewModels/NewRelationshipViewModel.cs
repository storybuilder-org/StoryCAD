using CommunityToolkit.Mvvm.ComponentModel;
using StoryBuilder.Models;
using System.Collections.ObjectModel;

namespace StoryBuilder.ViewModels;

//TODO: Figure out what to do with this
public class NewRelationshipViewModel : ObservableRecipient
{
    #region public Properties


    public StoryElement Member { get; set; }

    public ObservableCollection<StoryElement> ProspectivePartners;

    public ObservableCollection<RelationshipModel> Relationships;

    private StoryElement _selectedPartner;
    public StoryElement SelectedPartner 
    {
        get => _selectedPartner;
        set => SetProperty(ref _selectedPartner, value);
    }

    public ObservableCollection<RelationType> RelationTypes { get; set; }

    private RelationType _relationType;
    public RelationType RelationType 
    { 
        get => _relationType; 
        set => SetProperty(ref _relationType, value);
    }
        
    #endregion

    #region Constructor

    public NewRelationshipViewModel(StoryElement member)
    {
        Member = member;
        ProspectivePartners = new ObservableCollection<StoryElement>();
        RelationTypes = new ObservableCollection<RelationType>();
    }

    #endregion

}