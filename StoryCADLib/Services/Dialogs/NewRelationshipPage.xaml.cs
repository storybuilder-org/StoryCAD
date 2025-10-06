using System.Collections.ObjectModel;

namespace StoryCAD.Services.Dialogs;

public sealed partial class NewRelationshipPage : Page
{
    public CharacterViewModel CharVM = Ioc.Default.GetService<CharacterViewModel>();
    public NewRelationshipViewModel NewRelVM;

    public NewRelationshipPage(NewRelationshipViewModel vm)
    {
        InitializeComponent();
        RelationTypes = new ObservableCollection<RelationType>();
        NewRelVM = vm;
    }

    #region public Properties

    public StoryElementCollection StoryElements;
    public StoryElement Member { get; set; }

    public ObservableCollection<StoryElement> ProspectivePartners;

    public ObservableCollection<RelationshipModel> Relationships;

    public StoryElement SelectedPartner { get; set; }

    public ObservableCollection<RelationType> RelationTypes;
    public ObservableCollection<string> SimpleRelationTypes;

    #endregion
}
