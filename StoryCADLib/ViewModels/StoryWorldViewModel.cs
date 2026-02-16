using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using StoryCADLib.Models;
using StoryCADLib.Models.StoryWorld;
using StoryCADLib.Services;
using StoryCADLib.Services.Collaborator;
using StoryCADLib.Services.Messages;
using StoryCADLib.Services.Navigation;

namespace StoryCADLib.ViewModels;

public partial class StoryWorldViewModel : ObservableRecipient, INavigable, ISaveable, IReloadable
{
    #region Fields

    private readonly ILogService _logger;
    private readonly AppState _appState;
    // Reserved for future Collaborator AI worldbuilding guidance integration
    // See Collaborator/devdocs/storyworld-ai-parameters-parking.md
    #pragma warning disable IDE0052 // Remove unread private members
    private readonly CollaboratorService _collaboratorService;
    #pragma warning restore IDE0052
    private bool _changeable;
    private bool _changed;

    private void OnListChanged()
    {
        if (_changeable)
        {
            _changed = true;
            ShellViewModel.ShowChange();
        }
        OnPropertyChanged(nameof(RemoveButtonVisibility));
    }

    #endregion

    #region StoryElement Properties

    [ObservableProperty]
    private Guid _uuid;

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
        set
        {
            if (SetProperty(ref _worldType, value))
            {
                UpdateWorldTypeDescriptions();
                // Auto-populate axis values when World Type changes
                if (_changeable)
                {
                    AutoPopulateAxisValues();
                }
            }
        }
    }

    [ObservableProperty]
    private string _ontology = string.Empty;

    [ObservableProperty]
    private string _worldRelation = string.Empty;

    [ObservableProperty]
    private string _ruleTransparency = string.Empty;

    [ObservableProperty]
    private string _scaleOfDifference = string.Empty;

    [ObservableProperty]
    private string _agencySource = string.Empty;

    [ObservableProperty]
    private string _toneLogic = string.Empty;

    #endregion

    #region List Entry Navigators

    public ListNavigator<PhysicalWorldEntry> PhysicalWorldNav { get; private set; }
    public ListNavigator<SpeciesEntry> SpeciesNav { get; private set; }
    public ListNavigator<CultureEntry> CultureNav { get; private set; }
    public ListNavigator<GovernmentEntry> GovernmentNav { get; private set; }
    public ListNavigator<ReligionEntry> ReligionNav { get; private set; }

    #endregion

    #region World Type Description Properties

    private string _worldTypeDescription;
    /// <summary>
    /// Full description of the selected World Type.
    /// </summary>
    public string WorldTypeDescription
    {
        get => _worldTypeDescription;
        private set => SetProperty(ref _worldTypeDescription, value);
    }

    private string _worldTypeExamples;
    /// <summary>
    /// Example works for the selected World Type.
    /// </summary>
    public string WorldTypeExamples
    {
        get => _worldTypeExamples;
        private set => SetProperty(ref _worldTypeExamples, value);
    }

    #endregion

    #region Tab Selection and Context-Sensitive Buttons

    private int _selectedTabIndex;
    /// <summary>
    /// The currently selected tab index. Used for context-sensitive Add/Remove buttons.
    /// Tabs: 0=Structure, 1=Physical World, 2=Species, 3=Cultures, 4=Governments, 5=Religions, 6=History, 7=Economy, 8=Magic/Tech, 9=AI Parameters
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            if (SetProperty(ref _selectedTabIndex, value))
            {
                OnPropertyChanged(nameof(AddButtonLabel));
                OnPropertyChanged(nameof(AddButtonVisibility));
                OnPropertyChanged(nameof(RemoveButtonVisibility));
            }
        }
    }

    /// <summary>
    /// Label for the Add button based on selected tab.
    /// </summary>
    public string AddButtonLabel => SelectedTabIndex switch
    {
        1 => "+ Add World",
        2 => "+ Add Species",
        3 => "+ Add Culture",
        4 => "+ Add Government",
        5 => "+ Add Religion",
        _ => "+ Add"
    };

    /// <summary>
    /// Add button is only visible for list tabs (1-5).
    /// </summary>
    public Visibility AddButtonVisibility =>
        SelectedTabIndex >= 1 && SelectedTabIndex <= 5 ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Remove button is only visible when there are entries to remove in the current list tab.
    /// </summary>
    public Visibility RemoveButtonVisibility
    {
        get
        {
            var hasEntries = SelectedTabIndex switch
            {
                1 => PhysicalWorldNav?.HasItems ?? false,
                2 => SpeciesNav?.HasItems ?? false,
                3 => CultureNav?.HasItems ?? false,
                4 => GovernmentNav?.HasItems ?? false,
                5 => ReligionNav?.HasItems ?? false,
                _ => false
            };
            return hasEntries ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Unified Add command that adds to the appropriate list based on selected tab.
    /// </summary>
    [RelayCommand]
    private void AddEntry()
    {
        switch (SelectedTabIndex)
        {
            case 1: PhysicalWorldNav.Add(); break;
            case 2: SpeciesNav.Add(); break;
            case 3: CultureNav.Add(); break;
            case 4: GovernmentNav.Add(); break;
            case 5: ReligionNav.Add(); break;
        }
        OnPropertyChanged(nameof(RemoveButtonVisibility));
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

    [ObservableProperty]
    private string _foundingEvents = string.Empty;

    [ObservableProperty]
    private string _majorConflicts = string.Empty;

    [ObservableProperty]
    private string _eras = string.Empty;

    [ObservableProperty]
    private string _technologicalShifts = string.Empty;

    [ObservableProperty]
    private string _lostKnowledge = string.Empty;

    #endregion

    #region Economy Tab Properties

    [ObservableProperty]
    private string _economicSystem = string.Empty;

    [ObservableProperty]
    private string _currency = string.Empty;

    [ObservableProperty]
    private string _tradeRoutes = string.Empty;

    [ObservableProperty]
    private string _professions = string.Empty;

    [ObservableProperty]
    private string _wealthDistribution = string.Empty;

    #endregion

    #region Magic/Technology Tab Properties

    [ObservableProperty]
    private string _systemType = string.Empty;

    [ObservableProperty]
    private string _source = string.Empty;

    [ObservableProperty]
    private string _rules = string.Empty;

    [ObservableProperty]
    private string _limitations = string.Empty;

    [ObservableProperty]
    private string _cost = string.Empty;

    [ObservableProperty]
    private string _practitioners = string.Empty;

    [ObservableProperty]
    private string _socialImpact = string.Empty;

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
            // Note: Don't save Name - it's derived for display, node keeps "Story World"

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
        // Derive display name from story name (first node in ExplorerView is OverviewModel)
        var storyModel = _appState.CurrentDocument?.Model;
        var storyName = storyModel?.ExplorerView.Count > 0
            ? storyModel.ExplorerView[0].Name?.Trim() ?? string.Empty
            : string.Empty;
        Name = string.IsNullOrEmpty(storyName)
            ? "Story World"
            : storyName + " Story World";

        // Structure tab
        WorldType = Model.WorldType;
        Ontology = Model.Ontology;
        WorldRelation = Model.WorldRelation;
        RuleTransparency = Model.RuleTransparency;
        ScaleOfDifference = Model.ScaleOfDifference;
        AgencySource = Model.AgencySource;
        ToneLogic = Model.ToneLogic;

        // Lists - copy from Model to ObservableCollection and reset navigators
        ReloadCollection(PhysicalWorlds, Model.PhysicalWorlds);
        PhysicalWorldNav.Reset();

        ReloadCollection(Species, Model.Species);
        SpeciesNav.Reset();

        ReloadCollection(Cultures, Model.Cultures);
        CultureNav.Reset();

        ReloadCollection(Governments, Model.Governments);
        GovernmentNav.Reset();

        ReloadCollection(Religions, Model.Religions);
        ReligionNav.Reset();

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

    private static void ReloadCollection<T>(ObservableCollection<T> target, List<T> source)
    {
        target.Clear();
        foreach (var entry in source)
            target.Add(entry);
    }

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

    #region World Type Mapping

    /// <summary>
    /// Updates the description and examples based on the selected World Type.
    /// </summary>
    private void UpdateWorldTypeDescriptions()
    {
        var (description, examples) = GetWorldTypeInfo(WorldType);
        WorldTypeDescription = description;
        WorldTypeExamples = examples;
    }

    /// <summary>
    /// Auto-populates axis values based on the selected World Type.
    /// Uses the gestalt-to-axis mapping from design documents.
    /// </summary>
    private void AutoPopulateAxisValues()
    {
        var axes = GetAxisValuesForWorldType(WorldType);
        if (axes == null) return;

        Ontology = axes.Ontology;
        WorldRelation = axes.WorldRelation;
        RuleTransparency = axes.RuleTransparency;
        ScaleOfDifference = axes.ScaleOfDifference;
        AgencySource = axes.AgencySource;
        ToneLogic = axes.ToneLogic;
    }

    /// <summary>
    /// Returns description and examples for a World Type.
    /// </summary>
    private static (string Description, string Examples) GetWorldTypeInfo(string worldType)
    {
        return worldType switch
        {
            "Consensus Reality" => (
                "The world operates exactly as expected - no magic, no hidden layers, no alternate physics. " +
                "\"Consensus\" refers to a specific group or subculture whose reality you're depicting. " +
                "Every consensus reality story still requires worldbuilding the norms, rules, and insider knowledge of that particular slice of life.",
                "87th Precinct • Harry Bosch • Rabbit series • Grisham novels • Big Little Lies"),

            "Enchanted Reality" => (
                "Our world, but reality is porous. The impossible happens and is accepted rather than analyzed. " +
                "Magic or the supernatural exists but isn't systematized - it simply is. " +
                "This is the realm of magical realism and slipstream fiction.",
                "One Hundred Years of Solitude • Like Water for Chocolate • Beloved • Pan's Labyrinth"),

            "Hidden World" => (
                "Our world, but with concealed magical or supernatural layers beneath or alongside normal reality. " +
                "The \"mundane\" world operates normally, but a secret realm exists that most people don't know about. " +
                "Discovery of this hidden layer is often dangerous or comes at a cost.",
                "Harry Potter • Dresden Files • American Gods • The Matrix • Men in Black • Percy Jackson"),

            "Divergent World" => (
                "Our world, but history or conditions diverged at some point. " +
                "The rules of reality remain rational and logical, but society, technology, or events developed differently. " +
                "This includes alternate history, steampunk, cyberpunk, and near-future speculation.",
                "The Man in the High Castle • 11/22/63 • Neuromancer • The Handmaid's Tale"),

            "Constructed World" => (
                "A fully invented reality with its own geography, history, peoples, and rules. " +
                "The world doesn't derive from Earth at all - it's built from scratch. " +
                "This is the realm of epic fantasy and secondary-world science fiction. The author defines everything.",
                "A Song of Ice and Fire • Discworld • Dune • Star Wars • The Stormlight Archive"),

            "Mythic World" => (
                "A world where narrative meaning matters more than physical causality. " +
                "Fate, prophecy, and archetypes drive events. Things happen because they're meaningful, not because of cause and effect. " +
                "Gods and destiny are real forces.",
                "The Lord of the Rings • Earthsea • The Chronicles of Narnia • Circe • The Once and Future King"),

            "Estranged World" => (
                "A world that feels fundamentally alien or wrong. Rules may exist, but they resist human intuition. " +
                "The familiar becomes strange. " +
                "This is the realm of cosmic horror, New Weird, and hard SF that emphasizes how truly alien the universe can be.",
                "Solaris • Annihilation • Perdido Street Station • Blindsight • 2001: A Space Odyssey"),

            "Broken World" => (
                "A world where civilization, environment, or social order has collapsed or been corrupted. " +
                "Survival replaces progress. Resources are scarce, institutions have failed, and the focus is on enduring rather than building. " +
                "Includes post-apocalyptic and dystopian settings.",
                "The Road • Mad Max • 1984 • The Walking Dead • Station Eleven • A Canticle for Leibowitz"),

            _ => ("Select a World Type to see its description.", "")
        };
    }

    /// <summary>
    /// Returns axis values for a World Type based on the gestalt-to-axis mapping.
    /// </summary>
    private static AxisValues GetAxisValuesForWorldType(string worldType)
    {
        return worldType switch
        {
            "Consensus Reality" => new AxisValues
            {
                Ontology = "Mundane",
                WorldRelation = "Primary World",
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Cosmetic",
                AgencySource = "Human-Centric",
                ToneLogic = "Rational"
            },
            "Enchanted Reality" => new AxisValues
            {
                Ontology = "Supernatural",
                WorldRelation = "Primary World",
                RuleTransparency = "Implicit Rules",
                ScaleOfDifference = "Cosmetic",
                AgencySource = "Systemic Forces",
                ToneLogic = "Symbolic"
            },
            "Hidden World" => new AxisValues
            {
                Ontology = "Supernatural",
                WorldRelation = "Layered",
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Structural",
                AgencySource = "Nonhuman Intelligences",
                ToneLogic = "Rational"
            },
            "Divergent World" => new AxisValues
            {
                Ontology = "Scientific Speculative",
                WorldRelation = "Divergent Earth",
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Structural",
                AgencySource = "Human-Centric",
                ToneLogic = "Rational"
            },
            "Constructed World" => new AxisValues
            {
                Ontology = "Hybrid",
                WorldRelation = "Secondary World",
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Cosmological",
                AgencySource = "Human-Centric",  // Variable in spec, default to Human-Centric
                ToneLogic = "Rational"           // Variable in spec, default to Rational
            },
            "Mythic World" => new AxisValues
            {
                Ontology = "Symbolic",
                WorldRelation = "Secondary World",
                RuleTransparency = "Symbolic Rules",
                ScaleOfDifference = "Cosmological",
                AgencySource = "Fate / Providence",
                ToneLogic = "Mythic"
            },
            "Estranged World" => new AxisValues
            {
                Ontology = "Scientific Speculative",
                WorldRelation = "Secondary World",  // Variable in spec
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Cosmological",
                AgencySource = "Systemic Forces",
                ToneLogic = "Dark / Entropic"
            },
            "Broken World" => new AxisValues
            {
                Ontology = "Scientific Speculative",
                WorldRelation = "Divergent Earth",
                RuleTransparency = "Explicit Rules",
                ScaleOfDifference = "Structural",
                AgencySource = "Human-Centric",
                ToneLogic = "Dark / Entropic"
            },
            _ => null
        };
    }

    /// <summary>
    /// Helper class to hold axis values for mapping.
    /// </summary>
    private class AxisValues
    {
        public string Ontology { get; init; }
        public string WorldRelation { get; init; }
        public string RuleTransparency { get; init; }
        public string ScaleOfDifference { get; init; }
        public string AgencySource { get; init; }
        public string ToneLogic { get; init; }
    }

    #endregion

    #region Constructor

    public StoryWorldViewModel(ILogService logger, ListData listData, AppState appState, CollaboratorService collaboratorService)
    {
        _logger = logger;
        _appState = appState;
        _collaboratorService = collaboratorService;
        PropertyChanged += OnPropertyChanged;

        // Initialize description properties
        WorldTypeDescription = "Select a World Type to see its description.";
        WorldTypeExamples = string.Empty;

        // Initialize non-[ObservableProperty] string properties
        Name = string.Empty;
        WorldType = string.Empty;

        // Initialize list tab collections and navigators
        PhysicalWorlds = new ObservableCollection<PhysicalWorldEntry>();
        Species = new ObservableCollection<SpeciesEntry>();
        Cultures = new ObservableCollection<CultureEntry>();
        Governments = new ObservableCollection<GovernmentEntry>();
        Religions = new ObservableCollection<ReligionEntry>();

        PhysicalWorldNav = new(PhysicalWorlds, () => new PhysicalWorldEntry { Name = "New World" }, OnListChanged);
        SpeciesNav = new(Species, () => new SpeciesEntry { Name = "New Species" }, OnListChanged);
        CultureNav = new(Cultures, () => new CultureEntry { Name = "New Culture" }, OnListChanged);
        GovernmentNav = new(Governments, () => new GovernmentEntry { Name = "New Government" }, OnListChanged);
        ReligionNav = new(Religions, () => new ReligionEntry { Name = "New Religion" }, OnListChanged);

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
