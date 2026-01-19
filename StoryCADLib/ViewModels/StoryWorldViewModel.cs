using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using StoryCADLib.Models;
using StoryCADLib.Models.StoryWorld;
using StoryCADLib.Services;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Navigation;

namespace StoryCADLib.ViewModels;

public class StoryWorldViewModel : ObservableRecipient, INavigable, ISaveable, IReloadable
{
    #region Fields

    private readonly ILogService _logger;
    private readonly AppState _appState;
    private bool _changeable;
    private bool _changed;

    #endregion

    #region StoryElement Properties

    private Guid _uuid;
    public Guid Uuid
    {
        get => _uuid;
        set => SetProperty(ref _uuid, value);
    }

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (_changeable && _name != value)
            {
                _logger.Log(LogLevel.Info, $"Requesting Name change from {_name} to {value}");
                NameChangeMessage msg = new(_name, value);
                Messenger.Send(new NameChangedMessage(msg));
            }
            SetProperty(ref _name, value);
        }
    }

    private StoryWorldModel _model;
    public StoryWorldModel Model
    {
        get => _model;
        set => _model = value;
    }

    #endregion

    #region Structure Tab Properties

    private string _worldType;
    public string WorldType
    {
        get => _worldType;
        set => SetProperty(ref _worldType, value);
    }

    private string _ontology;
    public string Ontology
    {
        get => _ontology;
        set => SetProperty(ref _ontology, value);
    }

    private string _worldRelation;
    public string WorldRelation
    {
        get => _worldRelation;
        set => SetProperty(ref _worldRelation, value);
    }

    private string _ruleTransparency;
    public string RuleTransparency
    {
        get => _ruleTransparency;
        set => SetProperty(ref _ruleTransparency, value);
    }

    private string _scaleOfDifference;
    public string ScaleOfDifference
    {
        get => _scaleOfDifference;
        set => SetProperty(ref _scaleOfDifference, value);
    }

    private string _agencySource;
    public string AgencySource
    {
        get => _agencySource;
        set => SetProperty(ref _agencySource, value);
    }

    private string _toneLogic;
    public string ToneLogic
    {
        get => _toneLogic;
        set => SetProperty(ref _toneLogic, value);
    }

    #endregion

    #region List Tab Properties

    public ObservableCollection<PhysicalWorldEntry> PhysicalWorlds { get; private set; }
    public ObservableCollection<SpeciesEntry> Species { get; private set; }
    public ObservableCollection<CultureEntry> Cultures { get; private set; }
    public ObservableCollection<GovernmentEntry> Governments { get; private set; }
    public ObservableCollection<ReligionEntry> Religions { get; private set; }

    #endregion

    #region History Tab Properties

    private string _foundingEvents;
    public string FoundingEvents
    {
        get => _foundingEvents;
        set => SetProperty(ref _foundingEvents, value);
    }

    private string _majorConflicts;
    public string MajorConflicts
    {
        get => _majorConflicts;
        set => SetProperty(ref _majorConflicts, value);
    }

    private string _eras;
    public string Eras
    {
        get => _eras;
        set => SetProperty(ref _eras, value);
    }

    private string _technologicalShifts;
    public string TechnologicalShifts
    {
        get => _technologicalShifts;
        set => SetProperty(ref _technologicalShifts, value);
    }

    private string _lostKnowledge;
    public string LostKnowledge
    {
        get => _lostKnowledge;
        set => SetProperty(ref _lostKnowledge, value);
    }

    #endregion

    #region Economy Tab Properties

    private string _economicSystem;
    public string EconomicSystem
    {
        get => _economicSystem;
        set => SetProperty(ref _economicSystem, value);
    }

    private string _currency;
    public string Currency
    {
        get => _currency;
        set => SetProperty(ref _currency, value);
    }

    private string _tradeRoutes;
    public string TradeRoutes
    {
        get => _tradeRoutes;
        set => SetProperty(ref _tradeRoutes, value);
    }

    private string _professions;
    public string Professions
    {
        get => _professions;
        set => SetProperty(ref _professions, value);
    }

    private string _wealthDistribution;
    public string WealthDistribution
    {
        get => _wealthDistribution;
        set => SetProperty(ref _wealthDistribution, value);
    }

    #endregion

    #region Magic/Technology Tab Properties

    private string _systemType;
    public string SystemType
    {
        get => _systemType;
        set => SetProperty(ref _systemType, value);
    }

    private string _source;
    public string Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }

    private string _rules;
    public string Rules
    {
        get => _rules;
        set => SetProperty(ref _rules, value);
    }

    private string _limitations;
    public string Limitations
    {
        get => _limitations;
        set => SetProperty(ref _limitations, value);
    }

    private string _cost;
    public string Cost
    {
        get => _cost;
        set => SetProperty(ref _cost, value);
    }

    private string _practitioners;
    public string Practitioners
    {
        get => _practitioners;
        set => SetProperty(ref _practitioners, value);
    }

    private string _socialImpact;
    public string SocialImpact
    {
        get => _socialImpact;
        set => SetProperty(ref _socialImpact, value);
    }

    #endregion

    #region ComboBox ItemsSource Collections

    /// <summary>
    /// The 8 World Type gestalts for the dropdown.
    /// </summary>
    public ObservableCollection<string> WorldTypeList { get; private set; }

    // 6 Axis lists for advanced options
    public ObservableCollection<string> OntologyList { get; private set; }
    public ObservableCollection<string> WorldRelationList { get; private set; }
    public ObservableCollection<string> RuleTransparencyList { get; private set; }
    public ObservableCollection<string> ScaleOfDifferenceList { get; private set; }
    public ObservableCollection<string> AgencySourceList { get; private set; }
    public ObservableCollection<string> ToneLogicList { get; private set; }
    public ObservableCollection<string> SystemTypeList { get; private set; }

    #endregion

    #region INavigable Implementation

    public void Activate(object parameter)
    {
        var param = parameter as StoryWorldModel;
        _logger.Log(LogLevel.Info, $"StoryWorldViewModel.Activate: parameter={param?.Name} (Uuid={param?.Uuid})");
        _changeable = false;
        _changed = false;
        Model = (StoryWorldModel)parameter;
        _logger.Log(LogLevel.Info, $"StoryWorldViewModel.Activate: Model set to {Model?.Name} (Uuid={Model?.Uuid})");
        LoadModel();
    }

    public void Deactivate(object parameter)
    {
        _logger.Log(LogLevel.Info, $"StoryWorldViewModel.Deactivate: Saving to Model={Model?.Name} (Uuid={Model?.Uuid})");
        SaveModel();
    }

    #endregion

    #region ISaveable / IReloadable Implementation

    public void SaveModel()
    {
        try
        {
            // StoryElement properties
            Model.Name = Name;

            // Structure tab
            Model.WorldType = WorldType;
            Model.Ontology = Ontology;
            Model.WorldRelation = WorldRelation;
            Model.RuleTransparency = RuleTransparency;
            Model.ScaleOfDifference = ScaleOfDifference;
            Model.AgencySource = AgencySource;
            Model.ToneLogic = ToneLogic;

            // Lists - copy from ObservableCollection to List
            Model.PhysicalWorlds = new List<PhysicalWorldEntry>(PhysicalWorlds);
            Model.Species = new List<SpeciesEntry>(Species);
            Model.Cultures = new List<CultureEntry>(Cultures);
            Model.Governments = new List<GovernmentEntry>(Governments);
            Model.Religions = new List<ReligionEntry>(Religions);

            // History tab
            Model.FoundingEvents = FoundingEvents;
            Model.MajorConflicts = MajorConflicts;
            Model.Eras = Eras;
            Model.TechnologicalShifts = TechnologicalShifts;
            Model.LostKnowledge = LostKnowledge;

            // Economy tab
            Model.EconomicSystem = EconomicSystem;
            Model.Currency = Currency;
            Model.TradeRoutes = TradeRoutes;
            Model.Professions = Professions;
            Model.WealthDistribution = WealthDistribution;

            // Magic/Technology tab
            Model.SystemType = SystemType;
            Model.Source = Source;
            Model.Rules = Rules;
            Model.Limitations = Limitations;
            Model.Cost = Cost;
            Model.Practitioners = Practitioners;
            Model.SocialImpact = SocialImpact;
        }
        catch (Exception ex)
        {
            _logger.LogException(LogLevel.Error, ex, $"Failed to save StoryWorld model - {ex.Message}");
        }
    }

    public void ReloadFromModel()
    {
        if (Model != null)
        {
            LoadModel();
        }
    }

    private void LoadModel()
    {
        _changeable = false;
        _changed = false;

        // StoryElement properties
        Uuid = Model.Uuid;
        // Derive name from story name (first node in ExplorerView is OverviewModel)
        var storyModel = _appState.CurrentDocument?.Model;
        Name = storyModel?.ExplorerView.Count > 0
            ? storyModel.ExplorerView[0].Name + " World"
            : "Story World";

        // Structure tab
        WorldType = Model.WorldType;
        Ontology = Model.Ontology;
        WorldRelation = Model.WorldRelation;
        RuleTransparency = Model.RuleTransparency;
        ScaleOfDifference = Model.ScaleOfDifference;
        AgencySource = Model.AgencySource;
        ToneLogic = Model.ToneLogic;

        // Lists - copy from Model to ObservableCollection
        PhysicalWorlds.Clear();
        foreach (var entry in Model.PhysicalWorlds)
            PhysicalWorlds.Add(entry);

        Species.Clear();
        foreach (var entry in Model.Species)
            Species.Add(entry);

        Cultures.Clear();
        foreach (var entry in Model.Cultures)
            Cultures.Add(entry);

        Governments.Clear();
        foreach (var entry in Model.Governments)
            Governments.Add(entry);

        Religions.Clear();
        foreach (var entry in Model.Religions)
            Religions.Add(entry);

        // History tab
        FoundingEvents = Model.FoundingEvents;
        MajorConflicts = Model.MajorConflicts;
        Eras = Model.Eras;
        TechnologicalShifts = Model.TechnologicalShifts;
        LostKnowledge = Model.LostKnowledge;

        // Economy tab
        EconomicSystem = Model.EconomicSystem;
        Currency = Model.Currency;
        TradeRoutes = Model.TradeRoutes;
        Professions = Model.Professions;
        WealthDistribution = Model.WealthDistribution;

        // Magic/Technology tab
        SystemType = Model.SystemType;
        Source = Model.Source;
        Rules = Model.Rules;
        Limitations = Model.Limitations;
        Cost = Model.Cost;
        Practitioners = Model.Practitioners;
        SocialImpact = Model.SocialImpact;

        _changeable = true;
    }

    #endregion

    #region Property Change Handling

    private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Model))
            return;

        if (_changeable)
        {
            if (!_changed)
            {
                _logger.Log(LogLevel.Info, $"StoryWorldViewModel.OnPropertyChanged: {args.PropertyName} changed");
            }
            _changed = true;
            ShellViewModel.ShowChange();
        }
    }

    #endregion

    #region Constructor

    public StoryWorldViewModel(ILogService logger, ListData listData, AppState appState)
    {
        _logger = logger;
        _appState = appState;
        PropertyChanged += OnPropertyChanged;

        // Initialize string properties
        Name = string.Empty;
        WorldType = string.Empty;
        Ontology = string.Empty;
        WorldRelation = string.Empty;
        RuleTransparency = string.Empty;
        ScaleOfDifference = string.Empty;
        AgencySource = string.Empty;
        ToneLogic = string.Empty;
        FoundingEvents = string.Empty;
        MajorConflicts = string.Empty;
        Eras = string.Empty;
        TechnologicalShifts = string.Empty;
        LostKnowledge = string.Empty;
        EconomicSystem = string.Empty;
        Currency = string.Empty;
        TradeRoutes = string.Empty;
        Professions = string.Empty;
        WealthDistribution = string.Empty;
        SystemType = string.Empty;
        Source = string.Empty;
        Rules = string.Empty;
        Limitations = string.Empty;
        Cost = string.Empty;
        Practitioners = string.Empty;
        SocialImpact = string.Empty;

        // Initialize list tab collections
        PhysicalWorlds = new ObservableCollection<PhysicalWorldEntry>();
        Species = new ObservableCollection<SpeciesEntry>();
        Cultures = new ObservableCollection<CultureEntry>();
        Governments = new ObservableCollection<GovernmentEntry>();
        Religions = new ObservableCollection<ReligionEntry>();

        // Load ComboBox source collections from ListData
        var _lists = listData.ListControlSource;
        WorldTypeList = _lists["WorldType"];
        OntologyList = _lists["Ontology"];
        WorldRelationList = _lists["WorldRelation"];
        RuleTransparencyList = _lists["RuleTransparency"];
        ScaleOfDifferenceList = _lists["ScaleOfDifference"];
        AgencySourceList = _lists["AgencySource"];
        ToneLogicList = _lists["ToneLogic"];
        SystemTypeList = _lists["SystemType"];
    }

    #endregion
}
