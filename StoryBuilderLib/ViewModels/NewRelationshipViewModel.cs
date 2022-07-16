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

    public ObservableCollection<string> RelationTypes { get; set; }

    private string _relationType;
    public string RelationType 
    { 
        get => _relationType; 
        set => SetProperty(ref _relationType, value);
    }
    private string _inverserelationType;
    public string InverseRelationType
    {
        get => _inverserelationType;
        set => SetProperty(ref _inverserelationType, value);
    }
    private bool _inverseRelationship;
    public bool InverseRelationship
    {
        get => _inverseRelationship;
        set => SetProperty(ref _inverseRelationship, value);
    }

    #endregion

    #region Constructor

    public NewRelationshipViewModel(StoryElement member)
    {
        Member = member;
        ProspectivePartners = new ObservableCollection<StoryElement>();
        RelationTypes = new ObservableCollection<string>();
    }

    #endregion

}